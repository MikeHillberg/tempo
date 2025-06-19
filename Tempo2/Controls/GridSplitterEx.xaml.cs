using System;
using System.Diagnostics;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Tempo.Controls
{
    /// <summary>
    /// A reusable grid splitter control that can resize Grid rows or columns.
    /// Supports both horizontal and vertical orientations with configurable target properties.
    /// </summary>
    public sealed partial class GridSplitterEx : UserControl
    {
        private InputSystemCursor _horizontalCursor = null;
        private InputSystemCursor _verticalCursor = null;
        private double _pointerOffset = 0;
        private bool _isActive = false;

        public GridSplitterEx()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Specifies whether this splitter resizes rows or columns
        /// </summary>
        public GridSplitterOrientation Orientation
        {
            get { return (GridSplitterOrientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register("Orientation", typeof(GridSplitterOrientation), typeof(GridSplitterEx),
                new PropertyMetadata(GridSplitterOrientation.Vertical));

        /// <summary>
        /// The target Grid element to manipulate when resizing
        /// </summary>
        public Grid TargetGrid
        {
            get { return (Grid)GetValue(TargetGridProperty); }
            set { SetValue(TargetGridProperty, value); }
        }
        public static readonly DependencyProperty TargetGridProperty =
            DependencyProperty.Register("TargetGrid", typeof(Grid), typeof(GridSplitterEx), new PropertyMetadata(null));

        /// <summary>
        /// The index of the row or column to resize (0-based)
        /// </summary>
        public int TargetIndex
        {
            get { return (int)GetValue(TargetIndexProperty); }
            set { SetValue(TargetIndexProperty, value); }
        }
        public static readonly DependencyProperty TargetIndexProperty =
            DependencyProperty.Register("TargetIndex", typeof(int), typeof(GridSplitterEx), new PropertyMetadata(0));

        /// <summary>
        /// The dependency property to update when resizing (for databinding scenarios)
        /// </summary>
        public DependencyProperty TargetSizeProperty
        {
            get { return (DependencyProperty)GetValue(TargetSizePropertyProperty); }
            set { SetValue(TargetSizePropertyProperty, value); }
        }
        public static readonly DependencyProperty TargetSizePropertyProperty =
            DependencyProperty.Register("TargetSizeProperty", typeof(DependencyProperty), typeof(GridSplitterEx), new PropertyMetadata(null));

        /// <summary>
        /// The target object that owns the TargetSizeProperty (usually the parent control)
        /// </summary>
        public DependencyObject TargetSizeObject
        {
            get { return (DependencyObject)GetValue(TargetSizeObjectProperty); }
            set { SetValue(TargetSizeObjectProperty, value); }
        }
        public static readonly DependencyProperty TargetSizeObjectProperty =
            DependencyProperty.Register("TargetSizeObject", typeof(DependencyObject), typeof(GridSplitterEx), new PropertyMetadata(null));

        private void Splitter_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            // Change cursor based on orientation
            if (Orientation == GridSplitterOrientation.Horizontal)
            {
                if (_horizontalCursor == null)
                {
                    _horizontalCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth);
                }
                ProtectedCursor = _horizontalCursor;
            }
            else
            {
                if (_verticalCursor == null)
                {
                    _verticalCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
                }
                ProtectedCursor = _verticalCursor;
            }
        }

        private void Splitter_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (!_isActive)
            {
                ProtectedCursor = null;
            }
        }

        private void Splitter_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // Capture the pointer for dragging
            if (!_splitterRect.CapturePointer(e.Pointer))
            {
                return;
            }

            _isActive = true;

            // Calculate the initial offset for accurate dragging
            var currentPosition = e.GetCurrentPoint(this).Position;
            
            if (Orientation == GridSplitterOrientation.Horizontal)
            {
                // For horizontal splitter, calculate offset relative to the target row's bottom edge
                if (TargetGrid != null && TargetIndex < TargetGrid.RowDefinitions.Count)
                {
                    var targetRowHeight = TargetGrid.RowDefinitions[TargetIndex].ActualHeight;
                    var gridHeight = TargetGrid.ActualHeight;
                    _pointerOffset = currentPosition.Y - (gridHeight - targetRowHeight);
                }
                else
                {
                    _pointerOffset = 0;
                }
            }
            else
            {
                // For vertical splitter, calculate offset relative to the target column's right edge
                if (TargetGrid != null && TargetIndex < TargetGrid.ColumnDefinitions.Count)
                {
                    var targetColumnWidth = TargetGrid.ColumnDefinitions[TargetIndex].ActualWidth;
                    _pointerOffset = currentPosition.X - targetColumnWidth;
                }
                else
                {
                    _pointerOffset = 0;
                }
            }
        }

        private void Splitter_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isActive)
            {
                return;
            }

            var currentPosition = e.GetCurrentPoint(this).Position;

            if (Orientation == GridSplitterOrientation.Horizontal)
            {
                // Resize row height
                var newHeight = TargetGrid.ActualHeight - currentPosition.Y + _pointerOffset;
                newHeight = Math.Max(0, newHeight); // Ensure non-negative

                UpdateTargetSize(new GridLength(newHeight));
            }
            else
            {
                // Resize column width  
                var newWidth = currentPosition.X - _pointerOffset;
                newWidth = Math.Max(0, newWidth); // Ensure non-negative

                UpdateTargetSize(new GridLength(newWidth));
            }
        }

        private void Splitter_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (!_isActive)
            {
                return;
            }

            _splitterRect.ReleasePointerCapture(e.Pointer);
            _isActive = false;
            ProtectedCursor = null;
        }

        /// <summary>
        /// Updates the target size using either direct Grid manipulation or dependency property binding
        /// </summary>
        private void UpdateTargetSize(GridLength newSize)
        {
            // First try to update via dependency property binding (preferred for MVVM scenarios)
            if (TargetSizeProperty != null && TargetSizeObject != null)
            {
                TargetSizeObject.SetValue(TargetSizeProperty, newSize);
                return;
            }

            // Fallback to direct Grid manipulation
            if (TargetGrid != null)
            {
                if (Orientation == GridSplitterOrientation.Horizontal && TargetIndex < TargetGrid.RowDefinitions.Count)
                {
                    TargetGrid.RowDefinitions[TargetIndex].Height = newSize;
                }
                else if (Orientation == GridSplitterOrientation.Vertical && TargetIndex < TargetGrid.ColumnDefinitions.Count)
                {
                    TargetGrid.ColumnDefinitions[TargetIndex].Width = newSize;
                }
            }
        }
    }

    /// <summary>
    /// Specifies the orientation of the GridSplitterEx control
    /// </summary>
    public enum GridSplitterOrientation
    {
        /// <summary>
        /// Vertical splitter that resizes columns (default)
        /// </summary>
        Vertical,

        /// <summary>
        /// Horizontal splitter that resizes rows
        /// </summary>
        Horizontal
    }
}