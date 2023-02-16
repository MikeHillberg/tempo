using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Tempo
{
    /// <summary>
    /// Stack panel that keeps everything on axis uniform sized.
    /// So if horizontal, everything will be the same width
    /// </summary>
    public class UniformStackPanel : Panel
    {
        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register("Orientation", typeof(Orientation), typeof(UniformStackPanel), new PropertyMetadata(Orientation.Vertical));

        public double Spacing
        {
            get { return (double)GetValue(SpacingProperty); }
            set { SetValue(SpacingProperty, value); }
        }
        public static readonly DependencyProperty SpacingProperty =
            DependencyProperty.Register("Spacing", typeof(double), typeof(UniformStackPanel), new PropertyMetadata(0d));

        protected override Size MeasureOverride(Size availableSize)
        {
            double maxLengthOnAxis = 0;
            double maxLengthOffAxis = 0;

            var count = Children.Count;
            if(count == 0)
            {
                return new Size(0, 0);
            }

            availableSize = Orientation == Orientation.Horizontal
                            ? new Size(double.PositiveInfinity, availableSize.Height)
                            : new Size(availableSize.Width, double.PositiveInfinity);

            foreach (var child in Children)
            {
                // Get the child's desired size
                child.Measure(availableSize);

                // See if this is the new max for width or height
                if (Orientation == Orientation.Horizontal)
                {
                    maxLengthOnAxis = Math.Max(maxLengthOnAxis, child.DesiredSize.Width);
                    maxLengthOffAxis = Math.Max(maxLengthOffAxis, child.DesiredSize.Height);
                }
                else
                {
                    maxLengthOnAxis = Math.Max(maxLengthOnAxis, child.DesiredSize.Height);
                    maxLengthOffAxis = Math.Max(maxLengthOffAxis, child.DesiredSize.Width);
                }
            }

            // On axis it's the max length of the children plus the spacing
            // Of axis it's the max length
            return Orientation == Orientation.Horizontal
                    ? new Size(maxLengthOnAxis * count + Spacing * (count - 1),
                               maxLengthOffAxis)
                    : new Size(maxLengthOffAxis,
                               maxLengthOnAxis * count + Spacing * (count - 1));
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            bool first = true;

            var count = Children.Count;

            // Recalcuate what the length of the biggest child is on axis
            var maxLengthOnAxis = Orientation == Orientation.Horizontal
                                   ? this.DesiredSize.Width
                                   : this.DesiredSize.Height;
            maxLengthOnAxis -= Spacing * (count - 1);
            maxLengthOnAxis /= count;

            var offset = new Point(0, 0);
            foreach (var child in Children)
            {
                // Other than the first, add space for Spacing
                if (!first)
                {
                    if (Orientation == Orientation.Horizontal)
                    {
                        offset = new Point(offset.X + Spacing, offset.Y);
                    }
                    else
                    {
                        offset = new Point(offset.X, offset.Y + Spacing);
                    }
                }
                first = false;

                // Arrange the child to the new location
                Rect rect;
                if (Orientation == Orientation.Horizontal)
                {
                    rect = new Rect(offset.X, offset.Y,
                                    maxLengthOnAxis, finalSize.Height);
                    offset = new Point(offset.X + maxLengthOnAxis, offset.Y);
                }
                else
                {
                    rect = new Rect(offset.X, offset.Y,
                                    finalSize.Width, maxLengthOnAxis);
                    offset = new Point(offset.X, offset.Y + maxLengthOnAxis);
                }
                child.Arrange(rect);

            }

            return finalSize;
        }
    }
}
