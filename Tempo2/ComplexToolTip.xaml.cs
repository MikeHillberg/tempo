using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Tempo
{
    /// <summary>
    /// StackPanel meant to be used in a tool tip, has a Title and a Subtitle.
    /// Looks very similar to a TeachingTip
    /// </summary>
    public sealed partial class ComplexToolTip : StackPanel
    {
        public ComplexToolTip()
        {
            this.InitializeComponent();
        }



        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(ComplexToolTip), new PropertyMetadata(null));



        public string Subtitle
        {
            get { return (string)GetValue(SubtitleProperty); }
            set { SetValue(SubtitleProperty, value); }
        }
        public static readonly DependencyProperty SubtitleProperty =
            DependencyProperty.Register("Subtitle", typeof(string), typeof(ComplexToolTip), new PropertyMetadata(null));

    }
}
