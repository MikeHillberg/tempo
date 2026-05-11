using System;
using System.IO;

namespace Tempo
{
    internal class DotNetScopeLoader : ApiScopeLoader
    {
        internal DotNetScopeLoader()
        {
        }

        public override string Name => "DotNet";
        public override string MenuName => ".Net";

        protected override string LoadingMessage => "Loading DotNet types ...";

        DotNetAppTypeSetLoader _typeSetLoader;

        protected override TypeSetLoader GetTypeSetLoader()
        {
            if (_typeSetLoader != null)
            {
                return _typeSetLoader;
            }

            if (!string.IsNullOrEmpty(DotNetCorePath))
            {
                var files = Directory.GetFiles(DotNetCorePath);
                if (files == null || files.Length == 0)
                {
                    DebugLog.Append($"No assemblies found in {DotNetCorePath}");
                    return null;
                }

                _typeSetLoader = new DotNetAppTypeSetLoader(files);
            }

            return _typeSetLoader;
        }

        static public string DotNetCorePath = null;
        static public string DotNetWindowsPath = null;

        /// <summary>
        /// Check if DotNet is available (SDK is installed)
        /// </summary>
        static public void CheckForDotNet()
        {
            DebugLog.Append("Checking for dotnet");

            var dotNetPath = Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramFiles%"), "dotnet");
            if (!Directory.Exists(dotNetPath))
            {
                DebugLog.Append($"DotNet not found at {dotNetPath}");
                return;
            }

            var dotNetCorePath = Path.Combine(dotNetPath, @"shared\Microsoft.NETCore.App");
            if (!Directory.Exists(dotNetCorePath))
            {
                DebugLog.Append($"Couldn't find '{dotNetCorePath}'");
                return;
            }

            var version = FindHighestVersionDirectory(dotNetCorePath);
            if (version == null)
            {
                DebugLog.Append($"Couldn't find version in {dotNetCorePath}");
                return;
            }

            DotNetCorePath = version;
            DotNetTypeSet.DotNetCoreVersion = Path.GetFileName(version);
            DebugLog.Append($"Found {DotNetCorePath}");

            var dotNetWindowsPath = Path.Combine(dotNetPath, @"shared\Microsoft.WindowsDesktop.App");
            if (!Directory.Exists(dotNetWindowsPath))
            {
                DebugLog.Append($"Couldn't find '{dotNetWindowsPath}'");
                return; // Still return true because we found .Net Core
            }

            version = FindHighestVersionDirectory(dotNetWindowsPath);
            if (version == null)
            {
                DebugLog.Append($"Couldn't find version in {dotNetWindowsPath}");
            }
            DotNetWindowsPath = version;
            DebugLog.Append($"Found {DotNetWindowsPath}");

            return;
        }

        /// <summary>
        /// Search subdirectories that are 3-part versions for the highest
        /// </summary>
        static string FindHighestVersionDirectory(string path) => Helpers.FindHighestVersionDirectory(path);

        public override bool IsSelected
        {
            get => App.Instance.IsDotNetScope;
            set
            {
                App.Instance.IsDotNetScope = value;
            }
        }

        protected override void OnCanceled()
        {
            _ = MyMessageBox.Show(
                    "Unable to load dotnet package\n\nSwitching to Windows Platform APIs",
                    "Load error");

            // Go back to an API scope we know is there
            App.Instance.IsWinPlatformScope = true;
            App.GoHome();
        }

        protected override TypeSet GetTypeSet() => Manager.DotNetTypeSet;
        protected override void SetTypeSet(TypeSet typeSet)
        {
            Manager.DotNetTypeSet = typeSet;
        }
        protected override void ClearTypeSet()
        {
            Manager.DotNetTypeSet = null;
        }



    }
}
