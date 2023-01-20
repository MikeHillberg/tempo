using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace Tempo
{
    /// <summary>
    /// A custom Grid that displays a string array as name/value pairs.
    /// Not generally useful, hard-coded to this app.
    /// When using this, don't set ColumnDefinitions or RowDefinitions
    /// </summary>
    public class NameValuePanel : Grid
    {
        public NameValuePanel()
        {
            // Initialize to two columns
            ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
        }

        /// <summary>
        /// Style for cells in the first column
        /// </summary>
        public Style FirstStyle
        {
            get { return (Style)GetValue(FirstStyleProperty); }
            set { SetValue(FirstStyleProperty, value); }
        }
        public static readonly DependencyProperty FirstStyleProperty =
            DependencyProperty.Register("FirstStyle", typeof(Style), typeof(NameValuePanel),
                new PropertyMetadata(null, (d, dp) => (d as NameValuePanel).InvalidateMeasure()));

        /// <summary>
        /// Style for cells in the second column
        /// </summary>
        public Style SecondStyle
        {
            get { return (Style)GetValue(SecondStyleProperty); }
            set { SetValue(SecondStyleProperty, value); }
        }
        public static readonly DependencyProperty SecondStyleProperty =
            DependencyProperty.Register("SecondStyle", typeof(Style), typeof(NameValuePanel),
                new PropertyMetadata(null, (d, __) => (d as NameValuePanel).InvalidateMeasure()));


        SolidColorBrush _grayBrush = new SolidColorBrush(Colors.LightGray);

        /// <summary>
        /// During measure, set the Grid attached properties and RowDefinitions on the Children
        /// </summary>
        protected override Size MeasureOverride(Size availableSize)
        {
            for (int i = 0; i < Children.Count; i++)
            {
                // Harcoded to ContentPresenter
                var child = Children[i] as ContentPresenter;
                if (child == null)
                {
                    continue;
                }

                // Set the row if it's not already set
                var row = i / 2;
                var childRow = Grid.GetRow(child);
                if (childRow != row)
                {
                    Grid.SetRow(child, row);
                }

                // Set even rows to have a gray background
                if ((row % 2) == 0)
                {
                    child.Background = _grayBrush;
                }

                // Set the column if not already set
                var childColumn = Grid.GetColumn(child);
                var column = i % 2;
                if (column != childColumn)
                {
                    Grid.SetColumn(child, column);
                }

                // Set the styles
                if (column == 0 && child.Style != FirstStyle)
                {
                    child.Style = FirstStyle;
                }
                else if (column == 1 && child.Style != SecondStyle)
                {
                    child.Style = SecondStyle;
                }
            }

            // Create all the RowDefinitions we need
            var rowDefinitionCount = RowDefinitions.Count;
            var rowCount = (Children.Count + 1) / 2;
            if (rowDefinitionCount != rowCount)
            {
                RowDefinitions.Clear();
                for (int i = 0; i < rowCount; i++)
                {
                    RowDefinitions.Add(new RowDefinition());
                }
            }

            return base.MeasureOverride(availableSize);
        }
    }
}
