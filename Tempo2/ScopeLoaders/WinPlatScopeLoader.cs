using System;
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

        protected override string Name => "WinPlat";

        protected override string LoadingMessage => "Loading ...";


        protected override void DoOffThreadLoad()
        {
            DesktopManager2.LoadWindowsTypesWithMR(
                !App.Instance.UsingCppProjections,
                (assemblyName) => LocateAssembly(assemblyName));
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

        protected override bool IsSelected => App.Instance.IsWinPlatformScope;

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
        protected override void ClearTypeSet()
        {
            Manager.WindowsTypeSet = null;
        }


    }
}
