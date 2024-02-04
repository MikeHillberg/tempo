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

        }

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

            if (_initialLoad)
            {
                _initialLoad = false;

                // Wait until now to process the command line, because until now we didn't have a XamlRoot,
                // and without that we can't show an error dialog
                // Check the AppActivationArguments first, and if it doesn't do anything then the command line
                App.Instance.ProcessActivationArgs();

                if(Manager.Settings.CompareToBaseline == true)
                {
                    _baselineExpander.IsExpanded = true;
                }

                App.Instance.InitializeToPreviousScopeFromSettings();
            }


            ShowTeachingTips();
        }

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
            App.GotoNamespaces("Windows");
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


        private void OpenCustomClick(object sender, RoutedEventArgs e)
        {
            App.Instance.PickAndAddCustomApis();
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

        private async void OpenBaseline(object sender, RoutedEventArgs e)
        {

            // Get filenames
            var filenames = await App.TryPickMetadataFilesAsync();
            if (filenames == null)
            {
                return;
            }

            // Set them as the baseline
            //App.Instance.OpenBaseline(filenames);
            App.StartLoadBaselineScope(filenames.ToArray());
        }


        private void CloseBaseline(object sender, RoutedEventArgs e)
        {
            App.CloseBaselineScope();
            Settings.CompareToBaseline = false;

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

                App.Instance.AddCustomApis(paths.ToArray());
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

                    App.StartLoadBaselineScope(new string[] { storageFile.Path });
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
            if(filenames == null)
            {
                // First time startup
                return splitFilenames;
            }

            foreach (var filename in filenames)
            {
                if(string.IsNullOrEmpty(filename))
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
        private void RemoveCustomFile_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            var split = GetTextElementTag(sender) as SplitFilename;
            var path = $@"{split.PathPart}\{split.FilePart}";

            var oldList = DesktopManager2.CustomApiScopeFileNames.Value;
            var newList = new List<string>();
            foreach(var filename in oldList)
            {
                if(filename != path)
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

        private void Hyperlink_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            _baseline.StartBringIntoView();
        }
    }

    public class SplitFilename
    {
        public string FilePart;
        public string PathPart;
    }
}
