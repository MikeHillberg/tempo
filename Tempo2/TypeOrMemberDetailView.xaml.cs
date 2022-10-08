using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Tempo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TypeOrMemberDetailView : Page
    {
        public TypeOrMemberDetailView()
        {
            this.InitializeComponent();

            IsTopLevelChanged();
        }

        public bool IsTopLevel
        {
            get { return (bool)GetValue(IsTopLevelProperty); }
            set { SetValue(IsTopLevelProperty, value); }
        }
        public static readonly DependencyProperty IsTopLevelProperty =
            DependencyProperty.Register("IsTopLevel", typeof(bool), typeof(TypeOrMemberDetailView), 
                new PropertyMetadata(false, (s,e) => (s as TypeOrMemberDetailView).IsTopLevelChanged()));

        void IsTopLevelChanged()
        {
        }

        public void NavigateToItem(object item)
        {
            // Keep track of the current item (for the go-to-docs link)
            App.CurrentItem = item as MemberViewModel;

            // Based on the member, update the detail views

            if (item is TypeViewModel)
            {
                _typeDetail.DoActivate(item as TypeViewModel);
                _detailsGrid.Children.Remove(_typeDetail);
                _detailsGrid.Children.Add(_typeDetail);
            }
            else if (item is PropertyViewModel)
            {
                _detailsGrid.Children.Remove(_propertyDetail);
                _detailsGrid.Children.Add(_propertyDetail);
                _propertyDetail.DoActivate(item as PropertyViewModel);
            }
            else if (item is MethodViewModel)
            {
                _methodDetail.DoActivate(item as MethodViewModel);
                _detailsGrid.Children.Remove(_methodDetail);
                _detailsGrid.Children.Add(_methodDetail);
            }
            else if (item is EventViewModel)
            {
                _eventDetail.DoActivate(item as EventViewModel);
                _detailsGrid.Children.Remove(_eventDetail);
                _detailsGrid.Children.Add(_eventDetail);
            }
            else if (item is ConstructorViewModel)
            {
                _constructorDetail.DoActivate(item as ConstructorViewModel);
                _detailsGrid.Children.Remove(_constructorDetail);
                _detailsGrid.Children.Add(_constructorDetail);
            }
            else if (item is FieldViewModel)
            {
                _fieldDetail.DoActivate(item as FieldViewModel);
                _detailsGrid.Children.Remove(_fieldDetail);
                _detailsGrid.Children.Add(_fieldDetail);
            }
            else
            {
                Debug.Assert(false);
            }

        }



    }
}
