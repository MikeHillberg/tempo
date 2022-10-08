using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Microsoft.UI.Xaml;
using System.Diagnostics;
using System;

namespace Tempo
{
    public sealed partial class SearchBox : UserControl
    {
        public SearchBox()
        {
            this.InitializeComponent();
        }




        private void Search_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_searchBox.Text))
                return;

            App.Instance.GotoSearch(_searchBox.Text);
        }

        private void _searchBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
                Search_Click(null, null);
        }

        public void Focus()
        {
            _ = _searchBox.Focus(FocusState.Programmatic);

            // Move the cursor to the end of the line
            _searchBox.SelectionStart = _searchBox.Text.Length;
        }

        internal void FocusAndSelect()
        {
            _ = _searchBox.Focus(FocusState.Programmatic);
            _searchBox.SelectionStart = 0;

            // Select all of the text (so you can type and replace it)
            _searchBox.SelectionLength = _searchBox.Text.Length;
        }

        private void ShowAll(object sender, RoutedEventArgs e)
        {
            App.Instance.GotoSearch(null);
        }

        public bool IsAllVisible
        {
            get { return (bool)GetValue(IsAllVisibleProperty); }
            set { SetValue(IsAllVisibleProperty, value); }
        }
        public static readonly DependencyProperty IsAllVisibleProperty =
                    DependencyProperty.Register("IsAllVisible", typeof(bool), typeof(SearchBox), new PropertyMetadata(false));

        private void ShowFilters(object sender, RoutedEventArgs e)
        {
            App.GotoFilters(showOld:false);
        }

        /// <summary>
        /// Show the old/bad version of the filters
        /// </summary>
        private void ShowOldFilters(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            App.GotoFilters(showOld: true);
        }
    }
}
