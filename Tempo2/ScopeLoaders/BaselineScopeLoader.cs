using System.Threading.Tasks;

namespace Tempo
{
    internal class BaselineScopeLoader : ApiScopeLoader
    {
        internal BaselineScopeLoader() 
        {
        }

        protected override string Name => "Baseline";

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


        protected override void DoOffThreadLoad()
        {
            var typeSet = new MRTypeSet("Baseline", !App.Instance.UsingCppProjections);
            DesktopManager2.LoadTypeSetMiddleweightReflection(typeSet, App.Instance.BaselineFilenames);
            Manager.BaselineTypeSet = typeSet;
        }

        async protected override Task OnCompleted()
        {
            if (GetTypeSet()?.TypeCount == 0)
            {
                DebugLog.Append($"No APIs found in baseline");

                // Clear out Manager.BaselineTypeSet
                CloseBaselineScope();

                await MyMessageBox.Show("No APIs found in baseline", null, "OK");
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
        protected override bool IsSelected => false;

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

        protected override void ClearTypeSet()
        {
            Manager.BaselineTypeSet = null;
        }


    }
}
