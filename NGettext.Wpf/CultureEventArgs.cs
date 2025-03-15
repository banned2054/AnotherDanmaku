using System.Globalization;

namespace NGettext.Wpf
{
    public class CultureEventArgs(CultureInfo cultureInfo) : EventArgs
    {
        public CultureInfo CultureInfo { get; } = cultureInfo ?? throw new ArgumentNullException(nameof(cultureInfo));
    }
}
