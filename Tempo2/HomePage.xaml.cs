using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using System.Linq;
using System.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using System.Diagnostics;
using System.Threading;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using System.IO.Pipes;
using System.IO;
using Windows.ApplicationModel;

namespace Tempo
{
    /// <summary>
    /// Home page
    /// </summary>
    public sealed partial class HomePage : MySerializableControl
    {

        // bugbug:  This would make more sense on App, but you can't x:Bind to a static on the App class
        static public HomePageSettings AdaptiveSettings { get; private set; } = new HomePageSettings();

        static HomePage()
        {
            // mikehill_ua
            // Should have warned me about CoreWindow
            //var coreWindow = App.Window.CoreWindow;
            //coreWindow.SizeChanged += CoreWindow_SizeChanged;
            //CoreWindow_SizeChanged(coreWindow, null);

            //MainWindow.Instance.SizeChanged += Window_SizeChanged;

            CheckForDotNet();
        }

        static public HomePage Instance = null;

        public HomePage()
        {
            this.InitializeComponent();

            Debug.Assert(Instance == null);
            Instance = this;

            Loaded += DoLoaded;

            AtHome?.Invoke(null, null);

            Namespaces = App.Namespaces;

            App.HomePage = this;

            // There's a button and global accelerator to show the filters
            App.FilterRequested += (s, e) =>
            {
                if (AdaptiveSettings.IsWide && IsLoaded)
                {
                    // Mark to suppress a navigation to the filters
                    e.Handled = true;

                    Flyout flyout = e.ShowOld ? new FiltersFlyout() : new FiltersFlyout3();
                    var options = new FlyoutShowOptions() { Placement = FlyoutPlacementMode.Full };
                    flyout.ShowAt(this, options);
                }
            };
        }


        /// <summary>
        /// Show teaching tips for this page, if appropriate. (Wait to Loaded to invoke)
        /// </summary>
        void ShowTeachingTips()
        {
            var shouldContinue = TeachingTips.TryShow(
                TeachingTipIds.CustomFiles, _root, _customFileLabel,
                () => new TeachingTip()
                {
                    Title = "Pick your own files",
                    Subtitle = "View the APIs in nupkg/DLLs/WinMDs that you pick. For example download a nupkg from nuget.org and view its APIs",
                });
            if (!shouldContinue)
                return;

            shouldContinue = TeachingTips.TryShow(
                TeachingTipIds.CommandPrompt, _root, _commandPromptText,
                () => new TeachingTip()
                {
                    Title = "Start from the command prompt",
                    Subtitle = "Open a file of APIs directly from the command prompt",
                });
            if (!shouldContinue)
                return;

            shouldContinue = TeachingTips.TryShow(
                TeachingTipIds.Win32Scope, _root, _win32ScopeLabel,
                () => new TeachingTip()
                {
                    Title = "View the Win32 APIs",
                    Subtitle = "All of the Win32 APIs as exposed by the Win32 Metadata project",
                });
            if (!shouldContinue)
                return;

            shouldContinue = TeachingTips.TryShow(
                TeachingTipIds.WebView2Scope, _root, _webView2ScopeLabel,
                () => new TeachingTip()
                {
                    Title = "View the WebView2 APIs",
                    Subtitle = "Download the latest WebView2 Nuget package view its APIs",
                });
            if (!shouldContinue)
                return;

            shouldContinue = TeachingTips.TryShow(
                TeachingTipIds.PowerShell, _root, _psButton,
                () => new TeachingTip()
                {
                    Title = PowerShellTipText.Title,
                    Subtitle = PowerShellTipText.Subtitle,
                });
            if (!shouldContinue)
                return;

            shouldContinue = TeachingTips.TryShow(
                TeachingTipIds.DotNetScope, _root, _dotNetScopeLabel,
                () => new TeachingTip()
                {
                    Title = ".Net APIs",
                    Subtitle = "View the .Net APIs installed on this PC",
                });
            if (!shouldContinue)
                return;

            shouldContinue = TeachingTips.TryShow(
                TeachingTipIds.DotNetScope, _root, _dotNetWindowsScopeLabel,
                () => new TeachingTip()
                {
                    Title = ".Net Windows Desktop APIs",
                    Subtitle = "View the .Net Windows Desktop APIs installed on this PC (includes WPF and WinForms)",
                });
            if (!shouldContinue)
                return;
        }

