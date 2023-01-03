using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Microsoft.UI.Xaml;
using System.Diagnostics;
using System;
using Windows.Storage;

namespace Tempo
{
    /// <summary>
    /// Search UI used on both the home page and the search results page
    /// </summary>
    public sealed partial class SearchBox : UserControl
    {
        public SearchBox()
        {
            this.InitializeComponent();
            IsAllVisibleChanged();

            SearchExpression.SearchExpressionError += (s, e) => HasSearchExpressionError = true;
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            HasSearchExpressionError = false;
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


        /// <summary>
        /// Indicates an error in the search expression
        /// </summary>
        public bool HasSearchExpressionError
        {
            get { return (bool)GetValue(HasSearchExpressionErrorProperty); }
            set { SetValue(HasSearchExpressionErrorProperty, value); }
        }
        public static readonly DependencyProperty HasSearchExpressionErrorProperty =
            DependencyProperty.Register("HasSearchExpressionError", typeof(bool), typeof(SearchBox), new PropertyMetadata(false));



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
                    DependencyProperty.Register("IsAllVisible", typeof(bool), typeof(SearchBox), 
                        new PropertyMetadata(false, (d,dp) => (d as SearchBox).IsAllVisibleChanged()));

        void IsAllVisibleChanged()
        {
            if(IsAllVisible)
            {
                PlaceholderText = "Name of anything, simple or regex, can include Property:Value (Control+E)";
            }
            else
            {
                PlaceholderText = "Search for name (Control+E)";
            }
        }

        private void ShowFilters(object sender, RoutedEventArgs e)
        {
            App.GotoFilters(showOld:false);
        }

        /// <summary>
        /// Show the old/bad version of the filters (demo purposes)
        /// </summary>
        private void ShowOldFilters(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            App.GotoFilters(showOld: true);
        }

        public string PlaceholderText
        {
            get { return (string)GetValue(PlaceholderTextProperty); }
            set { SetValue(PlaceholderTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PlaceholderText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.Register("PlaceholderText", typeof(string), typeof(SearchBox), new PropertyMetadata(0));

        private void ShowHelp(object sender, RoutedEventArgs e)
        {
            App.Instance.ShowHelp();
        }
    }
}
