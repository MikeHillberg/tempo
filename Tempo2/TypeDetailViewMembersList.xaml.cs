using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Tempo
{
    public sealed partial class TypeDetailViewMembersList : global::Tempo.MySerializableControl
    {
        public TypeDetailViewMembersList()
        {
            this.InitializeComponent();
            IsSummaryDisabledChanged(); // bugbug
            
            _originalCopyText = _copyText.Text;
        }

        string _originalCopyText;

        protected override void OnActivated(object parameter)
        {
        }

        public bool IsSummaryDisabled
        {
            get { return (bool)GetValue(IsSummaryDisabledProperty); }
            set { SetValue(IsSummaryDisabledProperty, value); }
        }

        public static readonly DependencyProperty IsSummaryDisabledProperty =
            DependencyProperty.Register("IsSummaryDisabled", typeof(bool), typeof(TypeDetailViewMembersList),
                new PropertyMetadata(false, (s, e) => (s as TypeDetailViewMembersList).IsSummaryDisabledChanged()));

        void IsSummaryDisabledChanged()
        {
            if (IsSummaryDisabled)
                SummaryVisibility = Visibility.Collapsed;
            else
                SummaryVisibility = Visibility.Visible;
        }



        public Visibility SummaryVisibility
        {
            get { return (Visibility)GetValue(SummaryVisibilityProperty); }
            set { SetValue(SummaryVisibilityProperty, value); }
        }
        public static readonly DependencyProperty SummaryVisibilityProperty =
            DependencyProperty.Register("SummaryVisibility", typeof(Visibility), typeof(TypeDetailViewMembersList), new PropertyMetadata(Visibility.Collapsed));



        public TypeViewModel TypeVM
        {
            get { return (TypeViewModel)GetValue(TypeVMProperty); }
            set { SetValue(TypeVMProperty, value); }
        }
        public static readonly DependencyProperty TypeVMProperty =
            DependencyProperty.Register("TypeVM", typeof(TypeViewModel), typeof(TypeDetailViewMembersList),
                new PropertyMetadata(null, (d, e) => (d as TypeDetailViewMembersList).TypeVMChanged()));

        void TypeVMChanged()
        {
            if (TypeVM == null)
            {
                ShowMe = "";
                return;
            }

            if (TypeVM.FullName == "Microsoft.UI.Colors")
                ShowMe = "Show me the colors";
            else if (TypeVM.FullName == "Microsoft.UI.Xaml.Controls.Symbol")
                ShowMe = "Show me the symbols";
            else
                ShowMe = "";

        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            Navigated?.Invoke(this, new TypeNavigatedEventArgs(_listView.SelectedIndex, e.ClickedItem as MemberOrTypeViewModelBase));
        }

        public event TypedEventHandler<TypeDetailViewMembersList, TypeNavigatedEventArgs> Navigated;



        private void ShowMe_Click(object sender, RoutedEventArgs e)
        {
            // bugbug
            if (TypeVM.FullName == "Microsoft.UI.Colors")
                App.NavigateColorsPage();
            else if (TypeVM.FullName == "Microsoft.UI.Xaml.Controls.Symbol")
                App.NavigateSymbolsIllustration();
            else
                Debug.Assert(false);
        }



        protected override object OnSuspending()
        {
            return SaveScrollState();
        }

        public string SaveScrollState()
        {
            if (_listView.ItemsPanelRoot == null)
                return null;

            return ListViewPersistenceHelper.GetRelativeScrollPosition(
                _listView,
                (item) =>
                {
                    if (item is MemberList)
                    {
                        return (item as MemberList).Heading;
                    }
                    else
                        return (item as MemberOrTypeViewModelBase).FullName;

                });
        }

        protected override void OnReactivated(object parameter, object state)
        {
            LoadScrollState(state);
        }

        public void LoadScrollState(object state)
        {
            var t = ListViewPersistenceHelper.SetRelativeScrollPositionAsync(
                _listView, state as string,
                (key) =>
                {
                    return Task.FromResult<object>(ItemFromKey(key)).AsAsyncOperation();
                });
        }

        public string ShowMe
        {
            get { return (string)GetValue(ShowMeProperty); }
            set { SetValue(ShowMeProperty, value); }
        }
        public static readonly DependencyProperty ShowMeProperty =
            DependencyProperty.Register("ShowMe", typeof(string), typeof(TypeDetailView), new PropertyMetadata(null));




        // bugbug: consolidate with SearchResults.xaml.cs
        private object ItemFromKey(string key)
        {
            var memberLists = TypeVM.GroupedMembers;
            var members = TypeVM.Members;

            foreach (var member in members)
            {
                if (member.FullName == key)
                    return member;
            }

            foreach (var memberList in memberLists)
            {
                if (memberList.Heading == key)
                    return memberList;
            }

            return null;

        }


        private void CopyTypeDefinition(object sender, RoutedEventArgs e)
        {
            var def = DesktopManager2.GetCsTypeDefinition(this.TypeVM, MsdnHelper.CalculateWinMDMsdnAddress(this.TypeVM));

            var dataPackage = new DataPackage();
            dataPackage.SetText(def);
            Clipboard.SetContent(dataPackage);

            _copyText.Text = " copied";
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += (s, e) =>
            {
                _copyText.Text = _originalCopyText;
                timer.Stop();
            };

            timer.Start();
        }
    }




}
