using MpvNet.Help;
using MpvNet.Windows.UI;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;

namespace MpvNet.Windows.WPF;

public partial class LearnWindow : Window
{
    public  Binding? InputItem { get; set; }
    private string   _newKey = "";

    private const uint MapvkVkToVsc = 0;

    private const int VkMenu      = 0x12;
    private const int VkLeftMenu  = 0xA4;
    private const int VkRightMenu = 0xA5;

    private const int VkControl      = 0x11;
    private const int VkLeftControl  = 0xA2;
    private const int VkRightControl = 0xA3;

    private bool _blockMbtnLeft;
    private bool _blockMbtnRight;

    public LearnWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    public Theme? Theme => Theme.Current;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern short GetKeyState(int keyCode);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int ToUnicode(uint          wVirtKey, uint wScanCode, byte[] lpKeyState,
                                        StringBuilder pwszBuff, int  cchBuff,   uint   wFlags);

    [DllImport("user32.dll")]
    private static extern bool GetKeyboardState(byte[] lpKeyState);

    private string ToUnicode(uint vk, ref bool firstEmpty)
    {
        var keys = new byte[256];

        if (!GetKeyboardState(keys))
            return "";

        if ((keys[VkControl] & 0x80) != 0 && (keys[VkMenu] & 0x80) == 0)
            keys[VkLeftControl] = keys[VkRightControl] = keys[VkControl] = 0;

        var scanCode = MapVirtualKey(vk, MapvkVkToVsc);

        var ret = ToUnicode(vk, scanCode, keys);

        firstEmpty = ret == "";

        if (firstEmpty)
        {
            keys[VkLeftControl] = keys[VkRightControl] = keys[VkControl] = 0;
            keys[VkLeftMenu]    = keys[VkRightMenu]    = keys[VkMenu]    = 0;
            ret                 = ToUnicode(vk, scanCode, keys);
        }

        if (ret.Length == 1 && ret[0] < 32)
            return "";

        return ret;
    }

    public string ToUnicode(uint vk, uint scanCode, byte[] keys)
    {
        var sb = new StringBuilder(10);
        ToUnicode(vk, scanCode, keys, sb, sb.Capacity, 0);
        return sb.ToString();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        var m = new Message
        {
            HWnd   = hwnd,
            Msg    = msg,
            WParam = wParam,
            LParam = lParam
        };
        ProcessKeyEventArgs(ref m);
        return m.Result;
    }

    private void OnKeyDown(uint vk)
    {
        var firstEmpty = false;
        var key        = (Keys)vk;

        if (key is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.None or Keys.Tab)
            return;

        var text = ToUnicode(vk, ref firstEmpty);

        if ((int)key > 111 && (int)key < 136)
            text = "F" + ((int)key - 111);

        if ((int)key > 95 && (int)key < 106)
            text = "KP" + ((int)key - 96);

        text = text switch
        {
            "#"  => "Sharp",
            "´´" => "´",
            "``" => "`",
            "^^" => "^",
            _    => text
        };

        switch (key)
        {
            case Keys.Left :
                text = "Left";
                break;
            case Keys.Up :
                text = "Up";
                break;
            case Keys.Right :
                text = "Right";
                break;
            case Keys.Down :
                text = "Down";
                break;
            case Keys.Space :
                text = "Space";
                break;
            case Keys.Enter :
                text = "Enter";
                break;
            case Keys.Tab :
                text = "Tab";
                break;
            case Keys.Back :
                text = "BS";
                break;
            case Keys.Delete :
                text = "Del";
                break;
            case Keys.Insert :
                text = "Ins";
                break;
            case Keys.Home :
                text = "Home";
                break;
            case Keys.End :
                text = "End";
                break;
            case Keys.PageUp :
                text = "PGUP";
                break;
            case Keys.PageDown :
                text = "PGDWN";
                break;
            case Keys.Escape :
                text = "Esc";
                break;
            case Keys.Sleep :
                text = "Sleep";
                break;
            case Keys.Cancel :
                text = "Cancel";
                break;
            case Keys.PrintScreen :
                text = "Print";
                break;
            case Keys.BrowserFavorites :
                text = "Favorites";
                break;
            case Keys.BrowserSearch :
                text = "Search";
                break;
            case Keys.BrowserHome :
                text = "Homepage";
                break;
            case Keys.LaunchMail :
                text = "Mail";
                break;
            case Keys.Play :
                text = "Play";
                break;
            case Keys.Pause :
                text = "Pause";
                break;
            case Keys.MediaPlayPause :
                text = "PlayPause";
                break;
            case Keys.MediaStop :
                text = "Stop";
                break;
            case Keys.MediaNextTrack :
                text = "Next";
                break;
            case Keys.MediaPreviousTrack :
                text = "Prev";
                break;
            default :
                throw new ArgumentOutOfRangeException();
        }

        var isAlt   = GetKeyState(18) < 0;
        var isShift = GetKeyState(16) < 0;
        var isCtrl  = GetKeyState(17) < 0;

        var isLetter = (int)key > 64 && (int)key < 91;

        if (isLetter && isShift)
            text = text.ToUpper();

        var keyString = ToUnicode(vk, ref firstEmpty);

        if (isAlt && !isCtrl)
            text = "Alt+" + text;

        if (isShift && keyString is "" or " ")
            text = "Shift+" + text;

        text = isCtrl switch
        {
            true when isAlt && firstEmpty                   => "Ctrl+Alt+" + text,
            true when !(keyString != "" && isCtrl && isAlt) => "Ctrl+"     + text,
            _                                               => text
        };

        if (!string.IsNullOrEmpty(text))
            SetKey(text);
    }

