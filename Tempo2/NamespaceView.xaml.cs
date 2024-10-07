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
using System.Reflection.Metadata;

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
            //if (string.IsNullOrEmpty(parameter as string))
            //    return;

            var selectedNamespace = parameter as string;

            Initialize(selectedNamespace);
        }


        protected override object OnSuspending()
        {
            if (_listView.ItemsPanelRoot == null)
            {
                return null;
            }

            var relativeScrollPosition
                = ListViewPersistenceHelper.GetRelativeScrollPosition(
                _listView,
                (item) =>
                {
                    var group = item as ItemsGroup;
                    if (group != null)
                        return group.Key;

                    return item.ToString();
                });

            return relativeScrollPosition;
        }


        protected override void OnReactivated(object parameter, object state)
        {
            var selectedNamespace = parameter as string;

            Initialize(selectedNamespace);

            if (string.IsNullOrEmpty(state as string))
            {
                return;
            }

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



        public List<object> NamespacesAndTypes
        {
            get { return (List<object>)GetValue(NamespacesAndTypesProperty); }
            set { SetValue(NamespacesAndTypesProperty, value); }
        }
        public static readonly DependencyProperty NamespacesAndTypesProperty =
            DependencyProperty.Register("NamespacesAndTypes", typeof(List<object>), typeof(NamespaceView), new PropertyMetadata(null));




        public List<string> Namespaces
        {
            get { return (List<string>)GetValue(NamespacesProperty); }
            set { SetValue(NamespacesProperty, value); }
        }
        public static readonly DependencyProperty NamespacesProperty =
            DependencyProperty.Register("Namespaces", typeof(List<string>), typeof(NamespaceView), new PropertyMetadata(null));



        public List<TypeViewModel> Types
        {
            get { return (List<TypeViewModel>)GetValue(TypesProperty); }
            set { SetValue(TypesProperty, value); }
        }
        public static readonly DependencyProperty TypesProperty =
            DependencyProperty.Register("Types", typeof(List<TypeViewModel>), typeof(NamespaceView), new PropertyMetadata(null));



        public string SelectedNamespace
        {
            get { return (string)GetValue(SelectedNamespaceProperty); }
            set { SetValue(SelectedNamespaceProperty, value); }
        }
        public static readonly DependencyProperty SelectedNamespaceProperty =
            DependencyProperty.Register("SelectedNamespace", typeof(string), typeof(NamespaceView), new PropertyMetadata(null));


        private async void Initialize(string selectedNamespace)
        {
            // The namespace we're to show
            SelectedNamespace = selectedNamespace;

            // The SelectedNamespace but with a dot added.
            // This allows us to pick namespace Foo.Bar without getting confused by a namespace named Foo.Barbell
            var selectedNamespaceDot = selectedNamespace + ".";


            var selectedNamespacePartCount = 0;
            if (selectedNamespace != "")
            {
                selectedNamespacePartCount = selectedNamespaceDot.Split('.').Length - 1;
            }

            UpButtonVisibility = SelectedNamespace == "" ? Visibility.Collapsed : Visibility.Visible;

            var shouldContinue = await App.EnsureApiScopeLoadedAsync();
            if (!shouldContinue)
            {
                return;
            }

            // bugbug: async
            // bugbug: Should combine this with desktop Types2Namespaces.  But moving NamespaceTreeNode into
            // Common breaks the build for NamespaceViewer.xaml; for some reason the Xaml compiler refuses to recognize
            // a type in that project.
            var fullNamespaces = Manager.CurrentTypeSet.FullNamespaces;


            // Find the child namespaces of the selected namespace.
            // For example, if we're looking for "Foo", find "Foo.Bar" and "Foo.Baz",
            // but not "Foo.Bar.Bop"

            Namespaces = new List<string>();
            foreach (var fullNamespace in fullNamespaces)
            {
                if (selectedNamespace != "" && !fullNamespace.StartsWith(selectedNamespaceDot))
                {
                    // This isn't in the descendency of the selected namespace
                    continue;
                }

                // This is under the namespace we're looking for.
                // Say we're looking for children of "Foo". This will find
                // "Foo.Bar" and "Foo.Bar.Bop". Either way, we'll keep "Bar"
                // in `childNamespaceNodes`
                var parts = fullNamespace.Split('.');
                var leafNamespaceNode = parts[selectedNamespacePartCount];

                if (!Namespaces.Contains(leafNamespaceNode))
                {
                    Namespaces.Add(leafNamespaceNode);
                }

            }

            // Find the types in the selected namespace
            Types = (from t in Manager.CurrentTypeSet.Types
                     where t.Namespace == SelectedNamespace
                     //where t.IsPublic && !t.ShouldIgnore
                     where Manager.TypeIsPublicVolatile(t)
                     select t
                     ).ToList();


            // Make a list of items groups:
            //    "Namespaces", namespace list
            //    "Types", types list

            var namespacesGroup = new ItemsGroup(Namespaces.ToList())
            {
                Key = "Namespaces"
            };
            var typesGroup = new ItemsGroup(Types)
            {
                Key = "Types"
            };

            var namespacesAndTypes = new List<object>();
            if (namespacesGroup.Count != 0)
            {
                namespacesAndTypes.Add(namespacesGroup);
            }
            if (typesGroup.Count != 0)
            {
                namespacesAndTypes.Add(typesGroup);
            }

            // Display the grouped items
            NamespacesAndTypes = namespacesAndTypes;
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if(e.ClickedItem is string)
            {
                var childNamespace = e.ClickedItem as string;
                if( SelectedNamespace != "")
                {
                    childNamespace = $"{SelectedNamespace}.{childNamespace}";
                }
                App.GotoNamespaces(childNamespace);
            }
            else
            {
                Debug.Assert(e.ClickedItem is TypeViewModel);
                App.Navigate(e.ClickedItem as TypeViewModel);  
            }

        }

        private void DetailViewHeading_UpButtonClick(object sender, EventArgs e)
        {
            if (SelectedNamespace == "")
            {
                return;
            }

            string ns;
            var index = SelectedNamespace.LastIndexOf('.');
            if (index == -1)
            {
                ns = "";
            }
            else
            {
                ns = SelectedNamespace.Substring(0, index);
            }

            App.GotoNamespaces(ns);
        }
    }
}
