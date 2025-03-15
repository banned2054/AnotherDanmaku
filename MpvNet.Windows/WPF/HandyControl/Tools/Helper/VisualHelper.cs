using System.Windows;
using System.Windows.Media;

namespace MpvNet.Windows.WPF.HandyControl.Tools.Helper
{
    public static class VisualHelper
    {
        public static T? GetChild<T>(DependencyObject d) where T : DependencyObject
        {
            switch (d)
            {
                case null :
                    return default;
                case T t :
                    return t;
            }

            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(d); i++)
            {
                var child = VisualTreeHelper.GetChild(d, i);

                var result = GetChild<T>(child);
                if (result != null) return result;
            }

            return default;
        }

        public static T GetParent<T>(DependencyObject d) where T : DependencyObject
        {
            return (d switch
            {
                null               => default,
                T dependencyObject => dependencyObject,
                Window             => null,
                _                  => GetParent<T>(VisualTreeHelper.GetParent(d)!)
            })!;
        }
    }
}
