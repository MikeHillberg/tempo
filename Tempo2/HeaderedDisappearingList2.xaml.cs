using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Tempo
{
    /// <summary>
    /// Interaction logic for HeaderedDisappearingList.xaml
    /// </summary>
    public partial class HeaderedDisappearingList2 : UserControl
    {
        public HeaderedDisappearingList2()
        {
            InitializeComponent();
        }



        public string Header
        {
            get { return (string)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register("Header", typeof(string), typeof(HeaderedDisappearingList2), new PropertyMetadata(null));



        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(HeaderedDisappearingList2), 
            new PropertyMetadata(null, ItemsSourceChanged));
        static void ItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // If the list is null or empty, then don't take up any layout space

            var This = d as HeaderedDisappearingList2;
            var newList = e.NewValue as IEnumerable;
            if (newList == null || newList.Cast<object>().FirstOrDefault() == null)
            {
                This._grid.Visibility = Visibility.Collapsed;
            }
            else
            {
                This._grid.Visibility = Visibility.Visible;
            }
        }
    }
}
