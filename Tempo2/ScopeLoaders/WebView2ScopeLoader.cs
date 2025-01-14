namespace Tempo
{
    internal class WebView2ScopeLoader : ApiScopeLoader
    {
        internal WebView2ScopeLoader() 
        {
        }

        override protected string Name => "WebView2SDK";

        protected override string LoadingMessage => "Checking nuget.org for latest WebView2 package ...";


        protected override void DoOffThreadLoad()
        {
            DesktopManager2.LoadWebView2AssembliesSync(WinAppSDKChannel.Stable, !App.Instance.UsingCppProjections);
        }

        protected override bool IsSelected => App.Instance.IsWebView2Scope;

        protected override void OnCanceled()
        {
            _ = MyMessageBox.Show(
                    "Unable to load WebView2 package\n\nSwitching to Windows Platform APIs",
                    "Load error");

            // Go back to an API scope we know is there
            App.Instance.IsWinPlatformScope = true;
            App.GoHome();
        }

        protected override TypeSet GetTypeSet() => Manager.WebView2TypeSet;
        protected override void ClearTypeSet()
        {
            Manager.WebView2TypeSet = null;
        }


    }
}
