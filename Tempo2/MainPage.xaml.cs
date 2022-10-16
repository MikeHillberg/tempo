using System;
using System.Collections.Generic;
using Windows.Devices.Input;
using Microsoft.UI.Xaml;
using System.Linq;
using System.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.System;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Tempo
{
    /// <summary>
    /// Home page
    /// </summary>
    public sealed partial class MainPage : MySerializableControl
    {

        // bugbug:  This would make more sense on App, but you can't x:Bind to a static on the App class
        static public MainPageSettings AdaptiveSettings { get; private set; } = new MainPageSettings();

        static MainPage()
        {
            // mikehill_ua
            // Should have warned me about CoreWindow
            //var coreWindow = App.Window.CoreWindow;
            //coreWindow.SizeChanged += CoreWindow_SizeChanged;
            //CoreWindow_SizeChanged(coreWindow, null);

            //MainWindow.Instance.SizeChanged += Window_SizeChanged;
        }

        public MainPage()
        {
            this.InitializeComponent();

            Loaded += DoLoaded;

            AtHome?.Invoke(null, null);

            Namespaces = App.Namespaces;

            App.MainPage = this;

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

            ShowTeachingTips();
        }


        /// <summary>
        /// Show teaching tips for this page, if appropriate
        /// </summary>
        void ShowTeachingTips()
        {
            var shouldContinue = TeachingTips.TryShow(
                TeachingTipIds.CustomFiles, _root,
                () => new TeachingTip()
                {
                    Title = "Pick your own files",
                    Subtitle = "View the APIs in nupkg/DLLs/WinMDs that you pick. For example download a nupkg from nuget.org and view its APIs",
                    Target = _customFileLabel
                });
            if (!shouldContinue)
                return;

            shouldContinue = TeachingTips.TryShow(
                TeachingTipIds.CommandPrompt, _root,
                () => new TeachingTip()
                {
                    Title = "Start from the command prompt",
                    Subtitle = "Open a file of APIs directly from the command prompt",
                    Target = _commandPromptText
                });
            if (!shouldContinue)
                return;

        }

        // mikehill_ua:
        // Error CS0104	'WindowSizeChangedEventArgs' is an ambiguous reference between 'Windows.UI.Core.WindowSizeChangedEventArgs' and 'Microsoft.UI.Xaml.WindowSizeChangedEventArgs'	UwpTempo2 C:\Users\Mike\source\repos\TempoOnline\UwpTempo2\MainPage.xaml.cs	57	N/A
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
            var state = new MainPageNavivgationState()
            {
                SearchString = App.Instance.SearchText,
                OriginalSettings = _originalSettings
            };
            return state;
        }

        protected override void OnReactivated(object parameter, object state)
        {
            var s = state as MainPageNavivgationState;
            App.Instance.SearchText = s.SearchString;
            _originalSettings = s.OriginalSettings;

            BackButtonVisibility = App.CanGoBack ? Visibility.Visible : Visibility.Collapsed;
        }



        Settings _originalSettings;

        class MainPageNavivgationState
        {
            public int PivotIndex { get; set; }
            public string SearchString { get; set; }
            public Settings OriginalSettings { get; set; }
        }



        static public string PlaceholderText
        {
            get
            {
                var present = new KeyboardCapabilities().KeyboardPresent;

                // bugbug: raise notification if a keyboard is attached/detached
                if (present == 0)
                    return "Search string";
                else
                    return "Search string (Control+E)";
            }
        }

        static bool _initialLoad = true;
        private void DoLoaded(object sender, RoutedEventArgs e)
        {
            if (_selectedIndexOnLoaded != -1)
            {
                _selectedIndexOnLoaded = -1;
            }

            if (_initialLoad)
            {
                _initialLoad = false;
            }

            // Signal that we're at the Home page so the nav stack can reset
            MainPageLoaded?.Invoke(this, null);

            //_customFilesTip.IsOpen = true;

            // Wait until now to process the command line, because until now we didn't have a XamlRoot,
            // and without that we can't show an error dialog
            App.Instance.ProcessCommandLine();

            MainWindow.Instance.SetMicaBackdrop();
        }

        static public event EventHandler MainPageLoaded;




        public IList<object> Namespaces
        {
            get { return (IList<object>)GetValue(NamespacesProperty); }
            set { SetValue(NamespacesProperty, value); }
        }
        public static readonly DependencyProperty NamespacesProperty =
            DependencyProperty.Register("Namespaces", typeof(IList<object>), typeof(MainPage), new PropertyMetadata(null));




        public string SelectedNamespace
        {
            get { return (string)GetValue(SelectedNamespaceProperty); }
            set { SetValue(SelectedNamespaceProperty, value); }
        }
        public static readonly DependencyProperty SelectedNamespaceProperty =
            DependencyProperty.Register("SelectedNamespace", typeof(string), typeof(MainPage),
                new PropertyMetadata("", (s, e) => (s as MainPage).SelectedNamespaceChanged()));

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
            DependencyProperty.Register("BackButtonVisibility", typeof(Visibility), typeof(MainPage), new PropertyMetadata(Visibility.Collapsed));



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
        // the first filename and the path
        // (Currently you can only load multiple filenames from a single directory)
        internal string FilenamesToText(string[] filenames)
        {
            if (filenames == null || filenames.Length == 0)
            {
                return "";
            }

            var filename = filenames[0];
            if (!filename.Contains('\\'))
            {
                return filename;
            }

            var index = filename.LastIndexOf('\\');
            var pathPart = filename.Substring(0, index);
            var filePart = filename.Substring(index + 1);

            var sb = new StringBuilder();
            sb.AppendLine(filePart);
            if (filenames.Length > 1)
            {
                sb.AppendLine("...");
            }
            sb.Append(pathPart);
            return sb.ToString();
        }

        private void OpenCustomClick(object sender, RoutedEventArgs e)
        {
            App.ReloadCustomApiScope();
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
            if (Manager.BaselineTypeSet != null)
            {
                App.CloseBaselineScope();
            }

            var filenames = await App.TryPickMetadataFilesAsync();
            if (filenames == null)
            {
                return;
            }

            App.Instance.BaselineFilenames = filenames.ToArray();
            App.StartLoadBaselineScope(App.Instance.BaselineFilenames);
        }


        private void CloseBaseline(object sender, RoutedEventArgs e)
        {
            App.CloseBaselineScope();

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

        /// <summary>
        /// Useless teaching tip just to show a demo of what a teaching tip looks like
        /// </summary>
        
        private void TeachingTipDemo_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            // The normal teaching tip behavior is to only show once. This is here just to be able to 
            // demo at any time by doing a Control+Shift+MouseClick

            if(e.KeyModifiers.HasFlag(VirtualKeyModifiers.Control|VirtualKeyModifiers.Shift))
            {
                var tip = new TeachingTip()
                {
                    Title = "Pick your own files",
                    Subtitle = "View the APIs in nupkg/DLLs/WinMDs that you pick. For example download a nupkg from nuget.org and view its APIs",
                    Target = _customFileLabel
                };

                _root.Children.Add(tip);
                tip.Closed += (_, __) => _root.Children.Remove(tip);
                tip.IsOpen = true;
            }
        }
    }
}
