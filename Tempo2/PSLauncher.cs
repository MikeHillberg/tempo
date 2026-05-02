using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace Tempo
{
    internal class PSLauncher
    {
        /// <summary>
        /// Create a ProcessStartInfo configured to launch pwsh with the Tempo drive.
        /// </summary>
        static internal ProcessStartInfo CreatePwshStartInfo(
            string typeSetName,
            bool useWinRTProjections,
            IEnumerable<string> filenames)
        {
            var psexe = "pwsh.exe";
            var startInfo = new ProcessStartInfo(psexe);

            startInfo.WorkingDirectory = Package.Current.InstalledLocation.Path;
            startInfo.Arguments = $@"-ExecutionPolicy Bypass -noexit -command "". .\MapTempoDrive.ps1""; cd Tempo:\Types";

            var pipeName = $"TempoPipe_{Guid.NewGuid()}";
            startInfo.Environment[TempoPSProvider.TempoPSProvider.PipeNameKey] = pipeName;
            startInfo.Environment["Tempo_TypeSetName"] = typeSetName;
            startInfo.Environment[TempoPSProvider.TempoPSProvider.UseWinRTProjectionsKey]
                = useWinRTProjections ? "1" : "0";

            var builder = new StringBuilder();
            foreach (var filename in filenames)
            {
                builder.Append($"{filename};");
            }
            startInfo.Environment[TempoPSProvider.TempoPSProvider.FilenamesKey] = builder.ToString();

            startInfo.UseShellExecute = false;

            return startInfo;
        }

        /// <summary>
        /// Launch PowerShell from the UI (waits for API scope to load, shows error dialog on failure)
        /// </summary>
        async static internal void LaunchPSFromUI(XamlRoot xamlRoot)
        {
            DebugLog.Append("Launching PowerShell");

            var shouldContinue = await App.EnsureApiScopeLoadedAsync();
            if (!shouldContinue)
            {
                return;
            }

            // Gather filenames from the current type set
            var filenameList = new List<string>();
            foreach (var assemblyLocation in Manager.CurrentTypeSet.AssemblyLocations)
            {
                if (assemblyLocation.ContainerPath == null)
                {
                    filenameList.Add(assemblyLocation.Path);
                }
                else
                {
                    if (!filenameList.Contains(assemblyLocation.ContainerPath))
                    {
                        filenameList.Add(assemblyLocation.ContainerPath);
                    }
                }
            }

            foreach (var assembly in Manager.CurrentTypeSet.Assemblies)
            {
                if (assembly.Location != null && !filenameList.Contains(assembly.Location))
                {
                    filenameList.Add(assembly.Location);
                }
            }

            var startInfo = CreatePwshStartInfo(
                Manager.CurrentTypeSet.Name,
                !App.Instance.UsingCppProjections,
                filenameList);

            DebugLog.Append(startInfo.WorkingDirectory);
            DebugLog.Append(startInfo.Arguments);

            // Start a thread that will wait and listen for the PS process to call back
            var pipeName = startInfo.Environment[TempoPSProvider.TempoPSProvider.PipeNameKey];
            _ = Task.Run(() => PSOutTempoServerThread(pipeName));

            try
            {
                DebugLog.Append("Process start");
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                DebugLog.Append($"Failed to launch pwsh. {ex.Message}");
                var dialog = new CantStartPowerShell()
                {
                    XamlRoot = xamlRoot
                };
                _ = dialog.ShowAsync();
            }
        }

        /// <summary>
        /// Launch PowerShell directly without UI (for /ps mode).
        /// Opens in a new console window.
        /// </summary>
        static internal void LaunchPSFromConsole(IEnumerable<string> filenames)
        {
            // Expand any directories to their contained files
            filenames = Helpers.ExpandDirectories(filenames);

            var startInfo = CreatePwshStartInfo(
                "Custom",
                useWinRTProjections: true,
                filenames);

            // Need UseShellExecute so the new process gets its own console window
            startInfo.UseShellExecute = true;

            try
            {
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                DebugLog.Append(ex, "Failed to launch pwsh in direct mode");
            }
        }

        // Thread to listen for calls from PS (from the out-tempo cmdlet)
        static void PSOutTempoServerThread(string pipeName)
        {
        }

    }
}
