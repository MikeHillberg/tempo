using System.IO;

namespace Tempo
{
    internal class DotNetWindowsScopeLoader : ApiScopeLoader
    {
        internal DotNetWindowsScopeLoader() 
        {
        }

        protected override string Name => "DotNetWindows";

        protected override string LoadingMessage => "Loading DotNet Windows types ...";


        protected override void DoOffThreadLoad()
        {
            var files = Directory.GetFiles(App.DotNetWindowsPath);
            if(files == null || files.Length == 0)
            {
                DebugLog.Append($"No assemblies found in {App.DotNetWindowsPath}");
                return;
            }

            DesktopManager2.LoadDotNetWindowsAssembliesSync(!App.Instance.UsingCppProjections, files);
        }

        protected override bool IsSelected => App.Instance.IsDotNetWindowsScope;

        protected override void OnCanceled()
        {
            _ = MyMessageBox.Show(
                    "Unable to load dotnet Windows package\n\nSwitching to Windows Platform APIs",
                    "Load error");

            // Go back to an API scope we know is there
            App.Instance.IsWinPlatformScope = true;
            App.GoHome();
        }

        protected override TypeSet GetTypeSet() => Manager.DotNetWindowsTypeSet;
        protected override void ClearTypeSet()
        {
            Manager.DotNetWindowsTypeSet = null;
        }



    }
}
