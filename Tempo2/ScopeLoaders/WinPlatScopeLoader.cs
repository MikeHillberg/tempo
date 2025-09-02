using MiddleweightReflection;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace Tempo
{
    internal class WinPlatScopeLoader : ApiScopeLoader
    {
        internal WinPlatScopeLoader()
        {
        }

        public override string Name => "WinPlat";
        public override string MenuName => "Windows";

        protected override string LoadingMessage => "Loading ...";


        //protected override TypeSet DoOffThreadLoad()
        //{
        //    string version = null;
        //    DesktopManager2.GetLocalWindowsVersion(out var displayVersion, out var currentBuildNumber);
        //    if (displayVersion != null && currentBuildNumber != null)
        //    {
        //        version = $"{displayVersion}.{currentBuildNumber}";
        //    }

        //    var typeSet = DesktopManager2.LoadWindowsTypes(
        //        !App.Instance.UsingCppProjections,
        //        (assemblyName) => LocateAssembly(assemblyName),
        //        winUIWinMDFilename: null,
        //        cacheFolder: DesktopManager2.PackageCachePath);

        //    return typeSet;
        //}

        WinPlatTypeSetLoader _typeSetLoader;

        static string _windir = System.Environment.ExpandEnvironmentVariables("%SystemRoot%");
        static string _winMDDir = FindSystemMetadataDirectory();

        public static string FindSystemMetadataDirectory()
        {
            // On a 32 bit machine, WinMetadata is in \windows\system32\WinMetadata
            var winMDDir32 = _windir + @"\System32\WinMetadata\";

            // On a 64 bit machine, since this is a 32 bit app, \windows\system32 redirects to \windows\syswow64.
            // To get at the actual system32 directory, look in the secret \windows\SYSNATIVE.
            var winMDDir64 = _windir + @"\SysNative\WinMetadata\";

            // Find the real System32\WinMetadata directory
            string winMDDir = winMDDir32;
            if (!Directory.Exists(winMDDir))
            {
                // We're running on 64 bit machine
                winMDDir = winMDDir64;
            }

            return winMDDir;

        }
        protected override TypeSetLoader GetTypeSetLoader()
        {
            if (_typeSetLoader == null)
            {
                DesktopManager2.GetLocalWindowsVersion(out var displayVersion, out var version, out var ubr);
                if(!string.IsNullOrEmpty(ubr))
                {
                    version += $".{ubr}";
                }


                // Load from System32
                var allPaths = Directory.EnumerateFiles(_winMDDir, @"*.winmd").ToArray();

                _typeSetLoader = new WinPlatTypeSetLoader(
                    !App.Instance.UsingCppProjections,
                    allPaths,
                    version);

                //var typeSet = DesktopManager2.LoadWindowsTypes(
                //    !App.Instance.UsingCppProjections,
                //    (assemblyName) => LocateAssembly(assemblyName),
                //    winUIWinMDFilename: null,
                //    cacheFolder: DesktopManager2.PackageCachePath);
            }

            return _typeSetLoader;

        }

        /// <summary>
        /// Called by the MR loader when it can't find an assembly
        /// </summary>
        static string LocateAssembly(string assemblyName)
        {
            // Mostly, if a referenced assembly can't be found, let it be faked.
            // But we need some WinRT interop assemblies for things like GridLength
            if (_specialSystemAssemblyNames.Contains(assemblyName))
            {
                var task = StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assemblies/{assemblyName}.dll"));
                task.AsTask().Wait();
                return task.GetResults().Path;
            }

            return null;
        }

        static readonly string[] _specialSystemAssemblyNames = new string[]
        {
                    "System.Runtime.InteropServices.WindowsRuntime",
                    "System.Runtime.WindowsRuntime",
                    "System.Runtime.WindowsRuntime.UI.Xaml",
                    "System.Runtime",
                    "System.Private.CoreLib"
        };

        public override bool IsSelected
        {
            get => App.Instance.IsWinPlatformScope;
            set
            {
                App.Instance.IsWinPlatformScope = value;
            }
        }

        protected override void OnCanceled()
        {
            // Not sure what to do if WinPlat cancels load. Usually the fallback is to go to WinPlat
            // Try again
            // If we StartMakeCurrent now it will be ignored, because we're in the middle of a load. So post

            App.Instance.IsWinPlatformScope = true;
            Manager.PostToUIThread(StartMakeCurrent);
            App.GoHome();
        }

        protected override TypeSet GetTypeSet() => Manager.WindowsTypeSet;
        protected override void SetTypeSet(TypeSet typeSet)
        {
            Manager.WindowsTypeSet = typeSet;
        }
        protected override void ClearTypeSet()
        {
            Manager.WindowsTypeSet = null;
        }


    }
}
