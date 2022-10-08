using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.ApplicationModel.DataTransfer;

namespace Tempo
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;

            //// Bugbug: why is this both on Manager and MainWindow.InstanceOld?
            //Manager.Settings = new Settings();
            DesktopManager2.Initialize(false);

            // Load all of the contract names/versions
            ContractInformation.Load();

            // Load Windows winmds from system32
            LoadTypesWithMR();

            // Callback so that low-level CheckForMatch filter classes have access to Settings
            CheckForMatch.GetSettingsCallback = () => Manager.Settings;


            // Bugbug:  Move this to CommonLibrary
            Manager.Checkers = new CheckForMatch[]
            {
                new CheckForNamespace(),
                new CheckForFilterOnType(),
                new CheckForTypeRestrictions()
            };

            // bugbug: combine this with above
            UwpTypeViewModel2.PopulateCache();

            UnhandledException += App_UnhandledException;
        }


        private void LoadTypesWithMR()
        {
            DesktopManager2.LoadWindowsTypesWithMRSync(useWinRTProjections: true, (assemblyName) => LocateAssembly(assemblyName));
            Manager.CurrentTypeSet = Manager.WindowsTypeSetCS;
        }

        // This is called by the MR loader when it can't find an assembly
        string LocateAssembly(string assemblyName)
        {
            // Mostly if a referenced assembly can't be found, let it be faked.
            // But we need some WinRT interop assemblies for things like GridLength
            if (assemblyName == "System.Runtime.InteropServices.WindowsRuntime"
                || assemblyName == "System.Runtime.WindowsRuntime"
                || assemblyName == "System.Runtime.WindowsRuntime.UI.Xaml")
            {
                var task = StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assemblies/{assemblyName}.dll"));
                task.AsTask().Wait();
                return task.GetResults().Path;
            }

            return null;
        }

        private void LoadTypesFromCSV()
        {
            var typeSet = new UwpTypeSet();
            Manager.CurrentTypeSet = typeSet;

            CheckForMatch.GetSettingsCallback = () => Manager.Settings;

            var typeReader = GetAppTextResource("MetadataCopy/Types.csv");
            var memberReader = GetAppTextResource("MetadataCopy/Members.csv");
            var contractReader = GetAppTextResource("MetadataCopy/Contracts.csv");

            var types = new List<TypeViewModel>();
            UwpTypeSerializer.Load(typeSet, types, typeReader, memberReader, contractReader);

            // bugbug: pre-sort
            var sortedTypes = from type in types
                              orderby type.Name
                              select type;
            typeSet.Types = sortedTypes.ToList();


            // bugbug: async
            // bugbug: Should combine this with desktop Types2Namespaces.  But moving NamespaceTreeNode into
            // Common breaks the build for NamespaceViewer.xaml; for some reason the Xaml compiler refuses to recognize
            // a type in that project.
            var namespaces = (from t in typeSet.Types
                              select t.Namespace).Distinct().OrderBy(t => t).ToList<object>();

            namespaces.Insert(0, "Windows");
            Namespaces = namespaces;
        }

        TextReader GetAppTextResource(string name)
        {
            // bugbug: make this async

            var uri = new Uri("ms-appx:///" + name);
            var asyncOp = StorageFile.GetFileFromApplicationUriAsync(uri);
            var file = asyncOp.AsTask().GetAwaiter().GetResult();
            var inputStream = file.OpenReadAsync().AsTask().GetAwaiter().GetResult();
            var classicStream = inputStream.AsStreamForRead();
            return new StreamReader(classicStream);
        }

        static private void NavigateBackRequested(object sender, BackRequestedEventArgs e)
        {
            if (e.Handled)
                return;

            e.Handled = true;
            GoBack();
        }


        static internal Stack<object> NavigationStateStack = new Stack<object>();

        static void InternalNavigate(Type type, object parameter)
        {
            RootFrame.Navigate(type, parameter);
        }

        public static void GotoNamespaces(string initial)
        {
            InternalNavigate(typeof(NamespaceView), initial);
        }


        internal static void GotoFilters()
        {
            var args = new FilterRequestedEventArgs();
            FilterRequested?.Invoke(null, args);

            if(!args.Handled)
            {
                InternalNavigate(typeof(Filters), null);
            }
        }

        static internal event EventHandler<FilterRequestedEventArgs> FilterRequested;

        internal class FilterRequestedEventArgs : EventArgs
        {
            internal bool Handled { get; set; } = false;
        }


        public static void GotoSearch(string text)
        {
            InternalNavigate(typeof(SearchResults), text);
        }

        public static void GoHome(string searchString = null)
        {
            InternalNavigate(typeof(MainPage), searchString);
        }

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
                        return;
                }
            }

            if (RootFrame.CanGoBack)
                RootFrame.GoBack();
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
                return typeof(TypeDetailPage);
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

            Debug.Assert(false);
            return null;
        }

        public static MySerializableControl GetViewFor(MemberViewModel memberVM)
        {
            if (memberVM is TypeViewModel)
                return new TypeDetailPage();
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
#if DEBUG
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
            if(CurrentItem == null)
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


        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Debug.Assert(Manager.Settings.AreAllMembersDefault());

#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = false;
            }
#endif
            SystemNavigationManager.GetForCurrentView().BackRequested += NavigateBackRequested;

            RootFrame = App.Window.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (RootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                RootFrame = new Frame();

                // Too jarring
                //RootFrame.ContentTransitions = new TransitionCollection() { new ContentThemeTransition() };

                RootFrame.PointerPressed += (s, e2) =>
                {
                    if (e2.GetCurrentPoint(s as UIElement).Properties.IsXButton1Pressed)
                        App.GoBack();
                };

                RootFrame.KeyDown += (s, e2) =>
                {
                    if (e2.Handled)
                        return;

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
                        Manager.Settings = new Settings();
                        MoveToMainAndFocusToSearch();
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
                    else if(e2.Key == VirtualKey.S && _keyModifiers == KeyModifiers.Control)
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

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                App.Window.Content = RootFrame;
            }

            if (RootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                RootFrame.Navigate(typeof(MainPage), null); // e.Arguments);
            }


            // Ensure the current window is active
            App.Window.Activate();
        }

        // Add an accelerator to RootFrame.KeyboardAccelerators
        void AddAccelerator(VirtualKey key, VirtualKeyModifiers modifier, Action handler)
        {
            var keyboardAccelerator = new KeyboardAccelerator();
            keyboardAccelerator.Modifiers = modifier;
            keyboardAccelerator.Key = key;
            keyboardAccelerator.Invoked += (s, e) => handler();
            RootFrame.KeyboardAccelerators.Add(keyboardAccelerator);
        }

        // Copy the MSDN link for the MemberViewModel to the clipboard
        public static void CopyMsdnLink(bool asMarkdown = false)
        {
            if(CurrentItem == null)
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

        private static void MoveToMainAndFocusToSearch()
        {
            if (RootFrame.Content is MainPage)
            {
                (RootFrame.Content as MainPage).FocusToSearchString();
            }
            else
            {
                RootFrame.Navigate(typeof(MainPage)); // bugbug: helper method
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

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
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
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        // Used for search results highlighting
        static public SearchExpression SearchExpression { get; set; }


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
