using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.UI.Popups;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Tempo
{
    /// <summary>
    /// Show type's members and other info
    /// </summary>
    public sealed partial class TypeDetailView : MySerializableControl
    {
        public TypeDetailView()
        {
            this.InitializeComponent();
            UpdateArrangementAndView();

            SizeChanged += (s, e) =>
            {
                IsWide = (App.Window.Content as FrameworkElement).ActualWidth >= App.MinWidthForThreeColumns;
            };
        }

        public bool IsWide
        {
            get { return (bool)GetValue(IsWideProperty); }
            set { SetValue(IsWideProperty, value); }
        }
        public static readonly DependencyProperty IsWideProperty =
            DependencyProperty.Register("IsWide", typeof(bool), typeof(TypeDetailView), 
                new PropertyMetadata(true, (s,e) => (s as TypeDetailView).UpdateArrangementAndView()));

        void UpdateArrangementAndView()
        {
            if (IsWide)
            {
                HeadingOrientation = Orientation.Horizontal;

                // Content goes into the two-column Grid
                _skinnyModeCol0.Content = null;
                _skinnyModeCol1.Content = null;
                _skinnyMode.Visibility = Visibility.Collapsed;

                // bugbug: without this check the visual disappears
                if (_wideModeCol0.Child != _membersList)
                {
                    _wideModeCol0.Child = _membersList;
                }

                // If a member is selected (like the property of a class), put it in the right column
                if (_selectedMember != null)
                {
                    var view = App.GetViewFor(_selectedMember);
                    view.IsSecondSearchPane = true;
                    view.CanShowDocPane = false; // this.CanShowDocPane;
                    view.DoActivate(_selectedMember);

                    _wideModeCol1.Child = view;
                }

                // Other wise put the type info in the right column
                else
                {
                    _wideModeCol1.Child = _typeInfo;
                }

                _wideMode.Visibility = Visibility.Visible;
            }
            else
            {
                HeadingOrientation = Orientation.Vertical;

                // Content goes into the two-item Pivot
                _wideModeCol0.Child = null;
                _wideModeCol1.Child = null;
                _wideMode.Visibility = Visibility.Collapsed;

                _skinnyModeCol0.Content = _membersList;
                _skinnyModeCol1.Content = _typeInfo;
                _skinnyMode.Visibility = Visibility.Visible;
            }

            // Global that tracks the current item to know what docs page to use
            if (_selectedMember != null)
            {
                App.CurrentItem = _selectedMember;
            }
            else
            {
                App.CurrentItem = TypeVM;
            }
        }

        public string DependentsTitle
        {
            get { return (string)GetValue(DependentsTitleProperty); }
            set { SetValue(DependentsTitleProperty, value); }
        }
        public static readonly DependencyProperty DependentsTitleProperty =
            DependencyProperty.Register("DependentsTitle", typeof(string), 
                typeof(TypeDetailView), new PropertyMetadata(""));

        public IList<TypeViewModel> ReferencedBy
        {
            get { return (IList<TypeViewModel>)GetValue(ReferencedByProperty); }
            set { SetValue(ReferencedByProperty, value); }
        }
        public static readonly DependencyProperty ReferencedByProperty =
            DependencyProperty.Register("ReferencedBy", typeof(IList<TypeViewModel>), typeof(TypeDetailView), new PropertyMetadata(null));



        public TypeViewModel TypeVM
        {
            get { return (TypeViewModel)GetValue(TypeVMProperty); }
            set { SetValue(TypeVMProperty, value); }
        }
        public static readonly DependencyProperty TypeVMProperty =
            DependencyProperty.Register("TypeVM", typeof(TypeViewModel), typeof(TypeDetailView),
                new PropertyMetadata(null, (s,e) => (s as TypeDetailView).ResetSelectedMember()));

        private void ResetSelectedMember()
        {
            _selectedMember = null;
            UpdateArrangementAndView();
        }

        public GroupedTypeMembers GroupedTypeMembers
        {
            get { return (GroupedTypeMembers)GetValue(GroupedTypeMembersProperty); }
            set { SetValue(GroupedTypeMembersProperty, value); }
        }
        public static readonly DependencyProperty GroupedTypeMembersProperty =
            DependencyProperty.Register("GroupedTypeMembers", typeof(GroupedTypeMembers), typeof(TypeDetailView),
                new PropertyMetadata(null));


        public IList<object> InfoDetails
        {
            get { return (IList<object>)GetValue(InfoDetailsProperty); }
            set { SetValue(InfoDetailsProperty, value); }
        }
        // Using a DependencyProperty as the backing store for InfoDetails.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InfoDetailsProperty =
            DependencyProperty.Register("InfoDetails", typeof(IList<object>), typeof(TypeDetailView), new PropertyMetadata(null));


        protected override void OnActivated(object parameter)
        {
            TypeVM = parameter as TypeViewModel;
        }

        public Orientation HeadingOrientation
        {
            get { return (Orientation)GetValue(HeadingOrientationProperty); }
            set { SetValue(HeadingOrientationProperty, value); }
        }
        public static readonly DependencyProperty HeadingOrientationProperty =
            DependencyProperty.Register("HeadingOrientation", typeof(Orientation), typeof(TypeDetailView), new PropertyMetadata(Orientation.Vertical));



        protected override void OnReactivated(object parameter, object state)
        {
            TypeVM = parameter as TypeViewModel;

            try
            {
                if (state == null)
                {
                    _scrollState = null;
                    Debug.Assert(false);
                }
                else
                {

                    //TypeVM = UwpTypeViewModel.LookupByName(e.Parameter as string);

                    var stateParts = (state as string).Split(',');
                    Debug.Assert(stateParts.Length == 2);

                    SelectedPivot = int.Parse(stateParts[1]);
                    _scrollState = stateParts[0];

                    // Bugbug: Loading hasn't happened yet
                    Bindings.Initialize();

                    _membersList.LoadScrollState(_scrollState);

                    _scrollState = null;
                }


            }
#if DEBUG
            catch (Exception ex)
            {
                var d = new MessageDialog(ex.Message + "\n" + ex.StackTrace.ToString());
                var t = d.ShowAsync();
            }
#else
            catch (Exception)
            { }
#endif


        }

        string _scrollState = null;

        protected override object OnSuspending()
        {
            string relativeScrollPosition = null;

            // bugbug: open a bug on this.  If the ListView hasn't been in layout yet, it throws on the
            // GetRelativeScrollPosition call.
            if (_scrollState != null)
                relativeScrollPosition = _scrollState;
            else
            {
                // bugbug:  Even with above check, still sometimes getting "ItemsPanelRoot is null"
                try
                {
                    relativeScrollPosition = _membersList.SaveScrollState();
                    Debug.Assert(relativeScrollPosition == null || !relativeScrollPosition.Contains(','));
                }
                catch (ArgumentException)
                {
                    relativeScrollPosition = "";
                }
            }

            return relativeScrollPosition + "," + SelectedPivot.ToString();
        }

        /// <summary>
        /// TypeKind as a string, or "delegate" for delegate classes
        /// </summary>
        /// <param name="typeVM"></param>
        /// <returns></returns>
        string ConvertedTypeKind(TypeViewModel typeVM)
        {
            if(typeVM == null)
            {
                return "";
            }

            if(typeVM.TypeKind == TypeKind.Class)
            {
                if (typeVM.IsDelegate)
                    return "delegate";
                else
                    return typeVM.TypeKindString;
            }   
            else
            {
                return typeVM.TypeKindString;
            }
        }



        public int SelectedPivot
        {
            get { return (int)GetValue(SelectedPivotProperty); }
            set { SetValue(SelectedPivotProperty, value); }
        }
        public static readonly DependencyProperty SelectedPivotProperty =
            DependencyProperty.Register("SelectedPivot", typeof(int), typeof(TypeDetailView), new PropertyMetadata(0));

        // bugbug
        internal App Appp { get { return Application.Current as App; } }

        public static void GoToItem(object item)
        {
            App.Navigate(item as MemberOrTypeViewModelBase);
        }

        MemberOrTypeViewModelBase _selectedMember = null;
        private void _membersList_Navigated(TypeDetailViewMembersList sender, TypeNavigatedEventArgs args)
        {
            // In wide mode just update the second pane
            if (IsWide)
            {
                _selectedMember = args.MemberViewModel;
                UpdateArrangementAndView();
            }

            // In skinny mode do a navigate
            else
            {
                App.Navigate(args.MemberViewModel);
            }
        }

        Stack<int> _navigationStack = new Stack<int>();

        /// <summary>
        /// When anything but the member detail is tapped on, clear the member details
        /// </summary>
        private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            ResetSelectedMember();
        }

        internal GridLength TypeDetailViewCalcDocPaneHeight(bool canShowDocPane, GridLength docHeight, bool? isOpen)
        {
            if (!canShowDocPane)
            {
                return new GridLength(0);
            }

            return App.Instance.CalcDocPaneHeight(docHeight, isOpen);
        }
    }
}