        (string Title, string Subtitle) PowerShellTipText = (
                "Search/Display in PowerShell (Ctrl+Shift+P)",
                "Types and members are objects, and PowerShell loves objects");


        // mikehill_ua:
        // Error CS0104	'WindowSizeChangedEventArgs' is an ambiguous reference between 'Windows.UI.Core.WindowSizeChangedEventArgs' and 'Microsoft.UI.Xaml.WindowSizeChangedEventArgs'	UwpTempo2 C:\Users\Mike\source\repos\TempoOnline\UwpTempo2\HomePage.xaml.cs	57	N/A
        // Should be a TODO here because it's referencing a CoreWindow

        static private void Window_SizeChanged(object sender, Microsoft.UI.Xaml.WindowSizeChangedEventArgs args)
        {
            // bugbug: consolidate with TypeDetailPage (have it use this)

            if ((sender as Window).Bounds.Width >= 800)
                AdaptiveSettings.IsWide = true;
            else
                AdaptiveSettings.IsWide = false;
        }


        int _selectedIndexOnLoaded = -1;

        protected override void OnActivated(object parameter)
        {
            _originalSettings = Manager.Settings.Clone();

        }

        protected override object OnSuspending()
        {
            var state = new HomePageNavivgationState()
            {
                SearchString = App.Instance.SearchText,
                OriginalSettings = _originalSettings
            };
            return state;
        }

        protected override void OnReactivated(object parameter, object state)
        {
            var s = state as HomePageNavivgationState;
            App.Instance.SearchText = s.SearchString;
            _originalSettings = s.OriginalSettings;

            BackButtonVisibility = App.CanGoBack ? Visibility.Visible : Visibility.Collapsed;
        }



        Settings _originalSettings;

        class HomePageNavivgationState
        {
            public int PivotIndex { get; set; }
            public string SearchString { get; set; }
            public Settings OriginalSettings { get; set; }
        }



        static bool _initialLoad = true;
        private void DoLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= DoLoaded;

            if (_selectedIndexOnLoaded != -1)
            {
                _selectedIndexOnLoaded = -1;
            }

            // Signal that we're at the Home page so the nav stack can reset
            HomePageLoaded?.Invoke(this, null);


            MainWindow.Instance.SetMicaBackdrop();

            MonitorHighContrast();

            if (_initialLoad)
            {
                _initialLoad = false;

                // Wait until now to process the command line, because until now we didn't have a XamlRoot,
                // and without that we can't show an error dialog
                // Check the AppActivationArguments first, and if it doesn't do anything then the command line
                App.Instance.ProcessActivationArgs();

                if (Manager.Settings.CompareToBaseline == true)
                {
                    _baselineExpander.IsExpanded = true;
                }

                App.Instance.InitializeToPreviousScopeFromSettings();
            }


            ShowTeachingTips();
        }

        AccessibilitySettings _accessibilitySettings;

        /// <summary>
        /// Monitor high contrast settings in order to update the background
        /// </summary>
        private void MonitorHighContrast()
        {
            _accessibilitySettings = new AccessibilitySettings();

            // Not supported in Desktop. Switch to ThemeSettings after updating to latest WinAppSDK
            //_accessibilitySettings.HighContrastChanged += CheckHighContrast;


            CheckHighContrast(null, null);
        }

        /// <summary>
        /// Update the root background according to the current high contrast setting
        /// </summary>
        private void CheckHighContrast(AccessibilitySettings sender, object args)
        {
            if (_accessibilitySettings.HighContrast)
            {
                RootBackground = SystemControlBackgroundAltHighShape.Fill;
            }
            else
            {
                RootBackground = new SolidColorBrush() { Color = Colors.Transparent };
            }
        }



        /// <summary>
        /// Background for the root, may be transparent to allow for Mica backdrop
        /// </summary>
        public Brush RootBackground
        {
            get { return (Brush)GetValue(RootBackgroundProperty); }
            set { SetValue(RootBackgroundProperty, value); }
        }
        public static readonly DependencyProperty RootBackgroundProperty =
            DependencyProperty.Register("RootBackground", typeof(Brush), typeof(HomePage),
                new PropertyMetadata(new SolidColorBrush() { Color = Colors.Transparent }));





