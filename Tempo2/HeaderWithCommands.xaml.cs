using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Tempo
{
    public sealed partial class HeaderWithCommands : UserControl
    {
        public HeaderWithCommands()
        {
            this.InitializeComponent();
            BackgroundOpacityChanged();

            TextBlock = _heading;

            Loaded += DetailViewHeading_Loaded;
        }

        private void DetailViewHeading_Loaded(object sender, RoutedEventArgs e)
        {
            ShowTeachingTips();

            var connectedAnimation = ConnectedAnimationService.GetForCurrentView().GetAnimation(App.HeadingConnectedAnimationKey);
            if (connectedAnimation == null)
                return;

            var started = connectedAnimation.TryStart(_textBlock);

            //LightupHelper.StartConnectedAnimation(_textBlock, App.HeadingConnectedAnimationKey);
        }




        public bool IsExportVisible
        {
            get { return (bool)GetValue(IsExportVisibleProperty); }
            set { SetValue(IsExportVisibleProperty, value); }
        }
        public static readonly DependencyProperty IsExportVisibleProperty =
            DependencyProperty.Register("IsExportVisible", typeof(bool), typeof(HeaderWithCommands), new PropertyMetadata(false));



        public Brush ComputeBackgroundColor(bool darker)
        {
            var factor = 0.25;

            var accentColor = (new UISettings()).GetColorValue(UIColorType.Accent);

            if (!darker)
            {
                return new SolidColorBrush(accentColor);
            }

            Color newColor = new Color();
            newColor.A = accentColor.A;
            newColor.R = (byte)(accentColor.R * (1 - factor));
            newColor.G = (byte)(accentColor.G * (1 - factor));
            newColor.B = (byte)(accentColor.B * (1 - factor));

            return new SolidColorBrush(newColor);
        }



        public bool IsDarkBackground
        {
            get { return (bool)GetValue(IsDarkBackgroundProperty); }
            set { SetValue(IsDarkBackgroundProperty, value); }
        }
        public static readonly DependencyProperty IsDarkBackgroundProperty =
            DependencyProperty.Register("IsDarkBackground", typeof(bool), typeof(HeaderWithCommands), new PropertyMetadata(false));




        public Visibility FilterVisibility
        {
            get { return (Visibility)GetValue(FilterVisibilityProperty); }
            set { SetValue(FilterVisibilityProperty, value); }
        }
        public static readonly DependencyProperty FilterVisibilityProperty =
            DependencyProperty.Register("FilterVisibility", typeof(Visibility), typeof(HeaderWithCommands), new PropertyMetadata(Visibility.Collapsed));



        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register("Orientation", typeof(Orientation), typeof(HeaderWithCommands), 
                new PropertyMetadata(Orientation.Vertical, (d,dp) => (d as HeaderWithCommands).UpdateConvertedSubHeading()));



        public Visibility ShowBack
        {
            get { return (Visibility)GetValue(ShowBackProperty); }
            set { SetValue(ShowBackProperty, value); }
        }
        public static readonly DependencyProperty ShowBackProperty =
            DependencyProperty.Register("ShowBack", typeof(Visibility), typeof(HeaderWithCommands), 
                new PropertyMetadata(Visibility.Visible, (s,e) => (s as HeaderWithCommands).ShowBackChanged()));

        private void ShowBackChanged()
        {
            ShowHome = (ShowBack == Visibility.Visible);
        }

        public Visibility visibility
        {
            get { return (Visibility)GetValue(visibilityProperty); }
            set { SetValue(visibilityProperty, value); }
        }

        public static readonly DependencyProperty visibilityProperty =
            DependencyProperty.Register("visibility", typeof(Visibility), typeof(HeaderWithCommands), new PropertyMetadata(Visibility.Visible));


        public bool ShowHome
        {
            get { return (bool)GetValue(ShowHomeProperty); }
            private set { SetValue(ShowHomeProperty, value); }
        }

        public static readonly DependencyProperty ShowHomeProperty =
            DependencyProperty.Register("ShowHome", typeof(bool), typeof(HeaderWithCommands), new PropertyMetadata(true));




        public Brush BackgroundBrush
        {
            get { return (Brush)GetValue(BackgroundBrushProperty); }
            set { SetValue(BackgroundBrushProperty, value); }
        }
        public static readonly DependencyProperty BackgroundBrushProperty =
            DependencyProperty.Register("BackgroundBrush", typeof(Brush), typeof(HeaderWithCommands), new PropertyMetadata(null));



        public double BackgroundOpacity
        {
            get { return (double)GetValue(BackgroundOpacityProperty); }
            set { SetValue(BackgroundOpacityProperty, value); }
        }
        public static readonly DependencyProperty BackgroundOpacityProperty =
            DependencyProperty.Register("BackgroundOpacity", typeof(double), typeof(HeaderWithCommands), 
                new PropertyMetadata(1.0d, (d,e) => (d as HeaderWithCommands).BackgroundOpacityChanged()));
        void BackgroundOpacityChanged()
        {
            var color = (new UISettings()).GetColorValue(UIColorType.Accent);
            BackgroundBrush = new SolidColorBrush(color) { Opacity = BackgroundOpacity };
        }


        public bool CanGoBack
        {
            get
            {
                return App.CanGoBack;
            }
        }

        public TextBlock TextBlock { get; private set; }

        public string Heading
        {
            get { return (string)GetValue(HeadingProperty); }
            set { SetValue(HeadingProperty, value); }
        }
        public static readonly DependencyProperty HeadingProperty =
            DependencyProperty.Register("Heading", typeof(string), typeof(HeaderWithCommands), new PropertyMetadata(null));


        public string SubHeading
        {
            get { return (string)GetValue(SubHeadingProperty); }
            set { SetValue(SubHeadingProperty, value); }
        }
        public static readonly DependencyProperty SubHeadingProperty =
            DependencyProperty.Register("SubHeading", typeof(string), typeof(HeaderWithCommands), 
                new PropertyMetadata(null, (s,dp) => (s as HeaderWithCommands).UpdateConvertedSubHeading()));

        void UpdateConvertedSubHeading()
        {
            if (Orientation == Orientation.Horizontal && !string.IsNullOrEmpty(SubHeading))
            {
                TitleStyle = _subheaderTextBlockStyle;
                SubtitleStyle = _subtitleTextBlockStyle;
            }
            else
            {
                TitleStyle = _subtitleTextBlockStyle;
                SubtitleStyle = _baseTextBlockStyle;
            }
        }

        public Style TitleStyle
        {
            get { return (Style)GetValue(TitleStyleProperty); }
            set { SetValue(TitleStyleProperty, value); }
        }
        public static readonly DependencyProperty TitleStyleProperty =
            DependencyProperty.Register("TitleStyle", typeof(Style), typeof(HeaderWithCommands), new PropertyMetadata(null));

        public Style SubtitleStyle
        {
            get { return (Style)GetValue(SubtitleStyleProperty); }
            set { SetValue(SubtitleStyleProperty, value); }
        }
        public static readonly DependencyProperty SubtitleStyleProperty =
            DependencyProperty.Register("SubtitleStyle", typeof(Style), typeof(HeaderWithCommands), new PropertyMetadata(null));





        public string ConvertedSubHeading
        {
            get { return (string)GetValue(ConvertedSubHeadingProperty); }
            set { SetValue(ConvertedSubHeadingProperty, value); }
        }
        public static readonly DependencyProperty ConvertedSubHeadingProperty =
            DependencyProperty.Register("ConvertedSubHeading", typeof(string), typeof(HeaderWithCommands), new PropertyMetadata(""));




        public string SubSubHeading
        {
            get { return (string)GetValue(SubSubHeadingProperty); }
            set { SetValue(SubSubHeadingProperty, value); }
        }
        public static readonly DependencyProperty SubSubHeadingProperty =
            DependencyProperty.Register("SubSubHeading", typeof(string), typeof(HeaderWithCommands), new PropertyMetadata(null));




        public string PreviewString
        {
            get { return (string)GetValue(PreviewStringProperty); }
            private set { SetValue(PreviewStringProperty, value); }
        }
        public static readonly DependencyProperty PreviewStringProperty =
            DependencyProperty.Register("PreviewString", typeof(string), typeof(HeaderWithCommands), new PropertyMetadata(""));



        bool _isPreview = false;
        public bool IsPreview
        {
            get { return _isPreview; }
            set
            {
                _isPreview = value;

                // Removing this feature for now, not reliable enough
                //PreviewString = value ? "[Prerelease]" : "";

                PreviewString = "";
            }
        }

        void ShowTeachingTips()
        {
            var shouldContinue = TeachingTips.TryShow(
                TeachingTipIds.Filters, _root,
                () => new TeachingTip()
                {
                    Title = "Filter your search",
                    Subtitle = "Reduce results by filtering what you search. For example only search properties, or ignore base types. The Home button (F3) resets everything.",
                    Target = _filterButton
                });
            if (!shouldContinue)
                return;

            shouldContinue = TeachingTips.TryShow(
                TeachingTipIds.ApiScopeSwitcher, _root,
                () => new TeachingTip()
                {
                    Title = "Change your API scope",
                    Subtitle = "Change the APIs you're searching, for example search WinAppSDK rather than Windows APIs",
                    Target = _apiScopeButton
                });
            if (!shouldContinue)
                return;
        }



        // bugbug
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            App.GoBack();
        }

        public Visibility UpButtonVisibility
        {
            get { return (Visibility)GetValue(UpButtonVisibilityProperty); }
            set { SetValue(UpButtonVisibilityProperty, value); }
        }
        public static readonly DependencyProperty UpButtonVisibilityProperty =
            DependencyProperty.Register("UpButtonVisibility", typeof(Visibility), typeof(HeaderWithCommands), 
                new PropertyMetadata(Visibility.Collapsed));


        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            UpButtonClick?.Invoke(null,null);
        }

        public event EventHandler UpButtonClick;

        private void AppBarButton_Home(object sender, RoutedEventArgs e)
        {
            App.ResetAndGoHome();
        }

        private void GoToMsdn(object sender, RoutedEventArgs e)
        {
            App.GoToMsdn();
        }

        private void CopyRichTextToClipboard(object sender, RoutedEventArgs e)
        {
            App.CopyMsdnLink(asMarkdown: false);
        }

        private void CopyMarkdownToClipboard(object sender, RoutedEventArgs e)
        {
            App.CopyMsdnLink(asMarkdown: true);
        }



        public MemberViewModel MemberVM
        {
            get { return (MemberViewModel)GetValue(MemberVMProperty); }
            set { SetValue(MemberVMProperty, value); }
        }
        public static readonly DependencyProperty MemberVMProperty =
            DependencyProperty.Register("MemberVM", typeof(MemberViewModel), typeof(HeaderWithCommands),
                new PropertyMetadata(null, (s, e) => (s as HeaderWithCommands).MemberVMChanged()));

        private void MemberVMChanged()
        {
            MsdnVisibility = (MemberVM == null) ? Visibility.Collapsed : Visibility.Visible;

            // Use this to always keep a global copy of what's being viewed
            App.CurrentItem = MemberVM;
        }



        public Visibility MsdnVisibility
        {
            get { return (Visibility)GetValue(MsdnVisibilityProperty); }
            set { SetValue(MsdnVisibilityProperty, value); }
        }

        public static readonly DependencyProperty MsdnVisibilityProperty =
            DependencyProperty.Register("MsdnVisibility", typeof(Visibility), typeof(HeaderWithCommands), new PropertyMetadata(Visibility.Collapsed));

        private void GoToDocs_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            App.GoToMsdn();
        }

        private void SelectWindowsApis(object sender, RoutedEventArgs e)
        {
            if (App.Instance.IsWinPlatformScope)
            {
                return;
            }

            App.Instance.IsWinPlatformScope = true;
            App.Instance.GotoSearch();
        }

        private void SelectWinAppSdkApis(object sender, RoutedEventArgs e)
        {
            if(App.Instance.IsWinAppScope)
            {
                return;
            }

            App.Instance.IsWinAppScope = true;
            App.Instance.GotoSearch();
        }


        private async void SelectCustomApiScope(object sender, RoutedEventArgs e)
        {
            if (App.Instance.IsCustomApiScope)
            {
                return;
            }

            // Might need to show the file picker
            await App.SelectAndStartLoadCustomApiScopeAsync();

            App.Instance.IsCustomApiScope = true;
            App.Instance.GotoSearch();
        }


        public bool IsScopeVisible
        {
            get { return (bool)GetValue(IsScopeVisibleProperty); }
            set { SetValue(IsScopeVisibleProperty, value); }
        }
        public static readonly DependencyProperty IsScopeVisibleProperty =
            DependencyProperty.Register("IsScopeVisible", typeof(bool), typeof(HeaderWithCommands), new PropertyMetadata(false));



        public string ApiLabel
        {
            get { return (string)GetValue(ApiLabelProperty); }
            set { SetValue(ApiLabelProperty, value); }
        }
        public static readonly DependencyProperty ApiLabelProperty =
            DependencyProperty.Register("ApiLabel", typeof(string), typeof(HeaderWithCommands), new PropertyMetadata("Windows"));

        private void CopyToClipboardAsNameNamespace(object sender, RoutedEventArgs e)
        {
            var content = CopyExport.ConvertItemsToABigString(Results, asCsv: false, flat: false); // this.Settings.Flat);
            var dataPackage = new DataPackage();
            dataPackage.SetText(content);
            Clipboard.SetContent(dataPackage);
        }

        private void CopyToClipboardAsFlat(object sender, RoutedEventArgs e)
        {
            var content = CopyExport.ConvertItemsToABigString(Results, asCsv: false, flat: true);
            var dataPackage = new DataPackage();
            dataPackage.SetText(content);
            Clipboard.SetContent(dataPackage);
        }

        private void CopyToClipboardGroupedByNamespace(object sender, RoutedEventArgs e)
        {
            var content = CopyExport.ConvertItemsToABigString(Results, asCsv: false, flat: false, groupByNamespace: true);
            var dataPackage = new DataPackage();
            dataPackage.SetText(content);
            Clipboard.SetContent(dataPackage);
        }

        private void CopyToClipboardCompact(object sender, RoutedEventArgs e)
        {
            var content = CopyExport.ConvertItemsToABigString(Results, asCsv: false, flat: false, groupByNamespace: true, compressTypes: true);
            var dataPackage = new DataPackage();
            dataPackage.SetText(content);
            Clipboard.SetContent(dataPackage);
        }


        public IList<MemberViewModel> Results
        {
            get { return (IList<MemberViewModel>)GetValue(ResultsProperty); }
            set { SetValue(ResultsProperty, value); }
        }
        public static readonly DependencyProperty ResultsProperty =
            DependencyProperty.Register("Results", typeof(IList<MemberViewModel>), typeof(HeaderWithCommands), new PropertyMetadata(null));

        private void OpenInExcel(object sender, RoutedEventArgs e)
        {
            if(Results == null || Results.FirstOrDefault() == null)
            {
                return;
            }

            var items = from object i in Results
                        select i as BaseViewModel;

            var csv = CopyExport.GetItemsAsCsv(items);

            if (!CopyExport.OpenInExcel(csv, out var errorMessage))
            {
                _ = MyMessageBox.Show(
                        $"{errorMessage}\n\nPutting onto clipboard instead",
                        "Failed to open results in Excel");
                Utils.SetClipboardText(csv);
            }

        }
    }
}
