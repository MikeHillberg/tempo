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
        /// Configure environment variables and create a ProcessStartInfo for launching PowerShell.
        /// </summary>
        static (ProcessStartInfo startInfo, string pipeName) PreparePSLaunch(
            string filesEnv, string typeSetName, string useWinRTProjections)
        {
            var pipeName = $"TempoPipe_{Guid.NewGuid()}";
            Environment.SetEnvironmentVariable(TempoPSProvider.TempoPSProvider.PipeNameKey, pipeName);
            Environment.SetEnvironmentVariable("Tempo_TypeSetName", typeSetName);
            Environment.SetEnvironmentVariable(TempoPSProvider.TempoPSProvider.UseWinRTProjectionsKey, useWinRTProjections);
            Environment.SetEnvironmentVariable(TempoPSProvider.TempoPSProvider.FilenamesKey, filesEnv);

            var startInfo = new ProcessStartInfo("pwsh.exe")
            {
                WorkingDirectory = Package.Current.InstalledLocation.Path,
                Arguments = $@"-ExecutionPolicy Bypass -noexit -command "". .\MapTempoDrive.ps1""; cd Tempo:\Types",
                UseShellExecute = true
            };

            return (startInfo, pipeName);
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

            var filesEnv = string.Join(";", filenameList) + ";";
            var (startInfo, pipeName) = PreparePSLaunch(
                filesEnv,
                Manager.CurrentTypeSet.Name,
                App.Instance.UsingCppProjections ? "0" : "1");

            DebugLog.Append(startInfo.WorkingDirectory);
            DebugLog.Append(startInfo.Arguments);

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
        /// If filenames are provided, they are loaded as Custom API scope.
        /// Otherwise, the specified apiScope is used (or Windows if null).
        /// </summary>
        static internal void LaunchPSFromConsole(IEnumerable<string> filenames = null, string apiScope = null)
        {
            string filesEnv;
            string typeSetName;

            if (filenames != null && filenames.Any())
            {
                // Custom scope from filenames
                filenames = Helpers.ExpandDirectories(filenames);
                filesEnv = string.Join(";", filenames) + ";";
                typeSetName = "Custom";
            }
            else
            {
                // No filenames: pass the scope via environment variable
                filesEnv = "";
                typeSetName = apiScope ?? ApiScopeNames.Windows;
            }

            var (startInfo, _) = PreparePSLaunch(filesEnv, typeSetName, "1");

            // Pass the API scope and channel settings so the PS provider knows what to load
            Environment.SetEnvironmentVariable(TempoPSProvider.TempoPSProvider.ScopeKey, apiScope ?? "");
            Environment.SetEnvironmentVariable(TempoPSProvider.TempoPSProvider.WinAppSdkChannelKey,
                App.Instance.WinAppSDKChannel.ToString());
            Environment.SetEnvironmentVariable(TempoPSProvider.TempoPSProvider.WebView2ChannelKey,
                App.Instance.WebView2Channel.ToString());

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
