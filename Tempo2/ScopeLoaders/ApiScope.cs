using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Tempo
{
    /// <summary>
    /// Methods to load an API Scope (Windows, WinAppSDK, Custom)
    /// </summary>
    abstract internal class ApiScopeLoader
    {
        bool _isCanceled = false;

        ManualResetEvent _loadCompletedEvent = null;
        ManualResetEvent _loadingThreadEvent = null;

        internal ApiScopeLoader()
        {
            App.Instance.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != null && e.PropertyName == nameof(App.UsingCppProjections))
                {
                    DebugLog.Append($"UsingCppProjections changed: {App.Instance.UsingCppProjections}");

                    if (GetTypeSet() != null
                        && GetTypeSet().UsesWinRTProjections != !App.Instance.UsingCppProjections)
                    {
                        Close();
                        StartMakeCurrent();
                    }

                }
            };
        }


        protected abstract string Name { get; }

        protected abstract void DoOffThreadLoad();

        protected virtual Task OnCompleted()
        {

            // This needs to wait until CurrentTypeSet is set
            _ = BackgroundHelper2.DoWorkAsync<object>(() =>
            {
                // Warm the cache
                var filter = new SearchExpression();
                filter.RawValue = "Hello";
                var iteration = ++Manager.RecalculateIteration;
                Manager.GetMembers(filter, iteration);

                return null;
            });

            return Task.CompletedTask;
        }

        internal virtual void Close()
        {
            DebugLog.Append($"Closing {Name}");

            ClearTypeSet();
            _loadCompletedEvent = null;
            _loadingThreadEvent = null;
            _isCanceled = true;

            // bugbug: if you set this to null, the x:Bind for some reason ignores it
            // (note that it's a function xBind), due to this generated code.
            // Workaround is to set to an empty array
            //private void Update_Tempo_App_Instance_BaselineFilenames(global::System.String[] obj, int phase)
            //{
            //    if (obj != null)
            //    {
            //        this.Update_M_FilenamesToText_965212445(phase);
            //    }
            //}
            App.Instance.BaselineFilenames = new string[0]; //null;
        }

        protected virtual void OnCanceled()
        {
        }

        protected abstract TypeSet GetTypeSet();
        protected abstract void ClearTypeSet();

        protected abstract bool IsSelected { get; }


        protected abstract string LoadingMessage { get; }

        protected virtual Task<bool> OnNeedToLoadScope()
        {
            return Task.FromResult(true);
        }

        internal async void StartMakeCurrent()
        {
            DebugLog.Append($"StartMakeCurrent: {Name}");

            // If there's already in a load in progress, don't start another
            if (_loadCompletedEvent != null)
            {
                DebugLog.Append($"Load in progress ({Name})");
                return;
            }

            // If the target type set is already loaded,
            // just make it Current if it isn't already

            if (GetTypeSet() != null)
            {
                DebugLog.Append($"Target type set is already loaded: {Name}");
                Manager.CurrentTypeSet = GetTypeSet();
                return;
            }

            // We need to load the type set.
            // Initialize it to null (it may be non-null but the wrong projection language)

            ClearTypeSet();


            // This event will be signaled when the loading worker thread finishes
            // Make a local copy of this one too

            _loadingThreadEvent = new ManualResetEvent(false);
            var loadingThreadEvent = _loadingThreadEvent;

            // This event is signaled when the load is complete
            // This happens on the UI thread after the worker thread has completed
            // It's used to complete the async wait in the Ensure call

            _loadCompletedEvent = new ManualResetEvent(false);
            var loadCompletedEvent = _loadCompletedEvent;

            // This will be set true if the user cancels the Loading dialog
            _isCanceled = false;

            // Fork the load action
            _ = Task.Run(() =>
            {
                try
                {
                    DoOffThreadLoad();
                }
                catch (Exception)
                {
                    _isCanceled = true;
                }

                // Run a completion on the UI thread
                // The member variable could be null now so used the copy
                loadingThreadEvent.Set();
            });

            // The event will be released either by the load completing or by the Ensure method canceling the load
            // bugbug: there must be a better way to do this than forking another thread?
            await Task.Run(() => loadingThreadEvent.WaitOne());

            // We're on the UI thread after the load thread has completed, or we're canceling

            if (loadCompletedEvent != _loadCompletedEvent)
            {
                // By the time this load thread completed another load had started for this type set
                // Don't call OnCanceled; subclasses use that to pop a dialog to the user
                DebugLog.Append("Load aborted because another load started");
                return;
            }
            else if (_isCanceled)
            {
                DebugLog.Append($"Load was canceled ({Name})");

                // The "loading ..." dialog was canceled by the user
                OnCanceled();
            }
            else
            {
                // Completed successfully

                DebugLog.Append($"Load completed ({Name})");

                // Make this type set current if it's still the type set we
                // want after this async delay
                if (IsSelected)
                {
                    DebugLog.Append($"Making current: {Name}");
                    Manager.CurrentTypeSet = GetTypeSet();
                }
                else
                {
                    // Only occurs for Baseline (since it's never selected)?
                    if (!Name.Contains("Baseline"))
                    {
                        DebugLog.Append($"Making current but not selected: {Name}, {App.Instance.ApiScopeName}");
                    }
                }

                // This must be called after CurrentTypeSet is set
                await OnCompleted();
            }

            DebugLog.Append($"Finishing StartMakeCurrent ({Name})");
            // Done with the thread
            _loadingThreadEvent = null;

            // This will release Ensure()
            _loadCompletedEvent.Set();
            _loadCompletedEvent = null;

            return;
        }



        internal bool IsLoaded => GetTypeSet() != null;
        internal bool IsLoadingOrLoaded => _loadCompletedEvent != null || IsLoaded;

        /// <summary>
        //// Make sure loading has completed, showing a dialog if necessary
        /// </summary>
        internal Task<bool> EnsureLoadedAsync()
        {
            return EnsureLoadedAsync(LoadingMessage);
        }

        Task _contentDialogTask = null;
        LoadingDialog _contentDialog = null;


        private async Task<bool> EnsureLoadedAsync(string loadingMessage)
        {
            // We might be loaded already, or a load might have been canceled
            var loadCompletedEvent = _loadCompletedEvent;
            if (loadCompletedEvent == null)
            {
                DebugLog.Append($"EnsureLoaded already loaded ({Name})");

                if (IsSelected)
                {
                    DebugLog.Append($"EnsureLoaded already selected: {Name}");
                    return true;
                }
                else
                {
                    // This only happens for Baseline, because it's never selected?

                    if (!Name.Contains("Baseline"))
                    {
                        DebugLog.Append($"EnsureLoaded loaded but not selected: {Name}");
                    }

                    return false;
                }
            }

            // We're in the middle of a load

            try
            {
                // Show a "Loading" message while we're downloading from nuget.org
                _contentDialog = new LoadingDialog();
                _contentDialog.Message = loadingMessage;
                _contentDialog.XamlRoot = App.HomePage.XamlRoot;

                DebugLog.Append($"Showing loading dialog: {loadingMessage}");
                _contentDialogTask = _contentDialog.ShowAsync().AsTask();
                _contentDialog.CloseButtonText = "Cancel";

                // Convert the load-completed event to a task (must be a better way to do this?)
                var loadCompletedEventTask = Task.Run(() => loadCompletedEvent.WaitOne());

                // Wait for either the load to complete, or the dialog to be canceled by the user
                await Task.WhenAny(new Task[] { loadCompletedEventTask, _contentDialogTask });
                _contentDialog.Hide();

                DebugLog.Append($"Done with loading dialog {loadingMessage}");

                if (GetTypeSet() == null)
                {
                    DebugLog.Append($"EnsureLoaded canceled ({Name})");

                    // _contentDialogTask signaled, meaning that the dialog was canceled
                    _isCanceled = true;

                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                if (_contentDialog != null)
                {
                    _contentDialog.Hide();
                    _contentDialog = null;
                }

                DebugLog.Append($"Exception in EnsureLoadedAsync: ${ex.Message}");
                Debug.Assert(false);
                return false;
            }
        }

    }



}