        static public event EventHandler HomePageLoaded;


        public IList<object> Namespaces
        {
            get { return (IList<object>)GetValue(NamespacesProperty); }
            set { SetValue(NamespacesProperty, value); }
        }
        public static readonly DependencyProperty NamespacesProperty =
            DependencyProperty.Register("Namespaces", typeof(IList<object>), typeof(HomePage), new PropertyMetadata(null));




        public string SelectedNamespace
        {
            get { return (string)GetValue(SelectedNamespaceProperty); }
            set { SetValue(SelectedNamespaceProperty, value); }
        }
        public static readonly DependencyProperty SelectedNamespaceProperty =
            DependencyProperty.Register("SelectedNamespace", typeof(string), typeof(HomePage),
                new PropertyMetadata("", (s, e) => (s as HomePage).SelectedNamespaceChanged()));

        // bugbug:  Bind directly to Manager?
        private void SelectedNamespaceChanged()
        {
            Manager.Settings.Namespace = SelectedNamespace;
        }

        static public event EventHandler AtHome;

        private void ShowNamespaces(object sender, RoutedEventArgs e)
        {
            App.GotoNamespaces("");
        }

        public Settings Settings { get { return Manager.Settings; } }




        public Visibility BackButtonVisibility
        {
            get { return (Visibility)GetValue(BackButtonVisibilityProperty); }
            set { SetValue(BackButtonVisibilityProperty, value); }
        }
        public static readonly DependencyProperty BackButtonVisibilityProperty =
            DependencyProperty.Register("BackButtonVisibility", typeof(Visibility), typeof(HomePage), new PropertyMetadata(Visibility.Collapsed));



        private void NavigateBack(object sender, RoutedEventArgs e)
        {
            App.GoBack();
        }

        private void ShowColorsPage(object sender, RoutedEventArgs e)
        {
            App.NavigateColorsPage();
        }

        private void ShowTypeRampPage(object sender, RoutedEventArgs e)
        {
            App.NavigateTypeRampPage();
        }

        private void ShowSymbolsIllustration(object sender, RoutedEventArgs e)
        {
            App.NavigateSymbolsIllustration();
        }


        static public void SendFeedbackHelper()
        {
            //var uriString = @"mailto:CoffeeZeit@outlook.com?subject=Tempo%20feedback&body=(Thanks%20for%20the%20feedback!)%0A%0A%0A";
            //Launcher.LaunchUriAsync(new Uri(uriString));

            // mikehill_ua: Store Engagement no longer exists?
            //var launcher = Microsoft.Services.Store.Engagement.StoreServicesFeedbackLauncher.GetDefault();
            //var t = launcher.LaunchAsync();
        }

        private void SendFeedback(object sender, RoutedEventArgs e)
        {
            SendFeedbackHelper();
        }



        /// <summary>
        /// Check if DotNet is available (SDK is installed)
        /// </summary>
        static void CheckForDotNet()
        {
            DebugLog.Append("Checking for dotnet");
            var dotNetPath = Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramFiles%"), "dotnet");
            if (!Directory.Exists(dotNetPath))
            {
                DebugLog.Append($"DotNet not found at {dotNetPath}");
                return;
            }

            var dotNetCorePath = Path.Combine(dotNetPath, @"shared\Microsoft.NETCore.App");
            if (!Directory.Exists(dotNetCorePath))
            {
                DebugLog.Append($"Couldn't find '{dotNetCorePath}'");
                return;
            }

            var version = FindHighestVersionDirectory(dotNetCorePath);
            if (version == null)
            {
                DebugLog.Append($"Couldn't find version in {dotNetCorePath}");
                return;
            }
            App.DotNetCorePath = Path.Combine(dotNetCorePath, version);
            App.Instance.DotNetCoreVersion = version;
            DebugLog.Append($"Found {App.DotNetCorePath}");


            var dotNetWindowsPath = Path.Combine(dotNetPath, @"shared\Microsoft.WindowsDesktop.App");
            if (!Directory.Exists(dotNetWindowsPath))
            {
                DebugLog.Append($"Couldn't find '{dotNetWindowsPath}'");
                return;
            }

            version = FindHighestVersionDirectory(dotNetWindowsPath);
            if (version == null)
            {
                DebugLog.Append($"Couldn't find version in {dotNetWindowsPath}");
            }
            App.DotNetWindowsPath = Path.Combine(dotNetWindowsPath, version);
            DebugLog.Append($"Found {App.DotNetWindowsPath}");
        }

