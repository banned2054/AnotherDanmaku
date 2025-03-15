using System.Runtime.InteropServices;

namespace MpvNet.Windows.WPF.HandyControl.Tools.Interop
{
    public class InteropValues
    {
        internal static class ExternDll
        {
            public const string
                User32 = "user32.dll",
                Gdi32  = "gdi32.dll";
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct Point(int x, int y)
        {
            public int X = x;
            public int Y = y;
        }

        [Serializable, StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct Rect(int left, int top, int right, int bottom)
        {
            public int Left   = left;
            public int Top    = top;
            public int Right  = right;
            public int Bottom = bottom;

            public Rect(System.Windows.Rect rect) : this((int)rect.Left, (int)rect.Top, (int)rect.Right,
                                                         (int)rect.Bottom)
            {
            }

            public readonly System.Windows.Point Position => new(Left, Top);
            public readonly System.Windows.Size  Size     => new(Width, Height);

            public int Height
            {
                readonly get => Bottom - Top;
                set => Bottom = Top + value;
            }

            public int Width
            {
                readonly get => Right - Left;
                set => Right = Left + value;
            }
        }


        [StructLayout(LayoutKind.Sequential)]
        public class WindowPos
        {
            public int x;
            public int y;
            public int cy;
        }

        internal struct MonitorInfo
        {
            public uint cbSize;
            public Rect rcMonitor;
            public Rect rcWork;
        }
    }
}
