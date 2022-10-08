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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Tempo
{
    public sealed partial class FilterAppBarButtonMenu : MenuFlyout
    {
        public FilterAppBarButtonMenu()
        {
            this.InitializeComponent();
        }
        private void TypeNamesOnly_Click(object sender, RoutedEventArgs e)
        {
            App.ToggleTypeFilter();
        }
        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            App.GotoFilters();
        }

        private void PropertyNamesOnly_Click(object sender, RoutedEventArgs e)
        {
            App.TogglePropertyFilter();
        }
        private void MethodNamesOnly_Click(object sender, RoutedEventArgs e)
        {
            App.ToggleMethodFilter();
        }

        private void EventNamesOnly_Click(object sender, RoutedEventArgs e)
        {
            App.ToggleEventFilter();
        }

    }
}
