using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Tempo
{
    internal class CustomScopeLoader : ApiScopeLoader
    {
        internal CustomScopeLoader()
        {
        }

        internal void StartMakeCurrent(bool navigateToSearchResults)
        {
            _navigateToSearchResults = navigateToSearchResults;
            base.StartMakeCurrent();
        }



        internal override void Close()
        {
            base.Close();

            DesktopManager2.CustomApiScopeFileNames.Value = new string[0];
            App.SaveCustomFilenamesToSettings();

        }

        protected override string LoadingMessage => "Loading ...";

        protected override string Name => "Custom";

        public bool HasFile
        {
            get
            {
                var filenames = DesktopManager2.CustomApiScopeFileNames.Value;
                if (filenames != null
                    && filenames.Length > 0
                    && !string.IsNullOrEmpty(filenames[0]))
                {
                    return true;
                }

                return false;
            }
        }

        internal async void EnsurePickedAndStartMakeCurrent()
        {
            var navigateToSearchResults = false;

            if (!HasFile)
            {
                // Need to show custom files but we don't have any loaded yet

                Manager.CurrentTypeSet = null;
                var shouldContinue = await PickAndAddCustomApis();
                if (!shouldContinue)
                {
                    App.Instance.IsWinPlatformScope = true;
                    App.GoHome();
                    return;
                }

                navigateToSearchResults = true;
            }

            StartMakeCurrent(navigateToSearchResults);
        }


        /// <summary>
        /// Pick new custom metadata files and add to the current set
        /// </summary>
        internal async Task<bool> PickAndAddCustomApis()
        {
            var newFilenames = await App.TryPickMetadataFilesAsync();
            if (newFilenames == null)
            {
                return false;
            }

            AddCustomApis(newFilenames.ToArray());
            StartMakeCurrent();

            return true;
        }

        /// <summary>
        /// Add new custom metadata files to the current set
        /// </summary>
        public void AddCustomApis(string[] newFilenames)
        {
            var currentFilenames = DesktopManager2.CustomApiScopeFileNames.Value;

            App.CloseCustomScope(false);

            if (currentFilenames != null)
            {
                newFilenames
                    = currentFilenames.Union(newFilenames).ToArray();
            }

            DesktopManager2.CustomApiScopeFileNames.Value = newFilenames;
        }


        protected override void DoOffThreadLoad()
        {
            var typeSet = new MRTypeSet(MRTypeSet.CustomMRName, !App.Instance.UsingCppProjections);
            DesktopManager2.LoadTypeSetMiddleweightReflection(
                typeSet,
                DesktopManager2.CustomApiScopeFileNames.Value);

            // Use the filenames as the TypeSet version (nuget packages tend to have a version number in their name)
            var names = new List<string>();
            foreach(var filename in DesktopManager2.CustomApiScopeFileNames.Value)
            {
                var name = System.IO.Path.GetFileNameWithoutExtension(filename);
                names.Add(name);
            }

            typeSet.Version = string.Join(", ", names);

            Manager.CustomMRTypeSet = typeSet;

            // Now that we know that the load worked, save the list of custom filenames
            // If there's a bug and it doesn't load, don't want to save them,
            // or it would be difficult to get the device out of the error state
            App.SaveCustomFilenamesToSettings();
        }

        bool _navigateToSearchResults = false;

        protected override Task OnCompleted()
        {
            if (_navigateToSearchResults)
            {
                _navigateToSearchResults = false;

                App.Instance.GotoSearch();

                // This should happen in App automatically, but playing it safe
                App.EnableRoot();
            }

            return Task.CompletedTask;
        }

        protected override bool IsSelected => App.Instance.IsCustomApiScope;

        protected override void OnCanceled()
        {
            // We're not going to navigate, so re-enable now
            App.EnableRoot();

            _ = MyMessageBox.Show(
                    "Unable to load scope\n\nSwitching to Windows Platform APIs",
                    "Load error");

            // Go back to an API scope we know is there
            App.Instance.IsWinPlatformScope = true;
            App.GoHome();
        }

        protected override TypeSet GetTypeSet() => Manager.CustomMRTypeSet;
        protected override void ClearTypeSet()
        {
            Manager.CustomMRTypeSet = null;
        }




    }
}
