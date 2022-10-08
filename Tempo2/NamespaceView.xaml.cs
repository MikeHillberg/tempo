using System;
using System.Collections;
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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Tempo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NamespaceView : global::Tempo.MySerializableControl
    {
        public NamespaceView()
        {
            this.InitializeComponent();
        }

        protected override void OnActivated(object parameter)
        {
            if (string.IsNullOrEmpty(parameter as string))
                return;

            //Microsoft.HockeyApp.HockeyClient.Current.TrackEvent("Namespace view");

            var ns = (parameter as string) + ".";
            var selectedNamespace = parameter as string;

            Initialize(ns, selectedNamespace);
        }


        protected override object OnSuspending()
        {
            if (_listView.ItemsPanelRoot == null)
                return null;

            var relativeScrollPosition
                = ListViewPersistenceHelper.GetRelativeScrollPosition(
                _listView,
                (item) =>
                {
                    var group = item as ItemsGroup;
                    if (group != null)
                        return group.Key;

                    return (item as ItemWrapper).ItemAsString;
                });

            return relativeScrollPosition;
        }


        protected override void OnReactivated(object parameter, object state)
        {
            var ns = (parameter as string) + ".";
            var selectedNamespace = parameter as string;

            Initialize(ns, selectedNamespace);

            if (string.IsNullOrEmpty(state as string))
                return;

            var t = ListViewPersistenceHelper.SetRelativeScrollPositionAsync(
                _listView, (state as string),
                (key) =>
                {
                    return Task.FromResult<object>(KeyToItemHandler(key)).AsAsyncOperation();
                });

        }

        private object KeyToItemHandler(string key)
        {
            try
            {
                foreach (var i in NamespacesAndTypes)
                {
                    if (i.ToString() == key)
                        return i;

                    foreach (var j in i as IEnumerable)
                    {
                        if (j.ToString() == key)
                        {
                            return j;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                App.ShowDebugErrorDialog(e);
            }

            return null;
        }

        public Visibility UpButtonVisibility
        {
            get { return (Visibility)GetValue(UpButtonVisibilityProperty); }
            set { SetValue(UpButtonVisibilityProperty, value); }
        }
        public static readonly DependencyProperty UpButtonVisibilityProperty =
            DependencyProperty.Register("UpButtonVisibility", typeof(Visibility), typeof(NamespaceView), new PropertyMetadata(Visibility.Collapsed));



        //public IList<TypeViewModel> NamespaceTypes
        //{
        //    get { return (IList<TypeViewModel>)GetValue(NamespaceTypesProperty); }
        //    set { SetValue(NamespaceTypesProperty, value); }
        //}
        //public static readonly DependencyProperty NamespaceTypesProperty =
        //    DependencyProperty.Register("NamespaceTypes", typeof(IList<TypeViewModel>), typeof(NamespaceView), new PropertyMetadata(null));




        public List<object> NamespacesAndTypes
        {
            get { return (List<object>)GetValue(NamespacesAndTypesProperty); }
            set { SetValue(NamespacesAndTypesProperty, value); }
        }
        public static readonly DependencyProperty NamespacesAndTypesProperty =
            DependencyProperty.Register("NamespacesAndTypes", typeof(List<object>), typeof(NamespaceView), new PropertyMetadata(null));




        public List<ItemWrapper> Namespaces
        {
            get { return (List<ItemWrapper>)GetValue(NamespacesProperty); }
            set { SetValue(NamespacesProperty, value); }
        }
        public static readonly DependencyProperty NamespacesProperty =
            DependencyProperty.Register("Namespaces", typeof(List<string>), typeof(NamespaceView), new PropertyMetadata(null));



        public List<ItemWrapper> Types
        {
            get { return (List<ItemWrapper>)GetValue(TypesProperty); }
            set { SetValue(TypesProperty, value); }
        }
        public static readonly DependencyProperty TypesProperty =
            DependencyProperty.Register("Types", typeof(List<string>), typeof(NamespaceView), new PropertyMetadata(null));





        public string SelectedNamespace
        {
            get { return (string)GetValue(SelectedNamespaceProperty); }
            set { SetValue(SelectedNamespaceProperty, value); }
        }
        public static readonly DependencyProperty SelectedNamespaceProperty =
            DependencyProperty.Register("SelectedNamespace", typeof(string), typeof(NamespaceView), new PropertyMetadata(null));


        private void Initialize(string ns, string selectedNamespace) // bugbug: why twice?
        {
            SelectedNamespace = selectedNamespace;

            UpButtonVisibility = SelectedNamespace.Contains('.') ? Visibility.Visible : Visibility.Collapsed;

            var selectedParts = SelectedNamespace.Split('.');

            var groupedItems = new GroupedList();

            var basePartCount = ns.Split('.').Length - 1;

            var childNamespaces = new List<string>();

            // bugbug: async
            // bugbug: Should combine this with desktop Types2Namespaces.  But moving NamespaceTreeNode into
            // Common breaks the build for NamespaceViewer.xaml; for some reason the Xaml compiler refuses to recognize
            // a type in that project.
            var namespaces = Manager.CurrentTypeSet.FullNamespaces;

            // bugbug: need an entry for the root, which has no typesS
            namespaces.Insert(0, "Windows");

            foreach (var n in namespaces)
            {
                var str = n as string;
                if (!str.StartsWith(ns))
                    continue;

                var parts = str.Split('.');
                var childNamespace = parts[basePartCount];

                if (!childNamespaces.Contains(childNamespace))
                {
                    childNamespaces.Add(childNamespace);
                }

            }

            Namespaces = new List<ItemWrapper>();
            foreach (var childNamespace in childNamespaces)
            {
                Namespaces.Add(new ItemWrapper() { Item = childNamespace, ItemAsString = childNamespace });
            }

            var itemsGroup = new ItemsGroup(Namespaces.ToList())
            {
                Key = "Namespaces"
            };

            // bugbug: more efficient
            if (itemsGroup.Count != 0)
                groupedItems.Add(itemsGroup);

            Types = (from t in Manager.CurrentTypeSet.Types
                    where t.Namespace == SelectedNamespace
                    where t.IsPublic && !t.ShouldIgnore
                    select new ItemWrapper
                    {
                        Item = t,
                        ItemAsString = t.PrettyName
                    }).ToList();
            var typesGroup = new ItemsGroup(Types)
            {
                Key = "Types"
            };
            if (typesGroup.Count != 0)
                groupedItems.Add(typesGroup);

            NamespacesAndTypes = groupedItems;
        }

        Stack<string> _savedState = new Stack<string>();

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var itemWrapper = e.ClickedItem as ItemWrapper;
            if (itemWrapper.Item is string)
            {
                App.GotoNamespaces(SelectedNamespace + "." + itemWrapper.ItemAsString);
            }
            else
            {
                Debug.Assert(itemWrapper.Item is TypeViewModel);
                App.Navigate(itemWrapper.Item as TypeViewModel);
            }

        }

        private void DetailViewHeading_UpButtonClick(object sender, EventArgs e)
        {
            var index = SelectedNamespace.LastIndexOf('.');
            if (index == -1)
                return;

            var ns = SelectedNamespace.Substring(0, index);
            App.GotoNamespaces(ns);
        }
    }
}
