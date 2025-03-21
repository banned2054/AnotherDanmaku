using HandyControl.Data;
using HandyControl.Tools;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MpvNet.Windows.WPF.HandyControl.Controls
{
    public class ScrollViewer : System.Windows.Controls.ScrollViewer
    {
        private double _totalVerticalOffset;

        private double _totalHorizontalOffset;

        private bool _isRunning;

        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
             "Orientation", typeof(Orientation), typeof(ScrollViewer), new PropertyMetadata(Orientation.Vertical));

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        public static readonly DependencyProperty CanMouseWheelProperty = DependencyProperty.Register(
             "CanMouseWheel", typeof(bool), typeof(ScrollViewer), new PropertyMetadata(ValueBoxes.TrueBox));

        public bool CanMouseWheel
        {
            get => (bool)GetValue(CanMouseWheelProperty);
            set => SetValue(CanMouseWheelProperty, ValueBoxes.BooleanBox(value));
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (!CanMouseWheel) return;

            if (!IsInertiaEnabled)
            {
                if (Orientation == Orientation.Vertical)
                {
                    base.OnMouseWheel(e);
                }
                else
                {
                    _totalHorizontalOffset  = HorizontalOffset;
                    CurrentHorizontalOffset = HorizontalOffset;
                    _totalHorizontalOffset  = Math.Min(Math.Max(0, _totalHorizontalOffset - e.Delta), ScrollableWidth);
                    CurrentHorizontalOffset = _totalHorizontalOffset;
                }

                return;
            }

            e.Handled = true;

            if (Orientation == Orientation.Vertical)
            {
                if (!_isRunning)
                {
                    _totalVerticalOffset  = VerticalOffset;
                    CurrentVerticalOffset = VerticalOffset;
                }

                _totalVerticalOffset = Math.Min(Math.Max(0, _totalVerticalOffset - e.Delta), ScrollableHeight);
                ScrollToVerticalOffsetWithAnimation(_totalVerticalOffset);
            }
            else
            {
                if (!_isRunning)
                {
                    _totalHorizontalOffset  = HorizontalOffset;
                    CurrentHorizontalOffset = HorizontalOffset;
                }

                _totalHorizontalOffset = Math.Min(Math.Max(0, _totalHorizontalOffset - e.Delta), ScrollableWidth);
                ScrollToHorizontalOffsetWithAnimation(_totalHorizontalOffset);
            }
        }

        internal void ScrollToTopInternal(double milliseconds = 500)
        {
            if (!_isRunning)
            {
                _totalVerticalOffset  = VerticalOffset;
                CurrentVerticalOffset = VerticalOffset;
            }

            ScrollToVerticalOffsetWithAnimation(0, milliseconds);
        }

        public void ScrollToVerticalOffsetWithAnimation(double offset, double milliseconds = 500)
        {
            var animation = AnimationHelper.CreateAnimation(offset, milliseconds);
            animation.EasingFunction = new CubicEase
            {
                EasingMode = EasingMode.EaseOut
            };
            animation.FillBehavior = FillBehavior.Stop;
            animation.Completed += (s, e1) =>
            {
                CurrentVerticalOffset = offset;
                _isRunning            = false;
            };
            _isRunning = true;

            BeginAnimation(CurrentVerticalOffsetProperty, animation, HandoffBehavior.Compose);
        }

        public void ScrollToHorizontalOffsetWithAnimation(double offset, double milliseconds = 500)
        {
            var animation = AnimationHelper.CreateAnimation(offset, milliseconds);
            animation.EasingFunction = new CubicEase
            {
                EasingMode = EasingMode.EaseOut
            };
            animation.FillBehavior = FillBehavior.Stop;
            animation.Completed += (s, e1) =>
            {
                CurrentHorizontalOffset = offset;
                _isRunning              = false;
            };
            _isRunning = true;

            BeginAnimation(CurrentHorizontalOffsetProperty, animation, HandoffBehavior.Compose);
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters) =>
            (IsPenetrating ? null : base.HitTestCore(hitTestParameters))!;

        public static readonly DependencyProperty IsInertiaEnabledProperty = DependencyProperty.RegisterAttached(
             "IsInertiaEnabled", typeof(bool), typeof(ScrollViewer), new PropertyMetadata(ValueBoxes.FalseBox));

        public static void SetIsInertiaEnabled(DependencyObject element, bool value) =>
            element.SetValue(IsInertiaEnabledProperty, ValueBoxes.BooleanBox(value));

        public static bool GetIsInertiaEnabled(DependencyObject element) =>
            (bool)element.GetValue(IsInertiaEnabledProperty);

        public bool IsInertiaEnabled
        {
            get => (bool)GetValue(IsInertiaEnabledProperty);
            set => SetValue(IsInertiaEnabledProperty, ValueBoxes.BooleanBox(value));
        }

        public static readonly DependencyProperty IsPenetratingProperty = DependencyProperty.RegisterAttached(
             "IsPenetrating", typeof(bool), typeof(ScrollViewer), new PropertyMetadata(ValueBoxes.FalseBox));

        public bool IsPenetrating
        {
            get => (bool)GetValue(IsPenetratingProperty);
            set => SetValue(IsPenetratingProperty, ValueBoxes.BooleanBox(value));
        }

        public static void SetIsPenetrating(DependencyObject element, bool value) =>
            element.SetValue(IsPenetratingProperty, ValueBoxes.BooleanBox(value));

        public static bool GetIsPenetrating(DependencyObject element) => (bool)element.GetValue(IsPenetratingProperty);

        internal static readonly DependencyProperty CurrentVerticalOffsetProperty = DependencyProperty.Register(
             "CurrentVerticalOffset", typeof(double), typeof(ScrollViewer),
             new PropertyMetadata(ValueBoxes.Double0Box, OnCurrentVerticalOffsetChanged));

        private static void OnCurrentVerticalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer ctl && e.NewValue is double v)
            {
                ctl.ScrollToVerticalOffset(v);
            }
        }

        internal double CurrentVerticalOffset
        {
            get => (double)GetValue(CurrentVerticalOffsetProperty);
            set => SetValue(CurrentVerticalOffsetProperty, value);
        }

        internal static readonly DependencyProperty CurrentHorizontalOffsetProperty = DependencyProperty.Register(
             "CurrentHorizontalOffset", typeof(double), typeof(ScrollViewer),
             new PropertyMetadata(ValueBoxes.Double0Box, OnCurrentHorizontalOffsetChanged));

        private static void OnCurrentHorizontalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer ctl && e.NewValue is double v)
            {
                ctl.ScrollToHorizontalOffset(v);
            }
        }

        internal double CurrentHorizontalOffset
        {
            get => (double)GetValue(CurrentHorizontalOffsetProperty);
            set => SetValue(CurrentHorizontalOffsetProperty, value);
        }
    }
}
