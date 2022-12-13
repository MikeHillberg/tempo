using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage;
using System.Diagnostics;
using Windows.System;
using Windows.ApplicationModel.DataTransfer;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Text;
using Windows.ApplicationModel.Activation;

// mikehill_ua: got this error
// Error NETSDK1130	Microsoft.Services.Store.Engagement.winmd cannot be referenced. Referencing a Windows Metadata component directly when targeting .NET 5 or higher is not supported. For more information, see https://aka.ms/netsdk1130	UwpTempo2	C:\Program Files\dotnet\sdk\6.0.301\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.Sdk.targets	1007	


// mikehill_ua: Window title is "WinUI Desktop", but I had trouble figuring out where that was coming from

namespace Tempo
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application, INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            App.Instance = this;

            this.InitializeComponent();

            // mikehill_ua
            //this.Suspending += OnSuspending;

            DesktopManager2.Initialize(false);

            // Load all of the contract names/versions
            ContractInformation.Load();


            // Get the last picked dll/winmd/nupkg names
            LoadCustomFilenamesFromSettings();

            UnhandledException += App_UnhandledException;
        }

        /// <summary>
        /// INPC helper
        /// </summary>
        void RaisePropertyChange([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs e)
        {
            //for(int i = 0; i < 20; i++)
            //{
            //    if(Debugger.IsAttached)
            //    {
            //        break;
            //    }
            //    Thread.Sleep(500);
            //}

            // Is this the first app instance or a secondary?
            // If it's a secondary we'll forward to the existing and then exit
            var keyInstance = AppInstance.FindOrRegisterForKey("main");
            if (!keyInstance.IsCurrent)
            {
                // This isn't the existing app instance, so redirect and go away

                // Call RedirectActivationToAsync to forward this activation to the existing instance.
                // We need to ensure that that completes before we exit this process.
                // We can't wait for the async call to complete on this thread, because we don't have 
                // a dispatcher running (for the async to raise Completed on).
                // So run it on a separate thread, and use a semaphore to block waiting on it to complete.

                AppActivationArguments activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
                var sem = new Semaphore(0, 1);
                _ = Task.Run(() =>
                {
                    keyInstance.RedirectActivationToAsync(activationArgs).AsTask().Wait();
                    sem.Release();
                });

                // Wait on the main thread for the Redirect thread to complete
                sem.WaitOne();

                // Bugbug: is there a better way than Kill()?
                Process.GetCurrentProcess().Kill();
            }

            // If we get to this point, this process is the first instance of this app
            // The Activated event will raise if another process calls RedirectActivationToAsync to redirect
            // activation to here
            keyInstance.Activated += OnRedirected;



            // TODO This code defaults the app to a single instance app. If you need multi instance app, remove this part.
            // Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/applifecycle#single-instancing-in-applicationonlaunched
            // If this is the first instance launched, then register it as the "main" instance.
            // If this isn't the first instance launched, then "main" will already be registered,
            // so retrieve it.
            //var mainInstance = Microsoft.Windows.AppLifecycle.AppInstance.FindOrRegisterForKey("main");
            //var activatedEventArgs = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();

            //// If the instance that's executing the OnLaunched handler right now
            //// isn't the "main" instance.
            //if (!mainInstance.IsCurrent)
            //{
            //    // Redirect the activation (and args) to the "main" instance, and exit.
            //    await mainInstance.RedirectActivationToAsync(activatedEventArgs);
            //    System.Diagnostics.Process.GetCurrentProcess().Kill();
            //    return;
            //}

            // TODO This code handles app activation types. Add any other activation kinds you want to handle.
            // Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/applifecycle#file-type-association
            //if (activatedEventArgs.Kind == ExtendedActivationKind.File)
            //{
            //    OnFileActivated(activatedEventArgs);
            //}

            // Initialize MainWindow here
            Window = new MainWindow();
            Window.Title = "Tempo API Viewer";

            RootFrame = Window.Content as Frame;
            if (RootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                RootFrame = new Frame();

                // Place the frame in the current Window
                Window.Content = RootFrame;
            }

            // Force wide mode until skinny mode works again
            //RootFrame.MinWidth = 1024;

            if (RootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                // TODO Raname this HomePage type in case your app HomePage has a different name
                RootFrame.Navigate(typeof(HomePage), e.Arguments);
            }

            Window.Activate();
            WindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(Window);

            FinishLaunch();

        }


        /// <summary>
        /// This is called when a launch has been redirected to this process
        /// </summary>
        void OnRedirected(object sender, AppActivationArguments args)
        {
            App.Instance.ProcessActivationArgs(args);

            // Bugbug: this isn't actually activating (bringing to the foreground and giving focus)
            RunOnUIThread(() => App.Window.Activate());
        }



        protected void FinishLaunch()
        {
            Debug.Assert(Manager.Settings.AreAllMembersDefault());

            // Give the Net Standard library a way to post to the UI thread using a DispatcherQueue
            Manager.PostToUIThread = (action) => Window.DispatcherQueue.TryEnqueue(() => action());


            // Load the first API scope (now that the PostToUIThread has been set)

            // bugbug: shouldn't need to load this if we're going to load custom scope later from the command line,
            // but need to see if that actually works
            IsWinPlatformScope = true;

            //// The command line can be used to pass in filenames to load
            //ProcessCommandLine();


#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = false;
            }
#endif
            // mikehill_ua
            //SystemNavigationManager.GetForCurrentView().BackRequested += NavigateBackRequested;

            RootFrame = App.Window.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (RootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                RootFrame = new Frame();

            }
            // Too jarring
            //RootFrame.ContentTransitions = new TransitionCollection() { new ContentThemeTransition() };

            RootFrame.PointerPressed += (s, e2) =>
            {
                if (e2.GetCurrentPoint(s as UIElement).Properties.IsXButton1Pressed)
                    App.GoBack();
            };

            // Butbug: why are these accelerators doing key input rather than KeyboardAccelerator?
            // Is it because accelerators cause too many tooltips?
            RootFrame.KeyDown += (s, e2) =>
            {
                if (e2.Handled)
                {
                    return;
                }

                if (e2.Key == VirtualKey.E && (_keyModifiers == KeyModifiers.Control))
                {
                    // If we're in SearchResults, the search box is on the screen and all we have to do is focus it
                    if (RootFrame.Content is SearchResults)
                    {
                        (RootFrame.Content as SearchResults).FocusAndSelect();
                    }

                    // Otherwise jump to Home and focus the search box there.
                    else
                    {
                        MoveToMainAndFocusToSearch();
                    }
                }
                else if (e2.Key == VirtualKey.F3 && _keyModifiers == KeyModifiers.None)
                {
                    ResetAndGoHome();
                }
                else if (e2.Key == VirtualKey.Back
                            && _keyModifiers == KeyModifiers.None
                            && !(e2.OriginalSource is TextBox)) // bugbug!
                {
                    App.GoBack();
                }
                else if (e2.Key == VirtualKey.Left && _keyModifiers == KeyModifiers.Alt)
                {
                    App.GoBack();
                }
                else if (e2.Key == VirtualKey.T && _keyModifiers == KeyModifiers.Control)
                {
                    ToggleTypeFilter();
                }
                else if (e2.Key == VirtualKey.P && _keyModifiers == KeyModifiers.Control)
                {
                    TogglePropertyFilter();
                }
                else if (e2.Key == VirtualKey.H && _keyModifiers == KeyModifiers.Control)
                {
                    ToggleMethodFilter();
                }
                else if (e2.Key == VirtualKey.N && _keyModifiers == KeyModifiers.Control)
                {
                    ToggleEventFilter();
                }
                else if (e2.Key == VirtualKey.S && _keyModifiers == KeyModifiers.Control)
                {
                    Manager.Settings.CaseSensitive = !Manager.Settings.CaseSensitive;
                }
            };


            // Have to use HandledEventsToo for the Control key monitoring
            RootFrame.AddHandler(Frame.KeyDownEvent, new KeyEventHandler(RootFrame_KeyDown), true);
            RootFrame.AddHandler(Frame.KeyUpEvent, new KeyEventHandler(RootFrame_KeyUp), true);

            // bugbug: the handler always gets called twice (not very useful for a toggle accelerator)
            //AddAccelerator(VirtualKey.S, VirtualKeyModifiers.Control, () =>
            //{
            //    Manager.Settings.CaseSensitive = !Manager.Settings.CaseSensitive;
            //});


            RootFrame.NavigationFailed += OnNavigationFailed;

            // mikehill_ua
            //if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
            //{
            //    //TODO: Load state from previously suspended application
            //}


            // Place the frame in the current Window
            App.Window.Content = RootFrame;
            if (RootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                RootFrame.Navigate(typeof(HomePage), null); // e.Arguments);
            }

            // Enable the DragOver and Drop events
            RootFrame.AllowDrop = true;

            // Accept file drops
            RootFrame.DragOver += (s, e) =>
            {
                if (!e.DataView.AvailableFormats.Contains(StandardDataFormats.StorageItems))
                {
                    return;
                }

                e.AcceptedOperation = DataPackageOperation.Copy;
            };

            // When a file is dropped, try to open it
            RootFrame.Drop += async (s, e) =>
            {
                var deferral = e.GetDeferral();
                try
                {
                    var items = await e.DataView.GetStorageItemsAsync();

                    // Only support one file for now
                    if (items.Count == 1)
                    {
                        var storageFile = items[0] as StorageFile;
                        if (storageFile == null)
                        {
                            return;
                        }

                        CloseCustomScope(goHome: false);

                        // Switch to Custom API scope. Don't use the property setter because it will trigger a FilePicker
                        _isCustomApiScope = true;
                        RaisePropertyChange(nameof(IsCustomApiScope));

                        // Start loading and go to the search page
                        StartLoadCustomScope(
                            new string[] { storageFile.Path },
                            navigateToSearchResults: true);
                        App.Instance.GotoSearch(null);
                    }
                }
                finally
                {
                    deferral.Complete();
                }
            };


            // Ensure the current window is active
            App.Window.Activate();

            //while (!Debugger.IsAttached)
            //{
            //    Thread.Sleep(TimeSpan.FromSeconds(1));
            //}
        }

        /// <summary>
        /// Process the activation arguments (different than command line).
        /// This must be called on the tHread of the args.
        /// Returns true if something was processed.
        /// </summary>
        internal bool ProcessActivationArgs(AppActivationArguments activationArgs = null)
        {
            // On a redirected activation, we can't use AppInstance.GetActivatedEventArgs()
            if (activationArgs == null)
            {
                activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
            }

            // If launched with "tempo:Button", search for "Button"
            if (activationArgs.Kind == ExtendedActivationKind.Protocol)
            {
                var searchTerm = (activationArgs.Data as IProtocolActivatedEventArgs).Uri.AbsolutePath;

                // We've pulled everything out of the args, now if necessary we can switch threads

                RunOnUIThread(() => App.Instance.GotoSearch(searchTerm));

            }
            else
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Run an Action on the UI thread
        /// </summary>
        static void RunOnUIThread(Action action)
        {
            var dq = App.HomePage.DispatcherQueue;
            if (dq.HasThreadAccess)
            {
                action();
            }
            else
            {
                App.HomePage.DispatcherQueue.TryEnqueue(() => action());
            }
        }

        /// <summary>
        /// Check for filenames that should be loaded. This is called by HomePage by which point we have things loaded.
        /// </summary>
        internal void ProcessCommandLine()
        {
            // Not sure the best way to debug parameters that are passed in on the command line
            // (See AppExecutionAlias in Package.appxmanifest)
            // The solution here is to pass "/waitfordebugger" as a parameter, which will make it
            // sit until you attach a debugger to the process.


            var args = Environment.GetCommandLineArgs();

            // The first arg is the name of the exe
            if (args == null || args.Length == 1)
            {
                return;
            }

            List<string> commandLineFilenames = null;

            for (int i = 1; i < args.Length; i++)
            {
                // If asked, wait for a debugger to be attached
                if (args[i].ToLower() == "/waitfordebugger")
                {
                    while (!Debugger.IsAttached)
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                    }
                    continue;
                }

                if (commandLineFilenames == null)
                {
                    commandLineFilenames = new List<string>();
                }

                // Figure out the full path name
                // Path.GetFullPath() doesn't seem to mind invalidate characters coming in,
                // but the docs say it can throw, so show a message box if it does and abort.
                try
                {
                    var path = args[i];
                    path = Path.GetFullPath(path);
                    commandLineFilenames.Add(path);
                }
                catch (Exception)
                {
                    _ = MyMessageBox.Show(args[i], "Invalid path", isOKEnabled: true);
                    return;
                }

            }

            if (commandLineFilenames != null)
            {
                DesktopManager2.CustomApiScopeFileNames.Value = commandLineFilenames.ToArray();

                App.StartLoadCustomScope(
                    DesktopManager2.CustomApiScopeFileNames.Value,
                    navigateToSearchResults: true);

                // Switch to Custom API scope. Don't use the property setter because it will trigger a FilePicker
                _isCustomApiScope = true;
                RaisePropertyChange(nameof(IsCustomApiScope));
            }


        }

        public static MainWindow Window { get; private set; }

        public static IntPtr WindowHandle { get; private set; }


        // App.Current returns Application, App.Instance returns App
        // Getting App avoids the need for type casting, and also instance members
        // (Instance properties to enable INPC)
        static public App Instance { get; private set; }

        // Main window so that we can get the XamlRoot and DispatcherQueue
        static public FrameworkElement HomePage;

        // IsWinPlatformScope means show the Windows APIs
        static bool _isWinPlatformScope = false;
        public bool IsWinPlatformScope
        {
            get { return _isWinPlatformScope; }
            set
            {
                _isWinPlatformScope = value;
                if (value)
                {
                    if (Manager.WindowsTypeSetCS != null)
                    {
                        Manager.CurrentTypeSet = Manager.WindowsTypeSetCS;
                    }
                    else
                    {
                        // Start loading the APIs. Later when we call Ensure we'll make sure it's done
                        StartLoadWinPlatformScope();
                    }
                }

                RaisePropertyChange();
                RaisePropertyChange(nameof(ApiScopeName));
            }
        }

        /// <summary>
        /// Name of the API scope ("Windows", "WinAppSDK", "Custom")
        /// </summary>
        public string ApiScopeName
        {
            get
            {
                if(IsWinPlatformScope)
                {
                    return "Windows";
                }
                else if(IsWinAppScope)
                {
                    return "WASDK";
                }
                else if(IsCustomApiScope)
                {
                    return "Custom";
                }

                // Shouldn't ever get here, but it happens during bootstrapping
                return "API Scope";
            }
        }

        // IsWinAppScope means show the WinAppSDK APIs
        static bool _isWinAppScope = false;
        public bool IsWinAppScope
        {
            get { return _isWinAppScope; }
            set
            {
                if (_isWinAppScope == value)
                {
                    return;
                }

                _isWinAppScope = value;
                if (value)
                {
                    if (Manager.WindowsAppTypeSet != null)
                    {
                        Manager.CurrentTypeSet = Manager.WindowsAppTypeSet;
                    }
                    else
                    {
                        // Start loading the APIs. Later when we call Ensure we'll make sure it's done
                        StartLoadWinAppScope();
                    }
                }

                RaisePropertyChange();
                RaisePropertyChange(nameof(ApiScopeName));
            }
        }

        // IsCustomApiScope means show the APIs that were selected with a file picker
        public bool IsCustomApiScope
        {
            get { return _isCustomApiScope; }
            set
            {
                if (_isCustomApiScope == value)
                {
                    return;
                }

                _isCustomApiScope = value;
                if (value)
                {
                    _ = SelectAndStartLoadCustomApiScopeAsync();
                }

                RaisePropertyChange();
                RaisePropertyChange(nameof(ApiScopeName));
            }
        }

        /// <summary>
        /// Figure out what custom APIs to load and start loading them, or select them if already loaded
        /// </summary>
        /// <returns></returns>
        static public async Task SelectAndStartLoadCustomApiScopeAsync()
        {
            if (App.Instance.IsCustomApiScopeLoaded)
            {
                Manager.CurrentTypeSet = Manager.CustomMRTypeSet;
            }
            else if (_customApiScopeLoader != null)
            {
                // A load is already running
                return;
            }
            else
            {
                var filenames = DesktopManager2.CustomApiScopeFileNames.Value;
                if (filenames == null || filenames.Length == 0)
                {
                    // No filenames saved in app settings from last time
                    await PickAndStartLoadCustomApiScopeAsync();
                }
                else
                {
                    App.StartLoadCustomScope(
                        filenames,
                        navigateToSearchResults: false);
                }
            }

        }

        /// <summary>
        /// Close the custom API scope and load it again
        /// </summary>
        static public void ReloadCustomApiScope()
        {
            // Close anything that's open now
            App.CloseCustomScope(goHome: false);

            // Load custom APIs and make current scope selection
            if (App.Instance.IsCustomApiScope)
            {
                // Already selected, pick and load
                _ = App.SelectAndStartLoadCustomApiScopeAsync();
            }
            else
            {
                // Select, which will trigger a pick and load
                App.Instance.IsCustomApiScope = true;
            }
        }


        public bool IsCustomApiScopeLoaded
        {
            get { return _isCustomApiScopeLoaded; }
            set
            {
                if (value != _isCustomApiScopeLoaded)
                {
                    value = _isCustomApiScopeLoaded;
                    RaisePropertyChange();
                }
            }
        }
        bool _isCustomApiScopeLoaded = false;


        /// <summary>
        /// Show the file open picker to load metadata files, then start the load
        /// </summary>
        /// <returns></returns>
        static public async Task PickAndStartLoadCustomApiScopeAsync()
        {
            var filenames = await TryPickMetadataFilesAsync();

            if (filenames == null)
            {
                // Canceled. Go back to the OS APIs since we know that they're there
                App.Instance.IsWinPlatformScope = true;
                App.GoHome();
                return;
            }

            // Start the load
            App.StartLoadCustomScope(
                filenames.ToArray(),
                navigateToSearchResults: true);
        }


        /// <summary>
        /// Show a file picker to pick winmd/nupkg/dll files
        /// </summary>
        /// <returns></returns>
        static public async Task<IEnumerable<string>> TryPickMetadataFilesAsync()
        {
            // Create a folder picker.
            var filePicker = new Windows.Storage.Pickers.FileOpenPicker();

            // 1. Retrieve the window handle (HWND) of the current WinUI 3 window.
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow.Instance);

            // 2. Initialize the folder picker with the window handle (HWND).
            WinRT.Interop.InitializeWithWindow.Initialize(filePicker, hWnd);

            // Use the folder picker as usual.
            filePicker.FileTypeFilter.Add(".dll");
            filePicker.FileTypeFilter.Add(".winmd");
            filePicker.FileTypeFilter.Add(".nupkg");
            var files = await filePicker.PickMultipleFilesAsync();
            var filenames = from f in files select f.Path;
            if (filenames.FirstOrDefault() == null)
            {
                // Canceled
                return null;
            }

            return filenames;
        }


        /// <summary>
        /// Load the WinAppAPI scope (doesn't complete synchronously)
        /// </summary>
        void StartLoadWinAppScope()
        {
            _winAppScopeLoader = new ApiScopeLoader();

            _winAppScopeLoader.StartLoad(
                offThreadLoadAction: () => // Runs *off* UI thread
                {
                    DesktopManager2.LoadWinAppSdkAssembliesSync(WinAppSDKChannel.Stable, useWinrtProjections: true);
                },

                uiThreadCompletedAction: () => // Runs on UI thread (if succeeded)
                {
                    _winAppScopeLoader = null;
                    if (_isWinAppScope)
                    {
                        Manager.CurrentTypeSet = Manager.WindowsAppTypeSet;
                    }
                },

                uiThreadCanceledAction: () => // Runs on UI thread (if canceled)
                {
                    _winAppScopeLoader = null;

                    // Go back to an API scope we know is there
                    IsWinPlatformScope = true;
                    App.GoHome();
                });

        }

        static ApiScopeLoader _winAppScopeLoader = null;

        internal async static Task<bool> EnsureWinAppScopeLoadedAsync()
        {
            if (_winAppScopeLoader == null)
            {
                return true;
            }

            return await _winAppScopeLoader.EnsureLoadedAsync("Checking nuget.org for latest package ...");
        }



        static bool _isCustomApiScope = false;
        static ApiScopeLoader _customApiScopeLoader = null;

        /// <summary>
        /// Close the custom API scope, clear the settings, and go to the Windows APIs
        /// </summary>
        /// <param name="goHome"></param>
        public static void CloseCustomScope(bool goHome)
        {
            var isCurrent = Manager.CustomMRTypeSet != null && Manager.CustomMRTypeSet.IsCurrent;
            Manager.CustomMRTypeSet = Manager.CustomTypeSet = null;

            // Bugbug: workaround for x:Bind ignoring the two-way binding when this value is null
            DesktopManager2.CustomApiScopeFileNames.Value = new string[0];

            _customApiScopeLoader = null;
            App.Instance.IsCustomApiScopeLoaded = false;

            if (isCurrent && goHome)
            {
                App.Instance.IsWinPlatformScope = true;
                App.GoHome();
            }
        }

        /// <summary>
        /// Start loading the custom API scope (doesn't complete synchronously)
        /// </summary>
        /// <param name="filenames"></param>
        public static void StartLoadCustomScope(string[] filenames, bool navigateToSearchResults)
        {
            CloseCustomScope(goHome: false);

            var typeSet = new MRTypeSet(MRTypeSet.CustomMRName);
            _customApiScopeLoader = new ApiScopeLoader();

            _customApiScopeLoader.StartLoad(
                offThreadLoadAction: () =>
                {
                    DesktopManager2.LoadTypeSetMiddleweightReflection(typeSet, filenames, useWinRTProjections: true);
                },

                uiThreadCompletedAction: async () =>
                {
                    _customApiScopeLoader = null;

                    // Since this is completing what started off thread, the custom API scope might
                    // not be selected anymore (user might have clicked away)

                    if (_isCustomApiScope)
                    {
                        DesktopManager2.CustomApiScopeFileNames.Value = filenames;
                        SaveCustomFilenamesToSettings();

                        if (typeSet.TypeCount == 0)
                        {
                            await (new ContentDialog()
                            {
                                Content = "No APIs found",
                                XamlRoot = HomePage.XamlRoot,
                                CloseButtonText = "OK"
                            }).ShowAsync();

                            // Go to a scope we know exists
                            GoToWindowsScopeAndGoHome();
                            return;
                        }

                        Manager.CustomMRTypeSet = typeSet;
                        Manager.CurrentTypeSet = Manager.CustomMRTypeSet;
                        App.Instance.IsCustomApiScopeLoaded = true;

                        if (navigateToSearchResults)
                        {
                            App.Instance.GotoSearch();
                        }
                    }
                },

                uiThreadCanceledAction: () =>
                {
                    _customApiScopeLoader = null;

                    // Go to a scope we know exists
                    GoToWindowsScopeAndGoHome();
                });
        }

        /// <summary>
        /// Complete when StartCustomApiScope has finished
        /// </summary>
        async static Task<bool> EnsureCustomApiScopeAsync()
        {
            if (_customApiScopeLoader == null)
            {
                // Already finished
                return true;
            }

            // Wait for it to finish
            return await _customApiScopeLoader.EnsureLoadedAsync("Loading ...");
        }



        /// <summary>
        /// Baseline API scope used for comparissons
        /// </summary>
        public bool IsBaselineScopeLoaded
        {
            get
            {
                return Manager.BaselineTypeSet != null;
            }
        }

        static ApiScopeLoader _baselineScope = null;

        /// <summary>
        /// Make IsBaselineScopeLoaded go false
        /// </summary>
        public static void CloseBaselineScope()
        {
            _baselineScope = null;
            Manager.BaselineTypeSet = null;
            App.Instance.RaisePropertyChange(nameof(IsBaselineScopeLoaded));
        }

        /// <summary>
        /// Filenames that make up the baseline API scope (for comparissons)
        /// </summary>
        public string[] BaselineFilenames
        {
            get { return _baselineFilenames; }
            set
            {
                _baselineFilenames = value;
                RaisePropertyChange();
            }
        }
        string[] _baselineFilenames = null;


        /// <summary>
        /// Start loading the baseline API scope (doesn't complete synchronously)
        /// </summary>
        /// <param name="filenames"></param>
        public static void StartLoadBaselineScope(string[] filenames)
        {
            CloseBaselineScope();

            var typeSet = new MRTypeSet("Baseline");
            _baselineScope = new ApiScopeLoader();

            _baselineScope.StartLoad(
                offThreadLoadAction: () =>
                {
                    DesktopManager2.LoadTypeSetMiddleweightReflection(typeSet, filenames, useWinRTProjections: true);
                },

                uiThreadCompletedAction: async () =>
                {
                    _baselineScope = null;
                    if (typeSet.TypeCount == 0)
                    {
                        await (new ContentDialog()
                        {
                            Content = "No APIs found",
                            XamlRoot = HomePage.XamlRoot,
                            CloseButtonText = "OK"
                        }).ShowAsync();

                        return;
                    }

                    Manager.BaselineTypeSet = typeSet;
                    Manager.Settings.CompareToBaseline = true;
                    App.Instance.RaisePropertyChange(nameof(IsBaselineScopeLoaded));
                },

                uiThreadCanceledAction: () =>
                {
                    _baselineScope = null;
                });
        }


        static void GoToWindowsScopeAndGoHome()
        {
            App.Instance.IsWinPlatformScope = true;
            App.GoHome();
        }



        const string _customFilenamesKey = "CustomFilenames";

        /// <summary>
        /// Save CustomApiScopeFileNames to ApplicationData settings
        /// </summary>
        static void SaveCustomFilenamesToSettings()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            string setting;
            var sb = new StringBuilder();
            var filenames = DesktopManager2.CustomApiScopeFileNames.Value;

            if (filenames != null && filenames.Length != 0)
            {
                foreach (var f in DesktopManager2.CustomApiScopeFileNames.Value)
                {
                    sb.Append($"{f};");
                }

                sb.Remove(sb.Length - 1, 1);
            }

            setting = sb.ToString();

            // Write to the ApplicationDataContainer
            localSettings.Values[_customFilenamesKey] = setting;
        }

        /// <summary>
        /// Load CustomApiScopeFileNames from ApplicationData settings
        /// </summary>
        void LoadCustomFilenamesFromSettings()
        {
            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            var setting = localSettings.Values[_customFilenamesKey] as string;
            if (setting != null)
            {
                var filenames = setting.Split(';').ToArray();
                DesktopManager2.CustomApiScopeFileNames.Value = filenames;
            }
        }



        static ApiScopeLoader _winPlatformScopeLoader = null;

        /// <summary>
        /// Start loading the Windows APIs (doesn't complete synchronously)
        /// </summary>
        static void StartLoadWinPlatformScope()
        {
            _winPlatformScopeLoader = new ApiScopeLoader();

            _winPlatformScopeLoader.StartLoad(
                offThreadLoadAction: async () => // Runs *off* UI thread
                {
                    await DesktopManager2.LoadWindowsTypesWithMRAsync(
                        useWinRTProjections: true,
                        (assemblyName) => LocateAssembly(assemblyName));
                },

                uiThreadCompletedAction: () => // Runs on UI thread (if succeeded)
                {
                    _winPlatformScopeLoader = null;

                    // The API scope selection could have changed while we were away on the other thread
                    if (_isWinPlatformScope)
                    {
                        Manager.CurrentTypeSet = Manager.WindowsTypeSetCS;
                    }

                    // This needs to wait until CurrentTypeSet is set
                    // Bugbug: should be doing this for other api scopes too
                    _ = BackgroundHelper2.DoWorkAsync<object>(() =>
                    {
                        // Warm the cache
                        var filter = new SearchExpression();
                        filter.RawValue = "Hello";
                        var iteration = ++Manager.RecalculateIteration;
                        Manager.GetMembers(filter, iteration);

                        return null;
                    });
                },

                uiThreadCanceledAction: () => // Runs on UI thread (if canceled)
                {
                    _winPlatformScopeLoader = null;
                });
        }

        internal async static Task<bool> EnsureWinPlatformScopeLoaded()
        {
            if (_winPlatformScopeLoader == null)
            {
                return true;
            }

            return await _winPlatformScopeLoader.EnsureLoadedAsync("Loading");
        }



        /// <summary>
        /// Ensure that the current API scope is done loading
        /// </summary>
        /// <returns></returns>
        async public static Task<bool> EnsureApiScopeLoadedAsync()
        {
            // Regardless of what's selected, also ensure the baseline is selected
            if (_baselineScope != null)
            {
                await _baselineScope.EnsureLoadedAsync("Loading baseline");
            }

            if (_isWinAppScope)
            {
                // Returns false if the user cancels the load
                return await EnsureWinAppScopeLoadedAsync();
            }
            else if (_isCustomApiScope)
            {
                return await EnsureCustomApiScopeAsync();
            }
            else
            {
                return await EnsureWinPlatformScopeLoaded();
            }
        }


        /// <summary>
        /// Called by the MR loader when it can't find an assembly
        /// </summary>
        static string LocateAssembly(string assemblyName)
        {
            // Mostly, if a referenced assembly can't be found, let it be faked.
            // But we need some WinRT interop assemblies for things like GridLength
            if (_specialSystemAssemblyNames.Contains(assemblyName))
            {
                var task = StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assemblies/{assemblyName}.dll"));
                task.AsTask().Wait();
                return task.GetResults().Path;
            }

            return null;
        }

        static string[] _specialSystemAssemblyNames = new string[]
        {
            "System.Runtime.InteropServices.WindowsRuntime",
            "System.Runtime.WindowsRuntime",
            "System.Runtime.WindowsRuntime.UI.Xaml",
            "System.Runtime",
            "System.Private.CoreLib"
        };


        //TextReader GetAppTextResource(string name)
        //{
        //    // bugbug: make this async

        //    // mikehill_ua: missing using for StorageFile
        //    var uri = new Uri("ms-appx:///" + name);
        //    var asyncOp = StorageFile.GetFileFromApplicationUriAsync(uri);
        //    var file = asyncOp.AsTask().GetAwaiter().GetResult();
        //    var inputStream = file.OpenReadAsync().AsTask().GetAwaiter().GetResult();
        //    var classicStream = inputStream.AsStreamForRead();
        //    return new StreamReader(classicStream);
        //}

        // mikehill_ua: BackRequestedEventArgs could not be found
        //static private void NavigateBackRequested(object sender, BackRequestedEventArgs e)
        //{
        //    if (e.Handled)
        //        return;

        //    e.Handled = true;
        //    GoBack();
        //}


        static internal Stack<object> NavigationStateStack = new Stack<object>();

        static void InternalNavigate(Type type, object parameter)
        {
            RootFrame.Navigate(type, parameter);
        }

        public static void GotoNamespaces(string initial)
        {
            InternalNavigate(typeof(NamespaceView), initial);
        }


        /// <summary>
        /// Show the search filters, either navigating or in a flyout
        /// </summary>
        internal static void GotoFilters(bool showOld = false)
        {
            // See if someone wants to handle the filters (show it in a flyout)
            var args = new FilterRequestedEventArgs() { ShowOld = showOld };
            FilterRequested?.Invoke(null, args);

            // Otherwise, navigate to the Filters page
            if (!args.Handled)
            {
                InternalNavigate(typeof(Filters3), null);
            }
        }

        /// <summary>
        /// Event to get called back if the search filters UI should be shown
        /// </summary>
        static internal event EventHandler<FilterRequestedEventArgs> FilterRequested;

        internal class FilterRequestedEventArgs : EventArgs
        {
            internal bool Handled { get; set; } = false;

            /// <summary>
            /// Show the old/bad UI
            /// </summary>
            internal bool ShowOld { get; set; }
        }


        /// <summary>
        /// Go to the SearchResults page (if not already there) and put focus on the search box
        /// </summary>
        public void GotoSearch(string text = "")
        {
            if (text == "")
            {
                text = App.Instance.SearchText;
            }

            InternalNavigate(typeof(SearchResults), text);
        }

        /// <summary>
        /// Go to the home page
        /// </summary>
        public static void GoHome(string searchString = null)
        {
            InternalNavigate(typeof(HomePage), searchString);
        }

        /// <summary>
        /// The search text (search text boxes bind to this)
        /// </summary>        
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (value != _searchText)
                {
                    _searchText = value;
                    RaisePropertyChange();
                }
            }
        }
        string _searchText;

        public static void ClearBackStack()
        {
            RootFrame.BackStack.Clear();
            NavigationStateStack.Clear();
        }

        public static bool CanGoBack
        {
            get { return RootFrame.CanGoBack; }
        }

        public static void GoBack()
        {
            if (RootFrame.Content is SearchResults)
            {
                var root = (RootFrame.Content as SearchResults) as MySerializableControl;
                if (root != null)
                {
                    if (root.TryNavigateBack())
                    {
                        return;
                    }
                }
            }

            if (RootFrame.CanGoBack)
            {
                RootFrame.GoBack();
            }
        }

        public static string HeadingConnectedAnimationKey = "HeadingConnectedAnimationKey";

        public const int MinWidthForTwoColumns = 800;
        public const int MinWidthForThreeColumns = 1000;

        public static void NavigateColorsPage()
        {
            RootFrame.Navigate(typeof(ColorsIllustration), null);
        }
        public static void NavigateTypeRampPage()
        {
            RootFrame.Navigate(typeof(TypeRampIllustration));
        }
        public static void NavigateSymbolsIllustration()
        {
            RootFrame.Navigate(typeof(SymbolsIllustration));
        }

        public static void Navigate(MemberViewModel memberVM)
        {
            InternalNavigate(GetViewTypeFor(memberVM), memberVM);
        }

        public static Type GetViewTypeFor(MemberViewModel memberVM)
        {
            if (memberVM is TypeViewModel)
                return typeof(TypeDetailView);
            else if (memberVM is PropertyViewModel)
                return typeof(PropertyDetailView);
            else if (memberVM is MethodViewModel)
                return typeof(MethodDetailView);
            else if (memberVM is EventViewModel)
                return typeof(EventDetailView);
            else if (memberVM is ConstructorViewModel)
                return typeof(ConstructorDetailView);
            else if (memberVM is FieldViewModel)
                return typeof(FieldDetailView);

            // mikehill_ua: Missing using for Debug
            Debug.Assert(false);
            return null;
        }

        public static MySerializableControl GetViewFor(MemberViewModel memberVM)
        {
            if (memberVM is TypeViewModel)
                return new TypeDetailView();
            else if (memberVM is PropertyViewModel)
                return new PropertyDetailView();
            else if (memberVM is MethodViewModel)
                return new MethodDetailView();
            else if (memberVM is EventViewModel)
                return new EventDetailView();
            else if (memberVM is ConstructorViewModel)
                return new ConstructorDetailView();
            else if (memberVM is FieldViewModel)
                return new FieldDetailView();

            Debug.Assert(false);
            return null;
        }
        // bugbug: make this IEnumerable<string>
        static public IList<object> Namespaces { get; private set; }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            // mikehill_ua: Need a new implementation
