using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Diagnostics;

namespace Tempo
{
    /// <summary>
    /// Grid splitter control. Must be in the second row/column of a 2-row/column Grid,
    /// over that column's content, as a child of the Grid. It sizes that ColumnDef
    /// </summary>
    // (First pass of this was done by GitHub Copilot) 
    public sealed partial class GridSplitterEx : UserControl
    {
        // Cached north/south cursor
        private InputSystemCursor _horizontalCursor = null;

        // Cached east/west cursor
        private InputSystemCursor _verticalCursor = null;

        // Captured during pointer-down
        private double _pointerOffset = 0;
        private double _originalLength = 0;

        private bool _isActive = false;

        public GridSplitterEx()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Calculated value if this is horizontal, in the sense of a horizontal line
        /// (which means it's splitting two rows)
        /// </summary>
        bool IsHorizontal => this.ActualWidth > this.ActualHeight;

        /// <summary>
        /// This splitter should be a child of a Grid, get that Grid (or null if it's not)
        /// </summary>
        Grid GetParentGrid()
        {
            return this.Parent as Grid;
        }

        /// <summary>
        /// Change to a sizing cursor on pointer enter (depends on orientation)
        /// </summary>
        private void Splitter_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (IsHorizontal)
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

        /// <summary>
        /// Remove the sizing cursor on exit
        /// </summary>
        private void Splitter_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (!_isActive)
            {
                ProtectedCursor = null;
            }
        }

        /// <summary>
        /// On pointer down, activate splitting (resizing) mode
        /// </summary>
        private void Splitter_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var grid = GetParentGrid();
            if (grid == null)
            {
                return;
            }

            // Capture the pointer for dragging
            if (!_splitterRect.CapturePointer(e.Pointer))
            {
                return;
            }

            _isActive = true;

            // Figure out the current size and the current pointer position.
            // As the pointer moves we'll update that size
            var currentPosition = e.GetCurrentPoint(grid).Position;
            _pointerOffset = IsHorizontal ? currentPosition.Y : currentPosition.X;
            _originalLength = GetDefinitionAbsoluteLength();
        }

        /// <summary>
        /// When active, resize the column/row on pointer move
        /// </summary>
        private void Splitter_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isActive)
            {
                return;
            }

            var grid = GetParentGrid();
            if (grid == null)
            {
                return;
            }

            // Get the current location of the pointer so that we can calculate the delta from where we started
            var currentPosition = e.GetCurrentPoint(grid).Position;

            // Calculate the change in height/width
            var newAbsoluteLength = IsHorizontal ? currentPosition.Y : currentPosition.X;
            newAbsoluteLength = _originalLength - newAbsoluteLength + _pointerOffset;
            newAbsoluteLength = Math.Max(0, newAbsoluteLength); // Ensure non-negative

            // Update the row height or column width
            SetDefinitionAbsoluteLength(grid, newAbsoluteLength);
        }

        /// <summary>
        /// Exit out of splitting mode
        /// </summary>
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
        /// Get the current length (double) of the row or column definition that we're updating
        /// </summary>
        private double GetDefinitionAbsoluteLength()
        {
            var grid = GetParentGrid();
            if (grid == null)
            {
                return 0;
            }

            // Look at the attached property on this splitter to see what row/column it's in
            // Currently required to be the second
            int index = IsHorizontal ? Grid.GetRow(this) : Grid.GetColumn(this);
            Debug.Assert(index == 1);

            if (IsHorizontal)
            {
                var rowDef = grid.RowDefinitions[index];
                return rowDef?.ActualHeight ?? 0;
            }
            else
            {
                var columnDef = grid.ColumnDefinitions[index];
                return columnDef?.ActualWidth ?? 0;
            }
        }

        /// <summary>
        /// Set the current length (double) of the row or column definition that we're updating
        /// </summary>
        private void SetDefinitionAbsoluteLength(Grid grid, double newLength)
        {
            int index = IsHorizontal ? Grid.GetRow(this) : Grid.GetColumn(this);
            Debug.Assert(index == 1); // Currently required to be the second

            if (IsHorizontal)
            {
                var rowDef = grid.RowDefinitions[index];
                rowDef.Height = new GridLength(newLength);
            }
            else
            {
                var columnDef = grid.ColumnDefinitions[index];
                columnDef.Width = new GridLength(newLength);
            }
        }
    }
}