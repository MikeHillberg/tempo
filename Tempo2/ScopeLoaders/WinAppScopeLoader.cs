using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Tempo
{
    /// <summary>
    /// Loader for the WinAppSDK API Scope
    /// </summary>
    internal class WinAppScopeLoader : ApiScopeLoader
    {
        WinAppTypeSetLoader _typeSetLoader;

        override public string Name => WinAppTypeSet.StaticName;

        protected override string LoadingMessage => "Checking nuget.org for latest WinAppSDK package ...";

        protected override TypeSetLoader GetTypeSetLoader()
        {
            if (_typeSetLoader == null)
            {
                _typeSetLoader = new WinAppTypeSetLoader(
                    App.Instance.WinAppSDKChannel,
                    !App.Instance.UsingCppProjections);
            }

            return _typeSetLoader;
        }


        public override bool IsSelected
        {
            get => App.Instance.IsWinAppScope;
            set
            {
                App.Instance.IsWinAppScope = value;
            }
        }

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
        protected override void SetTypeSet(TypeSet typeSet)
        {
            Manager.WindowsAppTypeSet = typeSet;
        }
        protected override void ClearTypeSet()
        {
            _typeSetLoader?.ResetProjections(useWinrtProjections: !App.Instance.UsingCppProjections);
            Manager.WindowsAppTypeSet = null;
        }
    }
}