        /// <summary>
        /// Search subdirectories that are 3-part versions for the highest
        /// </summary>
        static string FindHighestVersionDirectory(string path)
        {
            var subdirs = Directory.GetDirectories(path);
            if (subdirs == null || subdirs.Length == 0)
            {
                return null;
            }

            DebugLog.Append($"Directories in {path}: {string.Join(", ", subdirs)}");

            string highest = null;
            var highestVersion = (0, 0, 0);
            foreach (var dir in subdirs)
            {
                var dirLeaf = Path.GetFileName(dir);

                // dirLeave should be in format "1.2.3"
                var parts = dirLeaf.Split('.');
                if (parts == null || parts.Length != 3)
                {
                    return null;
                }

                if (!Int32.TryParse(parts[0], out int part1)
                    || !Int32.TryParse(parts[1], out int part2)
                    || !Int32.TryParse(parts[2], out int part3))
                {
                    return null;
                }

                var version = (part1, part2, part3);
                var newHigh = false;
                if (highest == null)
                {
                    newHigh = true;
                }
                else
                {
                    int comparisson = highestVersion.CompareTo(version);
                    if (comparisson < 0)
                    {
                        newHigh = true;
                    }
                }

                if (newHigh)
                {
                    highest = dirLeaf;
                    highestVersion = version;
                }
            }

            return highest;
        }


        /// <summary>
        /// Move focus to the search box
        /// </summary>
        internal void FocusToSearchString()
        {
            _searchBox.Focus(FocusState.Programmatic);
        }

        // Convert a list of filenames into text that shows
        // the filename and the path
        internal string FilenamesToText(string[] filenames)
        {
            if (filenames == null || filenames.Length == 0)
            {
                return "";
            }

            var sb = new StringBuilder();
            foreach (var filename in filenames)
            {
                var index = filename.LastIndexOf('\\');
                var pathPart = filename.Substring(0, index);
                var filePart = filename.Substring(index + 1);

                sb.AppendLine(filePart);
                sb.AppendLine($"   {pathPart}");
            }

            return sb.ToString();
        }


        private async void OpenCustomClick(object sender, RoutedEventArgs e)
        {
            var pickedSomething = await App.CustomApiScopeLoader.PickAndAddCustomApis();
            if (pickedSomething)
            {
                App.Instance.IsCustomApiScope = true;
            }
            else if (!App.CustomApiScopeLoader.HasFile)
            {
                App.Instance.IsWinPlatformScope = true;
                App.GoHome();
            }
        }



        private void CloseCustomClick(object sender, RoutedEventArgs e)
        {
            App.CloseCustomScope(goHome: true);
        }

        private void TextRamp_Click(object sender, RoutedEventArgs e)
        {
            App.NavigateTypeRampPage();
        }

        private void ColorSamples_Click(object sender, RoutedEventArgs e)
        {
            App.NavigateColorsPage();
        }

        private void SymbolsSamples_Click(object sender, RoutedEventArgs e)
        {
            App.NavigateSymbolsIllustration();
        }

        private async void AddBaseline(object sender, RoutedEventArgs e)
        {
            // Get new filenames
            var filenames = await App.TryPickMetadataFilesAsync();
            if (filenames == null)
            {
                return;
            }

            if (App.Instance.BaselineFilenames != null)
            {
                filenames = App.Instance.BaselineFilenames.Union(filenames).ToArray();
            }

            // Set them as the baseline
            App.BaselineApiScopeLoader.StartMakeCurrent(filenames.ToArray());
        }


        private void CloseAllBaseline(object sender, RoutedEventArgs e)
        {
            Settings.CompareToBaseline = false;
            App.Instance.BaselineFilenames = null;
            App.BaselineApiScopeLoader.Close();
        }

        //private void RemoveBaselineFile_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        //{
        //    var split = GetTextElementTag(sender) as SplitFilename;
        //    var path = $@"{split.PathPart}\{split.FilePart}";

