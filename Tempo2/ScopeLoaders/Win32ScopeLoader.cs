namespace Tempo
{
    internal class Win32ScopeLoader : ApiScopeLoader
    {
        internal Win32ScopeLoader() 
        {
        }

        protected override string Name => "Win32";

        protected override string LoadingMessage => "Checking nuget.org for latest Win32 Metadata package ...";


        protected override void DoOffThreadLoad()
        {
            DesktopManager2.LoadWin32AssembliesSync(!App.Instance.UsingCppProjections);
        }

        protected override bool IsSelected => App.Instance.IsWin32Scope;

        protected override void OnCanceled()
        {
            _ = MyMessageBox.Show(
                    "Unable to load Win32 package\n\nSwitching to Windows Platform APIs",
                    "Load error");

            // Go back to an API scope we know is there
            App.Instance.IsWinPlatformScope = true;
            App.GoHome();
        }

        protected override TypeSet GetTypeSet() => Manager.Win32TypeSet;
        protected override void ClearTypeSet()
        {
            Manager.Win32TypeSet = null;
        }



    }
}
