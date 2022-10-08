using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Tempo
{
    public sealed partial class ToolTipTypeView : ToolTip
    {
        public ToolTipTypeView()
        {
            this.InitializeComponent();

            this.Opened += (s, e) => HasOpened = true;
        }



        public bool HasOpened
        {
            get { return (bool)GetValue(HasOpenedProperty); }
            set { SetValue(HasOpenedProperty, value); }
        }
        public static readonly DependencyProperty HasOpenedProperty =
            DependencyProperty.Register("HasOpened", typeof(bool), typeof(ToolTipTypeView), new PropertyMetadata(false));



        public TypeViewModel TypeViewModel
        {
            get { return (TypeViewModel)GetValue(TypeViewModelProperty); }
            set { SetValue(TypeViewModelProperty, value); }
        }
        public static readonly DependencyProperty TypeViewModelProperty =
            DependencyProperty.Register("TypeViewModel", typeof(TypeViewModel), typeof(ToolTipTypeView), new PropertyMetadata(null));

    }
}