        //    var oldList = App.Instance.BaselineFilenames;
        //    var newList = new List<string>();
        //    foreach (var filename in oldList)
        //    {
        //        if (filename != path)
        //        {
        //            newList.Add(filename);
        //        }
        //    }

        //    if (newList.Count == 0)
        //    {
        //        CloseAllBaseline(null, null);
        //    }
        //    else
        //    {
        //        App.BaselineApiScopeLoader.StartMakeCurrent(newList.ToArray());
        //    }
        //}

        private void BaselineFilenameRemoved(object sender, string path)
        {
            var oldList = App.Instance.BaselineFilenames;
            var newList = new List<string>();
            foreach (var filename in oldList)
            {
                if (filename != path)
                {
                    newList.Add(filename);
                }
            }

            if (newList.Count == 0)
            {
                CloseAllBaseline(null, null);
            }
            else
            {
                App.BaselineApiScopeLoader.StartMakeCurrent(newList.ToArray());
            }
        }

        string BaselineHeaderText(bool isExpanded)
        {
            return isExpanded ? "" : "Expand to set APIs to compare against";
        }

        int OpaqueIf(bool b)
        {
            return b ? 1 : 0;
        }

        int TransparentIf(bool b)
        {
            return b ? 0 : 1;
        }

        /// <summary>
        /// Useless teaching tip just to show a demo of what a teaching tip looks like
        /// </summary>

        private async void TeachingTipDemo_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            // The normal teaching tip behavior is to only show once. This is here just to be able to 
            // demo at any time by doing a Control+Shift+MouseClick

