using MpvNet.Windows.Help;
using MpvNet.Windows.Native;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MpvNet.Windows.WinForms;

public class SnapManager
{
    private int DragOffsetX { get; set; }
    private int DragOffsetY { get; set; }

    private IntPtr _handle;

    [Flags]
    public enum SnapLocation
    {
        None   = 0,
        Left   = 1 << 0,
        Top    = 1 << 1,
        Right  = 1 << 2,
        Bottom = 1 << 3,
        All    = Left | Top | Right | Bottom
    }

    public int AnchorDistance { get; set; }

    public int SnapDistance { get; set; }

    private bool InSnapRange(int a, int b) => Math.Abs(a - b) < SnapDistance;

    private void FindSnap(ref Rectangle effectiveBounds)
    {
        var currentScreen = Screen.FromPoint(effectiveBounds.Location);
        var workingArea   = WinApiHelp.GetWorkingArea(_handle, currentScreen.WorkingArea);

        if (InSnapRange(effectiveBounds.Left, workingArea.Left + AnchorDistance))
            effectiveBounds.X = workingArea.Left + AnchorDistance;
        else if (InSnapRange(effectiveBounds.Right, workingArea.Right - AnchorDistance))
            effectiveBounds.X = workingArea.Right - AnchorDistance - effectiveBounds.Width;
        if (InSnapRange(effectiveBounds.Top, workingArea.Top + AnchorDistance))
            effectiveBounds.Y = workingArea.Top + AnchorDistance;
        else if (InSnapRange(effectiveBounds.Bottom, workingArea.Bottom - AnchorDistance))
            effectiveBounds.Y = workingArea.Bottom - AnchorDistance - effectiveBounds.Height;
    }

    public void OnMoving(ref Message m)
    {
        if (_handle == IntPtr.Zero)
            return;

        var boundsLtrb = Marshal.PtrToStructure<WinApi.RECT>(m.LParam);
        var bounds     = boundsLtrb.ToRectangle();
        // This is where the window _would_ be located if snapping
        // had not occurred. This prevents the cursor from sliding
        // off the title bar if the snap distance is too large.
        var effectiveBounds = new Rectangle(
                                            Cursor.Position.X - DragOffsetX,
                                            Cursor.Position.Y - DragOffsetY,
                                            bounds.Width,
                                            bounds.Height);
        FindSnap(ref effectiveBounds);
        var newLtrb = WinApi.RECT.FromRectangle(effectiveBounds);
        Marshal.StructureToPtr(newLtrb, m.LParam, false);
        m.Result = new IntPtr(1);
    }

    public void OnSizeAndEnterSizeMove(Form form)
    {
        _handle      = form.Handle;
        SnapDistance = form.Font.Height;
        // Need to handle window size changed as well when
        // un-maximizing the form by dragging the title bar.
        DragOffsetX = Cursor.Position.X - form.Left;
        DragOffsetY = Cursor.Position.Y - form.Top;
    }
}
