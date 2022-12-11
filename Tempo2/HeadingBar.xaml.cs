using Windows.UI;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Tempo
{
    /// <summary>
    /// Heading and subheading text
    /// </summary>
    public sealed partial class HeadingBar : UserControl
    {
        public HeadingBar()
        {
            this.InitializeComponent();
            BackgroundOpacityChanged();
            UpdateStyles();

            TextBlock = _heading;

            Loaded += DetailViewHeading_Loaded;
        }

        private void DetailViewHeading_Loaded(object sender, RoutedEventArgs e)
        {
            var connectedAnimation = ConnectedAnimationService.GetForCurrentView().GetAnimation(App.HeadingConnectedAnimationKey);
            if (connectedAnimation == null)
                return;

            var started = connectedAnimation.TryStart(_heading);
        }


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
            DependencyProperty.Register("IsDarkBackground", typeof(bool), typeof(HeadingBar), new PropertyMetadata(false));


        public Visibility visibility
        {
            get { return (Visibility)GetValue(visibilityProperty); }
            set { SetValue(visibilityProperty, value); }
        }

        public static readonly DependencyProperty visibilityProperty =
            DependencyProperty.Register("visibility", typeof(Visibility), typeof(HeadingBar), new PropertyMetadata(Visibility.Visible));



        public Brush BackgroundBrush
        {
            get { return (Brush)GetValue(BackgroundBrushProperty); }
            set { SetValue(BackgroundBrushProperty, value); }
        }
        public static readonly DependencyProperty BackgroundBrushProperty =
            DependencyProperty.Register("BackgroundBrush", typeof(Brush), typeof(HeadingBar), new PropertyMetadata(null));



        public double BackgroundOpacity
        {
            get { return (double)GetValue(BackgroundOpacityProperty); }
            set { SetValue(BackgroundOpacityProperty, value); }
        }
        public static readonly DependencyProperty BackgroundOpacityProperty =
            DependencyProperty.Register("BackgroundOpacity", typeof(double), typeof(HeadingBar), 
                new PropertyMetadata(1.0d, (d,e) => (d as HeadingBar).BackgroundOpacityChanged()));
        void BackgroundOpacityChanged()
        {
            var color = (new UISettings()).GetColorValue(UIColorType.Accent);
            BackgroundBrush = new SolidColorBrush(color) { Opacity = BackgroundOpacity };
        }


        public TextBlock TextBlock { get; private set; }

        public string Heading
        {
            get { return (string)GetValue(HeadingProperty); }
            set { SetValue(HeadingProperty, value); }
        }
        public static readonly DependencyProperty HeadingProperty =
            DependencyProperty.Register("Heading", typeof(string), typeof(HeadingBar), new PropertyMetadata(null));


        public string SubHeading
        {
            get { return (string)GetValue(SubHeadingProperty); }
            set { SetValue(SubHeadingProperty, value); }
        }
        public static readonly DependencyProperty SubHeadingProperty =
            DependencyProperty.Register("SubHeading", typeof(string), typeof(HeadingBar), 
                new PropertyMetadata(null, (s,dp) => (s as HeadingBar).UpdateStyles()));

        void UpdateStyles()
        {
            if(IsSubheading)
            {
                TitleStyle = _mediumTextBlockStyle;
                SubtitleStyle = _baseTextBlockStyle;
            }
            else
            {
                TitleStyle = _largeTextBlockStyle;
                SubtitleStyle = _mediumTextBlockStyle;
            }
        }

        /// <summary>
        /// A sub-heading (so smaller fonts)
        /// </summary>
        public bool IsSubheading
        {
            get { return (bool)GetValue(IsSubheadingProperty); }
            set { SetValue(IsSubheadingProperty, value); }
        }
        public static readonly DependencyProperty IsSubheadingProperty =
            DependencyProperty.Register("IsSubheading", typeof(bool), typeof(HeadingBar), 
                new PropertyMetadata(false, (d,dp) => (d as HeadingBar).UpdateStyles()));


        public bool IsTopLevel
        {
            get { return (bool)GetValue(IsTopLevelProperty); }
            set { SetValue(IsTopLevelProperty, value); }
        }
        public static readonly DependencyProperty IsTopLevelProperty =
            DependencyProperty.Register("IsTopLevel", typeof(bool), typeof(HeadingBar), new PropertyMetadata(false));



        public Style TitleStyle // jjj
        {
            get { return (Style)GetValue(TitleStyleProperty); }
            set { SetValue(TitleStyleProperty, value); }
        }
        public static readonly DependencyProperty TitleStyleProperty =
            DependencyProperty.Register("TitleStyle", typeof(Style), typeof(HeadingBar), new PropertyMetadata(null));

        public Style SubtitleStyle
        {
            get { return (Style)GetValue(SubtitleStyleProperty); }
            set { SetValue(SubtitleStyleProperty, value); }
        }
        public static readonly DependencyProperty SubtitleStyleProperty =
            DependencyProperty.Register("SubtitleStyle", typeof(Style), typeof(HeadingBar), new PropertyMetadata(null));





        public string ConvertedSubHeading
        {
            get { return (string)GetValue(ConvertedSubHeadingProperty); }
            set { SetValue(ConvertedSubHeadingProperty, value); }
        }
        public static readonly DependencyProperty ConvertedSubHeadingProperty =
            DependencyProperty.Register("ConvertedSubHeading", typeof(string), typeof(HeadingBar), new PropertyMetadata(""));




        public string SubSubHeading
        {
            get { return (string)GetValue(SubSubHeadingProperty); }
            set { SetValue(SubSubHeadingProperty, value); }
        }
        public static readonly DependencyProperty SubSubHeadingProperty =
            DependencyProperty.Register("SubSubHeading", typeof(string), typeof(HeadingBar), new PropertyMetadata(null));




        public string PreviewString
        {
            get { return (string)GetValue(PreviewStringProperty); }
            private set { SetValue(PreviewStringProperty, value); }
        }
        public static readonly DependencyProperty PreviewStringProperty =
            DependencyProperty.Register("PreviewString", typeof(string), typeof(HeadingBar), new PropertyMetadata(""));



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





    }
}
