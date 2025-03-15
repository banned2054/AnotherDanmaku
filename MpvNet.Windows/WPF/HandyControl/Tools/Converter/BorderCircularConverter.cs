using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MpvNet.Windows.WPF.HandyControl.Tools.Converter
{
    public class BorderCircularConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values is not [double width, double height])
                return DependencyProperty.UnsetValue;
            if (width < double.Epsilon || height < double.Epsilon)
            {
                return new CornerRadius();
            }

            var min = Math.Min(width, height);
            return new CornerRadius(min / 2);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
