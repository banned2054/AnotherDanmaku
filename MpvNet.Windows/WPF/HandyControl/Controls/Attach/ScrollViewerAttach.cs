using HandyControl.Data;
using System.Windows;

namespace MpvNet.Windows.WPF.HandyControl.Controls.Attach
{
    public class ScrollViewerAttach
    {
        public static readonly DependencyProperty AutoHideProperty = DependencyProperty.RegisterAttached(
             "AutoHide", typeof(bool), typeof(ScrollViewerAttach),
             new FrameworkPropertyMetadata(ValueBoxes.TrueBox, FrameworkPropertyMetadataOptions.Inherits));

        public static void SetAutoHide(DependencyObject element, bool value)
            => element.SetValue(AutoHideProperty, value);

        public static bool GetAutoHide(DependencyObject element)
            => (bool)element.GetValue(AutoHideProperty);
    }
}
