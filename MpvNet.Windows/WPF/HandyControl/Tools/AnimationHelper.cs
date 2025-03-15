using System.Windows;
using System.Windows.Media.Animation;

namespace MpvNet.Windows.WPF.HandyControl.Tools
{
    public class AnimationHelper
    {
        public static DoubleAnimation CreateAnimation(double toValue, double milliseconds = 200)
        {
            return new DoubleAnimation(toValue, new Duration(TimeSpan.FromMilliseconds(milliseconds)))
            {
                EasingFunction = new PowerEase { EasingMode = EasingMode.EaseInOut }
            };
        }
    }
}
