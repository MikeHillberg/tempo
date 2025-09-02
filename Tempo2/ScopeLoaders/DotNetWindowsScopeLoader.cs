using System.IO;

namespace Tempo
{
    internal class DotNetWindowsScopeLoader : ApiScopeLoader
    {
        internal DotNetWindowsScopeLoader() 
        {
        }

        public override string Name => "DotNetWindows";
        public override string MenuName => ".Net Windows";

        protected override string LoadingMessage => "Loading DotNet Windows types ...";


        //protected override TypeSet DoOffThreadLoad()
        //{
        //    var files = Directory.GetFiles(App.DotNetWindowsPath);
        //    if(files == null || files.Length == 0)
        //    {
        //        DebugLog.Append($"No assemblies found in {App.DotNetWindowsPath}");
        //        return null;
        //    }

        //    var typeSet = DesktopManager2.LoadDotNetWindowsAssembliesSync(files);
        //    return typeSet;
        //}

        DotNetWindowsTypeSetLoader _typeSetLoader = null;

        protected override TypeSetLoader GetTypeSetLoader()
        {
            if (_typeSetLoader == null && !string.IsNullOrEmpty(DotNetScopeLoader.DotNetWindowsPath))
            {
                var files = Directory.GetFiles(DotNetScopeLoader.DotNetWindowsPath);
                if (files == null || files.Length == 0)
                {
                    DebugLog.Append($"No assemblies found in {DotNetScopeLoader.DotNetWindowsPath}");
                    return null;
                }

                _typeSetLoader = new DotNetWindowsTypeSetLoader(files);
            }

            return _typeSetLoader;
        }



        public override bool IsSelected
        {
            get => App.Instance.IsDotNetWindowsScope;
            set
            {
                App.Instance.IsDotNetWindowsScope = value;
            }
        }

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
        protected override void SetTypeSet(TypeSet typeSet)
        {
            Manager.DotNetWindowsTypeSet = typeSet;
        }
        protected override void ClearTypeSet()
        {
            Manager.DotNetWindowsTypeSet = null;
        }



    }
}