#if false
            var d = new MessageDialog(e.Exception.Message + "\n" + e.Exception.StackTrace.ToString());
            e.Handled = true;
            var t = d.ShowAsync();
#endif
        }

        static private Frame RootFrame
        {
            get; set;
        }

        internal static void GoToMsdn()
        {
            if (CurrentItem == null)
            {
                return;
            }

            _ = Launcher.LaunchUriAsync(new Uri(MsdnHelper.CalculateWinMDMsdnAddress(CurrentItem)));
        }

        static public List<TypeViewModel> AllTypes;


        [Flags]
        enum KeyModifiers
        {
            None = 0,
            Control = 1,
            Alt = 2,
        }
        KeyModifiers _keyModifiers = KeyModifiers.None;
        //bool _leftAlt = false;
        //bool _rightAlt = false;


        // Copy the MSDN link for the MemberViewModel to the clipboard
        public static void CopyMsdnLink(bool asMarkdown = false)
        {
            if (CurrentItem == null)
            {
                return;
            }
            var msdnAddress = MsdnHelper.CalculateWinMDMsdnAddress(CurrentItem);

            var dataPackage = new DataPackage();
            if (asMarkdown)
            {
                dataPackage.SetText($"[{CurrentItem.MemberPrettyName}]({msdnAddress})");
                Clipboard.SetContent(dataPackage);
            }
            else
            {
                var htmlFragment = $"<a href='{msdnAddress}'>{CurrentItem}</a>";
                string clipboardData = ClipboardHelper3.BuildClipboardData(htmlFragment, null, new Uri(msdnAddress));

                // If this is a generic type, get the angle brackets out before we go to HTML
                var itemName = CurrentItem.MemberPrettyName;
                itemName = itemName.Replace("<", "&lt;");
                itemName = itemName.Replace(">", "&gt;");

                dataPackage.SetHtmlFormat(clipboardData);
                dataPackage.SetText(msdnAddress);
                Clipboard.SetContent(dataPackage);
            }
        }

        // The current member/type being viewed
        public static MemberViewModel CurrentItem { get; internal set; }

        public static void ToggleEventFilter()
        {
            if (Manager.Settings.MemberKind != MemberKind.Event)
                Manager.Settings.MemberKind = MemberKind.Event;
            else
                Manager.Settings.MemberKind = MemberKind.Any;
        }

        public static void ToggleMethodFilter()
        {
            if (Manager.Settings.MemberKind != MemberKind.Method)
                Manager.Settings.MemberKind = MemberKind.Method;
            else
                Manager.Settings.MemberKind = MemberKind.Any;
        }

        public static void TogglePropertyFilter()
        {
            if (Manager.Settings.MemberKind != MemberKind.Property)
                Manager.Settings.MemberKind = MemberKind.Property;
            else
                Manager.Settings.MemberKind = MemberKind.Any;
        }

        public static void ToggleTypeFilter()
        {
            if (Manager.Settings.MemberKind != MemberKind.Type)
                Manager.Settings.MemberKind = MemberKind.Type;
            else
                Manager.Settings.MemberKind = MemberKind.Any;
        }

        internal static void ResetAndGoHome()
        {
            Manager.Settings = new Settings();
            Instance.SearchText = "";
            MoveToMainAndFocusToSearch();
        }

        private static void MoveToMainAndFocusToSearch()
        {
            if (RootFrame.Content is HomePage)
            {
                (RootFrame.Content as HomePage).FocusToSearchString();
            }
            else
            {
                RootFrame.Navigate(typeof(HomePage)); // bugbug: helper method
            }
        }

        private void RootFrame_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Control)
                _keyModifiers |= KeyModifiers.Control;

            // bugbug: Not sure how to track Alt.  When you do Alt-Tab, you see the Alt
            // got down, but not come back up
            //else if (e2.Key == VirtualKey.Menu)
            //    _keyModifiers |= KeyModifiers.Alt;

        }
        private void RootFrame_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            // bugbug: no way to get key modifiers?
            if (e.Key == VirtualKey.Control)
                _keyModifiers &= ~KeyModifiers.Control;
            else if (e.Key == VirtualKey.Menu)
                _keyModifiers &= ~KeyModifiers.Alt;
        }

        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        //private void OnSuspending(object sender, SuspendingEventArgs e)
        //{
        //    var deferral = e.SuspendingOperation.GetDeferral();
        //    //TODO: Save application state and stop any background activity
        //    deferral.Complete();
        //}

        // Used for search results highlighting
        static SearchExpression _searchExpression = null;
        static public SearchExpression SearchExpression
        {
            get { return _searchExpression; }
            set
            {
                if (_searchExpression == value)
                {
                    return;
                }

                _searchExpression = value;
                SearchExpressionChanged?.Invoke(null, null);
            }
        }

        static public event EventHandler<object> SearchExpressionChanged;

        [Conditional("DEBUG")]
        static public void ShowDebugErrorDialog(Exception e)
        {
            var contentDialog = new ContentDialog();
            contentDialog.PrimaryButtonText = "Close";
            contentDialog.Title = "Exception";
            contentDialog.Content = e.Message;
            var t = contentDialog.ShowAsync();
        }

    }
}
