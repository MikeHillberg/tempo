using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Microsoft.UI.Xaml;
using System.Diagnostics;
using System;
using Windows.Storage;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;

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

            SearchExpression.SearchExpressionError += (s, e) =>
            {
                HasSearchExpressionError = true;
                SearchErrorMessage = e;
            };

            this.Loaded += (_, __) =>
            {
                ShowTeachingTips();
            };
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            DebugLog.Append("Search_Click");
            HasSearchExpressionError = false;
            App.Instance.GotoSearch(_searchBox.Text);
        }

        public string Text
        {
            get { return _searchBox.Text; }
            set { _searchBox.Text = value; }
        }

        public void Focus()
        {
            _ = _searchBox.Focus(FocusState.Programmatic);

            // Hack: we need the ASB's text box so that we can select the text
            // (That lets the user just start typing)
            if (_asbTextBox == null)
            {
                var child = VisualTreeHelper.GetChild(_searchBox, 0);
                if (child != null)
                {
                    child = VisualTreeHelper.GetChild(child, 0);
                    if (child != null)
                    {
                        _asbTextBox = child as TextBox;
                    }
                }
            }

            if (_asbTextBox != null)
            {
                _asbTextBox.SelectionStart = 0;

                // Move the cursor to the end of the line
                _asbTextBox.SelectionLength = _asbTextBox.Text.Length;
            }


        }

        TextBox _asbTextBox = null;


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



        public string SearchErrorMessage
        {
            get { return (string)GetValue(SearchErrorMessageProperty); }
            set { SetValue(SearchErrorMessageProperty, value); }
        }
        public static readonly DependencyProperty SearchErrorMessageProperty =
            DependencyProperty.Register("SearchErrorMessage", typeof(string), typeof(SearchBox), new PropertyMetadata(null));




        //internal void FocusAndSelect()
        //{
        //    //_ = _searchBox.Focus(FocusState.Programmatic);
        //    //_searchBox.SelectionStart = 0;

        //    //// Select all of the text (so you can type and replace it)
        //    //_searchBox.SelectionLength = _searchBox.Text.Length;
        //}


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
            if (!ShowSearchSyntaxTip(force: false))
                return;

            if (!ShowProjectionsTip(force: false))
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

        bool ShowProjectionsTip(bool force)
        {
            return TeachingTips.TryShow(
                TeachingTipIds.CppProjection, _root, _cppProjectionButton,
                () => new TeachingTip()
                {
                    Title = "Use C# or C++ projections",
                    Subtitle = "There are a few differences in the C# and C++ projections, for example IList<T> vs IVector<T>",
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

        private void CppProjections_Click(object sender, RoutedEventArgs e)
        {
            App.Instance.UsingCppProjections = true;
            App.Instance.ReloadCurrentApiScope();
        }

        private void CsProjections_Click(object sender, RoutedEventArgs e)
        {
            App.Instance.UsingCppProjections = false;
            App.Instance.ReloadCurrentApiScope();
        }


        // Used in the TextChanged handler
        int _textChangedGeneration = 0;

        /// <summary>
        /// In AutoSuggestBox.TextChanged, generate the candidate list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void _searchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (string.IsNullOrEmpty(_searchBox.Text))
            {
                HasSearchExpressionError = false;
            }

            // This can be called if the user is moving through the candidate list.
            // Only do something with actual typing
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
            {
                return;
            }

            // If we haven't generated a list of names yet, nothing to do
            var allNames = Manager.CurrentTypeSet?.AllNames;
            if (allNames == null)
            {
                return;
            }

            var text = sender.Text;
            if (string.IsNullOrEmpty(text))
            {
                sender.ItemsSource = null;
                return;
            }

            // allNames matching is in upper case
            text = text.ToUpper();

            // We're about to make an async call, and we could end up having multiple going at the same time.
            // Keep a counter so that we ignore all but the last
            var generation = ++_textChangedGeneration;

            IEnumerable<string> matches = null;
            await Task.Run(() =>
            {
                matches = from name in allNames
                          where name.Key.Contains(text)
                          select name.Value;
            });

            if (generation != _textChangedGeneration)
            {
                // After this search started, another search was begun. So ignore.
                matches = null;
            }

            sender.ItemsSource = matches;
        }

        private void _searchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            this.Search_Click(null, null);
        }

        private void _searchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            sender.Text = args.SelectedItem.ToString();
        }
    }
}
