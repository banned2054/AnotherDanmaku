using HandyControl.Tools.Interop;
using MpvNet.Windows.WPF.HandyControl.Tools.Interop.Handle;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using HandleCollector = MpvNet.Windows.WPF.HandyControl.Tools.Interop.Handle.HandleCollector;

namespace MpvNet.Windows.WPF.HandyControl.Tools.Interop
{
    internal class InteropMethods
    {
        #region common

        [DllImport(InteropValues.ExternDll.User32, CharSet = CharSet.Auto)]
        internal static extern bool GetCursorPos(out InteropValues.POINT pt);

        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        [DllImport(InteropValues.ExternDll.User32, SetLastError = true, ExactSpelling = true,
                   EntryPoint = nameof(GetDc),
                   CharSet = CharSet.Auto)]
        internal static extern IntPtr IntGetDC(HandleRef hWnd);

        [SecurityCritical]
        internal static IntPtr GetDc(HandleRef hWnd)
        {
            var hDc = IntGetDC(hWnd);
            if (hDc == IntPtr.Zero) throw new Win32Exception();

            return HandleCollector.Add(hDc, CommonHandles.HDC);
        }

        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        [DllImport(InteropValues.ExternDll.User32, ExactSpelling = true, EntryPoint = nameof(ReleaseDc),
                   CharSet = CharSet.Auto)]
        internal static extern int IntReleaseDC(HandleRef hWnd, HandleRef hDc);

        [SecurityCritical]
        internal static int ReleaseDc(HandleRef hWnd, HandleRef hDc)
        {
            HandleCollector.Remove((IntPtr)hDc, CommonHandles.HDC);
            return IntReleaseDC(hWnd, hDc);
        }

        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        [DllImport(InteropValues.ExternDll.User32, EntryPoint = nameof(DestroyIcon), CharSet = CharSet.Auto,
                   SetLastError = true)]
        private static extern bool IntDestroyIcon(IntPtr hIcon);

        [SecurityCritical]
        internal static bool DestroyIcon(IntPtr hIcon)
        {
            var result = IntDestroyIcon(hIcon);
            return result;
        }

        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        [DllImport(InteropValues.ExternDll.Gdi32, EntryPoint = nameof(DeleteObject), CharSet = CharSet.Auto,
                   SetLastError = true)]
        private static extern bool IntDeleteObject(IntPtr hObject);

        [SecurityCritical]
        internal static bool DeleteObject(IntPtr hObject)
        {
            var result = IntDeleteObject(hObject);
            return result;
        }

        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        [DllImport(InteropValues.ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto,
                   EntryPoint = nameof(CreateBitmap))]
        private static extern BitmapHandle PrivateCreateBitmap(int    width, int height, int planes, int bitsPerPixel,
                                                               byte[] lpvBits);

        [SecurityCritical]
        internal static BitmapHandle CreateBitmap(int width, int height, int planes, int bitsPerPixel, byte[] lpvBits)
        {
            var hBitmap = PrivateCreateBitmap(width, height, planes, bitsPerPixel, lpvBits);
            return hBitmap;
        }

        [DllImport(InteropValues.ExternDll.User32, CharSet = CharSet.Auto)]
        internal static extern IntPtr GetDC(IntPtr ptr);

        [DllImport(InteropValues.ExternDll.User32)]
        internal static extern IntPtr MonitorFromPoint(InteropValues.POINT pt, int flags);

        internal static System.Windows.Point GetCursorPos()
        {
            var result = default(System.Windows.Point);
            if (!GetCursorPos(out var point)) return result;
            result.X = point.X;
            result.Y = point.Y;

            return result;
        }

        #endregion
    }
}
