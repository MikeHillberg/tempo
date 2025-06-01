using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Tempo
{
    public sealed partial class BuildNumberText : UserControl
    {
        public BuildNumberText()
        {
            this.InitializeComponent();
        }

        public MemberOrTypeViewModelBase MemberVM
        {
            get { return (MemberOrTypeViewModelBase)GetValue(MemberVMProperty); }
            set { SetValue(MemberVMProperty, value); }
        }
        public static readonly DependencyProperty MemberVMProperty =
            DependencyProperty.Register("MemberVM", typeof(MemberOrTypeViewModelBase), typeof(BuildNumberText), new PropertyMetadata(null));
    }
}
