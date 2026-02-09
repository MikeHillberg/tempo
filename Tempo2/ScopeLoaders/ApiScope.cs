using CommonLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Tempo
{
    /// <summary>
    /// Methods to load an API Scope (Windows, WinAppSDK, Custom)
    /// Most of the work is done here, a few customizations in the per-scope derived classes
    /// </summary>
    abstract internal class ApiScopeLoader
    {
        bool _isCanceled = false;

        ManualResetEvent _loadCompletedEvent = null;
        ManualResetEvent _loadingThreadEvent = null;
        static bool _isPreloaded = false;

        internal ApiScopeLoader()
        {
            App.Instance.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != null && e.PropertyName == nameof(App.UsingCppProjections))
                {
                    DebugLog.Append($"UsingCppProjections changed for {this.Name}: {App.Instance.UsingCppProjections}");

                    var typeSet = GetTypeSet();
                    if (typeSet != null
                        && typeSet.IsWinmd
                        && typeSet.UsesWinRTProjections != !App.Instance.UsingCppProjections)
                    {
                        Close();
                        StartMakeCurrent();
                    }
                }
            };

#if DEBUG
            foreach (var loader in ScopeLoaders)
            {
                Debug.Assert(loader.GetType() != this.GetType());
            }
#endif

            // Keep a static list of all the scope loaders
            ScopeLoaders.Add(this);
        }

        public static List<ApiScopeLoader> ScopeLoaders = new List<ApiScopeLoader>();

        /// <summary>
        /// Called when the TypeSet property is set
        /// </summary>
        void OnTypeSetLoaded()
        {
            if (_isPreloaded)
            {
                return;
            }
            _isPreloaded = true;

            // Fork a thread to do the preloading in the background
            var thread = new Thread(PreloadThread);
            thread.Start();
        }


        // Kernel32!SetThreadPriority
        // (Should use cswinrt package, but Copilot provided this and it's just this one simple thing)
        [DllImport("kernel32.dll")]
        static extern IntPtr GetCurrentThread();
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetThreadPriority(IntPtr hThread, int nPriority);
        const int THREAD_MODE_BACKGROUND_BEGIN = 0x00010000;

        /// <summary>
        /// Thread that runs preload on all the Api Scope Loaders
        /// </summary>
        static void PreloadThread()
        {
            // Make this a fully background thread
            Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
            if (!SetThreadPriority(GetCurrentThread(), THREAD_MODE_BACKGROUND_BEGIN))
            {
                DebugLog.Append("Failed to enter background mode. Error: " + Marshal.GetLastWin32Error());
            }

            // Make a copy to avoid any risk of modifying while enumerating
            var scopeLoaders = ScopeLoaders.ToArray();

            // Pass 1 of preload on all the scope loaders
            foreach (var loader in scopeLoaders)
            {
                loader.CachedAllNames = loader.Preload1();
            }

            // Pass 2 of preload on all the scope loaders
            foreach (var loader in scopeLoaders)
            {
                loader.CachedAllNames = loader.Preload2();
            }

            DebugLog.Append("ApiScopeLoader Preload complete");
            Manager.PostToUIThread(() =>
            {
                AreApiScopesPreloading.Value = false;
            });
        }

        protected KeyValuePair<string, string>[] Preload1()
        {
            Debug.Assert(Thread.CurrentThread != Manager.MainThread);

            var typeSetLoader = this.GetTypeSetLoader();
            if (typeSetLoader != null)
            {
                return typeSetLoader.Preload1();
            }
            return null;
        }

        protected KeyValuePair<string, string>[] Preload2()
        {
            Debug.Assert(Thread.CurrentThread != Manager.MainThread);

            var typeSetLoader = this.GetTypeSetLoader();
            if (typeSetLoader != null)
            {
                return typeSetLoader.Preload2();
            }
            return null;
        }

        public static ReifiedProperty<bool> AreApiScopesPreloading = new(true, oneChangeNotification:true);


        //// Implement Preload pass 1
        //protected abstract KeyValuePair<string, string>[] Preload1();

        //// Implement Preload pass 2
        //protected abstract KeyValuePair<string, string>[] Preload2();

        protected virtual TypeSetLoader GetTypeSetLoader()
        {
            return null;
        }

        // Do a full load, called from a worker thread
        protected virtual TypeSet DoOffThreadLoad()
        {
            Debug.Assert(Thread.CurrentThread != Manager.MainThread);

            var typeSetLoader = GetTypeSetLoader();
            if (typeSetLoader == null)
            {
                return null;
            }

            var typeSet = typeSetLoader.Load();
            return typeSet;
        }

        public abstract string Name { get; }

        public virtual string MenuName => Name;

        protected virtual void OnCompleted()
        {
            // This needs to wait until CurrentTypeSet is set
            _ = BackgroundHelper2.DoWorkAsync<object>(() =>
            {
                if (!IsSelected)
                {
                    return null;
                }

                // Warm the cache
                var filter = new SearchExpression();
                filter.RawValue = "Hello";

                // Don't change the iteration because we could be doing a real calculation,
                // and we don't want to interfere with that
                var iteration = Manager.RecalculateIteration;
                Manager.GetMembers(filter, iteration);

                return null;
            });
        }

        internal virtual void Close()
        {
            DebugLog.Append($"Closing {Name}");

            ClearTypeSet();
            _loadCompletedEvent = null;
            _loadingThreadEvent = null;
            _isCanceled = true;

            // No throw risk; returns false if not found
            ScopeLoaders.Remove(this);

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
        protected abstract void SetTypeSet(TypeSet typeSet);
        protected abstract void ClearTypeSet();

        public abstract bool IsSelected { get; set; }

        KeyValuePair<string, string>[] _cachedAllNames;
        protected KeyValuePair<string, string>[] CachedAllNames
        {
            get => _cachedAllNames;
            private set
            {
                _cachedAllNames = value;
                if (value != null)
                {
                    RaiseAllNamesChanged();
                }
            }
        }

        /// <summary>
        /// All type/member names in this type set
        /// </summary>
        public KeyValuePair<string, string>[] AllNames
        {
            get
            {
                // If we have a TypeSet, and it's loaded its names, use that
                var typeSet = GetTypeSet();
                if (typeSet != null )
                {
                    if (typeSet.AllNames != null)
                    {
                        return typeSet.AllNames;
                    }
                    else
                    {
                        typeSet.RegisterAllNamesChanged(RaiseAllNamesChanged);
                    }    
                }

                // Otherwise return the cached value (which too could be null)
                return CachedAllNames;
            }
        }

        OneTimeCallbacks _allNamesChangedCallback = new OneTimeCallbacks();
        public void RegisterAllNamesChanged(Action callback)
        {
            _allNamesChangedCallback.Add(callback);
        }

        void RaiseAllNamesChanged()
        {
            _allNamesChangedCallback?.Invoke();
        }

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
            // bugbug: should be creating a new loader on language changes now?
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
                    var typeSet = DoOffThreadLoad();
                    Debug.Assert(GetTypeSet() == null);

                    // TypeSet is stored by the subclass
                    SetTypeSet(typeSet);

                    // Process the new type set in the base class
                    OnTypeSetLoaded();
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

                // MainWindow puts info about the loaded api scope in its title bar
                RaiseApiScopeInfoChanged();

                // This must be called after CurrentTypeSet is set
                OnCompleted();
            }

            DebugLog.Append($"Finishing StartMakeCurrent ({Name})");

            // Done with the thread
            _loadingThreadEvent = null;

            // This will release Ensure()
            _loadCompletedEvent.Set();
            _loadCompletedEvent = null;

            return;
        }

        /// <summary>
        /// Info about the API Scope changed (such as WASDK channel)
        /// </summary>
        static public event EventHandler ApiScopeInfoChanged;

        static public void RaiseApiScopeInfoChanged()
        {
            ApiScopeInfoChanged?.Invoke(null, EventArgs.Empty);
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

                DebugLog.Append(ex, $"Exception in EnsureLoadedAsync");
                Debug.Assert(false);
                return false;
            }
        }

    }



}
