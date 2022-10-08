using System;
using System.Collections.Generic;
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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Tempo
{
    public sealed partial class MethodDetailView : global::Tempo.MySerializableControl
    {
        public MethodDetailView()
        {
            this.InitializeComponent();
        }

        public MethodViewModel MethodVM
        {
            get { return (MethodViewModel)GetValue(MethodVMProperty); }
            set { SetValue(MethodVMProperty, value); }
        }
        public static readonly DependencyProperty MethodVMProperty =
            DependencyProperty.Register("MethodVM", typeof(MethodViewModel), typeof(MethodDetailView), new PropertyMetadata(null));


        protected override void OnActivated(object parameter)
        {
            MethodVM = parameter as MethodViewModel;
            App.CurrentItem = MethodVM;
        }
        protected override object OnSuspending()
        {
            return null;
        }
        protected override void OnReactivated(object parameter, object state)
        {
            OnActivated(parameter);
        }

    }
}