            if (e.KeyModifiers.HasFlag(VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift))
            {
                var tip = new TeachingTip()
                {
                    Title = "Pick your own files",
                    Subtitle = "View the APIs in nupkg/DLLs/WinMDs that you pick. For example download a nupkg from nuget.org and view its APIs",
                    Target = _customFileLabel
                };

                _root.Children.Add(tip);
                tip.Closed += (_, __) => _root.Children.Remove(tip);

                // bugbug: without this delay, the tip opens, but won't close
                await Task.Delay(100);

                tip.IsOpen = true;
            }
        }

        /// <summary>
        /// Listen to DragOver event and decide to accept or not
        /// </summary>
        private void HandleDragOver(object sender, DragEventArgs e)
        {
            if (!e.DataView.AvailableFormats.Contains(StandardDataFormats.StorageItems))
            {
                Debug.WriteLine("Dragging non-StorageItems");
                return;
            }

            e.AcceptedOperation = DataPackageOperation.Link;
        }

        /// <summary>
        /// Process dropped custom API scope files
        /// </summary>
        private async void CustomScope_Drop(object sender, DragEventArgs e)
        {
            var deferral = e.GetDeferral();
            try
            {
                var items = await e.DataView.GetStorageItemsAsync();

                var paths = new List<string>();
                foreach (var item in items)
                {
                    var storageFile = item as StorageFile;
                    if (storageFile != null)
                    {
                        paths.Add(storageFile.Path);
                    }
                }

                App.CustomApiScopeLoader.AddCustomApis(paths.ToArray());
            }
            finally
            {
                deferral.Complete();
            }
        }

        /// <summary>
        /// Convert DragEnter/DragLeave into a property
        /// </summary>
        private void CustomScope_DragEnter(object sender, DragEventArgs e)
        {
            Debug.WriteLine("Drag enter");
            IsDraggingOverCustom = true;
        }
        private void CustomScope_DragLeave(object sender, DragEventArgs e)
        {
            Debug.WriteLine("Drag leave");
            IsDraggingOverCustom = false;
        }


        public bool IsDraggingOverCustom
        {
            get { return (bool)GetValue(IsDraggingOverCustomProperty); }
            set { SetValue(IsDraggingOverCustomProperty, value); }
        }
        public static readonly DependencyProperty IsDraggingOverCustomProperty =
            DependencyProperty.Register("IsDraggingOverCustom", typeof(bool), typeof(HomePage), new PropertyMetadata(false));

        /// <summary>
        /// Convert Drag enter/leave into an IsDraggingOverBaseline property
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Baseline_DragEnter(object sender, DragEventArgs e)
        {
            IsDraggingOverBaseline = true;
        }
        private void Baseline_DragLeave(object sender, DragEventArgs e)
        {
            IsDraggingOverBaseline = false;
        }

        /// <summary>
        ///  Indicates if a drag is happening over the baseline files area
        /// </summary>
        public bool IsDraggingOverBaseline
        {
            get { return (bool)GetValue(IsDraggingOverBaselineProperty); }
            set { SetValue(IsDraggingOverBaselineProperty, value); }
        }
        public static readonly DependencyProperty IsDraggingOverBaselineProperty =
            DependencyProperty.Register("IsDraggingOverBaseline", typeof(bool), typeof(HomePage), new PropertyMetadata(false));

        /// <summary>
        /// Handle a drop of new baseline files
        /// </summary>
        private async void Baseline_Drop(object sender, DragEventArgs e)
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

                    App.BaselineApiScopeLoader.StartMakeCurrent(new string[] { storageFile.Path });
                }
            }
            finally
            {
                deferral.Complete();
            }
        }

        /// <summary>
        /// Placeholder text for the search box
        /// </summary>
        string GetPlaceholderText(bool isWildcardSyntax)
        {
            var s = isWildcardSyntax ? "with wildcards" : "regex";
            return $"Search for anything, simple or {s}, can include Property:Value (Ctrl+E)";
        }

        private void ShowHelp(object sender, RoutedEventArgs e)
        {
            App.Instance.ShowHelp();
        }

        private void BrowseAll(object sender, RoutedEventArgs e)
        {
            App.GotoNamespaces("");
        }

        private void ShowAll(object sender, RoutedEventArgs e)
        {
            App.Instance.GotoSearch("");
        }

        /// <summary>
        /// Split an array of filenames into name/path tuples
        /// </summary>
        IEnumerable<SplitFilename> SplitFilenames(string[] filenames)
        {
            var splitFilenames = new List<SplitFilename>();
            if (filenames == null)
            {
                // First time startup
                return splitFilenames;
            }

            foreach (var filename in filenames)
            {
                if (string.IsNullOrEmpty(filename))
                {
                    continue;
                }

                var index = filename.LastIndexOf('\\');

                splitFilenames.Add(new SplitFilename()
                {
                    FilePart = filename.Substring(index + 1),
                    PathPart = filename.Substring(0, index)
                });
            }

            return splitFilenames;
        }

        /// <summary>
        /// Event handler to remove a custom file
        /// </summary>
        private void RemoveCustomFile_Click(object sender, string path)
        {
            var oldList = DesktopManager2.CustomApiScopeFileNames.Value;
            var newList = new List<string>();
            foreach (var filename in oldList)
            {
                if (filename != path)
                {
                    newList.Add(filename);
                }
            }

            App.Instance.ReplaceCustomApis(newList.ToArray());
        }



        /// <summary>
        /// TextElement type doesn't have a Tag property, so add one
        /// </summary>
        public static object GetTextElementTag(DependencyObject obj)
        {
            return (object)obj.GetValue(TextElementTagProperty);
        }
        public static void SetTextElementTag(DependencyObject obj, object value)
        {
            obj.SetValue(TextElementTagProperty, value);
        }
        public static readonly DependencyProperty TextElementTagProperty =
            DependencyProperty.RegisterAttached("Tag", typeof(object), typeof(HomePage),
                    new PropertyMetadata(null));

        private void ApiScope_RadioButton_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            App.Instance.GotoSearch();
        }

        //private void Hyperlink_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        //{
        //    _baseline.StartBringIntoView();
        //}


        private void GoToPSClick(object sender, object args)
        {
            PSLauncher.GoToPS(this.XamlRoot);
        }

        private void SelectWasdkStable(object sender, RoutedEventArgs e)
        {
            App.Instance.WinAppSDKChannel = WinAppSDKChannel.Stable;
        }
        private void SelectWasdkPreview(object sender, RoutedEventArgs e)
        {
            App.Instance.WinAppSDKChannel = WinAppSDKChannel.Preview;
        }
        private void SelectWasdkExperimental(object sender, RoutedEventArgs e)
        {
            App.Instance.WinAppSDKChannel = WinAppSDKChannel.Experimental;
        }
    }

    public class SplitFilename
    {
        public string FilePart;
        public string PathPart;
    }
}
