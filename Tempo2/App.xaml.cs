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
using NuGet.Configuration;

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

            // Figure out light/dark mode
            LoadThemeSetting();

            UnhandledException += App_UnhandledException;

            Settings.Changed += (_, e) =>
            {
                // SearchSyntaxName is a function of a Settings property
                if (string.IsNullOrEmpty(e.PropertyName)
                    || e.PropertyName == nameof(Settings.IsWildcardSyntax))
                {
                    RaisePropertyChange(nameof(SearchSyntaxName));
                }
            };
        }

        //private void App_TestEvent2(object sender, EventArgs e)
        //{
        //    throw new NotImplementedException();
        //}

        //private void App_TestEvent1(object sender, EventArgs e)
        //{
        //    throw new NotImplementedException();
        //}

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
                RootFrame = new RootFrame();

                // Place the frame in the current Window
                Window.Content = RootFrame;
            }

            // Force wide mode until skinny mode works again
            //RootFrame.MinWidth = 1024;

            RootFrame.Navigated += (s, e) =>
            {
                // Enable the whole UI now
                RootFrame.IsEnabled = true;
            };

            if (RootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                // TODO Raname this HomePage type in case your app HomePage has a different name
                RootFrame.Navigate(typeof(HomePage), e.Arguments);
            }

            ConfigureSavedSettings();

            Window.Activate();
            WindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(Window);

            FinishLaunch();

        }

        /// <summary>
        /// Restore saved settings and track changes to save updates
        /// </summary>
        void ConfigureSavedSettings()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            // Probably a way to avoid some redundancy here if this grows to many more

            var wildcardKeyName = nameof(Settings.IsWildcardSyntax);
            if (localSettings.Values.Keys.Contains(wildcardKeyName))
            {
                Manager.Settings.IsWildcardSyntax = (bool)localSettings.Values[wildcardKeyName];
            }

            Settings.Changed += (_, e) =>
            {
                if (e.PropertyName == wildcardKeyName)
                {
                    localSettings.Values[wildcardKeyName] = Manager.Settings.IsWildcardSyntax;
                }
            };
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
            // Update: No, it's because the handlers need to mark the args as Handled
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
            };


            // Have to use HandledEventsToo for the Control key monitoring
            RootFrame.AddHandler(Frame.KeyDownEvent, new KeyEventHandler(RootFrame_KeyDown), true);
            RootFrame.AddHandler(Frame.KeyUpEvent, new KeyEventHandler(RootFrame_KeyUp), true);

            SetupGlobalAccelerators();

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




            // Ensure the current window is active
            App.Window.Activate();

            //while (!Debugger.IsAttached)
            //{
            //    Thread.Sleep(TimeSpan.FromSeconds(1));
            //}
        }

        /// <summary>
        /// Hook up some accelerators to the root to ensure that it always works, no matter what page we're on
        /// </summary>
        void SetupGlobalAccelerators()
        {
            // With the keyboard accelerator on the root, the tool tip for it will
            // always appear on every pixel of every page. So shut it off.
            // The normal affordance for it is a hyperlink on the search results page,
            // so it's not a secret. I just can't think of a good place to put it on the home page.
            RootFrame.KeyboardAcceleratorPlacementMode = KeyboardAcceleratorPlacementMode.Hidden;


            // Set up accelerator for opening the debug log
            SetupAccelerator(
                VirtualKey.G,
                VirtualKeyModifiers.Shift | VirtualKeyModifiers.Control,
                () => DebugLogViewer.Show());

            // Accelerators for scaling content up and down
            SetupAccelerator(
                VirtualKey.Number0,
                VirtualKeyModifiers.Control,
                () => ContentScalingPercent = 100);
            SetupAccelerator(
                (VirtualKey)187,
                VirtualKeyModifiers.Control,
                () => ContentScalingPercent = Math.Min(ContentScalingPercent + 10, 200));
            SetupAccelerator(
                (VirtualKey)189,
                VirtualKeyModifiers.Control,
                () => ContentScalingPercent = Math.Max(ContentScalingPercent - 10, 100));

            // Reset is Alt+Home to match Edge, or F3 to match something else that I don't remember what
            SetupAccelerator(
                VirtualKey.Home,
                VirtualKeyModifiers.Menu,
                () => App.ResetAndGoHome());
            SetupAccelerator(
                VirtualKey.F3,
                VirtualKeyModifiers.None,
                () => App.ResetSettings());
            SetupAccelerator(
                VirtualKey.T,
                VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
                () => App.Instance.ToggleTheme());

        }

        /// <summary>
        /// Set up a single root accelerators
        /// </summary>
        void SetupAccelerator(VirtualKey key, VirtualKeyModifiers modifiers, Action action)
        {
            var accel = new KeyboardAccelerator()
            {
                Key = key,
                Modifiers = modifiers
            };
            accel.Invoked += (_, e) =>
            {
                // Mark handled or the invoke will raise twice
                e.Handled = true;

                action();
            };
            RootFrame.KeyboardAccelerators.Add(accel);
        }


        // Process the activation args we get when activated by app redirection
        internal void ProcessActivationArgs(AppActivationArguments args = null)
        {
            if (!ProcessActivationArgsWorker(args))
            {
                ProcessCommandLine();
            }
        }


        /// <summary>
        /// Process the activation arguments (different than command line).
        /// This must be called on the tHread of the args.
        /// Returns true if something was processed.
        /// </summary>
        bool ProcessActivationArgsWorker(AppActivationArguments activationArgs = null)
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

                // Post so that we can finish initialization
                Manager.PostToUIThread(() => App.Instance.GotoSearch(searchTerm));

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


        public string CustomFullPath = null;

        /// <summary>
        /// Check for filenames that should be loaded. This is called by HomePage by which point we have things loaded.
        /// </summary>
        void ProcessCommandLine()
        {
            // Not sure the best way to debug parameters that are passed in on the command line
            // (See AppExecutionAlias in Package.appxmanifest)
            // The solution here is to pass "/waitfordebugger" as a parameter, which will make it
            // sit until you attach a debugger to the process.


            var args = Environment.GetCommandLineArgs();
            string baselineFilename = null;

            // The first arg is the name of the exe
            if (args == null || args.Length == 1)
            {
                return;
            }

            List<string> commandLineFilenames = null;

            // These flags track the processing if the /diff switch
            var waitingForFirstDiff = false;
            var waitingForSecondDiff = false;

            // Process the command line arguments
            // (Skip [0]; it's the exe filename)
            for (int i = 1; i < args.Length; i++)
            {
                var arg = args[i].ToLower();

                // If asked, wait for a debugger to be attached
                if (arg == "/waitfordebugger")
                {
                    while (!Debugger.IsAttached)
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                    }
                    continue;
                }

                if (arg == "/diff")
                {
                    // Next we should see the baseline file
                    // Skip to the next argument
                    waitingForFirstDiff = true;
                    waitingForSecondDiff = false;
                    continue;
                }
                else if (waitingForFirstDiff)
                {
                    // Next we should see the new file
                    // Skip to the next argument
                    baselineFilename = Path.GetFullPath(arg);
                    waitingForSecondDiff = true;
                    waitingForFirstDiff = false;
                    continue;
                }
                else if (waitingForSecondDiff)
                {
                    // We have a good /diff command line, we know the two filenames
                    Manager.Settings.CompareToBaseline = true;

                    // On the initial display of the diff, show a message box offering to copy to the clipboard
                    OfferToCopyResultsToClipboard = true;
                }
                waitingForFirstDiff = waitingForSecondDiff = false;

                if (commandLineFilenames == null)
                {
                    commandLineFilenames = new List<string>();
                }

                // If we get here, we have a custom filename
                // (Maybe also a baseline filename)

                // Figure out the full custom path name
                // Path.GetFullPath() doesn't seem to mind invalid characters coming in,
                // but the docs say it can throw, so show a message box if it does and abort.
                // Bugbug: is this really necessary? Not doing this for the baseline filename,
                // maybe something downstream is handling it.
                try
                {
                    var path = args[i];
                    path = Path.GetFullPath(path);
                    commandLineFilenames.Add(path);
                }
                catch (Exception)
                {
                    _ = MyMessageBox.Show(args[i], "Invalid path", closeButtonText: "OK");
                    return;
                }

            }

            // If /diff was used on the command line, check for syntax errors
            if (waitingForFirstDiff || waitingForSecondDiff)
            {
                _ = MyMessageBox.Show("Usage: Tempo /diff file1 file2\r\nFiles can be dll, winmd, or nupkg",
                                      "Invalid paramters",
                                      closeButtonText: "OK");
            }

            // If we got custom filenames, start opening them
            if (commandLineFilenames != null && commandLineFilenames.Count != 0)
            {
                _initialScopeSet = true;

                DesktopManager2.CustomApiScopeFileNames.Value = commandLineFilenames.ToArray();

                App.StartLoadCustomScope(
                    DesktopManager2.CustomApiScopeFileNames.Value,
                    navigateToSearchResults: true,
                    useWinRTProjections: !App.Instance.UsingCppProjections);

                // Switch to Custom API scope. Don't use the property setter because it will trigger a FilePicker
                _isCustomApiScope = true;
                SaveCurrentScope();

                RaisePropertyChange(nameof(IsCustomApiScope));
            }

            // If this was a /diff command line, start opening the baseline filenames
            if (baselineFilename != null)
            {
                StartLoadBaselineScope(new string[] { baselineFilename });
            }

        }

        // DoSearch checks this, and when true (when using /diff),
        // prompts the user to copy results to the clipboard
        public static bool OfferToCopyResultsToClipboard = false;

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
                SaveCurrentScope();

                if (value)
                {
                    EnsureWinPlatformScopeStarted();
                }

                RaisePropertyChange();
                RaisePropertyChange(nameof(ApiScopeName));
            }
        }


        const string _currentScopeSettingName = "ApiScope";

        /// <summary>
        /// Persist the scope to settings
        /// </summary>
        /// <param name="scope"></param>
        void SaveCurrentScope()
        {
            string scope = nameof(IsWinPlatformScope);

            if (IsWinAppScope)
            {
                scope = nameof(IsWinAppScope);
            }
            else if (IsCustomApiScope)
            {
                scope = nameof(IsCustomApiScope);
            }

            ApplicationDataContainer settings = ApplicationData.Current.RoamingSettings;
            settings.Values[_currentScopeSettingName] = scope;
        }

        /// <summary>
        /// Update the current API scope from settings
        /// </summary>
        /// 
        bool _initialScopeSet = false;
        internal void InitializeToPreviousScopeFromSettings()
        {
            if (_initialScopeSet)
            {
                return;
            }
            _initialScopeSet = true;

            ApplicationDataContainer settings = ApplicationData.Current.RoamingSettings;
            if (settings.Values.TryGetValue(_currentScopeSettingName, out var scope))
            {
                // Windows platform is the default unless something else is set

                switch (scope as string)
                {
                    case "IsWinAppScope":
                        IsWinAppScope = true;
                        break;

                    case "IsCustomApiScope":
                        IsCustomApiScope = true;
                        break;

                    default:
                        IsWinPlatformScope = true;
                        break;
                }
            }
            else
            {
                // This is a first-time startup, default to Windows APIs
                IsWinPlatformScope = true;
            }
        }


        /// <summary>
        /// Reload the current API scope with latest settings.
        /// This is called after the projection mode is changed between c# and c++
        /// </summary>
        internal void ReloadCurrentApiScope()
        {
            if (_isWinPlatformScope)
            {
                EnsureWinPlatformScopeStarted();
            }
            else if (_isWinAppScope)
            {
                EnsureWinAppScopeStarted();
            }
            else
            {
                Debug.Assert(_isCustomApiScope);
                EnsureCustomScopeStarted(
                    DesktopManager2.CustomApiScopeFileNames.Value,
                    navigateToSearchResults: false);
            }
        }

        /// <summary>
        /// Call StartLoadWinPlatformScope if necessary and get Manager.WindowsTypeSet set
        /// </summary>
        internal void EnsureWinPlatformScopeStarted()
        {
            EnsureScopeStarted(
                () => Manager.WindowsTypeSet,
                (typeSet) => Manager.WindowsTypeSet = typeSet,
                (b) => StartLoadWinPlatformScope(b));
        }

        /// <summary>
        /// Helper for callers like EnsureWinPlatformScopeStarted
        /// </summary>
        static private void EnsureScopeStarted(
            Func<TypeSet> getTypeScope,
            Action<TypeSet> setTypeScope,
            Action<bool> startLoad)
        {
            // If we have everything we need, set CurrentTypeSet
            // (Given that it does this, this method could use a better name)
            if (getTypeScope() != null
                && getTypeScope().UsesWinRTProjections == !App.Instance.UsingCppProjections)
            {
                Manager.CurrentTypeSet = getTypeScope();
                return;
            }

            setTypeScope(null);

            // Start loading the APIs. Later when we call Ensure we'll make sure it's done
            startLoad(!App.Instance.UsingCppProjections);
        }




        /// <summary>
        /// Either "Wildcard" or "Regex"
        /// </summary>
        public string SearchSyntaxName
        {
            get
            {
                if (Manager.Settings.IsWildcardSyntax)
                {
                    return "Allow wildcards";
                }
                else
                {
                    return "Allow Regex";
                }
            }
        }


        /// <summary>
        /// Name of the API scope ("Windows", "WinAppSDK", "Custom")
        /// </summary>
        public string ApiScopeName
        {
            get
            {
                if (IsWinPlatformScope)
                {
                    return "Windows APIs";
                }
                else if (IsWinAppScope)
                {
                    return "WASDK APIs";
                }
                else if (IsCustomApiScope)
                {
                    return "Custom APIs";
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
                SaveCurrentScope();

                if (value)
                {
                    EnsureWinAppScopeStarted();
                }

                RaisePropertyChange();
                RaisePropertyChange(nameof(ApiScopeName));
            }
        }

        /// <summary>
        /// Call StartLoadWinAppScope if necessary and get Manager.WindowsAppTypeSet set
        /// </summary>
        internal void EnsureWinAppScopeStarted()
        {
            EnsureScopeStarted(
                    () => Manager.WindowsAppTypeSet,
                    (typeSet) => Manager.WindowsAppTypeSet = typeSet,
                    (b) => StartLoadWinAppScope(b));
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
                SaveCurrentScope();

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
                if (filenames == null
                    || filenames.Length == 0
                    || filenames.Length == 1 && string.IsNullOrEmpty(filenames[0]))
                {
                    // No filenames saved in app settings from last time
                    await PickAndStartLoadCustomApiScopeAsync();
                }
                else
                {
                    EnsureCustomScopeStarted(filenames, navigateToSearchResults: false);
                }
            }

        }

        /// <summary>
        /// Call StartLoadCustomScope if necessary and get Manager.CustomMRTypeSet set
        /// </summary>
        /// <param name="files"></param>
        /// <param name="navigateToSearchResults"></param>
        static internal void EnsureCustomScopeStarted(
            string[] files,
            bool navigateToSearchResults)
        {
            EnsureScopeStarted(
                () => Manager.CustomMRTypeSet,
                (typeSet) => Manager.CustomMRTypeSet = typeSet,
                (b) => StartLoadCustomScope(files,
                                            navigateToSearchResults,
                                            b));
        }

        /// <summary>
        /// Close the custom API scope and load it again
        /// </summary>
        static public void ReloadCustomApiScope()
        {
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
        /// Pick new custom metadata files and add to the current set
        /// </summary>
        async public void PickAndAddCustomApis()
        {
            var newFilenames = await TryPickMetadataFilesAsync();
            if (newFilenames == null)
            {
                return;
            }

            AddCustomApis(newFilenames.ToArray());
        }

        /// <summary>
        /// Add new custom metadata files to the current set
        /// </summary>
        public void AddCustomApis(string[] newFilenames)
        {
            var currentFilenames = DesktopManager2.CustomApiScopeFileNames.Value;

            CloseCustomScope(false);

            DesktopManager2.CustomApiScopeFileNames.Value
                = currentFilenames.Union(newFilenames).ToArray();

            ReloadCustomApiScope();
        }

        /// <summary>
        /// Replace the current custom filenames with a new array, re-loading files
        /// </summary>
        public void ReplaceCustomApis(string[] newFilenames)
        {
            CloseCustomScope(false);

            DesktopManager2.CustomApiScopeFileNames.Value = newFilenames;

            if (IsCustomApiScope)
            {
                if (newFilenames.Length == 0)
                {
                    // Custom APIs are being shown, but we just closed the last one
                    IsWinPlatformScope = true;
                }

                else
                {
                    // Reload the new, smaller list of files
                    ReloadCustomApiScope();
                }
            }
        }



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
            App.EnsureCustomScopeStarted(
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
        void StartLoadWinAppScope(bool useWinRTProjections)
        {
            // Test: delete downloaded, start on Windows, then switch back/forth a bunch rapidly
            if(_winAppScopeLoader != null)
            {
                // Already a load in progress
                return;
            }

            _winAppScopeLoader = new ApiScopeLoader();

            _winAppScopeLoader.StartLoad(
                offThreadLoadAction: () => // Runs *off* UI thread
                {
                    DesktopManager2.LoadWinAppSdkAssembliesSync(WinAppSDKChannel.Stable, useWinRTProjections);
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

                    _ = MyMessageBox.Show(
                            "Unable to load WinAppSDK package\n\nSwitching to Windows Platform APIs",
                            "Load error");

                    // Go back to an API scope we know is there
                    IsWinPlatformScope = true;
                    App.GoHome();
                });

        }

        static ApiScopeLoader _winAppScopeLoader = null;

        internal async static Task<bool> EnsureWinAppScopeLoadedAsync()
        {
            var winAppScopeLoader = _winAppScopeLoader;
            if (winAppScopeLoader == null)
            {
                return true;
            }

            return await winAppScopeLoader.EnsureLoadedAsync("Checking nuget.org for latest WinAppSDK package ...");
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
            SaveCustomFilenamesToSettings();

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
        public static void StartLoadCustomScope(
            string[] filenames,
            bool navigateToSearchResults,
            bool useWinRTProjections)
        {
            CloseCustomScope(goHome: false);

            // Update the filenames now so it doesn't cause a flicker
            DesktopManager2.CustomApiScopeFileNames.Value = filenames;
            SaveCustomFilenamesToSettings();

            var typeSet = new MRTypeSet(MRTypeSet.CustomMRName, !App.Instance.UsingCppProjections);
            _customApiScopeLoader = new ApiScopeLoader();

            _customApiScopeLoader.StartLoad(
                offThreadLoadAction: () =>
                {
                    DesktopManager2.LoadTypeSetMiddleweightReflection(typeSet, filenames, useWinRTProjections);
                },

                uiThreadCompletedAction: async () =>
                {
                    _customApiScopeLoader = null;

                    // Since this is completing what started off thread, the custom API scope might
                    // not be selected anymore (user might have clicked away)

                    if (_isCustomApiScope)
                    {

                        if (typeSet.TypeCount == 0)
                        {
                            await (new ContentDialog()
                            {
                                Content = "No APIs found, switching to Windows Platform APIs",
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

        static ApiScopeLoader _baselineScopeLoader = null;

        /// <summary>
        /// Make IsBaselineScopeLoaded go false
        /// </summary>
        public static void CloseBaselineScope()
        {
            if (_baselineScopeLoader == null)
            {
                return;
            }

            Manager.Settings.CompareToBaseline = false;
            _baselineScopeLoader = null;
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
            DebugLog.Append($"Loading baseline scope {filenames[0]}");

            CloseBaselineScope();

            App.Instance.BaselineFilenames = filenames.ToArray();

            var typeSet = new MRTypeSet("Baseline", !App.Instance.UsingCppProjections);
            _baselineScopeLoader = new ApiScopeLoader();

            _baselineScopeLoader.StartLoad(
                offThreadLoadAction: () =>
                {
                    DesktopManager2.LoadTypeSetMiddleweightReflection(typeSet, filenames, useWinRTProjections: true);
                },

                uiThreadCompletedAction: async () =>
                {
                    _baselineScopeLoader = null;
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
                    _baselineScopeLoader = null;
                });
        }


        /// <summary>
        /// Complete when StartLoadBaselineScope has finished
        /// </summary>
        public async static Task<bool> EnsureBaselineScopeAsync()
        {
            if (_baselineScopeLoader == null)
            {
                // Already finished
                return true;
            }

            // Wait for it to finish
            return await _baselineScopeLoader.EnsureLoadedAsync("Loading baseline ...");
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
        static void StartLoadWinPlatformScope(bool useWinRTProjections)
        {
            if (_winPlatformScopeLoader != null)
            {
                // Already loading
                return;
            }

            _winPlatformScopeLoader = new ApiScopeLoader();

            _winPlatformScopeLoader.StartLoad(
                offThreadLoadAction: () => // Runs *off* UI thread
                {
                    DesktopManager2.LoadWindowsTypesWithMRAsync(
                        useWinRTProjections,
                        (assemblyName) => LocateAssembly(assemblyName));
                },

                uiThreadCompletedAction: () => // Runs on UI thread (if succeeded)
                {
                    _winPlatformScopeLoader = null;

                    // The API scope selection could have changed while we were away on the other thread
                    if (_isWinPlatformScope)
                    {
                        Manager.CurrentTypeSet = Manager.WindowsTypeSet;
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
            var winPlatformScopeLoader = _winPlatformScopeLoader;
            if (winPlatformScopeLoader == null)
            {
                return true;
            }

            return await winPlatformScopeLoader.EnsureLoadedAsync("Loading");
        }



        /// <summary>
        /// Ensure that the current API scope is done loading
        /// </summary>
        /// <returns></returns>
        async public static Task<bool> EnsureApiScopeLoadedAsync()
        {
            // Regardless of what's selected, also ensure the baseline is selected
            if (_baselineScopeLoader != null)
            {
                await _baselineScopeLoader.EnsureLoadedAsync("Loading baseline");
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
            // Disable the UI until ready
            RootFrame.IsEnabled = false;

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
        public void GotoSearch(string text = null)
        {
            if (text == null)
            {
                text = App.Instance.SearchText;
            }

            InternalNavigate(typeof(SearchResults), text);
        }

        double _contentScaling = 1.0;
        public int ContentScalingPercent
        {
            get { return (int)(_contentScaling * 100); }
            set
            {
                _contentScaling = (float)value / 100.0;
                RaisePropertyChange();
                RaisePropertyChange(nameof(ContentScaling));
            }
        }

        public double ContentScaling
        {
            get { return _contentScaling; }
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

        public static void Navigate(MemberOrTypeViewModelBase memberVM)
        {
            InternalNavigate(GetViewTypeFor(memberVM), memberVM);
        }

        /// <summary>
        /// Show a page with types that reference the given type
        /// </summary>
        public static void NavigateToReferencingTypes(TypeViewModel typeVM)
        {
            InternalNavigate(typeof(ReferencingTypes), typeVM);
        }

        public static Type GetViewTypeFor(MemberOrTypeViewModelBase memberVM)
        {
            if (memberVM is TypeViewModel)
            {
                return typeof(TypeDetailView);
            }
            else
            {
                return typeof(MemberDetailView);
            }

        }

        public static MySerializableControl GetViewFor(MemberOrTypeViewModelBase memberVM)
        {
            if (memberVM is TypeViewModel)
            {
                return new TypeDetailView();
            }
            else
            {
                return new MemberDetailView();
            }

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

            _ = Launcher.LaunchUriAsync(new Uri(MsdnHelper.CalculateDocPageAddress(CurrentItem)));
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
            var msdnAddress = MsdnHelper.CalculateDocPageAddress(CurrentItem);

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
        static MemberOrTypeViewModelBase _currentItem = null;
        public static MemberOrTypeViewModelBase CurrentItem
        {
            get { return _currentItem; }
            internal set
            {
                _currentItem = value;

                // The DocUrl is calculated from the CurrentItem
                Instance.RaisePropertyChange(nameof(CurrentItemDocUrl));
            }
        }

        /// <summary>
        /// URL of the document page for the current item
        /// </summary>
        public Uri CurrentItemDocUrl
        {
            get
            {
                var address = MsdnHelper.CalculateDocPageAddress(CurrentItem);
                if (string.IsNullOrEmpty(address))
                {
                    return null;
                }
                else
                {
                    return new Uri(address);
                }
            }
        }

        public static void ToggleEventFilter()
        {
            if (Manager.Settings.MemberKind != MemberKind.Event)
                Manager.Settings.MemberKind = MemberKind.Event;
            else
                Manager.Settings.MemberKind = MemberKind.Any;
        }

        public static void ToggleFieldFilter()
        {
            if (Manager.Settings.MemberKind != MemberKind.Field)
                Manager.Settings.MemberKind = MemberKind.Field;
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

        public static void ToggleConstructorFilter()
        {
            if (Manager.Settings.MemberKind != MemberKind.Constructor)
                Manager.Settings.MemberKind = MemberKind.Constructor;
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

        public static void ToggleCaseSensitive()
        {
            Manager.Settings.CaseSensitive = !Manager.Settings.CaseSensitive;
        }

        public static void ToggleInternals()
        {
            Manager.Settings.InternalInterfaces = !Manager.Settings.InternalInterfaces;
        }

        /// <summary>
        /// True if we're in the process of navigating to Home
        /// </summary>
        static public bool HeadedHome = false;
        internal static void ResetAndGoHome()
        {
            // This tells us, when Settings changeds below, that we don't need
            // to respond to it with a new search
            HeadedHome = true;

            try
            {
                ResetSettings();
            }
            finally
            {
                HeadedHome = false;
            }

            Instance.SearchText = "";
            MoveToMainAndFocusToSearch();
        }

        static internal void ResetSettings()
        {
            Manager.ResetSettings();
            //var oldSettings = Manager.Settings;
            //Manager.Settings = new Settings();
            //Manager.Settings.TransferEvents(oldSettings);
        }

        bool? _usingCppProjections = null;

        /// <summary>
        /// Indicates that we're projecting to C++ (e.g. IVector) rather than C# (e.g. IList)
        /// </summary>
        public bool UsingCppProjections
        {
            get
            {
                if (_usingCppProjections == null)
                {
                    _usingCppProjections = false;

                    ApplicationDataContainer settings = ApplicationData.Current.RoamingSettings;
                    if (settings.Values.TryGetValue(nameof(UsingCppProjections), out var value))
                    {
                        _usingCppProjections = (bool)value;
                    }

                }
                return _usingCppProjections == true;
            }
            set
            {
                _usingCppProjections = value;

                ApplicationDataContainer settings = ApplicationData.Current.RoamingSettings;
                settings.Values[nameof(UsingCppProjections)] = value;

                RaisePropertyChange();
            }
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

        // Used for search results highlighting.
        // Globals are bad, but it's nice to be able to drop in a SearchHighlighter at any point and have it just work.
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

        /// <summary>
        /// Show help in a separate window
        /// </summary>
        internal void ShowHelp()
        {
            var helpPage = new HelpPage();

            // Use MainWindow because it has the Mica support
            var window = new MainWindow();
            helpPage.Loaded += (_, __) => window.SetMicaBackdrop();

            window.Content = helpPage;
            window.Title = "Tempo Help";

            window.Activate();
        }

        public bool IsLightTheme
        {
            get { return _isLightTheme; }

            set
            {
                if (_isLightTheme != value)
                {
                    _isLightTheme = value;

                    if (value)
                    {
                        // Need to maintain mutual exclusion manually or the radio buttons get confused
                        IsDarkTheme = IsSystemTheme = false;
                        SaveThemeSetting();
                    }

                    RaisePropertyChange();
                }
            }
        }
        bool _isLightTheme = false;

        public bool IsDarkTheme
        {
            get { return _isDarkTheme; }
            set
            {
                if (_isDarkTheme != value)
                {
                    _isDarkTheme = value;

                    if (value)
                    {
                        // Need to maintain mutual exclusion manually or the radio buttons get confused
                        IsLightTheme = IsSystemTheme = false;
                        SaveThemeSetting();
                    }

                    RaisePropertyChange();
                }
            }
        }
        bool _isDarkTheme = false;

        public bool IsSystemTheme
        {
            get { return _isSystemTheme; }
            set
            {
                if (_isSystemTheme != value)
                {
                    _isSystemTheme = value;

                    if (value)
                    {
                        // Need to maintain mutual exclusion manually or the radio buttons get confused
                        IsLightTheme = IsDarkTheme = false;
                        SaveThemeSetting();
                    }

                    RaisePropertyChange();

                }
            }
        }
        bool _isSystemTheme = false;

        /// <summary>
        /// Save the light/dark theme setting
        /// </summary>
        void SaveThemeSetting([CallerMemberName] string theme = null)
        {
            try
            {
                // bugbug:
                // This call to the Values vector is frequently AV'ing, only in a Release build,
                // at this call stack. And only this use of ADC.
                // Repro is to just launch the app a bunch of times
                //
                //  System.Private.CoreLib.dll!System.Delegate.DynamicInvokeImpl(object[] args) Line 92 C#
                // 	System.Private.CoreLib.dll!System.Delegate.DynamicInvoke(object[] args) Line 61 C#
                // 	WinRT.Runtime.dll!WinRT.DelegateExtensions.DynamicInvokeAbi(System.Delegate del, object[] invoke_params)    Unknown
                //  WinRT.Runtime.dll!ABI.Windows.Foundation.Collections.IMapMethods<string, object>.Insert(WinRT.IObjectReference obj, string key, object value)   Unknown
                //  WinRT.Runtime.dll!ABI.System.Collections.Generic.IDictionaryMethods<string, object>.Indexer_Set(WinRT.IObjectReference obj, string key, object value)   Unknown
                //  Microsoft.Windows.SDK.NET.dll!Windows.Storage.ApplicationDataContainerSettings.this[string].set(string key, object value)   Unknown
                //  TempoUwp.dll!Tempo.App.SaveThemeSetting(string theme) Line 2149 C#

                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values[_themeSettingName] = theme;
            }
            catch (Exception)
            { }

            RaisePropertyChange(nameof(ElementTheme));
        }
        static string _themeSettingName = "ThemeSetting3";

        /// <summary>
        /// Load the light/dark theme setting
        /// </summary>
        void LoadThemeSetting()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.TryGetValue(_themeSettingName, out var theme))
            {
                switch (theme)
                {
                    case nameof(IsLightTheme):
                        IsLightTheme = true;
                        break;

                    case nameof(IsDarkTheme):
                        IsDarkTheme = true;
                        break;

                    default:
                        IsSystemTheme = true;
                        break;
                }
            }
            else
            {
                IsSystemTheme = true;
            }
        }

        /// <summary>
        /// Element theme (light/dark)
        /// </summary>
        public ElementTheme ElementTheme
        {
            get
            {
                if (IsLightTheme)
                    return ElementTheme.Light;
                else if (IsDarkTheme)
                    return ElementTheme.Dark;
                else
                    return ElementTheme.Default;
            }
        }

        /// <summary>
        /// Cycle theme through default/light/dark with repeated calls
        /// </summary>
        public void ToggleTheme()
        {
            if (IsSystemTheme)
            {
                IsLightTheme = true;
            }
            else if (IsLightTheme)
            {
                IsDarkTheme = true;
            }
            else
            {
                IsSystemTheme = true;
            }
        }
    }
}

