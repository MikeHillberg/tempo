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
    public sealed partial class EventDetailView : global::Tempo.MySerializableControl
    {
        public EventDetailView()
        {
            this.InitializeComponent();
        }

        public EventViewModel EventVM
        {
            get { return (EventViewModel)GetValue(EventVMProperty); }
            set { SetValue(EventVMProperty, value); }
        }
        public static readonly DependencyProperty EventVMProperty =
            DependencyProperty.Register("EventVM", typeof(EventViewModel), typeof(EventDetailView), 
                new PropertyMetadata(null));



        protected override void OnActivated(object parameter)
        {
            EventVM = parameter as EventViewModel;
            App.CurrentItem = EventVM;
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
    