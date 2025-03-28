using System.Windows;
using System.Windows.Controls;

namespace MpvNet.Windows.WPF.HandyControl.Controls
{
    public class SimplePanel : Panel
    {
        protected override Size MeasureOverride(Size constraint)
        {
            var maxSize = new Size();

            foreach (UIElement child in InternalChildren)
            {
                if (child == null) continue;
                child.Measure(constraint);
                maxSize.Width  = Math.Max(maxSize.Width, child.DesiredSize.Width);
                maxSize.Height = Math.Max(maxSize.Height, child.DesiredSize.Height);
            }

            return maxSize;
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            foreach (UIElement child in InternalChildren)
            {
                child?.Arrange(new Rect(arrangeSize));
            }

            return arrangeSize;
        }
    }
}
