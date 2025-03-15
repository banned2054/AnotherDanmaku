// https://www.codeproject.com/Articles/5290638/Customizable-WPF-MessageBox

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace MpvNet.Windows.WPF.MsgBox;

public sealed partial class MessageBoxEx : INotifyPropertyChanged
{
    #region fields

    private const bool                    EnableCloseButton = true;
    public static bool                    isSilent;
    public static MessageBoxButtonDefault staticButtonDefault;

    #endregion fields

    #region properties

    public static  Color                 DefaultUrlForegroundColor => Colors.Blue;
    private static string?               MsgBoxIconToolTip         { get; set; }
    private static MsgBoxExDelegate?     DelegateObj               { get; set; }
    private static bool                  ExitAfterErrorAction      { get; set; }
    public static  string?               ButtonTemplateName        { get; set; }
    public static  Brush?                MessageBackground         { get; set; }
    public static  Brush?                MessageForeground         { get; set; }
    public static  Brush?                ButtonBackground          { get; set; }
    public static  Visibility            ShowDetailsBtn            { get; set; } = Visibility.Collapsed;
    public static  string?               DetailsText               { get; set; }
    public static  Visibility            ShowCheckBox              { get; set; } = Visibility.Collapsed;
    public static  MsgBoxExCheckBoxData? CheckBoxData              { get; set; }
    public static  FontFamily            MsgFontFamily             { get; set; } = new("Segoe UI");
    public static  double                MsgFontSize               { get; set; } = 12;
    public static  Uri?                  Url                       { get; set; }
    public static  string?               UrlDisplayName            { get; set; }
    public static  string?               DelegateToolTip           { get; set; }

    #endregion properties

    #region methods

    public static void SetFont(string familyName) => MsgFontFamily = new FontFamily(familyName);

    public static MessageBoxResult OpenMessageBox(
        string msg, string title, MessageBoxButton buttons, MessageBoxImage image)
    {
        var window = new MessageBoxEx(msg, title, buttons, image);
        SetOwner(window);
        window.ShowDialog();
        return window.MessageResult;
    }

    public static MessageBoxResultEx OpenMessageBox(
        string             msg,
        string             title,
        MessageBoxButtonEx buttons,
        MessageBoxImage    image)
    {
        var window = new MessageBoxEx(msg, title, buttons, image);
        SetOwner(window);
        window.ShowDialog();
        return window.MessageResultEx;
    }

    public static void SetOwner(Window window)
    {
        var parentHandle = GetParentHandle();

        if (parentHandle != IntPtr.Zero)
            new WindowInteropHelper(window).Owner = parentHandle;
    }

    public static IntPtr GetParentHandle()
    {
        var foregroundWindow = GetForegroundWindow();
        GetWindowThreadProcessId(foregroundWindow, out var procId);

        using var proc = Process.GetCurrentProcess();
        return proc.Id == procId ? foregroundWindow : IntPtr.Zero;
    }

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    public static Color ColorFromString(string colorString)
    {
        var wpfColor = Colors.Black;

        try
        {
            wpfColor = (Color)ColorConverter.ConvertFromString(colorString);
        }
        catch (Exception)
        {
            // ignored
        }

        return wpfColor;
    }

    #endregion methods
}
