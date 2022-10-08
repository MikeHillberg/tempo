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


        public MemberViewModel MemberVM
        {
            get { return (MemberViewModel)GetValue(MemberVMProperty); }
            set { SetValue(MemberVMProperty, value); }
        }
        public static readonly DependencyProperty MemberVMProperty =
            DependencyProperty.Register("MemberVM", typeof(MemberViewModel), typeof(BuildNumberText), new PropertyMetadata(null));






    }
}
