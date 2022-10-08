using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Tempo
{
    interface ICanBeEmpty
    {
        public bool IsEmpty { get; }
        public event EventHandler<EventArgs> Changed;
    }


    public static class CollapseIfEmpty
    {
        public static object GetSource(FrameworkElement obj)
        {
            return (object)obj.GetValue(SourceProperty);
        }

        public static void SetSource(FrameworkElement obj, object value)
        {
            obj.SetValue(SourceProperty, value);
        }
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.RegisterAttached("Source", typeof(object), typeof(CollapseIfEmpty), 
                new PropertyMetadata(null, (s,e) => SourceChanged(s as FrameworkElement)));

        static void SourceChanged(FrameworkElement fe)
        {
            var source = GetSource(fe);

            var empty = false;

            if (source == null)
                empty = true;
            else if (source is IList && (source as IList).Count == 0)
                empty = true;
            else if (source is string && string.IsNullOrEmpty(source as string))
                empty = true;

            if (empty)
                fe.Visibility = Visibility.Collapsed;
            else
                fe.Visibility = Visibility.Visible;
        }



        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }
        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(CollapseIfEmpty), 
                new PropertyMetadata(false, (s,e) => IsEnabledChanged(s as FrameworkElement)));

        private static void IsEnabledChanged(FrameworkElement frameworkElement, bool isUpdate = false)
        {
            // bugbug: handle false case for this and IsEnabledFor

            var empty = IsEmpty(frameworkElement, out var changeProperty, out var canBeEmptyObject);

            if (empty)
                frameworkElement.Visibility = Visibility.Collapsed;
            else
                frameworkElement.Visibility = Visibility.Visible;

            if (!isUpdate)
            {
                // bugbug:  Never unregisters
                frameworkElement.RegisterPropertyChangedCallback(changeProperty, PropertyChanged);
            }

        }

        // Check if the value is "empty", for whatever definition of empty is appropriate to the type
        private static bool IsEmpty(Object value, out DependencyProperty changeProperty, out ICanBeEmpty canBeEmptyObject)
        {
            var isEmpty = false;
            changeProperty = null;
            canBeEmptyObject = null;

            if(value == null)
            {
                isEmpty = true;
            }
            else if(value is string)
            {
                isEmpty = string.IsNullOrEmpty(value as string);
            }
            else if(value is ICanBeEmpty)
            {
                canBeEmptyObject = value as ICanBeEmpty;
                isEmpty = canBeEmptyObject.IsEmpty;
            }
            else if (value is TextBlock)
            {
                var textBlock = value as TextBlock;
                changeProperty = TextBlock.TextProperty;

                if (string.IsNullOrEmpty(textBlock.Text))
                {
                    isEmpty = true;
                }
            }
            else if (value is ItemsControl)
            {
                var ic = value as ItemsControl;
                changeProperty = ItemsControl.ItemsSourceProperty;


                if (ic.ItemsSource == null)
                {
                    isEmpty = true;
                }
                else
                {
                    var source = ic.ItemsSource;
                    if (source is IEnumerable)
                    {
                        isEmpty = true;
                        foreach (var item in source as IEnumerable)
                        {
                            isEmpty = false;
                            break;
                        }
                    }
                }
            }

            return isEmpty;
        }

        public static object GetIsEnabledFor(DependencyObject obj)
        {
            return obj.GetValue(IsEnabledForProperty);
        }

        public static void SetIsEnabledFor(DependencyObject obj, Object value)
        {
            obj.SetValue(IsEnabledForProperty, value);
        }
        public static readonly DependencyProperty IsEnabledForProperty =
            DependencyProperty.RegisterAttached("IsEnabledFor", typeof(Object), typeof(CollapseIfEmpty), 
                new PropertyMetadata(null, (s,e) => IsEnabledForChanged(s as FrameworkElement)));

        private static void IsEnabledForChanged(FrameworkElement frameworkElement, bool isUpdate = false)
        {
            // bugbug: Consolidate this more with IsEnabledChanged

            var target = GetIsEnabledFor(frameworkElement);
            var empty = IsEmpty(target, out var changeProperty, out var canBeEmptyObject);

            if (empty)
                frameworkElement.Visibility = Visibility.Collapsed;
            else
                frameworkElement.Visibility = Visibility.Visible;

            if (!isUpdate)
            {
                if ((target is FrameworkElement) && changeProperty != null)
                {
                    // bugbug:  Never unregisters
                    (target as FrameworkElement).RegisterPropertyChangedCallback(changeProperty, (s, dp) => TargetPropertyChanged(frameworkElement));
                }
                else if (canBeEmptyObject != null)
                {
                    canBeEmptyObject.Changed += (s, e) => TargetPropertyChanged(frameworkElement);
                }
            }
        }

        //private static void TargetPropertyChanged(DependencyObject sender, DependencyProperty dp)
        private static void TargetPropertyChanged(FrameworkElement host)
        {
            IsEnabledForChanged(host, true);
        }

        private static void PropertyChanged(DependencyObject sender, DependencyProperty dp)
        {
            IsEnabledChanged(sender as FrameworkElement, true);
        }


    }
}
