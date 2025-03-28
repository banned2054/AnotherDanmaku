using HandyControl.Tools;
using HandyControl.Tools.Interop;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace MpvNet.Windows.WPF.HandyControl.Controls.Attach
{
    public class MenuTopLineAttach
    {
        public static readonly DependencyProperty PopupProperty = DependencyProperty.RegisterAttached(
             "Popup", typeof(Popup), typeof(MenuTopLineAttach), new PropertyMetadata(default(Popup), OnPopupChanged));

        private static void OnPopupChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var topLine = (FrameworkElement)d;

            if (e.NewValue is not Popup popup) return;
            if (popup.TemplatedParent is not MenuItem menuItem) return;
            SetTopLine(menuItem, topLine);
            menuItem.Loaded += MenuItem_Loaded;
        }

        private static void MenuItem_Loaded(object sender, RoutedEventArgs e)
        {
            var menuItem = (FrameworkElement)sender;
            menuItem.Unloaded += MenuItem_Unloaded;
            var topLine = GetTopLine(menuItem);
            var popup   = GetPopup(topLine);
            popup.Opened += Popup_Opened!;
        }

        private static void MenuItem_Unloaded(object sender, RoutedEventArgs e)
        {
            var menuItem = (FrameworkElement)sender;
            menuItem.Unloaded -= MenuItem_Unloaded;
            var topLine = GetTopLine(menuItem);
            var popup   = GetPopup(topLine);
            popup.Opened -= Popup_Opened!;
        }

        private static void Popup_Opened(object sender, EventArgs e)
        {
            var popup = (Popup)sender;
            if (popup.TemplatedParent is not MenuItem menuItem) return;
            var topLine = GetTopLine(menuItem);

            topLine.HorizontalAlignment = HorizontalAlignment.Left;
            topLine.Width               = menuItem.ActualWidth;
            topLine.Margin              = new Thickness();

            var positionLeftTop = menuItem.PointToScreen(new Point());
            var positionRightBottom =
                menuItem.PointToScreen(new Point(menuItem.ActualWidth, menuItem.ActualHeight));
            ScreenHelper.FindMonitorRectsFromPoint(InteropMethods.GetCursorPos(), out var _,
                                                   out var workAreaRect);
            var panel = VisualHelper.GetParent<Panel>(topLine);

            if (positionLeftTop.X < 0)
            {
                topLine.Margin = new Thickness(positionLeftTop.X - panel.Margin.Left, 0, 0, 0);
            }
            else if (positionLeftTop.X + panel.ActualWidth > workAreaRect.Right)
            {
                var overflowWidth = positionRightBottom.X - workAreaRect.Right;
                if (overflowWidth > 0)
                {
                    topLine.Width -= overflowWidth + panel.Margin.Right;
                }

                topLine.HorizontalAlignment = HorizontalAlignment.Left;
                topLine.Margin =
                    new Thickness(positionLeftTop.X + panel.ActualWidth - workAreaRect.Right + panel.Margin.Right,
                                  0, 0, 0);
            }

            if (!(positionRightBottom.Y > workAreaRect.Bottom)) return;
            topLine.Width               = 0;
            topLine.HorizontalAlignment = HorizontalAlignment.Stretch;
            topLine.Margin              = new Thickness();
        }

        public static void SetPopup(DependencyObject element, Popup value)
            => element.SetValue(PopupProperty, value);

        public static Popup GetPopup(DependencyObject element)
            => (Popup)element.GetValue(PopupProperty);

        internal static readonly DependencyProperty TopLineProperty = DependencyProperty.RegisterAttached(
             "TopLine", typeof(FrameworkElement), typeof(MenuTopLineAttach),
             new PropertyMetadata(default(FrameworkElement)));

        internal static void SetTopLine(DependencyObject element, FrameworkElement value)
            => element.SetValue(TopLineProperty, value);

        internal static FrameworkElement GetTopLine(DependencyObject element)
            => (FrameworkElement)element.GetValue(TopLineProperty);
    }
}
