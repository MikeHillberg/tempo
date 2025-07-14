using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Tempo
{
    /// <summary>
    /// Displays an API as appropriate (could be a type or a member)
    /// </summary>
    public sealed partial class TypeOrMemberDetailView : Page
    {
        public TypeOrMemberDetailView()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// "Top Level", meaning, show the command bar?
        /// </summary>
        public bool IsTopLevel
        {
            get { return (bool)GetValue(IsTopLevelProperty); }
            set { SetValue(IsTopLevelProperty, value); }
        }
        public static readonly DependencyProperty IsTopLevelProperty =
            DependencyProperty.Register("IsTopLevel", typeof(bool), typeof(TypeOrMemberDetailView), 
                new PropertyMetadata(false));

        public void NavigateToItem(object item)
        {
            // Keep track of the current item (for the go-to-docs link)
            App.CurrentItem = item as MemberOrTypeViewModelBase;

            // Based on the member, update the detail views

            foreach(var view in _detailsGrid.Children)
            {
                view.Visibility = Visibility.Collapsed;
            }

            if (item is TypeViewModel)
            {
                _typeDetail.DoActivate(item as TypeViewModel);
                _typeDetail.Visibility = Visibility.Visible;
            }
            else
            {
                _memberDetail.DoActivate(item as MemberViewModelBase);
                _memberDetail.Visibility = Visibility.Visible;
            }

        }

        /// <summary>
        /// OK to show the doc pane button
        /// </summary>
        public bool CanShowDocPane
        {
            get { return (bool)GetValue(CanShowDocPaneProperty); }
            set { SetValue(CanShowDocPaneProperty, value); }
        }

        public static readonly DependencyProperty CanShowDocPaneProperty =
            DependencyProperty.Register("CanShowDocPane", typeof(bool), typeof(TypeOrMemberDetailView), new PropertyMetadata(true));
    }
}
