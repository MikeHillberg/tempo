using System.Threading.Tasks;

namespace Tempo
{
    internal class BaselineScopeLoader : ApiScopeLoader
    {
        internal BaselineScopeLoader() 
        {
        }

        public override string Name => "Baseline";

        protected override string LoadingMessage => "Loading baseline  ...";

        public void StartMakeCurrent(string[] filenames)
        {
            CloseBaselineScope();

            App.Instance.BaselineFilenames = filenames;

            foreach (var filename in filenames)
            {
                DebugLog.Append($"Loading baseline scope {filename}");
            }

            base.StartMakeCurrent();

        }


        protected override TypeSet DoOffThreadLoad()
        {
            var typeSet = new MRTypeSet("Baseline", !App.Instance.UsingCppProjections);
            DesktopManager2.LoadTypeSetMiddleweightReflection(typeSet, App.Instance.BaselineFilenames);
            return typeSet;
        }

        protected override void OnCompleted()
        {
            if (GetTypeSet()?.TypeCount == 0)
            {
                DebugLog.Append($"No APIs found in baseline");

                // Clear out Manager.BaselineTypeSet
                CloseBaselineScope();

                _ = MyMessageBox.Show("No APIs found in baseline", null, "OK");
                return;
            }


            // Queue this so we don't go into a reentrant change handler
            Manager.PostToUIThread(() =>
            {
                Manager.Settings.CompareToBaseline = true;

                // IsBaselineScopeLoaded is a function of BaselineTypeSet
                App.Instance.RaisePropertyChange(nameof(App.IsBaselineScopeLoaded));
            });
        }

        // Baseline is never selected
        public override bool IsSelected
        {
            get => false;
            set => throw new System.NotSupportedException("Cannot select Baseline scope");
        }

        public static void CloseBaselineScope()
        {
            Manager.Settings.CompareToBaseline = false;
            Manager.BaselineTypeSet = null;

            App.Instance.RaisePropertyChange(nameof(App.IsBaselineScopeLoaded));
        }


        protected override TypeSet GetTypeSet()
        { 
            return Manager.BaselineTypeSet; 
        }

        protected override void SetTypeSet(TypeSet typeSet)
        {
            Manager.BaselineTypeSet = typeSet;
        }

        protected override void ClearTypeSet()
        {
            Manager.BaselineTypeSet = null;
        }


    }
}
