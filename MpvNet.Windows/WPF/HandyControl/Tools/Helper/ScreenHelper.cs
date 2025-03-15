using System.Runtime.InteropServices;
using System.Windows;
using HandyControl.Tools.Interop;

namespace MpvNet.Windows.WPF.HandyControl.Tools.Helper
{
    internal class ScreenHelper
    {
        internal static void FindMonitorRectsFromPoint(Point point, out Rect monitorRect, out Rect workAreaRect)
        {
            var intPtr = InteropMethods.MonitorFromPoint(new InteropValues.POINT
            {
                X = (int)point.X,
                Y = (int)point.Y
            }, 2);

            monitorRect  = new Rect(0.0, 0.0, 0.0, 0.0);
            workAreaRect = new Rect(0.0, 0.0, 0.0, 0.0);

            if (intPtr == IntPtr.Zero) return;
            InteropValues.MONITORINFO monitorInfo = default;
            monitorInfo.cbSize = (uint)Marshal.SizeOf(typeof(InteropValues.MONITORINFO));
            InteropMethods.GetMonitorInfo(intPtr, ref monitorInfo);
            monitorRect  = new Rect(monitorInfo.rcMonitor.Position, monitorInfo.rcMonitor.Size);
            workAreaRect = new Rect(monitorInfo.rcWork.Position, monitorInfo.rcWork.Size);
        }
    }
}
