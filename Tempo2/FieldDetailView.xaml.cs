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
    public sealed partial class FieldDetailView : global::Tempo.MySerializableControl
    {
        public FieldDetailView()
        {
            this.InitializeComponent();
        }

        public FieldViewModel FieldVM
        {
            get { return (FieldViewModel)GetValue(FieldVMProperty); }
            set { SetValue(FieldVMProperty, value); }
        }
        public static readonly DependencyProperty FieldVMProperty =
            DependencyProperty.Register("FieldVM", typeof(FieldViewModel), typeof(FieldDetailView), new PropertyMetadata(null));

        protected override void OnActivated(object parameter)
        {
            FieldVM = parameter as FieldViewModel;
            App.CurrentItem = FieldVM;
        }
        protected override object OnSuspending()
        {
            return null;
        }
        protected override void OnReactivated(object parameter, object state)
        {
            OnActivated(parameter);
        }

        public bool IsWide
        {
            get { return (bool)GetValue(IsWideProperty); }
            set { SetValue(IsWideProperty, value); }
        }
        public static readonly DependencyProperty IsWideProperty =
            DependencyProperty.Register("IsWide", typeof(bool), typeof(FieldDetailView), new PropertyMetadata(false));

    }
}
