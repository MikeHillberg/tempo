
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Tempo
{
    /// <summary>
    /// Content of an AppBarButton to make it look like a DropDown button
    /// </summary>
    public sealed partial class DropDownAppBarButtonContent : UserControl
    {
        public DropDownAppBarButtonContent()
        {
            this.InitializeComponent();
        }

        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(string), typeof(DropDownAppBarButtonContent), new PropertyMetadata(null));

        public Symbol Symbol
        {
            get { return (Symbol)GetValue(SymbolProperty); }
            set { SetValue(SymbolProperty, value); }
        }
        public static readonly DependencyProperty SymbolProperty =
            DependencyProperty.Register("Symbol", typeof(Symbol), typeof(DropDownAppBarButtonContent), new PropertyMetadata(null));
    }
}
