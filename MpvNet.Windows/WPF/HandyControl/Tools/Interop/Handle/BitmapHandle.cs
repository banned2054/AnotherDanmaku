using HandyControl.Tools.Interop;
using System.Security;

namespace MpvNet.Windows.WPF.HandyControl.Tools.Interop.Handle
{
    internal sealed class BitmapHandle : WpfSafeHandle
    {
        [SecurityCritical]
        private BitmapHandle(bool ownsHandle) : base(ownsHandle, CommonHandles.GDI)
        {
        }

        public static BitmapHandle CreateInstance(bool ownsHandle)
        {
            return new BitmapHandle(ownsHandle);
        }

        [SecurityCritical]
        protected override bool ReleaseHandle()
        {
            return InteropMethods.DeleteObject(handle);
        }
    }
}
