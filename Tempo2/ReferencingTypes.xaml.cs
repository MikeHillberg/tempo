// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Tempo
{
    public sealed partial class ReferencingTypes : MySerializableControl
    {
        public ReferencingTypes()
        {
            this.InitializeComponent();
        }

        protected override void OnActivated(object parameter)
        {
            ReferencedType = parameter as TypeViewModel;
            Types = TypeReferenceHelper.FindReturnedByTypesClosure(ReferencedType);
        }

        protected override void OnReactivated(object parameter, object state)
        {
        }

        protected override object OnSuspending()
        {
            return null;
        }



        public TypeViewModel ReferencedType
        {
            get { return (TypeViewModel)GetValue(ReferencedTypeProperty); }
            set { SetValue(ReferencedTypeProperty, value); }
        }
        public static readonly DependencyProperty ReferencedTypeProperty =
            DependencyProperty.Register("ReferencedType", typeof(TypeViewModel), typeof(ReferencingTypes), new PropertyMetadata(null));



        public List<List<string>> Routes
        {
            get { return (List<List<string>>)GetValue(RoutesProperty); }
            set { SetValue(RoutesProperty, value); }
        }
        public static readonly DependencyProperty RoutesProperty =
            DependencyProperty.Register("Routes", typeof(List<List<string>>), typeof(ReferencingTypes), new PropertyMetadata(null));

        TypeViewModel _selectedItem;
        public TypeViewModel SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                _selectedItem = value;
                var routes = TypeReferenceHelper.FindPathsToType(value, ReferencedType);
                Routes = routes;
            }
        }



        public IList<TypeViewModel> Types
        {
            get { return (IList<TypeViewModel>)GetValue(TypesProperty); }
            set { SetValue(TypesProperty, value); }
        }
        public static readonly DependencyProperty TypesProperty =
            DependencyProperty.Register("Types", typeof(IList<TypeViewModel>), typeof(ReferencingTypes),
                new PropertyMetadata(0));

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
