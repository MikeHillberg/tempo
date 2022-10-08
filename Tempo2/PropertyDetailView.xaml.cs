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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Tempo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PropertyDetailView : global::Tempo.MySerializableControl
    {
        public PropertyDetailView()
        {
            this.InitializeComponent();
        }

        public PropertyViewModel PropertyVM
        {
            get { return (PropertyViewModel)GetValue(PropertyVMProperty); }
            set { SetValue(PropertyVMProperty, value); }
        }
        public static readonly DependencyProperty PropertyVMProperty =
            DependencyProperty.Register("PropertyVM", typeof(PropertyViewModel), typeof(PropertyDetailView), new PropertyMetadata(null));



        protected override void OnActivated(object parameter)
        {
            PropertyVM = parameter as PropertyViewModel;
            App.CurrentItem = PropertyVM;
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
