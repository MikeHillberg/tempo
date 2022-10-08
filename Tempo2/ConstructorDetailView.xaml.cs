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
    public sealed partial class ConstructorDetailView : global::Tempo.MySerializableControl
    {
        public ConstructorDetailView()
        {
            this.InitializeComponent();
        }

        public ConstructorViewModel ConstructorVM
        {
            get { return (ConstructorViewModel)GetValue(ConstructorVMProperty); }
            set { SetValue(ConstructorVMProperty, value); }
        }
        public static readonly DependencyProperty ConstructorVMProperty =
            DependencyProperty.Register("ConstructorVM", typeof(ConstructorViewModel), typeof(ConstructorDetailView), new PropertyMetadata(null));


        protected override void OnActivated(object parameter)
        {
            ConstructorVM = parameter as ConstructorViewModel;
            App.CurrentItem = ConstructorVM;
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
            DependencyProperty.Register("IsWide", typeof(bool), typeof(ConstructorDetailView), new PropertyMetadata(false));


    }
}
