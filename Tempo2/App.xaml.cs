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
using Windows.UI.Core;
using Microsoft.UI.Input;
using NuGet.Configuration;
using System.Web;
using Windows.Foundation.Collections;
using System.Runtime.InteropServices;

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

            // Initialize all the scope loaders. This doesn't cause anything to load yet
            // (Must be done after App.Instance has been set)
            _winPlatformScopeLoader = new WinPlatScopeLoader();
            _winAppScopeLoader = new WinAppScopeLoader();
            _webView2ScopeLoader = new WebView2ScopeLoader();
            _win32ScopeLoader = new Win32ScopeLoader();
            _dotNetScopeLoader = new DotNetScopeLoader();
            _dotNetWindowsScopeLoader = new DotNetWindowsScopeLoader();
            CustomApiScopeLoader = new CustomScopeLoader();
            BaselineApiScopeLoader = new BaselineScopeLoader();


            // mikehill_ua
            //this.Suspending += OnSuspending;

            // Put "Tempo" in the name of the local path for storing all files so that SafeDelete() will be happy
            var localFolderPath = Path.Combine(ApplicationData.Current.LocalCacheFolder.Path, "Tempo");
            DesktopManager2.Initialize(false, localFolderPath);

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
        internal void RaisePropertyChange([CallerMemberName] string name = "")
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

            // Optionally support single instancing
            HandleSingleInstancing();

            // Initialize MainWindow here
            Window = new MainWindow();
            Window.Title = "Tempo API Viewer";

            RootFrame = Window.Content as RootFrame;
            if (RootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                RootFrame = new RootFrame();

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

            RootFrame.Navigated += (s, e) =>
            {
                // During startup, when we hook up this event handler, we've already
                // navigated to the home page. We might disable it during startup until
                // we've navigated to the search page. In that case, re-enable here.
                // It's over-kill to enable on every navigate, but this is ensurance against a bug.
                RootFrame.IsEnabled = true;

                // bugbug: can't set Set cursor to work
                //RootFrame.ClearCursor();
            };

            ConfigureSavedSettings();

            Window.Activate();
            WindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(Window);

            FinishLaunch();

        }


        /// <summary>
        /// Register for or redirect to single instance, if requested.
        /// </summary>
        private void HandleSingleInstancing()
        {
            if (!ShouldAllowSingleInstance())
            {
                return;
            }

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
        }

        static string _singleInstanceCommandLineArgument = "/singleinstance";

        /// <summary>
        /// Check if single-instancing is requested (command-line argument)
        /// </summary>
        /// <returns></returns>
        bool ShouldAllowSingleInstance()
        {
            // There's a ProcessCommandLine method that runs later (and must be later)
            // that processes the rest of the arguments. We need to check this earlier
            // than that though

            var args = Environment.GetCommandLineArgs();

            // The first arg is the name of the exe
            if (args == null || args.Length == 1)
            {
                return false;
            }

            for (int i = 1; i < args.Length; i++)
            {
                var arg = args[i].ToLower();
                if (arg == _singleInstanceCommandLineArgument)
                {
                    return true;
                }
            }

            return false;
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

            RootFrame = App.Window.Content as RootFrame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (RootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                RootFrame = new RootFrame();

            }
            // Too jarring
            //RootFrame.ContentTransitions = new TransitionCollection() { new ContentThemeTransition() };

            //// Bugbug: why are these accelerators doing key input rather than KeyboardAccelerator?
            //// Is it because accelerators cause too many tooltips?
            //// Update: No, it's because the handlers need to mark the args as Handled
            //RootFrame.KeyDown += (s, e2) =>
            //{
            //    ProcessRootKeyDown(e2);
            //};



            //// Have to use HandledEventsToo for the Control key monitoring
            //RootFrame.AddHandler(Frame.KeyDownEvent, new KeyEventHandler(RootFrame_KeyDown), true);
            //RootFrame.AddHandler(Frame.KeyUpEvent, new KeyEventHandler(RootFrame_KeyUp), true);

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
        /// Move to the nearest search box (search page or Home)
        /// </summary>
        void MoveToSearchBox()
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


        /// <summary>
        /// Get the keyboard modifiers (control/shift/alt) for the current state of the message pump
        /// </summary>
        KeyModifiers GetKeyModifiersForThread()
        {
            var modifiers = KeyModifiers.None;
            var downState = CoreVirtualKeyStates.Down;

            if ((InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu) & downState) == downState)
            {
                modifiers |= KeyModifiers.Alt;
            }

            if ((InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control) & downState) == downState)
            {
                modifiers |= KeyModifiers.Control;
            }

            if ((InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift) & downState) == downState)
            {
                modifiers |= KeyModifiers.Shift;
            }

            // Windows key shortcuts don't reliably work because sometimes they get intercepted by the system.
            // (E.g. Windows+Back)
            //if((InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.LeftWindows) & downState) == downState)
            //{
            //    modifiers |= KeyModifiers.Windows;
            //}

            return modifiers;
        }




        /// <summary>
        /// Hook up some accelerators to the root to ensure that it always works, no matter what page we're on
        /// </summary>
        void SetupGlobalAccelerators()
        {
            // If the left mouse button is pressed (and nothing else), navigate back
            RootFrame.PointerPressed += (s, e2) =>
            {
                var pointerProperties = e2.GetCurrentPoint(s as UIElement).Properties;
                if (pointerProperties.IsXButton1Pressed
                    && !pointerProperties.IsXButton2Pressed
                    && GetKeyModifiersForThread() == KeyModifiers.None)
                {
                    App.GoBack();
                }
            };

            // Back navigation on GoBack or Alt+Left
            // GoBack is raised by the old multimedia keyboard? PTP gesture?
            SetupAccelerator(
                VirtualKey.GoBack,
                VirtualKeyModifiers.None,
                () => App.GoBack());
            SetupAccelerator(
                VirtualKey.Left,
                VirtualKeyModifiers.Menu,
                () => App.GoBack());

            // With the keyboard accelerators on the root, the tool tips will
            // always appear on every pixel of every page. So shut them off.
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

            // F3 resets settings.
            // F3 used to be a common pattern, but I don't remember where I got it from.
            SetupAccelerator(
                VirtualKey.F3,
                VirtualKeyModifiers.None,
                () => App.ResetSettings());

            // Control+T to toggle light/dark mode
            SetupAccelerator(
                VirtualKey.T,
                VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
                () => App.Instance.ToggleTheme());

            // Control+E moves to the search text box
            SetupAccelerator(
                VirtualKey.E,
                VirtualKeyModifiers.Control,
                () => MoveToSearchBox());

            // Control+Shift+P launches PowerShell
            SetupAccelerator(
                VirtualKey.P,
                VirtualKeyModifiers.Shift | VirtualKeyModifiers.Control,
                () =>
                {
                    if (App.HomePage != null && App.HomePage.XamlRoot != null)
                    {
                        PSLauncher.GoToPS(App.HomePage.XamlRoot);
                    }
                });

            // Reset is Alt+Home to match Edge
            // A key accelerator doesn't work though if keyboard focus is in the ListView because of this issue:
            // https://github.com/microsoft/microsoft-ui-xaml/issues/9885
            // As a workaround, intercept Alt+Home before it goes to the ListView

            //SetupAccelerator(
            //    VirtualKey.Home,
            //    VirtualKeyModifiers.Menu,
            //    () => App.ResetAndGoHome());

            RootFrame.PreviewKeyDown += (s, e2) =>
            {
                if (e2.Key != VirtualKey.Home)
                {
                    return;
                }

                if (GetKeyModifiersForThread() == KeyModifiers.Alt)
                {
                    App.ResetAndGoHome();
                    e2.Handled = true;
                }
            };

        }

        /// <summary>
        /// Set up a single root accelerator
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

                var uri = (activationArgs.Data as IProtocolActivatedEventArgs).Uri;
                DebugLog.Append($"protocol launch: {uri}");

                // Say the launch is "tempo:Button?scope=winappsdk"
                // AbsolutePath is "Button"
                var searchTerm = uri.AbsolutePath;

                // Query is "?scope=winappsdk" in that example
                var query = uri.Query;
                if (!string.IsNullOrEmpty(query))
                {
                    var queryParams = HttpUtility.ParseQueryString(query);
                    var scope = queryParams.Get("scope");

                    // In the search results, what should be selected
                    InitialSelection = queryParams.Get("selection");

                    if (scope != null)
                    {
                        // Set the ApiScope to the requested value. We'll set _initialScopeSet
                        // so that we don't overwrite this with what we saved on the last run

                        switch (scope.ToLower())
                        {
                            case "winappsdk":
                                IsWinAppScope = true;
                                _initialScopeSet = true;
                                break;

                            case "custom":
                                IsCustomApiScope = true;
                                _initialScopeSet = true;
                                break;

                            case "windows":
                                IsWinPlatformScope = true;
                                _initialScopeSet = true;
                                break;

                            case "win32":
                                IsWin32Scope = true;
                                _initialScopeSet = true;
                                break;

                            default:
                                // We haven't set _initialScopeSet,
                                // so later we'll restore to whatever scope was used the last time
                                DebugLog.Append($"Unknown scope: {scope}");
                                break;

                        }
                    }


                    // Copy in the Settings if we got them in the arguments
                    var settings = queryParams.Get("settings");
                    if (settings != null)
                    {
                        Manager.Settings = Settings.FromJson(settings);
                    }

                }


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
        /// In the first navigation to SearchResults, make this the selected item
        /// </summary>
        static internal string InitialSelection = null;

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
                if(arg.Length == 0)
                {
                    // Don't think this is possible, but don't crash
                    continue;
                }

                // Allow -foo or /foo interchangeably
                if (arg[0] == '-')
                {
                    arg = $"/" + arg.Substring(1);
                }

                // If asked, wait for a debugger to be attached
                if (arg == "/waitfordebugger")
                {
                    while (!Debugger.IsAttached)
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                    }
                    continue;
                }

                if (arg == _singleInstanceCommandLineArgument)
                {
                    // This is special-cased in the method ShouldAllowSingleInstance()
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
                    // We have /diff and the baseline file
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

                    // (bugbug) When launched with /diff, it hangs on the HomePage for a bit while loading.
                    // It should be going straight to the loading page and showing a dialog.
                    // Work around this for now by at least disabling the HomePage
                    var homePage = HomePage.Instance;
                    if (homePage != null)
                    {
                        homePage.IsEnabled = false;
                    }
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
                _ = MyMessageBox.Show("Usage: Tempo /diff file1 file2\r\nFiles can be dll, winmd, nupkg, or directory",
                                      "Invalid paramters",
                                      closeButtonText: "OK");
            }

            // If we got custom filenames, start opening them
            if (commandLineFilenames != null && commandLineFilenames.Count != 0)
            {
                // commandLineFilenames is a list of file or directory names
                // Replace the directory names with the names of all the children files
                commandLineFilenames = ExpandDirectories(commandLineFilenames);

                // Set this so that we don't change the scope to whatever was used the last time
                _initialScopeSet = true;

                DesktopManager2.CustomApiScopeFileNames.Value = commandLineFilenames.ToArray();

                CustomApiScopeLoader.StartMakeCurrent(navigateToSearchResults: true);

                // Switch to Custom API scope. Don't use the property setter because it will trigger a FilePicker
                _isCustomApiScope = true;
                SaveCurrentScope();

                RaisePropertyChange(nameof(IsCustomApiScope));
            }

            // If this was a /diff command line, start opening the baseline filenames
            if (baselineFilename != null)
            {
                // baselineFilename is a single file or directory name
                // Replace the directory name with the names of all the children files
                var baselineFilenames = ExpandDirectories(new string[] { baselineFilename });
                BaselineApiScopeLoader.StartMakeCurrent(baselineFilenames.ToArray());

                // This has to be called after the StartMakeCurrent,
                // because Start clears it (bugbug)
                Manager.Settings.CompareToBaseline = true;
            }

        }

        /// <summary>
        /// For each provided path, if it's a file add it to the return list,
        /// if it's a directory then add its children files to the return list
        /// </summary>
        private List<string> ExpandDirectories(IEnumerable<string> paths)
        {
            var newList = new List<string>();

            foreach (var path in paths)
            {
                if(!Directory.Exists(path))
                {
                    newList.Add(path);
                    continue;
                }

                var files = Directory.EnumerateFiles(path);
                newList.AddRange(files);
            }

            return newList;
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
        static public HomePage HomePage;

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
                    // bugbug: optimize this so that if it's already started, don't start again?
                    _winPlatformScopeLoader.StartMakeCurrent();
                }

                RaisePropertyChange();
                RaisePropertyChange(nameof(ApiScopeName));
            }
        }

        // AppDataContainer setting names
        const string _currentScopeSettingName = "ApiScope";
        const string _winAppSdkChannelSettingName = "WinAppSdkChannel";

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
            else if (IsWebView2Scope)
            {
                scope = nameof(IsWebView2Scope);
            }
            else if (IsDotNetScope)
            {
                scope = nameof(IsDotNetScope);
            }
            else if (IsDotNetWindowsScope)
            {
                scope = nameof(IsDotNetWindowsScope);
            }
            else if (IsCustomApiScope)
            {
                scope = nameof(IsCustomApiScope);
            }
            else if (IsWin32Scope)
            {
                scope = nameof(IsWin32Scope);
            }
            else
            {
                Debug.Assert(scope == nameof(IsWinPlatformScope));
            }

            ApplicationDataContainer settings = ApplicationData.Current.RoamingSettings;
            settings.Values[_currentScopeSettingName] = scope;

            RaisePropertyChange(nameof(ApiScopeName));

            // Save the WinAppSDK channel name (preview/experimental/stable)
            settings.Values[_winAppSdkChannelSettingName] = WinAppSDKChannel.ToString();
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

            // Need to read the WASDK channel name before setting the scope
            if (settings.Values.TryGetValue(_winAppSdkChannelSettingName, out var channelSetting))
            {
                if (Enum.TryParse(typeof(WinAppSDKChannel), channelSetting as string, out var channel))
                {
                    _winAppSDKChannel = (WinAppSDKChannel)channel;
                    RaisePropertyChange(nameof(WinAppSDKChannel));
                }
            }

            if (settings.Values.TryGetValue(_currentScopeSettingName, out var scope))
            {
                // Windows platform is the default unless something else is set

                switch (scope as string)
                {
                    case "IsWinAppScope":
                        IsWinAppScope = true;
                        break;

                    case "IsCustomApiScope":
                        if (CustomApiScopeLoader.HasFile)
                        {
                            IsCustomApiScope = true;
                        }
                        else
                        {
                            IsWinPlatformScope = true;
                        }
                        break;

                    case "IsWebView2Scope":
                        IsWebView2Scope = true;
                        break;

                    case "IsDotNetScope":
                        IsDotNetScope = true;
                        break;

                    case "IsDotNetWindowsScope":
                        IsDotNetWindowsScope = true;
                        break;

                    case "IsWin32Scope":
                        IsWin32Scope = true;
                        break;

                    default:
                        Debug.Assert((scope as string) == nameof(IsWinPlatformScope));
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
                _winPlatformScopeLoader.StartMakeCurrent();
            }
            else if (_isWinAppScope)
            {
                _winAppScopeLoader.StartMakeCurrent();
            }
            else if (_isWin32Scope)
            {
                _win32ScopeLoader.StartMakeCurrent();
            }
            else
            {
                CustomApiScopeLoader.StartMakeCurrent();
            }

            // Also, if the baseline is loaded, reload it too
            if (Manager.Settings.CompareToBaseline == true)
            {
                BaselineApiScopeLoader.StartMakeCurrent(App.Instance.BaselineFilenames);
            }
        }

        static WinAppScopeLoader _winAppScopeLoader = null;

        /// <summary>
        /// Call StartLoadWinPlatformScope if necessary and get Manager.WindowsTypeSet set
        /// </summary>

        /// <summary>
        /// Helper for callers like EnsureWinPlatformScopeStarted
        /// </summary>






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
        /// Name of the API scope ("Windows", "WinAppSDK", "Win32", "Custom")
        /// </summary>
        public string ApiScopeName
        {
            get
            {
                // bugbug: redo this

                if (IsWinPlatformScope)
                {
                    return "Windows APIs";
                }
                else if (IsWinAppScope)
                {
                    return "WindowsAppSdk APIs";
                }
                else if (IsCustomApiScope)
                {
                    return "Custom APIs";
                }
                else if (IsWin32Scope)
                {
                    return "Win32 APIs";
                }
                else if (IsWebView2Scope)
                {
                    return "WebView2 APIs";
                }
                else if(IsDotNetScope)
                {
                    return "DotNet Runtime APIs";
                }
                else if(IsDotNetWindowsScope)
                {
                    return "DotNet Windows Desktop APIs";
                }

                // can get here if everything's false temporarily during a transition
                return "";
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
                    _winAppScopeLoader.StartMakeCurrent();
                }

                RaisePropertyChange();
                RaisePropertyChange(nameof(ApiScopeName));
            }
        }


        // IsWebView2Scope means show the WebView2 APIs
        public bool IsWebView2Scope
        {
            get { return _isWebView2Scope; }
            set
            {
                if (_isWebView2Scope == value)
                {
                    return;
                }

                _isWebView2Scope = value;
                SaveCurrentScope();

                if (value)
                {
                    _webView2ScopeLoader.StartMakeCurrent();
                }

                RaisePropertyChange();
                RaisePropertyChange(nameof(ApiScopeName));
            }
        }
        static bool _isWebView2Scope = false;
        static WebView2ScopeLoader _webView2ScopeLoader = null;


        // IsWin32Scope means show the Win32 metadata project
        static bool _isWin32Scope = false;
        public bool IsWin32Scope
        {
            get { return _isWin32Scope; }
            set
            {
                if (_isWin32Scope == value)
                {
                    return;
                }

                _isWin32Scope = value;
                SaveCurrentScope();

                if (value)
                {
                    _win32ScopeLoader.StartMakeCurrent();
                }

                RaisePropertyChange();
                RaisePropertyChange(nameof(ApiScopeName));
            }
        }
        static Win32ScopeLoader _win32ScopeLoader = null;


        public bool IsDotNetScope
        {
            get { return _isDotNetScope; }
            set
            {
                if (_isDotNetScope == value)
                {
                    return;
                }

                _isDotNetScope = value;
                SaveCurrentScope();

                if (value)
                {
                    _dotNetScopeLoader.StartMakeCurrent();
                }

                RaisePropertyChange();
                RaisePropertyChange(nameof(ApiScopeName));
            }
        }
        static DotNetScopeLoader _dotNetScopeLoader = null;
        static bool _isDotNetScope = false;



        public bool IsDotNetWindowsScope
        {
            get { return _isDotNetWindowsScope; }
            set
            {
                if (_isDotNetWindowsScope == value)
                {
                    return;
                }

                _isDotNetWindowsScope = value;
                SaveCurrentScope();

                if (value)
                {
                    _dotNetWindowsScopeLoader.StartMakeCurrent();
                }

                RaisePropertyChange();
                RaisePropertyChange(nameof(ApiScopeName));
            }
        }
        static DotNetWindowsScopeLoader _dotNetWindowsScopeLoader = null;
        static bool _isDotNetWindowsScope = false;

        // bugbug: move these to api scope loaders
        internal static string DotNetCorePath;
        internal string DotNetCoreVersion;
        internal bool IsDotNetCoreEnabled => DotNetCorePath != null;

        internal static string DotNetWindowsPath;
        internal bool IsDotNetWindowsEnabled => DotNetWindowsPath != null;



        /// <summary>
        /// Ensure the Win32 Metadata is loaded or loading
        /// </summary>



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
                    CustomApiScopeLoader.EnsurePickedAndStartMakeCurrent();
                }

                RaisePropertyChange();
                RaisePropertyChange(nameof(ApiScopeName));
            }
        }

        /// <summary>
        /// Figure out what custom APIs to load and start loading them, or select them if already loaded
        /// </summary>
        /// <returns></returns>


        /// <summary>
        /// Call StartLoadCustomScope if necessary and get Manager.CustomMRTypeSet set
        /// </summary>
        /// <param name="files"></param>
        /// <param name="navigateToSearchResults"></param>

        /// <summary>
        /// Close the custom API scope and load it again
        /// </summary>
        static public void ReloadCustomApiScope()
        {
            // Load custom APIs and make current scope selection
            if (App.Instance.IsCustomApiScope)
            {
                // Already selected, pick and load
                CustomApiScopeLoader.StartMakeCurrent();
            }
            else
            {
                // Select, which will trigger a pick and load
                App.Instance.IsCustomApiScope = true;
            }
        }



        /// <summary>
        /// Pick new custom metadata files and add to the current set
        /// </summary>






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


        static bool _isCustomApiScope = false;
        internal static CustomScopeLoader CustomApiScopeLoader = null;

        /// <summary>
        /// Close the custom API scope, clear the settings, and go to the Windows APIs
        /// </summary>
        /// <param name="goHome"></param>
        public static void CloseCustomScope(bool goHome)
        {
            var isCurrent = Manager.CustomMRTypeSet != null && Manager.CustomMRTypeSet.IsCurrent;
            Manager.CustomMRTypeSet = Manager.CustomTypeSet = null;

            // Bugbug: workaround for x:Bind ignoring the two-way binding when this value is null

            App.CustomApiScopeLoader.Close();

            if (isCurrent && goHome)
            {
                App.Instance.IsWinPlatformScope = true;
                App.GoHome();
            }
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

        /// <summary>
        /// Make IsBaselineScopeLoaded go false
        /// </summary>


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

        internal static BaselineScopeLoader BaselineApiScopeLoader = null;

        /// <summary>
        /// Complete when StartLoadBaselineScope has finished
        /// </summary>
        public async static Task<bool> EnsureBaselineScopeAsync()
        {
            if (BaselineApiScopeLoader == null)
            {
                // Already finished
                return true;
            }

            // Wait for it to finish
            return await BaselineApiScopeLoader.EnsureLoadedAsync();
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
        static internal void SaveCustomFilenamesToSettings()
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
            // There's a size limit on these values and it can throw
            try
            {
                //localSettings.Values[_customFilenamesKey] = setting;
                WriteSetting(localSettings.Values, _customFilenamesKey, setting);
            }
            catch (Exception ex)
            {
                DebugLog.Append($"Failed to save custom filenames setting: {ex.Message}");
            }
        }

        /// <summary>
        /// Write a value to an IPropertySet, which is an ApplicationDataContainer storage.
        /// This works around the size limitation to ADC by spreading over multiple values
        /// </summary>
        static void WriteSetting(IPropertySet properties, string key, string setting)
        {
            // Clear out any old settings
            for (int i = 0; i < _maxSettingsValues; i++)
            {
                string keyT;
                if (i == 0)
                    keyT = key;
                else
                    keyT = key + i.ToString();

                properties.Remove(keyT);
            }

            // Write the setting to as many values as is necessary
            // (Write 8000 bytes at a time, the limit is 8192?)
            for (int i = 0; i < _maxSettingsValues; i++)
            {
                // The first key is just whatever was input. Then it's "foo1", "foo2", ...
                string keyT;
                if (i == 0)
                    keyT = key;
                else
                    keyT = key + i.ToString();

                if (setting.Length < _maxSettingsValueSize)
                {
                    // What's left to write is small enough, write it and we're done
                    properties[keyT] = setting;
                    return;
                }

                // Write the first chunk of bytes, remove it from the bytes to write, then move on
                properties[keyT] = setting.Substring(0, _maxSettingsValueSize);
                setting = setting.Substring(_maxSettingsValueSize);
            }

            throw new Exception("Setting too long");

        }

        /// <summary>
        /// Load CustomApiScopeFileNames from ApplicationData settings
        /// </summary>
        void LoadCustomFilenamesFromSettings()
        {
            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            var sb = new StringBuilder();
            var key = _customFilenamesKey;

            // Read the value, which may be spread over multiple keys due to ADC value size limit
            for (int i = 0; i < _maxSettingsValues; i++)
            {
                // First value is "foo", then "foo1", "foo2", etc.
                string keyT;
                if (i == 0)
                    keyT = key;
                else
                    keyT = key + i.ToString();

                var value = localSettings.Values[keyT] as string;
                if (string.IsNullOrEmpty(value))
                {
                    // We've run out of values to read. Take what's been accumlated and use that
                    var string2 = sb.ToString();
                    if (string.IsNullOrEmpty(string2))
                    {
                        return;
                    }

                    var filenames = sb.ToString().Split(';').ToArray();
                    DesktopManager2.CustomApiScopeFileNames.Value = filenames;
                    return;
                }

                sb.Append(value);
            }
        }

        const int _maxSettingsValues = 10;
        const int _maxSettingsValueSize = 4000;

        static ApiScopeLoader _winPlatformScopeLoader = null;


        /// <summary>
        /// Ensure that the current API scope is done loading.
        /// Returns false if canceled
        /// </summary>
        /// <returns></returns>
        async public static Task<bool> EnsureApiScopeLoadedAsync()
        {
            // Regardless of what's selected, also ensure the baseline is selected
            if (BaselineApiScopeLoader.IsLoadingOrLoaded)
            {
                await BaselineApiScopeLoader.EnsureLoadedAsync();
            }

            if (_isWinAppScope)
            {
                // Returns false if the user cancels the load
                return await _winAppScopeLoader.EnsureLoadedAsync();
            }
            else if (_isWebView2Scope)
            {
                // Returns false if the user cancels the load
                return await _webView2ScopeLoader.EnsureLoadedAsync();
            }
            else if (_isCustomApiScope)
            {
                return await CustomApiScopeLoader.EnsureLoadedAsync();
            }
            else if (_isWin32Scope)
            {
                return await _win32ScopeLoader.EnsureLoadedAsync();
            }
            else if (_isDotNetScope)
            {
                return await _dotNetScopeLoader.EnsureLoadedAsync();
            }
            else if (_isDotNetWindowsScope)
            {
                return await _dotNetWindowsScopeLoader.EnsureLoadedAsync();
            }
            else
            {
                Debug.Assert(_isWinPlatformScope);
                return await _winPlatformScopeLoader.EnsureLoadedAsync();
            }
        }






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

        static private RootFrame RootFrame
        {
            get; set;
        }

        static internal void EnableRoot()
        {
            RootFrame.IsEnabled = true;
        }

        internal static void GoToMsdn()
        {
            if (CurrentItem == null)
            {
                return;
            }

            var address = MsdnHelper.CalculateDocPageAddress(CurrentItem);
            if (string.IsNullOrEmpty(address))
            {
                DebugLog.Append($"No MSDN address for {CurrentItem}");
                return;
            }

            // Defense in depth: don't crash the app if we come up with a bad URI
            try
            {
                DebugLog.Append($"Launching {address}");
                _ = Launcher.LaunchUriAsync(new Uri(address));
            }
            catch (Exception ex)
            {
                DebugLog.Append($"Failed to launch {address}: {ex.Message}");
            }
        }

        static public List<TypeViewModel> AllTypes;


        [Flags]
        enum KeyModifiers
        {
            None = 0,
            Control = 1,
            Alt = 2,
            Shift = 4,

            // Not reliable
            //Windows = 8
        }
        //KeyModifiers _keyModifiers = KeyModifiers.None;
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

                    DebugLog.Append($"{(_usingCppProjections == true ? "C++" : "C#")} projections");

                }
                return _usingCppProjections == true;
            }
            set
            {
                _usingCppProjections = value;

                DebugLog.Append($"{(_usingCppProjections == true ? "C++" : "C#")} projections");

                ApplicationDataContainer settings = ApplicationData.Current.RoamingSettings;
                settings.Values[nameof(UsingCppProjections)] = value;

                RaisePropertyChange();
            }
        }

        WinAppSDKChannel _winAppSDKChannel = WinAppSDKChannel.Stable;
        public WinAppSDKChannel WinAppSDKChannel
        {
            get { return _winAppSDKChannel; }
            set
            {
                if (_winAppSDKChannel != value)
                {
                    _winAppSDKChannel = value;
                    RaisePropertyChange();

                    // Reload WinAppSDK
                    Manager.WindowsAppTypeSet = null;
                    _winAppScopeLoader = new WinAppScopeLoader();
                    _winAppScopeLoader.StartMakeCurrent();

                    // Notify the Window to update its title bar
                    ApiScopeLoader.RaiseApiScopeInfoChanged();

                    // Save this change
                    SaveCurrentScope();
                }
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

        //private void RootFrame_KeyDown(object sender, KeyRoutedEventArgs e)
        //{
        //    if (e.Key == VirtualKey.Control)
        //        _keyModifiers |= KeyModifiers.Control;

        //    // bugbug: Not sure how to track Alt.  When you do Alt-Tab, you see the Alt
        //    // got down, but not come back up
        //    //else if (e2.Key == VirtualKey.Menu)
        //    //    _keyModifiers |= KeyModifiers.Alt;

        //}
        //private void RootFrame_KeyUp(object sender, KeyRoutedEventArgs e)
        //{
        //    // bugbug: no way to get key modifiers?
        //    if (e.Key == VirtualKey.Control)
        //        _keyModifiers &= ~KeyModifiers.Control;
        //    else if (e.Key == VirtualKey.Menu)
        //        _keyModifiers &= ~KeyModifiers.Alt;
        //}

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

