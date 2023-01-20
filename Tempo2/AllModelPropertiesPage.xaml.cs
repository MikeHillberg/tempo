
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;


namespace Tempo
{
    /// <summary>
    /// Page that shows an intro and then all properties of a MemberOrTypeViewModel
    /// </summary>
    public sealed partial class AllModelPropertiesPage : UserControl
    {
        public AllModelPropertiesPage()
        {
            this.InitializeComponent();
        }

        public MemberOrTypeViewModelBase VM
        {
            get { return (MemberOrTypeViewModelBase)GetValue(VMProperty); }
            set { SetValue(VMProperty, value); }
        }
        public static readonly DependencyProperty VMProperty =
            DependencyProperty.Register("VM", typeof(MemberOrTypeViewModelBase), typeof(AllModelPropertiesPage), new PropertyMetadata(null));

        /// <summary>
        /// Show a window with all the properties of a ViewModel
        /// </summary>
        /// <param name="vm"></param>
        static public void ShowWindow(MemberOrTypeViewModelBase vm)
        {
            var window = new Window();
            window.Content = new AllModelPropertiesPage()
            {
                VM = vm
            };

            window.Title = "All model properties";
            window.Activate();
        }

    }
}
