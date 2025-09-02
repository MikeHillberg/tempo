namespace Tempo
{
    internal class Win32ScopeLoader : ApiScopeLoader
    {
        Win32TypeSetLoader _typeSetLoader = null;

        internal Win32ScopeLoader() 
        {
        }

        public override string Name => "Win32";

        protected override string LoadingMessage => "Checking nuget.org for latest Win32 Metadata package ...";


        protected override TypeSetLoader GetTypeSetLoader()
        {
            if (_typeSetLoader == null)
            {
                _typeSetLoader = new Win32TypeSetLoader(!App.Instance.UsingCppProjections);
            }

            return _typeSetLoader;
        }


        //protected override TypeSet DoOffThreadLoad()
        //{
        //    var typeSet = DesktopManager2.LoadWin32AssembliesSync(!App.Instance.UsingCppProjections);
        //    return typeSet;
        //}

        public override bool IsSelected
        {
            get => App.Instance.IsWin32Scope;
            set
            {
                App.Instance.IsWin32Scope = value;
            }
        }

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
        protected override void SetTypeSet(TypeSet typeSet)
        {
            Manager.Win32TypeSet = typeSet;
        }
        protected override void ClearTypeSet()
        {
            Manager.Win32TypeSet = null;
        }



    }
}
