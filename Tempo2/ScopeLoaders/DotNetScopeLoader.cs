using System.IO;

namespace Tempo
{
    internal class DotNetScopeLoader : ApiScopeLoader
    {
        internal DotNetScopeLoader() 
        {
        }

        protected override string Name => "DotNet";

        protected override string LoadingMessage => "Loading DotNet types ...";


        protected override void DoOffThreadLoad()
        {
            var files = Directory.GetFiles(App.DotNetCorePath);
            if(files == null || files.Length == 0)
            {
                DebugLog.Append($"No assemblies found in {App.DotNetCorePath}");
                return;
            }

            DesktopManager2.LoadDotNetAssembliesSync(!App.Instance.UsingCppProjections, files);
        }

        protected override bool IsSelected => App.Instance.IsDotNetScope;

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
        protected override void ClearTypeSet()
        {
            Manager.DotNetTypeSet = null;
        }



    }
}
