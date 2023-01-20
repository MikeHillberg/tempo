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

            this.Loaded += (_, __) =>
            {
                ShowTeachingTips();
            };
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


        public bool IsAllVisible
        {
            get { return (bool)GetValue(IsAllVisibleProperty); }
            set { SetValue(IsAllVisibleProperty, value); }
        }
        public static readonly DependencyProperty IsAllVisibleProperty =
                    DependencyProperty.Register("IsAllVisible", typeof(bool), typeof(SearchBox),
                        new PropertyMetadata(false, (d, dp) => (d as SearchBox).IsAllVisibleChanged()));

        void IsAllVisibleChanged()
        {
        }

        private void ShowFilters(object sender, RoutedEventArgs e)
        {
            App.GotoFilters(showOld: false);
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


        private void SearchHelp_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            App.Instance.ShowHelp();
        }

        void ShowTeachingTips()
        {
            var shouldContinue = ShowSearchSyntaxTip(force: false);
            if (!shouldContinue)
                return;
        }

        bool ShowSearchSyntaxTip(bool force)
        {
            return TeachingTips.TryShow(
                TeachingTipIds.SearchSyntax, _root, _syntaxButton,
                () => new TeachingTip()
                {
                    Title = "Choose your search syntax",
                    Subtitle = "You can search for a simple string, but you can also use either Regex syntax or Wildcard syntax. See help (F1) for more information",
                },
                force);

        }

        private void OptionsHelp_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            // Force this tip to show even if e.g. it's already been shown
            ShowSearchSyntaxTip(force: true);
        }
        private void RegexMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Manager.Settings.IsWildcardSyntax = false;
        }

        private void WildcardMenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            Manager.Settings.IsWildcardSyntax = true;
        }
    }
}