    private void SetKey(string? key)
    {
        _newKey           = key!;
        KeyTextBlock.Text = key;
    }

    private void ProcessKeyEventArgs(ref Message m)
    {
        const int wmKeydown       = 0x100;
        const int wmSystemKeyDown = 0x104;
        const int wmAppcommand    = 0x319;

        switch (m.Msg)
        {
            case wmKeydown or wmSystemKeyDown :
                OnKeyDown((uint)m.WParam.ToInt64());
                break;
            case wmAppcommand :
            {
                var value = MpvHelp.WM_APPCOMMAND_to_mpv_key((int)(m.LParam.ToInt64() >> 16 & ~0xf000));

                if (value != null)
                    SetKey(value);
                break;
            }
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        source?.AddHook(WndProc);
        SetKey(InputItem?.Input);
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        InputItem!.Input = _newKey;
        Close();
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        InputItem!.Input = "";
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Delta > 0)
            SetKey(GetModifierText() + "WHEEL_UP");
        else
            SetKey(GetModifierText() + "WHEEL_DOWN");
    }

    private void Window_MouseUp(object sender, MouseButtonEventArgs e)
    {
        switch (e.ChangedButton)
        {
            case MouseButton.Left :
                if (_blockMbtnLeft)
                    _blockMbtnLeft = false;
                else
                    SetKey(GetModifierText() + "MBTN_LEFT");
                break;
            case MouseButton.Right :
                if (_blockMbtnRight)
                    _blockMbtnRight = false;
                else
                    SetKey(GetModifierText() + "MBTN_RIGHT");
                break;
            case MouseButton.Middle :
                SetKey(GetModifierText() + "MBTN_MID");
                break;
            case MouseButton.XButton1 :
                SetKey(GetModifierText() + "MBTN_BACK");
                break;
            case MouseButton.XButton2 :
                SetKey(GetModifierText() + "MBTN_FORWARD");
                break;
        }
    }

    private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        switch (e.ChangedButton)
        {
            case MouseButton.Left :
                SetKey(GetModifierText() + "MBTN_LEFT_DBL");
                _blockMbtnLeft = true;
                break;
            case MouseButton.Right :
                SetKey(GetModifierText() + "MBTN_RIGHT_DBL");
                _blockMbtnRight = true;
                break;
        }
    }

    private static string GetModifierText()
    {
        var ret = "";

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            ret = "Alt+" + ret;

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            ret = "Ctrl+" + ret;

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            ret = "Shift+" + ret;

        return ret;
    }
}
