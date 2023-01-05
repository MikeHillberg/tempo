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

            Manager.Settings.PropertyChanged += (s, e) =>
            {
                UpdateCaseSensitive();
            };

            UpdateCaseSensitive();
        }

        // Manually keep Settings.CaseSensitive in sync with the toggle button,
        // since x:Bind isn't supported on a Flyout
        void UpdateCaseSensitive()
        {
            _caseSensitive.IsChecked = Manager.Settings.CaseSensitive;
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

        private void CaseSensitive_Click(object sender, RoutedEventArgs e)
        {
            App.ToggleCaseSensitive();
        }

        private void _caseSensitive_Click(object sender, RoutedEventArgs e)
        {
            App.ToggleCaseSensitive();
        }
    }
}
