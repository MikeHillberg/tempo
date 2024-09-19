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
        /// Launch PowerShell
        /// </summary>
        async static internal void GoToPS(XamlRoot xamlRoot)
        {
            DebugLog.Append("Launching PowerShell");

            var shouldContinue = await App.EnsureApiScopeLoadedAsync();
            if (!shouldContinue)
            {
                // User canceled the loading dialog, nothing to search
                return;
            }


            // Hope that pwsh (PSCore) is in the path
            var psexe = "pwsh.exe";
            var startInfo = new ProcessStartInfo(psexe);

            // Need to be in the Tempo directory in order to find everything
            // bugbug: Would be a little better to be in the User directory, but add Tempo to the path?
            startInfo.WorkingDirectory = Package.Current.InstalledLocation.Path;
            DebugLog.Append(startInfo.WorkingDirectory);

            // Run the startup script to load the Tempo: drive, then cd to it
            // Bypassing the execution policy check allows scripts to run
            startInfo.Arguments = $@"-ExecutionPolicy Bypass -noexit -command "". .\MapTempoDrive.ps1""; cd Tempo:\Types";
            DebugLog.Append(startInfo.Arguments);

            // Pass in the exe name so it can call back
            var pipeName = $"TempoPipe_{Guid.NewGuid().ToString()}";
            startInfo.Environment[TempoPSProvider.TempoPSProvider.PipeNameKey] = pipeName;
            startInfo.Environment["Tempo_TypeSetName"] = Manager.CurrentTypeSet.Name;

            // Pass in whether to be in C# or C++ mode
            startInfo.Environment[TempoPSProvider.TempoPSProvider.UseWinRTProjectionsKey]
                = App.Instance.UsingCppProjections ? "0" : "1";

            // Figure out the filenames of what we're looking at.
            // For ML type sets it's in AssemblyLocations
            var filenameList = new List<string>();

            // bugbug: CurrentTypeSet can be null
            DebugLog.Append(Manager.CurrentTypeSet.Name);
            foreach (var assemblyLocation in Manager.CurrentTypeSet.AssemblyLocations)
            {
                // AssemblyLocation has a path that's either an actual file path
                // or a relative path within a nupkg
                if (assemblyLocation.ContainerPath == null)
                {
                    filenameList.Add(assemblyLocation.Path);
                }
                else
                {
                    // Add the nupkg path but only once
                    if (!filenameList.Contains(assemblyLocation.ContainerPath))
                    {
                        filenameList.Add(assemblyLocation.ContainerPath);
                    }
                }
            }

            // WPF is still using reflection, so we have actual Assemblies
            foreach (var assembly in Manager.CurrentTypeSet.Assemblies)
            {
                if (!filenameList.Contains(assembly.Location))
                {
                    filenameList.Add(assembly.Location);
                }
            }

            // Create a semi-colon separated list of filenames and set it into the environment
            // for the PS process to pick up
            var builder = new StringBuilder();
            foreach (var filename in filenameList)
            {
                builder.Append($"{filename};");
            }
            startInfo.Environment[TempoPSProvider.TempoPSProvider.FilenamesKey] = builder.ToString();

            // Need to set this when you set an environment variable
            startInfo.UseShellExecute = false;

            // Start a thread that will wait and listen for the PS process to call back (from out-tempo cmdlet)
            _ = Task.Run(() => PSOutTempoServerThread(pipeName));

            try
            {
                // Start pwsh
                DebugLog.Append("Process start");
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                // Probably don't have pwsh installed

                DebugLog.Append($"Failed to launch pwsh. {ex.Message}");
                var dialog = new CantStartPowerShell()
                {
                    XamlRoot = xamlRoot
                };
                _ = dialog.ShowAsync();
            }
        }

        // Thread to listen for calls from PS (from the out-tempo cmdlet)
        static void PSOutTempoServerThread(string pipeName)
        {
            //using (var pipe = new NamedPipeServerStream(pipeName, PipeDirection.In))
            //{
            //    try
            //    {
            //        // Wait for out-tempo cmdlet to run
            //        pipe.WaitForConnection();

            //        using (var reader = new StreamReader(pipe))
            //        {
            //            // Respond to requests
            //            while (true)
            //            {
            //                // Each line is a request
            //                var line = reader.ReadLine();
            //                if (line == null)
            //                {
            //                    // PS has exited
            //                    break;
            //                }

            //                // Process the request
            //                ProcessRequestFromPS(line);
            //            }
            //        }
            //    }
            //    catch (IOException)
            //    {
            //        // Catch the IOException that is raised if the pipe is broken
            //        // or disconnected.
            //    }
            //}
        }

    }
}
