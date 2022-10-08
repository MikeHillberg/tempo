using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml;
using Microsoft.UI;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Tempo
{
    public sealed partial class MemberItemView : UserControl
    {
        public MemberItemView()
        {
            this.InitializeComponent();
        }


        public MemberViewModel MemberVM
        {
            get { return (MemberViewModel)GetValue(MemberVMProperty); }
            set { SetValue(MemberVMProperty, value); }
        }
        public static readonly DependencyProperty MemberVMProperty =
            DependencyProperty.Register("MemberVM", typeof(MemberViewModel), typeof(MemberItemView),
                new PropertyMetadata(null, (d,e) => (d as MemberItemView).MemberVMChanged()));

        void MemberVMChanged()
        {
            if(MemberVM == null)
            {
                return;
            }

            // bugbug: AVs
            //_static.ClearValue(ForegroundProperty);

            // Show/hide the text runs that give an icon for different types of members

            _static.Foreground = MemberVM.IsStatic ? _tb.Foreground : _transparentBrush;
            _deprecated.Foreground = MemberVM.IsDeprecated ? _tb.Foreground : _transparentBrush;
            _experimental.Foreground = MemberVM.IsExperimental ? _tb.Foreground : _transparentBrush;
            _readOnly.Foreground = (MemberVM is PropertyViewModel) && !(MemberVM as PropertyViewModel).CanWrite ? _tb.Foreground : _transparentBrush;
        } 

        SolidColorBrush _transparentBrush = new SolidColorBrush(Colors.Transparent);

    }
}
