namespace Tempo
{
    internal class WinAppScopeLoader : ApiScopeLoader
    {
        internal WinAppScopeLoader() 
        {
        }

        override protected string Name => "WinAppSDK";

        protected override string LoadingMessage => "Checking nuget.org for latest WinAppSDK package ...";


        protected override void DoOffThreadLoad()
        {
            DesktopManager2.LoadWinAppSdkAssembliesSync(WinAppSDKChannel.Stable, !App.Instance.UsingCppProjections);
        }

        protected override bool IsSelected => App.Instance.IsWinAppScope;

        protected override void OnCanceled()
        {
            _ = MyMessageBox.Show(
                    "Unable to load WinAppSDK package\n\nSwitching to Windows Platform APIs",
                    "Load error");

            // Go back to an API scope we know is there
            App.Instance.IsWinPlatformScope = true;
            App.GoHome();
        }

        protected override TypeSet GetTypeSet() => Manager.WindowsAppTypeSet;
        protected override void ClearTypeSet()
        {
            Manager.WindowsAppTypeSet = null;
        }


    }
}
