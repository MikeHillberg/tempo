namespace Tempo
{
    internal class WebView2ScopeLoader : ApiScopeLoader
    {
        WebView2TypeSetLoader _typeSetLoader;

        override public string Name => "WebView2SDK";
        public override string MenuName => "WebView2";

        protected override string LoadingMessage => "Checking nuget.org for latest WebView2 package ...";

        protected override TypeSetLoader GetTypeSetLoader()
        {
            if (_typeSetLoader == null)
            {
                _typeSetLoader = new WebView2TypeSetLoader(
                    !App.Instance.UsingCppProjections);
            }

            return _typeSetLoader;
        }

        //protected override TypeSet DoOffThreadLoad()
        //{
        //    var typeSet = DesktopManager2.LoadWebView2AssembliesSync(
        //        WinAppSDKChannel.Stable, 
        //        !App.Instance.UsingCppProjections);

        //    return typeSet;
        //}

        public override bool IsSelected
        {
            get => App.Instance.IsWebView2Scope;
            set
            {
                App.Instance.IsWebView2Scope = value;
            }
        }

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
        protected override void SetTypeSet(TypeSet typeSet)
        {
            Manager.WebView2TypeSet = typeSet;
        }
        protected override void ClearTypeSet()
        {
            _typeSetLoader?.ResetProjections(!App.Instance.UsingCppProjections);
            Manager.WebView2TypeSet = null;
        }
    }
}
