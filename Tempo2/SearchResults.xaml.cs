using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Popups;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI;
using System.Text.RegularExpressions;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Shapes;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Tempo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SearchResults : MySerializableControl
    {
        public SearchResults()
        {
            this.InitializeComponent();

            SizeChanged += (s, e) => ReconfigureLayoutsWhenAnythingChanges();
            Loaded += (s, e) => ReconfigureLayoutsWhenAnythingChanges();

            Loaded += (s, e) => OnLoaded();
            Unloaded += (s, e) => OnUnloaded();

            // bugbug: got to be a better way
            IsWideChanged();
            IsDetailWideChanged();

            ReconfigureLayoutsWhenAnythingChanges();

            // Initialize the docs button
            _toggleDocsButton_Click(null, null);

            // There's a button and global accelerator to show the filters
            App.FilterRequested += (s, e) =>
            {
                if (HomePage.AdaptiveSettings.IsWide && IsLoaded)
                {
                    // Mark to suppress a navigation to the filters
                    e.Handled = true;

                    // Show it as a flyout, but (bugbug) for skinny mode it should
                    // be a navigation.
                    var options = new FlyoutShowOptions()
                    {
                        Placement = FlyoutPlacementMode.RightEdgeAlignedTop
                    };

                    Flyout flyout;
                    if (e.ShowOld)
                    {
                        flyout = new FiltersFlyout();
                    }
                    else
                    {
                        flyout = new FiltersFlyout3();
                    }
                    flyout.ShowAt(Pane0, options);
                }
            };

            GotFocus += (_, __) => _isFocused = true;
            LostFocus += (_, __) => _isFocused = false;

            // When we get to the Home page (navigate there or hit the Home button)
            // reset the nav stack
            HomePage.HomePageLoaded += (s, e) =>
            {
                _saveSelectedItem = null;
                this._navigationStack.Clear();
            };

            // Adjust the WebView height based on the window size.
            // (This is ignored when it's collapsed.)
            this.SizeChanged += (_, __) =>
            {
                DefaultDocHeight = this.ActualHeight / 3;
            };
        }

        bool _isFocused = false;

        //private MonitoredValue<int> ActivePaneState;
        ActivePane ActivePane = ActivePane.Left;

        private void OnUnloaded()
        {
            Settings.Changed -= OnSettingsResetting;
        }

        private void OnLoaded()
        {
            Settings.Changed += OnSettingsResetting;

            _searchBox.Focus();
        }

        /// <summary>
        /// Height that the doc page should be. Defaults to a portion of the window height,
        /// but can be adjusted by a splitter.
        /// </summary>
        public double DefaultDocHeight
        {
            get { return (double)GetValue(DefaultDocHeightProperty); }
            set { SetValue(DefaultDocHeightProperty, value); }
        }
        public static readonly DependencyProperty DefaultDocHeightProperty =
            DependencyProperty.Register("DefaultDocHeight", typeof(double), typeof(SearchResults),
                new PropertyMetadata(0d));

        /// <summary>
        /// Calculate the doc height based to be the default or zero.
        /// The DefaultDocHeight is passed in as a parameter, rather than using the property,
        /// so that it can be used in a OneWay x:Bind, which will track changes to it.
        /// </summary>
        internal GridLength DocHeight(bool? isVisible, double defaultDocHeight)
        {
            return isVisible == true ? new GridLength(defaultDocHeight) : new GridLength(0);
        }

        private void OnSettingsResetting(object sender, EventArgs e)
        {
            // Re-run search when Settings changes, because it might change the search results.
            // Skip though if we're headed to the home page, where search results won't show anyway.

            if (!App.HeadedHome)
            {
                // DoSearch is usually called as part of NavigateToSearch, which passes in the search string.
                // bugbug: this could be cleaner
                _searchString = App.Instance.SearchText;
                DoSearch();
            }
        }


        /// <summary>
        /// Label for the button that opens/hides the doc page
        /// </summary>
        public string DocPageButtonLabel
        {
            get { return (string)GetValue(DocPageButtonLabelProperty); }
            set { SetValue(DocPageButtonLabelProperty, value); }
        }
        public static readonly DependencyProperty DocPageButtonLabelProperty =
            DependencyProperty.Register("DocPageButtonLabel", typeof(string), typeof(SearchResults),
                new PropertyMetadata(""));



        public bool IsWide
        {
            get { return (bool)GetValue(IsWideProperty); }
            set { SetValue(IsWideProperty, value); }
        }
        public static readonly DependencyProperty IsWideProperty =
            DependencyProperty.Register("IsWide", typeof(bool), typeof(SearchResults),
                new PropertyMetadata(
                    true,
                    (d, dp) => (d as SearchResults).IsWideChanged()));

        void IsWideChanged()
        {
            CurrentSelectionMode = IsWide ? ListViewSelectionMode.Single
                                          : ListViewSelectionMode.None;
        }

        // Process updates to width or the ActivePane
        PaneConfig _lastPaneConfig = new PaneConfig();

        private void ReconfigureLayoutsWhenAnythingChanges()
        {
            if (ActualWidth == 0)
            {
                return;
            }

            IsWide = ActualWidth >= App.MinWidthForTwoColumns;
            IsDetailWide = ActualWidth >= App.MinWidthForThreeColumns;

            // If we don't need to switch the pane count, and the active 
            // pane hasn't changed, we're done
            if (IsWide == _lastPaneConfig.IsWide
                && ActivePane == _lastPaneConfig.ActivePane)
            {
                return;
            }

            // Remember this new configuration
            _lastPaneConfig.IsWide = IsWide;
            _lastPaneConfig.ActivePane = ActivePane;

            if (IsWide)
            {
                if (Pane0.Parent != _wideMode)
                {
                    // Put both panes into the two Grid columns
                    _wideMode.Children.Remove(Pane0);
                    _wideMode.Children.Remove(Pane1);
                    _skinnyMode.Content = null;

                    _wideMode.Children.Add(Pane0);
                    _wideMode.Children.Add(Pane1);
                }
            }
            else
            {
                // Put the active pane into the ContentControl, leave the 
                // other pane out of the tree
                _wideMode.Children.Remove(Pane0);
                _wideMode.Children.Remove(Pane1);
                _skinnyMode.Content = null;

                if (ActivePane == ActivePane.Left && _skinnyMode.Content != (object)Pane0)
                {
                    _skinnyMode.Content = Pane0;
                }
                else if (ActivePane == ActivePane.Right && _skinnyMode.Content != (object)Pane1)
                {
                    _skinnyMode.Content = Pane1;
                }
            }
        }

        public string Heading
        {
            get { return (string)GetValue(HeadingProperty); }
            set { SetValue(HeadingProperty, value); }
        }
        public static readonly DependencyProperty HeadingProperty =
            DependencyProperty.Register("Heading", typeof(string), typeof(SearchResults), new PropertyMetadata(""));


        public IList<MemberOrTypeViewModelBase> Results
        {
            get { return (IList<MemberOrTypeViewModelBase>)GetValue(ResultsProperty); }
            set { SetValue(ResultsProperty, value); }
        }
        public static readonly DependencyProperty ResultsProperty =
            DependencyProperty.Register("Results", typeof(IList<MemberOrTypeViewModelBase>), typeof(SearchResults), new PropertyMetadata(null, (s, e) => (s as SearchResults).ResultsChanged()));
        public void ResultsChanged()
        {
            // After the Results property changes the ItemsSource property will change,
            // and at that point we want to figure out which item should be selected.
            // So defer in order to give ItemsSource a chance to get updated, then run
            // layout so that we can access item containers.
            DispatcherQueue.TryEnqueue(() =>
            {
                UpdateLayout();
                PickSelectedItem();
            });
        }

        public string SearchString
        {
            get { return (string)GetValue(SearchStringProperty); }
            set { SetValue(SearchStringProperty, value); }
        }
        public static readonly DependencyProperty SearchStringProperty =
            DependencyProperty.Register("SearchString", typeof(string), typeof(SearchResults), new PropertyMetadata(null));

        /// <summary>
        /// Indicates the search results are empty
        /// </summary>
        public bool NothingFound
        {
            get { return (bool)GetValue(NothingFoundProperty); }
            set { SetValue(NothingFoundProperty, value); }
        }
        public static readonly DependencyProperty NothingFoundProperty =
            DependencyProperty.Register("NothingFound", typeof(bool), typeof(SearchResults),
                new PropertyMetadata(true, (d, dp) => (d as SearchResults).NothingFoundChanged()));

        void NothingFoundChanged()
        {
            if (NothingFound)
                SetValue(SomethingFoundProperty, false);
            else
                SetValue(SomethingFoundProperty, true);
        }

        Visibility AndNot(bool a, bool b)
        {
            return (a && !b) ? Visibility.Visible : Visibility.Collapsed;
        }


        /// <summary>
        /// Indicates the search results are non-empty
        /// </summary>
        public bool SomethingFound
        {
            get { return (bool)GetValue(SomethingFoundProperty); }
            set { SetValue(SomethingFoundProperty, value); }
        }
        public static readonly DependencyProperty SomethingFoundProperty =
            DependencyProperty.Register("SomethingFound", typeof(bool), typeof(SearchResult), new PropertyMetadata(false));


        string _searchString = null;
        protected override void OnActivated(object parameter)
        {
            ActivePane = ActivePane.Left;
            _saveSelectedItem = null;
            ReconfigureLayoutsWhenAnythingChanges();
            NothingFoundChanged();

            _root.Tag = ++_cacheCounter;
            _searchString = parameter as string;
            DoSearch();

            _searchBox.Focus();

            //_listView.Loaded += _listView_Loaded;
            //_listView.Items.VectorChanged += (s, e) => PickSelectedItem();
        }

        protected override object OnSuspending()
        {
            if (_listView.ItemsPanelRoot == null)
                return null;

            var relativeScrollPosition = ListViewPersistenceHelper.GetRelativeScrollPosition(
                _listView,
                (item) => (item as MemberOrTypeViewModelBase).FullName);

            return new SearchResultsNavigationState()
            {
                RelativeScrollPosition = relativeScrollPosition,
                CacheCounter = (int)_root.Tag,
                FilterCounter = Filters1.ShowCount,
                NavigationStack = _navigationStack
            };
        }

        protected override void OnReactivated(object parameter, object state)
        {
            var s = state as SearchResultsNavigationState;
            if (s.CacheCounter != (int)_root.Tag || s.FilterCounter != Filters1.ShowCount)
            {
                _root.Tag = s.CacheCounter;
                _searchString = parameter as string;
                DoSearch();
            }
            else
            {
                var t = ListViewPersistenceHelper.SetRelativeScrollPositionAsync(
                    _listView, s.RelativeScrollPosition,
                    (key) =>
                    {
                        return Task.FromResult<object>(KeyToItemHandler(key)).AsAsyncOperation();
                    });
                _navigationStack = s.NavigationStack;
            }

        }

        static int _cacheCounter = 0;

        private async void DoSearch()
        {
            // HomePage gets disabled when loading with /diff command line
            HomePage.Instance.IsEnabled = true;

            var shouldContinue = await App.EnsureApiScopeLoadedAsync();
            if (!shouldContinue)
            {
                // User canceled the loading dialog, nothing to search
                return;
            }

            // If we're in /diff mode, make sure the baseline files are loaded too
            if (Manager.Settings.CompareToBaseline == true)
            {
                // Ignore the result of this Ensure() call; it will always return false because the baseline is never selected/current
                // Instead just check that it's really loaded
                _ = await App.EnsureBaselineScopeAsync();
                if (!App.BaselineApiScopeLoader.IsLoaded)
                {
                    return;
                }
            }

            DebugLog.Append($"Searching: {_searchString}");

            SearchString = _searchString;

            // Make sure what the user sees is in sync
            // (They can get out of sync if the search string came in as a command line argument)
            if(_searchBox.Text != _searchString)
            {
                _searchBox.Text = _searchString;
            }


            // Do the search
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = SearchString;

            // Keep track for search results highlighting
            App.SearchExpression = searchExpression;

            // Can't x:Bind to statics in TH2
            Heading = "\"" + SearchString + "\" (" + Manager.MatchingStats.MatchingTotal + " matches)";

            IList<MemberOrTypeViewModelBase> newResults = null;
            DateTime _startTime = DateTime.Now;

            // We might get multiple searches going at once. Ignore all but the last.
            var iteration = ++Manager.RecalculateIteration;

            var searchTask = Task.Run(() =>
            {
                newResults = Manager.GetMembers(searchExpression, iteration);
            });

            // Do a blocking wait for a half second to see if the search can complete quickly
            var delayTask = Task.Delay(500);
            Task.WaitAny(new Task[] { searchTask, delayTask });

            // If the search didn't complete in a half second, put up a dialog to show progress
            if (!searchTask.IsCompleted)
            {
                var dialog = new ContentDialog() { XamlRoot = App.HomePage.XamlRoot };
                dialog.Content = new StackPanel()
                {
                    Children =
                    {
                        new TextBlock() { Text = "Searching" },
                        new ProgressBar() { IsIndeterminate = true, Margin = new Thickness(0, 10, 0, 0) }
                    }
                };

                // Bugbug: sometimes the ShowAsync returns:
                // "An async operation was not properly started. (0x80000019)"
                // (KeyShowAsync)
                //try
                //{
                //    var dialogTask = dialog.ShowAsync().AsTask();
                //}
                //catch(Exception e)
                //{
                //    UnhandledExceptionManager.ProcessException(e);
                //}

                SlowSearchInProgress = true;

                // Get rid of the stale data while the search continues
                Results = null;

                // Wait again for the search to complete, while the dialog is showing.
                // But make sure the dialog shows for at least half a second so that the screen's not flashing
                delayTask = Task.Delay(1);
                await Task.WhenAll(new Task[] { searchTask /*, delayTask*/ });

                // Search complete
                //dialog.Hide();
            }


            // Ignore the results unless this was the last concurrent search started
            if (iteration == Manager.RecalculateIteration)
            {
                SlowSearchInProgress = false;

                SearchDelay = (DateTime.Now - _startTime).Milliseconds;
                Results = newResults;
                NothingFound = (Results.Count == 0);

                if (!NothingFound
                    && App.OfferToCopyResultsToClipboard
                    && Manager.Settings.CompareToBaseline)
                {
                    // When doing an API diff from the command line,
                    // offer to put the results on the clipboard automatically
                    // (rather than having to find the copy menu and figure it out)

                    var result = await (new ContentDialog()
                    {
                        Content = "Copy diff to clipboard? \r\n(This can also be done from the Copy menu)",
                        XamlRoot = App.HomePage.XamlRoot,
                        CloseButtonText = "Cancel",
                        PrimaryButtonText = "Copy",
                    }).ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        CommonCommandBar.CopyToClipboardCompactHelper(Results);
                    }

                }

                App.OfferToCopyResultsToClipboard = false;
                DebugLog.Append($"Found {Results.Count} results in {SearchDelay}ms");
            }

        }


        /// <summary>
        /// Indicates that search has taken too long, we should show a progress UI
        /// </summary>
        public bool SlowSearchInProgress
        {
            get { return (bool)GetValue(SlowSearchInProgressProperty); }
            set { SetValue(SlowSearchInProgressProperty, value); }
        }
        public static readonly DependencyProperty SlowSearchInProgressProperty =
            DependencyProperty.Register("SlowSearchInProgress", typeof(bool), typeof(SearchResults), new PropertyMetadata(false));



        public int SearchDelay
        {
            get { return (int)GetValue(SearchDelayProperty); }
            set { SetValue(SearchDelayProperty, value); }
        }
        public static readonly DependencyProperty SearchDelayProperty =
            DependencyProperty.Register("SearchDelay", typeof(int), typeof(SearchResults), new PropertyMetadata(0));

        Settings Settings => Manager.Settings;


        static Stack<string> _relativeScrollPositions = new Stack<string>();

        class SearchResultsNavigationState
        {
            public string RelativeScrollPosition { get; set; }
            public int CacheCounter { get; set; }
            public int FilterCounter { get; set; }
            public Stack<object> NavigationStack { get; set; }
        }




        private object KeyToItemHandler(string key)
        {
            try
            {
                foreach (var memberVM in Results)
                {
                    if (memberVM.FullName == key)
                        return memberVM;
                }
            }
            catch (Exception e)
            {
                App.ShowDebugErrorDialog(e);
            }

            return null;
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // In wide mode we navigate on selection change.  But in skinny mode, there 
            // is no selection, so we have some work to do.
            if (IsWide)
            {
                return;
            }

            if (CurrentItem == e.ClickedItem)
            {
                // The user clicked on the already current item.  So the CurrentItem
                // doesn't change, but we still need to navigate from the Left pane 
                // to the Rigth one.
                NavigateToItem(CurrentItem);
            }
            else
            {
                // Update the CurrentItem to that which was clicked.
                // (This will trigger a navigate)
                CurrentItem = e.ClickedItem;
            }
        }

        public object CurrentItem
        {
            get { return (object)GetValue(CurrentItemProperty); }
            set { SetValue(CurrentItemProperty, value); }
        }
        public static readonly DependencyProperty
            CurrentItemProperty =
            DependencyProperty.Register("CurrentItem",
                typeof(object), typeof(SearchResults),
                new PropertyMetadata(
                    null,
                    (s, e) => (s as SearchResults).CurrentItemChanged()));

        void CurrentItemChanged()
        {
            if (CurrentItem == null)
            {
                return;
            }

            NavigateToItem(CurrentItem);
        }


        object _saveSelectedItem = null;
        Stack<object> _navigationStack = new Stack<object>();


        public bool IsDetailWide
        {
            get { return (bool)GetValue(IsDetailWideProperty); }
            set { SetValue(IsDetailWideProperty, value); }
        }

        internal void FocusAndSelect()
        {
            _searchBox.Focus();
        }

        public static readonly DependencyProperty IsDetailWideProperty =
            DependencyProperty.Register("IsDetailWide", typeof(bool), typeof(SearchResults),
                new PropertyMetadata(false, (s, e) => (s as SearchResults).IsDetailWideChanged()));

        void IsDetailWideChanged()
        {
            SecondColumnWidth = IsDetailWide ? new GridLength(2, GridUnitType.Star)
                                             : new GridLength(1, GridUnitType.Star);
        }

        public ListViewSelectionMode CurrentSelectionMode
        {
            get { return (ListViewSelectionMode)GetValue(CurrentSelectionModeProperty); }
            set { SetValue(CurrentSelectionModeProperty, value); }
        }
        public static readonly DependencyProperty CurrentSelectionModeProperty =
            DependencyProperty.Register("CurrentSelectionMode", typeof(ListViewSelectionMode), typeof(SearchResults),
                new PropertyMetadata(ListViewSelectionMode.Single));




        public GridLength SecondColumnWidth
        {
            get { return (GridLength)GetValue(SecondColumnWidthProperty); }
            set { SetValue(SecondColumnWidthProperty, value); }
        }

        public static readonly DependencyProperty SecondColumnWidthProperty =
            DependencyProperty.Register("SecondColumnWidth", typeof(GridLength), typeof(SearchResults), new PropertyMetadata(new GridLength()));


        private void NavigateToItem(object item)
        {
            // Push the previous item onto the stack if we're navigating forward
            // (If we're navigating backward we alreayd popped the stack)
            if (!_goingBack)
            {
                if (ActivePane != ActivePane.Left
                   || IsWide && _saveSelectedItem != null)
                {
                    _navigationStack.Push(_saveSelectedItem);
                }
            }

            // The left pane is only active after navigating here from Home,
            // or navigating to the point that Home is the only "back" left
            ActivePane = _saveSelectedItem == null ? ActivePane.Left : ActivePane.Right;

            // Save this item so that if we navigate forward again we can push it onto the stack
            // Bugbug: in order to have one less thing, push this onto the nav stack instead now,
            // and just don't get confused by it on a nav-back
            _saveSelectedItem = item;

            // In skinny mode, if navigating from the results list to an item,
            // animate the heading from the list item to the detail header
            if (!IsWide
                && _lastPaneConfig.ActivePane != ActivePane
                && ActivePane == ActivePane.Right)
            {
                var container = _listView.ContainerFromItem(item) as ListViewItem;
                if (container != null)
                {
                    var service = ConnectedAnimationService.GetForCurrentView();
                    service.PrepareToAnimate(
                        App.HeadingConnectedAnimationKey,
                        (container.ContentTemplateRoot as SearchResult)
                            .ConnectedAnimationElement);
                }
            }

            _detailsView.NavigateToItem(item);

            ReconfigureLayoutsWhenAnythingChanges();
        }

        Brush PickColor(bool isWide)
        {
            var color = isWide ? Color.FromArgb(0xff, 0xf0, 0xf0, 0xf0) : Colors.White;
            return new SolidColorBrush(color);
        }

        protected override bool OnNavigateBack()
        {
            // If we got a "Back", and the nav stack is empty, we might need to navigate between panes
            if (_navigationStack.Count == 0)
            {
                // If we're on the second pane and in single pane mode, then switch to the first pane
                if (ActivePane == ActivePane.Right && !IsWide)
                {
                    ActivePane = ActivePane.Left;
                    ReconfigureLayoutsWhenAnythingChanges();
                    return true;
                }

                // Otherwise, noop
                return base.OnNavigateBack();
            }

            _goingBack = true; // Don't add to the nav stack

            var newItem = _navigationStack.Pop();
            Debug.Assert(newItem != null);

            // bugbug
            // Should never throw, but getting crash reports on the call to put_SelectedItem
            try
            {
                if (_listView.Items.Contains(newItem))
                {
                    //_listView.SelectedItem = newItem;
                    CurrentItem = newItem;
                }
                else
                {
                    NavigateToItem(newItem);
                }
            }
#if DEBUG
            catch (Exception e)
            {
                Debug.Assert(false);
                var d = new MessageDialog(e.Message + "\n" + newItem.ToString() + "\n" + e.StackTrace.ToString());
                var t = d.ShowAsync();
            }
#else
            catch (Exception)
            { }
#endif

            _goingBack = false;

            return true;
        }

        bool _goingBack = false;

        private void _listView_Loaded(object sender, RoutedEventArgs e)
        {
            _listView.Loaded -= _listView_Loaded;
            PickSelectedItem();
        }

        // Pick the select item that best matches the search string
        private void PickSelectedItem()
        {
            // bugbug: how does this relate to virtualization?
            // bugbug: consolidate with desktop

            MemberOrTypeViewModelBase targetItem = null;

            // InitialSelection will be set if the app was launched with a parameter
            if (App.InitialSelection != null)
            {
                // App was launched with a parameter specifying which item to start selected
                // E.g. "type:Windows.UI.Xaml.Controls.Button"
                // (Could be a type or a member)

                var parts = App.InitialSelection.Split(':');
                App.InitialSelection = null;
                if (parts == null || parts.Length != 2)
                {
                    return;
                }

                var name = parts[1];
                var isType = false;
                if (parts[0] == "type")
                {
                    isType = true;
                }

                // Search the items to find a match
                foreach (var item in _listView.Items)
                {
                    var itemVM = item as MemberOrTypeViewModelBase;
                    if(isType)
                    {
                        var typeVM = item as TypeViewModel;
                        if (typeVM != null && typeVM.FullName == name)
                        {
                            targetItem = itemVM;
                            break;
                        }
                    }
                    else
                    {
                        var memberVM = item as MemberViewModelBase;
                        if(memberVM != null && memberVM.FullName == name)
                        {
                            targetItem = memberVM;
                            break;
                        }
                    }
                }
            }

            else if (App.SearchExpression.MemberRegex == null
                   || App.SearchExpression.TypeRegex == null)
            {
                // We're showing everything, not searching.

                var items = _listView.Items;

                if (items.Count == 0)
                {
                    // Don't think this case happens
                    return;
                }

                // Pick a type at random, so every time you see something new
                // Try to avoid the Xaml Direct APIs because there's so many of them
                int index = -1;
                for (int i = 0; i <= 3; i++)
                {
                    index = Random.Shared.Next(items.Count - 1);
                    targetItem = items[0] as MemberOrTypeViewModelBase;
                    var typeName = targetItem.DeclaringType.Name;
                    if (typeName != "XamlPropertyIndex" && typeName != "XamlEventIndex" && typeName != "XamlTypeIndex")
                    {
                        break;
                    }
                    else
                    {
                        Debug.Assert(false);
                    }
                }

                for (int i = index; i >= 0; i--)
                {
                    if (_listView.Items[i] is TypeViewModel)
                    {
                        targetItem = items[i] as MemberOrTypeViewModelBase;
                        break;
                    }
                }
            }

            else
            {
                // Find the most interesting match

                // We'll give all the items a priority and take the higest one
                var highestPriority = MatchPriority.None;

                foreach (var item in _listView.Items)
                {
                    var member = item as MemberOrTypeViewModelBase;
                    if (member == null)
                    {
                        continue;
                    }

                    var priority = MatchPriority.None;

                    if (member is TypeViewModel)
                    {
                        var match = App.SearchExpression.TypeRegex.Match(member.Name);
                        if (match != Match.Empty)
                        {
                            if (match.Value == member.Name)
                            {
                                priority = MatchPriority.ExactMatchType;
                            }
                            else if (member.Name.StartsWith(match.Value))
                            {
                                priority = MatchPriority.StartsWithType;
                            }
                            else
                            {
                                priority = MatchPriority.MatchesType;
                            }
                        }
                    }
                    else
                    {
                        var match = App.SearchExpression.MemberRegex.Match(member.Name);
                        if (match != Match.Empty)
                        {
                            if (match.Value == member.Name)
                            {
                                priority = MatchPriority.ExactMatchMember;
                            }
                            else if (member.Name.StartsWith(match.Value))
                            {
                                priority = MatchPriority.StartsWithMember;
                            }
                            else
                            {
                                priority = MatchPriority.MatchesMember;
                            }
                        }
                    }

                    if (priority > highestPriority)
                    {
                        highestPriority = priority;
                        targetItem = member;
                    }

                    if (priority == MatchPriority.Max)
                    {
                        break;
                    }
                }
            }

            if (targetItem == null)
            {
                // We didn't find anything, just select the first item

                if (_listView.Items.Count != 0)
                {
                    _listView.SelectedIndex = 0;
                }
            }

            else
            {
                // Select the identified item and scroll it into view

                _listView.ScrollIntoView(targetItem, ScrollIntoViewAlignment.Leading);

                _listView.SelectedIndex = Results.IndexOf(targetItem);

                // In case for some reason layout hasn't run, we might not have a container
                var selectedContainer = _listView.ContainerFromIndex(_listView.SelectedIndex) as ListViewItem;
                if (selectedContainer != null)
                {
                    if (_isFocused)
                    {
                        // Pointer to keep the focus rect from showing up
                        // If you focus the ListView it changes the scrolling.
                        selectedContainer.Focus(FocusState.Pointer);
                    }
                }
                else
                {
                    Debug.Assert(true); // Why didn't layout run?
                }
            }
        }



        /// <summary>
        /// Used by `PickSelectedItem`
        /// </summary>
        private enum MatchPriority
        {
            // Low priority to high, using Button.Click as an example

            None = 0,
            MatchesMember,     // "ick"
            MatchesType,       // "utton"
            StartsWithMember,  // "Cli"
            StartsWithType,    // "But"
            ExactMatchMember,  // "Click"
            ExactMatchType,    // "Button"
            Max = ExactMatchType
        }


        //private void GotoFilters(object sender, RoutedEventArgs e)
        //{
        //    App.GotoFilters();
        //}

        private void GoHome(object sender, RoutedEventArgs e)
        {
            App.GoHome();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("");
        }

        private void ShowDebugLog_Click(object sender, RoutedEventArgs e)
        {
            DebugLogViewer.Show();
        }

        private void _toggleDocsButton_Click(object sender, RoutedEventArgs e)
        {
            DocPageButtonLabel = _toggleDocsButton.IsChecked == true
                ? "Hide doc page"
                : "Show doc page";
        }


        /// <summary>
        /// Grid column width of the results column. Defaults to 1*, but can be adjusted by a splitter
        /// </summary>
        public GridLength ResultsColumnWidth
        {
            get { return (GridLength)GetValue(ResultsColumnWidthProperty); }
            set { SetValue(ResultsColumnWidthProperty, value); }
        }
        public static readonly DependencyProperty ResultsColumnWidthProperty =
            DependencyProperty.Register("ResultsColumnWidth", typeof(GridLength), typeof(SearchResults),
                new PropertyMetadata(new GridLength(1, GridUnitType.Star)));

        InputSystemCursor _ewCursor = null;
        InputSystemCursor _nsCursor = null;

        private void Splitter_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            // When the pointer is over the splitter, change the cursor

            var splitter = sender as Rectangle;
            if (splitter == _resultsSplitter)
            {
                if (_ewCursor == null)
                {
                    _ewCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
                }
                ProtectedCursor = _ewCursor;
            }
            else
            {
                Debug.Assert(splitter == _docPageSplitter);

                if (_nsCursor == null)
                {
                    _nsCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth);
                }
                ProtectedCursor = _nsCursor;
            }
        }

        private void Splitter_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            // Pointer's not over the splitter anymore, which implies we're not moving it,
            // so restore the pointer.

            ProtectedCursor = null;
        }

        double _splitterPointerOffset = 0;
        FrameworkElement _activeSplitter = null;

        private void Splitter_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            // Start moving the splitter

            // Capture the pointer so that we still get messages when the mouse isn't actually over it anymore
            if (!(sender as FrameworkElement).CapturePointer(e.Pointer))
            {
                return;
            }

            // Keep track of where the pointer is right now relative to the edge of the splitter,
            // for use later in Move

            var currentPosition = e.GetCurrentPoint(this).Position;
            _activeSplitter = sender as FrameworkElement;
            if (_activeSplitter == _resultsSplitter)
            {
                _splitterPointerOffset = currentPosition.X - _resultsColumn.ActualWidth;
            }
            else
            {
                Debug.Assert(_activeSplitter == _docPageSplitter);
                _splitterPointerOffset = currentPosition.Y - (this.ActualHeight - _docPageRow.ActualHeight);
            }
        }

        private void Splitter_PointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (_activeSplitter == null)
            {
                return;
            }

            // We're in the middle of dragging the splitter.
            // Update the column or row according to the new pointer position.
            // Since the pointer likely didn't start exactly on the edge of the column/row that's
            // being resized, take into account the offset of the pointer from that edge at the start.

            var currentPoint = e.GetCurrentPoint(this).Position;

            if (_activeSplitter == _resultsSplitter)
            {
                this.ResultsColumnWidth = new GridLength(currentPoint.X - _splitterPointerOffset);
            }
            else
            {
                this.DefaultDocHeight = this.ActualHeight - currentPoint.Y + _splitterPointerOffset;
            }
        }

        private void Splitter_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (_activeSplitter == null)
            {
                return;
            }

            // Done splitting

            ReleasePointerCapture(e.Pointer);
            _activeSplitter = null;
        }

        private void ShowDebugLog_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            DebugLogViewer.Show();
        }
    }


    public enum ActivePane { Left, Right }

    public class PaneConfig
    {
        public ActivePane ActivePane { get; set; }
        public bool IsWide { get; set; } = true;
    }

}