using System.Collections;
using CommunityToolkit.Mvvm.Messaging;
using MpvNet.ExtensionMethod;
using MpvNet.Help;
using MpvNet.MVVM;
using MpvNet.Windows.UI;
using MpvNet.Windows.WPF;
using MpvNet.Windows.WPF.MsgBox;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Threading;
using static MpvNet.Windows.Help.WinApiHelp;
using static MpvNet.Windows.Native.WinApi;
using WpfControls = System.Windows.Controls;

namespace MpvNet.Windows.WinForms;

public partial class MainForm : Form
{
    public static MainForm? Instance { get; set; }

    public SnapManager SnapManager = new();
    public IntPtr      MpvWindowHandle { get; set; }
    public bool        WasShown        { get; set; }

    private WpfControls.ContextMenu ContextMenu        { get; } = new();
    private AutoResetEvent          MenuAutoResetEvent { get; } = new(false);
    private Point                   _lastCursorPosition;
    private Taskbar?                _taskBar;
    private Point                   _mouseDownLocation;
    private List<Binding>?          _confBindings;

    private readonly int _taskBarButtonCreatedMessage;

    private int _lastCursorChanged;
    private int _lastCycleFullscreen;
    private int _cursorAutohide = 1000;

    private bool _contextMenuIsReady;
    private bool _wasMaximized;
    private bool _maxSizeSet;
    private bool _isCursorVisible = true;

    public MainForm()
    {
        InitializeComponent();

        UpdateDarkMode();

        try
        {
            Instance = this;

            Player.FileLoaded         += Player_FileLoaded;
            Player.Pause              += Player_Pause;
            Player.PlaylistPosChanged += Player_PlaylistPosChanged;
            Player.Seek               += UpdateProgressBar;
            Player.Shutdown           += Player_Shutdown;
            Player.VideoSizeChanged   += Player_VideoSizeChanged;
            Player.ClientMessage      += Player_ClientMessage;

            GuiCommand.Current.ScaleWindow    += GuiCommand_ScaleWindow;
            GuiCommand.Current.MoveWindow     += GuiCommand_MoveWindow;
            GuiCommand.Current.WindowScaleNet += GuiCommand_WindowScaleNet;
            GuiCommand.Current.ShowMenu       += GuiCommand_ShowMenu;

            Player.Init(Handle, true);

            Player.ObserveProperty("window-maximized", PropChangeWindowMaximized); // bool methods not working correctly
            Player.ObserveProperty("window-minimized", PropChangeWindowMinimized); // bool methods not working correctly
            Player.ObserveProperty("cursor-autohide", PropChangeCursorAutohide);

            Player.ObservePropertyBool("border", PropChangeBorder);
            Player.ObservePropertyBool("fullscreen", PropChangeFullscreen);
            Player.ObservePropertyBool("keepaspect-window", value => Player.KeepAspectWindow = value);
            Player.ObservePropertyBool("ontop", PropChangeOnTop);
            Player.ObservePropertyBool("title-bar", PropChangeTitleBar);

            Player.ObservePropertyString("sid", PropChangeSid);
            Player.ObservePropertyString("aid", PropChangeAid);
            Player.ObservePropertyString("vid", PropChangeVid);

            Player.ObservePropertyString("title", PropChangeTitle);

            Player.ObservePropertyInt("edition", PropChangeEdition);

            Player.ObservePropertyDouble("window-scale", PropChangeWindowScale);

            CommandLine.ProcessCommandLineArgsPostInit();
            CommandLine.ProcessCommandLineFiles();

            _taskBarButtonCreatedMessage = RegisterWindowMessage("TaskbarButtonCreated");

            if (Player.Screen > -1)
            {
                var targetIndex = Player.Screen;
                var screens     = Screen.AllScreens;

                if (targetIndex < 0)
                    targetIndex = 0;

                if (targetIndex > screens.Length - 1)
                    targetIndex = screens.Length - 1;

                var screen = screens[Array.IndexOf(screens, screens[targetIndex])];
                var target = screen.Bounds;
                Left = target.X + (target.Width  - Width)  / 2;
                Top  = target.Y + (target.Height - Height) / 2;
            }

            if (!Player.Border)
                FormBorderStyle = FormBorderStyle.None;

            var pos = App.Settings.WindowPosition;

            if ((pos.X != 0 || pos.Y != 0) && App.RememberWindowPosition)
            {
                Left = pos.X - Width  / 2;
                Top  = pos.Y - Height / 2;

                var location = App.Settings.WindowLocation;

                Left = location.X switch
                {
                    -1 => pos.X,
                    1  => pos.X - Width,
                    _  => Left
                };
                Top = location.Y switch
                {
                    -1 => pos.Y,
                    1  => pos.Y - Height,
                    _  => Top
                };
            }

            if (Player.WindowMaximized)
            {
                SetFormPosAndSize(true);
                WindowState = FormWindowState.Maximized;
            }

            if (!Player.WindowMinimized) return;
            SetFormPosAndSize(true);
            WindowState = FormWindowState.Minimized;
        }
        catch (Exception ex)
        {
            Msg.ShowException(ex);
        }
    }

    private void UpdateDarkMode()
    {
        if (Environment.OSVersion.Version >= new Version(10, 0, 18985))
            DwmSetWindowAttribute(Handle, 20, new[] { Theme.DarkMode ? 1 : 0 },
                                  4); // DWMWA_USE_IMMERSIVE_DARK_MODE = 20
    }

    private void Player_ClientMessage(string[] args)
    {
        if (Command.Current.Commands.ContainsKey(args[0]))
            Command.Current.Commands[args[0]].Invoke(new ArraySegment<string>(args, 1, args.Length - 1));
        else if (GuiCommand.Current.Commands.ContainsKey(args[0]))
            BeginInvoke(() => GuiCommand.Current.Commands[args[0]]
                                        .Invoke(new ArraySegment<string>(args, 1, args.Length - 1)));
    }

    private void Player_PlaylistPosChanged(int pos)
    {
        if (pos == -1)
            SetTitle();
    }

    private void PropChangeWindowScale(double scale)
    {
        if (!WasShown)
            return;

        BeginInvoke(() =>
        {
            SetSize(
                    (int)(Player.VideoSize.Width * scale),
                    (int)Math.Floor(Player.VideoSize.Height * scale),
                    Screen.FromControl(this), false);
        });
    }

    private void Player_Shutdown() => BeginInvoke(Close);

    private void Player_VideoSizeChanged(Size value) => BeginInvoke(() =>
    {
        if (!KeepSize())
            SetFormPosAndSize();
    });

    private void GuiCommand_ScaleWindow(float scale)
    {
        BeginInvoke(() =>
        {
            int w, h;

            if (KeepSize())
            {
                w = (int)(ClientSize.Width  * scale);
                h = (int)(ClientSize.Height * scale);
            }
            else
            {
                w = (int)(ClientSize.Width * scale);
                h = (int)Math.Floor(w * Player.VideoSize.Height / (double)Player.VideoSize.Width);
            }

            SetSize(w, h, Screen.FromControl(this), false);
        });
    }

    private void GuiCommand_MoveWindow(string direction)
    {
        BeginInvoke(() =>
        {
            var screen      = Screen.FromControl(this);
            var workingArea = GetWorkingArea(Handle, screen.WorkingArea);

            switch (direction)
            {
                case "left" :
                    Left = workingArea.Left;
                    break;
                case "top" :
                    Top = 0;
                    break;
                case "right" :
                    Left = workingArea.Width - Width + workingArea.Left;
                    break;
                case "bottom" :
                    Top = workingArea.Height - Height;
                    break;
                case "center" :
                    Left = (screen.Bounds.Width  - Width)  / 2;
                    Top  = (screen.Bounds.Height - Height) / 2;
                    break;
            }
        });
    }

    private void GuiCommand_WindowScaleNet(float scale)
    {
        BeginInvoke(() =>
        {
            SetSize(
                    (int)(Player.VideoSize.Width * scale),
                    (int)Math.Floor(Player.VideoSize.Height * scale),
                    Screen.FromControl(this), false);
            Player.Command($"show-text \"window-scale {scale.ToString(CultureInfo.InvariantCulture)}\"");
        });
    }

    private void GuiCommand_ShowMenu()
    {
        BeginInvoke(() =>
        {
            if (IsMouseInOsc())
                return;

            ShowCursor();
            UpdateMenu();
            ContextMenu.IsOpen = true;
        });
    }

    private void PropChangeFullscreen(bool value) => BeginInvoke(() => CycleFullscreen(value));

    private bool IsFullscreen => WindowState == FormWindowState.Maximized && FormBorderStyle == FormBorderStyle.None;

    private static bool KeepSize() => App.StartSize is "session" or "always";

    private bool IsMouseInOsc()
    {
        var   pos = PointToClient(MousePosition);
        float top = 0;

        if (!Player.Border)
            top = ClientSize.Height * 0.1f;

        return pos.X < ClientSize.Width * 0.1 ||
               pos.X > ClientSize.Width * 0.9 ||
               pos.Y < top                    ||
               pos.Y > ClientSize.Height * 0.78;
    }

    private void UpdateMenu()
    {
        Player.UpdateExternalTracks();

        lock (Player.MediaTracksLock)
        {
            var trackMenuItem = FindMenuItem(_("Track"), "Track");

            if (trackMenuItem != null)
            {
                trackMenuItem.Items.Clear();

                var audTracks = Player.MediaTracks.Where(track => track.Type == "a");
                var subTracks = Player.MediaTracks.Where(track => track.Type == "s");
                var vidTracks = Player.MediaTracks.Where(track => track.Type == "v");
                var ediTracks = Player.MediaTracks.Where(track => track.Type == "e");

                var videoTrackList = vidTracks.ToList();
                foreach (var track in videoTrackList)
                {
                    var menuItem = new WpfControls.MenuItem() { Header = track.Text.Replace("_", "__") };
                    menuItem.Click     += (_, _) => Player.CommandV("set", "vid", track.Id.ToString());
                    menuItem.IsChecked =  Player.Vid == track.Id.ToString();
                    trackMenuItem.Items.Add(menuItem);
                }

                if (videoTrackList.Any())
                    trackMenuItem.Items.Add(new WpfControls.Separator());

                foreach (var track in audTracks)
                {
                    var menuItem = new WpfControls.MenuItem() { Header = track.Text.Replace("_", "__") };
                    menuItem.Click     += (_, _) => Player.CommandV("set", "aid", track.Id.ToString());
                    menuItem.IsChecked =  Player.Aid == track.Id.ToString();
                    trackMenuItem.Items.Add(menuItem);
                }

                var subtitleTrackList = subTracks.ToList();
                if (subtitleTrackList.Any())
                    trackMenuItem.Items.Add(new WpfControls.Separator());

                foreach (var track in subtitleTrackList)
                {
                    var menuItem = new WpfControls.MenuItem() { Header = track.Text.Replace("_", "__") };
                    menuItem.Click     += (_, _) => Player.CommandV("set", "sid", track.Id.ToString());
                    menuItem.IsChecked =  Player.Sid == track.Id.ToString();
                    trackMenuItem.Items.Add(menuItem);
                }

                if (subtitleTrackList.Any())
                {
                    var menuItem = new WpfControls.MenuItem() { Header = "S: No subtitles" };
                    menuItem.Click     += (_, _) => Player.CommandV("set", "sid", "no");
                    menuItem.IsChecked =  Player.Sid == "no";
                    trackMenuItem.Items.Add(menuItem);
                }

                var editorTrackList = ediTracks.ToList();
                if (editorTrackList.Any())
                    trackMenuItem.Items.Add(new WpfControls.Separator());

                foreach (var track in editorTrackList)
                {
                    var menuItem = new WpfControls.MenuItem() { Header = track.Text.Replace("_", "__") };
                    menuItem.Click     += (_, _) => Player.CommandV("set", "edition", track.Id.ToString());
                    menuItem.IsChecked =  Player.Edition == track.Id;
                    trackMenuItem.Items.Add(menuItem);
                }
            }
        }

        var chaptersMenuItem = FindMenuItem(_("Chapter"), "Chapters");

        if (chaptersMenuItem != null)
        {
            chaptersMenuItem.Items.Clear();

            foreach (var chapter in Player.GetChapters())
            {
                var menuItem = new WpfControls.MenuItem
                {
                    Header           = chapter.Title,
                    InputGestureText = chapter.TimeDisplay
                };

                menuItem.Click += (_, _) =>
                    Player.CommandV("seek", chapter.Time.ToString(CultureInfo.InvariantCulture), "absolute");

                chaptersMenuItem.Items.Add(menuItem);
            }
        }

        var recentMenuItem = FindMenuItem(_("Recent Files"), "Recent");

        if (recentMenuItem != null)
        {
            recentMenuItem.Items.Clear();

            foreach (var path in App.Settings.RecentFiles)
            {
                var file     = AppClass.GetTitleAndPath(path);
                var menuItem = MenuHelp.Add(recentMenuItem.Items, file.Title.ShortPath(100));

                if (menuItem != null)
                    menuItem.Click += (_, _) => Player.LoadFiles(new[] { file.Path }, true, false);
            }

            recentMenuItem.Items.Add(new WpfControls.Separator());
            var clearMenuItem = new WpfControls.MenuItem() { Header = _("Clear List") };
            clearMenuItem.Click += (_, _) => App.Settings.RecentFiles.Clear();
            recentMenuItem.Items.Add(clearMenuItem);
        }

        var titlesMenuItem = FindMenuItem(_("Title"), "Titles");

        if (titlesMenuItem != null)
        {
            titlesMenuItem.Items.Clear();

            lock (Player.BluRayTitles)
            {
                List<(int Index, TimeSpan Length)> items = Player.BluRayTitles.Select((t, i) => (i, t)).ToList();

                var titleItems = items.OrderByDescending(item => item.Length)
                                      .Take(20)
                                      .OrderBy(item => item.Index);

                foreach (var item in titleItems)
                {
                    if (item.Length == TimeSpan.Zero) continue;
                    var menuItem = MenuHelp.Add(titlesMenuItem.Items, $"Title {item.Index + 1}");

                    if (menuItem == null) continue;
                    menuItem.InputGestureText =  item.Length.ToString();
                    menuItem.Click            += (_, _) => Player.SetBluRayTitle(item.Index);
                }
            }
        }

        var profilesMenuItem = FindMenuItem(_("Profile"), "Profile");

        if (profilesMenuItem is { HasItems: false })
        {
            foreach (var profile in Player.ProfileNames)
            {
                if (profile.StartsWith("extension.")) continue;
                var menuItem = MenuHelp.Add(profilesMenuItem.Items, profile);

                if (menuItem != null)
                {
                    menuItem.Click += (_, _) =>
                    {
                        Player.CommandV("show-text", profile);
                        Player.CommandV("apply-profile", profile);
                    };
                }
            }

            profilesMenuItem.Items.Add(new WpfControls.Separator());
            var showProfilesMenuItem = new WpfControls.MenuItem() { Header = _("Show Profiles") };
            showProfilesMenuItem.Click += (_, _) => Player.Command("script-message-to mpvnet show-profiles");
            profilesMenuItem.Items.Add(showProfilesMenuItem);
        }

        var audioDevicesMenuItem = FindMenuItem(_("Audio Device"), "Audio Device");

        if (audioDevicesMenuItem != null)
        {
            audioDevicesMenuItem.Items.Clear();

            foreach (var pair in Player.AudioDevices)
            {
                var menuItem = MenuHelp.Add(audioDevicesMenuItem.Items, pair.Value);

                if (menuItem == null) continue;
                menuItem.IsChecked = pair.Name == Player.GetPropertyString("audio-device");

                menuItem.Click += (_, _) =>
                {
                    Player.SetPropertyString("audio-device", pair.Name);
                    Player.CommandV("show-text", pair.Value);
                    App.Settings.AudioDevice = pair.Name;
                };
            }
        }

        var customMenuItem = FindMenuItem(_("Custom"), "Custom");

        if (customMenuItem == null || customMenuItem.HasItems) return;
        {
            var customBindings = _confBindings!.Where(it => it.IsCustomMenu);

            var customList = customBindings.ToList();
            if (customList.Any())
            {
                foreach (var binding in customList)
                {
                    var menuItem = MenuHelp.Add(customMenuItem.Items, binding.Comment);

                    if (menuItem == null) continue;
                    menuItem.Click            += (_, _) => Player.Command(binding.Command);
                    menuItem.InputGestureText =  binding.Input;
                }
            }
            else
            {
                if (ContextMenu.Items.Contains(customMenuItem))
                    ContextMenu.Items.Remove(customMenuItem);
            }
        }
    }

    public WpfControls.MenuItem? FindMenuItem(string text, string text2 = "")
    {
        var ret = FindMenuItem(text, ContextMenu.Items);

        if (ret == null && text2 != "")
            return FindMenuItem(text2, ContextMenu.Items);

        return ret;
    }

    private static WpfControls.MenuItem? FindMenuItem(string text, IEnumerable? items)
    {
        foreach (var item in items!)
        {
            if (item is not WpfControls.MenuItem mi) continue;
            if (mi.Header.ToString().StartsWithEx(text) && mi.Header.ToString().TrimEx() == text)
                return mi;

            if (mi.Items.Count <= 0) continue;
            var val = FindMenuItem(text, mi.Items);

            if (val != null)
                return val;
        }

        return null;
    }

    private void SetFormPosAndSize(bool force = false, bool checkAutofit = true, bool load = false)
    {
        if (!force)
        {
            if (WindowState != FormWindowState.Normal)
                return;

            if (Player.Fullscreen)
            {
                CycleFullscreen(true);
                return;
            }
        }

        var screen        = Screen.FromControl(this);
        var workingArea   = GetWorkingArea(Handle, screen.WorkingArea);
        var autoFitHeight = Convert.ToInt32(workingArea.Height * Player.AutoFit);

        if (App.AutofitAudio > 1)
            App.AutofitAudio = 1;

        if (App.AutofitImage > 1)
            App.AutofitImage = 1;

        var isAudio = FileTypes.IsAudio(Player.Path.Ext());

        if (isAudio)
            autoFitHeight = Convert.ToInt32(workingArea.Height * App.AutofitAudio);

        if (FileTypes.IsImage(Player.Path.Ext()))
            autoFitHeight = Convert.ToInt32(workingArea.Height * App.AutofitImage);

        if (Player.VideoSize.Height == 0 || Player.VideoSize.Width == 0)
            Player.VideoSize = new Size((int)(autoFitHeight * (16 / 9f)), autoFitHeight);

        var minAspectRatio = isAudio ? App.MinimumAspectRatioAudio : App.MinimumAspectRatio;

        if (minAspectRatio != 0 && Player.VideoSize.Width / (float)Player.VideoSize.Height < minAspectRatio)
            Player.VideoSize = new Size((int)(autoFitHeight * minAspectRatio), autoFitHeight);

        var videoSize = Player.VideoSize;

        var height = videoSize.Height;
        var width  = videoSize.Width;

        if (App.StartSize == "previous")
            App.StartSize = "height-session";

        if (Player.WasInitialSizeSet)
        {
            if (KeepSize())
            {
                width  = ClientSize.Width;
                height = ClientSize.Height;
            }
            else
                switch (App.StartSize)
                {
                    case "height-always" :
                    case "height-session" :
                        height = ClientSize.Height;
                        width  = (int)Math.Ceiling(height * videoSize.Width / (double)videoSize.Height);
                        break;
                    case "width-always" :
                    case "width-session" :
                        width  = ClientSize.Width;
                        height = (int)Math.Floor(width * videoSize.Height / (double)videoSize.Width);
                        break;
                }
        }
        else
        {
            var windowSize = App.Settings.WindowSize;

            switch (App.StartSize)
            {
                case "height-always" when windowSize.Height != 0 :
                    height = windowSize.Height;
                    width  = (int)Math.Ceiling(height * videoSize.Width / (double)videoSize.Height);
                    break;
                case "height-session" :
                case "session" :
                    height = autoFitHeight;
                    width  = (int)Math.Ceiling(height * videoSize.Width / (double)videoSize.Height);
                    break;
                case "width-always" when windowSize.Height != 0 :
                    width  = windowSize.Width;
                    height = (int)Math.Floor(width * videoSize.Height / (double)videoSize.Width);
                    break;
                case "width-session" :
                    width  = autoFitHeight / 9 * 16;
                    height = (int)Math.Floor(width * videoSize.Height / (double)videoSize.Width);
                    break;
                case "always" when windowSize.Height != 0 :
                    height = windowSize.Height;
                    width  = windowSize.Width;
                    break;
            }

            Player.WasInitialSizeSet = true;
        }

        SetSize(width, height, screen, checkAutofit, load);
    }

    private void SetSize(int width, int height, Screen screen, bool checkAutofit = true, bool load = false)
    {
        var workingArea = GetWorkingArea(Handle, screen.WorkingArea);

        var maxHeight = workingArea.Height - (Height - ClientSize.Height) - 2;
        var maxWidth  = workingArea.Width  - (Width  - ClientSize.Width);

        var startWidth  = width;
        var startHeight = height;

        if (checkAutofit)
        {
            if (height < maxHeight * Player.AutoFitSmaller)
            {
                height = (int)(maxHeight * Player.AutoFitSmaller);
                width  = (int)Math.Ceiling(height * startWidth / (double)startHeight);
            }

            if (height > maxHeight * Player.AutoFitLarger)
            {
                height = (int)(maxHeight * Player.AutoFitLarger);
                width  = (int)Math.Ceiling(height * startWidth / (double)startHeight);
            }
        }

        if (width > maxWidth)
        {
            width  = maxWidth;
            height = (int)Math.Floor(width * startHeight / (double)startWidth);
        }

        if (height > maxHeight)
        {
            height = maxHeight;
            width  = (int)Math.Ceiling(height * startWidth / (double)startHeight);
        }

        if (height < maxHeight * 0.1)
        {
            height = (int)(maxHeight * 0.1);
            width  = (int)Math.Ceiling(height * startWidth / (double)startHeight);
        }

        var middlePos = new Point(Left + Width / 2, Top + Height / 2);
        var rect      = new RECT(new Rectangle(screen.Bounds.X, screen.Bounds.Y, width, height));

        AddWindowBorders(Handle, ref rect, GetDpi(Handle), !Player.TitleBar);

        width  = rect.Width;
        height = rect.Height;

        var left = Convert.ToInt32(middlePos.X - width  / 2.0);
        var top  = Convert.ToInt32(middlePos.Y - height / 2.0);

        if (!Player.TitleBar)
            top -= Convert.ToInt32(GetTitleBarHeight(Handle, GetDpi(Handle)) / 2.0);

        var currentRect = new Rectangle(Left, Top, Width, Height);

        if (GetHorizontalLocation(screen) == -1) left = Left;
        if (GetHorizontalLocation(screen) == 1) left  = currentRect.Right - width;

        if (GetVerticalLocation(screen) == -1) top = Top;
        if (GetVerticalLocation(screen) == 1) top  = currentRect.Bottom - height;

        var screens = Screen.AllScreens;

        var minLeft   = screens.Select(val => GetWorkingArea(Handle, val.WorkingArea).X).Min();
        var maxRight  = screens.Select(val => GetWorkingArea(Handle, val.WorkingArea).Right).Max();
        var minTop    = screens.Select(val => GetWorkingArea(Handle, val.WorkingArea).Y).Min();
        var maxBottom = screens.Select(val => GetWorkingArea(Handle, val.WorkingArea).Bottom).Max();

        if (load)
        {
            var geometryString = Player.GetPropertyString("geometry");

            if (!string.IsNullOrEmpty(geometryString))
            {
                var pos = ParseGeometry(geometryString, width, height);

                if (pos.X != int.MaxValue)
                    left = pos.X;

                if (pos.Y != int.MaxValue)
                    top = pos.Y;
            }
        }

        if (left < minLeft)
            left = minLeft;

        if (left + width > maxRight)
            left = maxRight - width;

        if (top < minTop)
            top = minTop;

        if (top + height > maxBottom)
            top = maxBottom - height;

        const uint swpNoActivate = 0x0010;
        SetWindowPos(Handle, IntPtr.Zero, left, top, width, height, swpNoActivate);
    }

    private Point ParseGeometry(string input, int width, int height)
    {
        var x = int.MaxValue;
        var y = int.MaxValue;

        var match = Regex.Match(input, @"^\+(\d+)%?\+(\d+)%?$");

        if (!match.Success) return new Point(x, y);
        var workingArea = GetWorkingArea(Handle, Screen.FromHandle(Handle).WorkingArea);

        x = int.Parse(match.Groups[1].Value);
        y = int.Parse(match.Groups[2].Value);

        x = workingArea.Left + Convert.ToInt32((workingArea.Width  - width)  / 100.0 * x);
        y = workingArea.Top  + Convert.ToInt32((workingArea.Height - height) / 100.0 * y);

        return new Point(x, y);
    }

    public void CycleFullscreen(bool enabled)
    {
        _lastCycleFullscreen = Environment.TickCount;
        Player.Fullscreen    = enabled;

        if (enabled)
        {
            if (WindowState == FormWindowState.Maximized && FormBorderStyle == FormBorderStyle.None) return;
            FormBorderStyle = FormBorderStyle.None;
            WindowState     = FormWindowState.Maximized;

            if (!_wasMaximized) return;
            var        bounds        = Screen.FromControl(this).Bounds;
            const uint swpShowWindow = 0x0040;
            var        hwndTop       = IntPtr.Zero;
            SetWindowPos(Handle, hwndTop, bounds.X, bounds.Y, bounds.Width, bounds.Height, swpShowWindow);
        }
        else
        {
            if (WindowState != FormWindowState.Maximized || FormBorderStyle != FormBorderStyle.None) return;
            if (_wasMaximized)
                WindowState = FormWindowState.Maximized;
            else
            {
                WindowState = FormWindowState.Normal;

                if (!Player.WasInitialSizeSet)
                    SetFormPosAndSize();
            }

            FormBorderStyle = Player.Border ? FormBorderStyle.Sizable : FormBorderStyle.None;

            if (!KeepSize())
                SetFormPosAndSize();
        }
    }

    public int GetHorizontalLocation(Screen screen)
    {
        var workingArea = GetWorkingArea(Handle, screen.WorkingArea);
        var rect        = new Rectangle(Left - workingArea.X, Top - workingArea.Y, Width, Height);

        if (workingArea.Width / (float)Width < 1.1)
            return 0;

        if (rect.X * 3 < workingArea.Width - rect.Right)
            return -1;

        return rect.X > (workingArea.Width - rect.Right) * 3 ? 1 : 0;
    }

    public int GetVerticalLocation(Screen screen)
    {
        var workingArea = GetWorkingArea(Handle, screen.WorkingArea);
        var rect        = new Rectangle(Left - workingArea.X, Top - workingArea.Y, Width, Height);

        if (workingArea.Height / (float)Height < 1.1)
            return 0;

        if (rect.Y * 3 < workingArea.Height - rect.Bottom)
            return -1;

        return rect.Y > (workingArea.Height - rect.Bottom) * 3 ? 1 : 0;
    }

    public void InitAndBuildContextMenu()
    {
        ContextMenu.Closed            += ContextMenu_Closed;
        ContextMenu.UseLayoutRounding =  true;

        var (menuBindings, confBindings) = App.InputConf.GetBindings();
        _confBindings                    = confBindings;
        var activeBindings = InputHelp.GetActiveBindings(menuBindings);

        foreach (var binding in menuBindings)
        {
            var tempBinding = binding;

            if (!binding.IsMenu)
                continue;

            var menuItem = MenuHelp.Add(ContextMenu.Items, tempBinding.Comment);

            if (menuItem == null) continue;
            menuItem.Click += (_, _) =>
            {
                try
                {
                    TaskHelp.Run(() =>
                    {
                        MenuAutoResetEvent.WaitOne();
                        System.Windows.Application.Current.Dispatcher.Invoke(
                                                                             DispatcherPriority.Background,
                                                                             new Action(delegate { }));
                        if (!string.IsNullOrEmpty(tempBinding.Command))
                            Player.Command(tempBinding.Command);
                    });
                }
                catch (Exception ex)
                {
                    Msg.ShowException(ex);
                }
            };

            menuItem.InputGestureText = InputHelp.GetBindingsForCommand(activeBindings, tempBinding.Command);
        }

        _contextMenuIsReady = true;
    }

    private void Player_FileLoaded()
    {
        BeginInvoke(() =>
        {
            SetTitleInternal();

            var interval = (int)(Player.Duration.TotalMilliseconds / 100);

            if (interval < 100)
                interval = 100;

            if (interval > 1000)
                interval = 1000;

            ProgressTimer.Interval = interval;
            UpdateProgressBar();
        });

        var path = Player.GetPropertyString("path");

        path = MainPlayer.ConvertFilePath(path);

        if (path.Contains("://"))
        {
            var title = Player.GetPropertyString("media-title");

            if (!string.IsNullOrEmpty(title) && path != title)
                path = path + "|" + title;
        }

        if (string.IsNullOrEmpty(path) || path == "-" || path == @"bd://" || path == @"dvd://") return;
        if (App.Settings.RecentFiles.Contains(path))
            App.Settings.RecentFiles.Remove(path);

        App.Settings.RecentFiles.Insert(0, path);

        while (App.Settings.RecentFiles.Count > App.RecentCount)
            App.Settings.RecentFiles.RemoveAt(App.RecentCount);
    }

    private void SetTitle() => BeginInvoke(SetTitleInternal);

    private void SetTitleInternal()
    {
        var title = Title;

        if (title == "${filename}" && Player.Path.ContainsEx("://"))
            title = "${media-title}";

        var text = Player.Expand(title);

        if (text == "(unavailable)" || Player.PlaylistPos == -1)
            text = "mpv.net";

        Text = text;
    }

    private void SaveWindowProperties()
    {
        if (WindowState != FormWindowState.Normal || !WasShown) return;
        SavePosition();
        App.Settings.WindowSize = ClientSize;
    }

    private void SavePosition()
    {
        var pos    = new Point(Left + Width / 2, Top + Height / 2);
        var screen = Screen.FromControl(this);

        var x = GetHorizontalLocation(screen);
        var y = GetVerticalLocation(screen);

        pos.X = x switch
        {
            -1 => Left,
            1  => Left + Width,
            _  => pos.X
        };
        pos.Y = y switch
        {
            -1 => Top,
            1  => Top + Height,
            _  => pos.Y
        };

        App.Settings.WindowPosition = pos;
        App.Settings.WindowLocation = new Point(x, y);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.Style |= 0x00020000 /* WS_MINIMIZEBOX */;
            return cp;
        }
    }

    private string? _title;

    public string? Title
    {
        get => _title;
        set
        {
            if (string.IsNullOrEmpty(value))
                return;

            if (value.EndsWith("} - mpv"))
                value = value.Replace("} - mpv", "} - mpv.net");

            _title = value;
        }
    }

    protected override void WndProc(ref Message m)
    {
        switch (m.Msg)
        {
            case 0x0007 : // WM_SETFOCUS
            case 0x0008 : // WM_KILLFOCUS
            case 0x0021 : // WM_MOUSEACTIVATE
            case 0x0100 : // WM_KEYDOWN
            case 0x0101 : // WM_KEYUP
            case 0x0104 : // WM_SYSKEYDOWN
            case 0x0105 : // WM_SYSKEYUP
            case 0x0201 : // WM_LBUTTONDOWN
            case 0x0202 : // WM_LBUTTONUP
            case 0x0204 : // WM_RBUTTONDOWN
            case 0x0205 : // WM_RBUTTONUP
            case 0x0206 : // WM_RBUTTONDBLCLK
            case 0x0207 : // WM_MBUTTONDOWN
            case 0x0208 : // WM_MBUTTONUP
            case 0x0209 : // WM_MBUTTONDBLCLK
            case 0x020a : // WM_MOUSEWHEEL
            case 0x020b : // WM_XBUTTONDOWN
            case 0x020c : // WM_XBUTTONUP
            case 0x020e : // WM_MOUSEHWHEEL
            case 0x0280 : // WM_IME_REPORT
            case 0x0281 : // WM_IME_SETCONTEXT
            case 0x0282 : // WM_IME_NOTIFY
            case 0x0283 : // WM_IME_CONTROL
            case 0x0284 : // WM_IME_COMPOSITIONFULL
            case 0x0285 : // WM_IME_SELECT
            case 0x0286 : // WM_IME_CHAR
            case 0x0288 : // WM_IME_REQUEST
            case 0x0290 : // WM_IME_KEYDOWN
            case 0x0291 : // WM_IME_KEYUP
            case 0x02a3 : // WM_MOUSELEAVE
            {
                var ignore = false;

                if (m.Msg == 0x0100) // WM_KEYDOWN
                {
                    var keyCode = (Keys)(int)m.WParam & Keys.KeyCode;

                    if (keyCode == Keys.Escape && _contextMenuIsReady && ContextMenu.IsOpen)
                    {
                        ignore             = true;
                        ContextMenu.IsOpen = false;
                    }
                }

                if (MpvWindowHandle == IntPtr.Zero)
                    MpvWindowHandle = FindWindowEx(Handle, IntPtr.Zero, "mpv", null);

                if (MpvWindowHandle != IntPtr.Zero && !ignore)
                    m.Result = SendMessage(MpvWindowHandle, m.Msg, m.WParam, m.LParam);
            }
                break;
            case 0x001A : // WM_SETTINGCHANGE
                UpdateDarkMode();
                break;
            case 0x51 : // WM_INPUTLANGCHANGE
                ActivateKeyboardLayout(m.LParam, 0x00000100u /*KLF_SETFORPROCESS*/);
                break;
            case 0x319 : // WM_APPCOMMAND
            {
                var key            = MpvHelp.WmAppCommandToMpvKey((int)(m.LParam.ToInt64() >> 16 & ~0xf000));
                var inputMediaKeys = Player.GetPropertyBool("input-media-keys");

                if (key != null && inputMediaKeys)
                {
                    Player.Command("keypress " + key);
                    m.Result = new IntPtr(1);
                    return;
                }
            }
                break;
            case 0x312 : // WM_HOTKEY
                GlobalHotkey.Execute(m.WParam.ToInt32());
                break;
            case 0x200 : // WM_MOUSEMOVE
                if (Environment.TickCount - _lastCycleFullscreen > 500)
                {
                    var pos = PointToClient(Cursor.Position);
                    Player.Command($"mouse {pos.X} {pos.Y}");
                }

                if (IsCursorPosDifferent(_lastCursorPosition))
                    ShowCursor();
                break;
            case 0x203 : // WM_LBUTTONDBLCLK
            {
                var pos = PointToClient(Cursor.Position);
                Player.Command($"mouse {pos.X} {pos.Y} 0 double");
            }
                break;
            case 0x2E0 : // WM_DPICHANGED
            {
                if (!WasShown)
                    break;

                var rect = Marshal.PtrToStructure<RECT>(m.LParam);
                SetWindowPos(Handle, IntPtr.Zero, rect.Left, rect.Top, rect.Width, rect.Height, 0);
            }
                break;
            case 0x0112 : // WM_SYSCOMMAND
            {
                // with title-bar=no when the window is restored from minimizing the height is too high  
                if (!Player.TitleBar)
                {
                    const int scMinimize = 0xF020;

                    if (m.WParam == (nint)scMinimize)
                    {
                        MaximumSize = Size;
                        _maxSizeSet = true;
                    }
                }
            }
                break;
            case 0x0083 : // WM_NCCALCSIZE
                if ((int)m.WParam == 1 && !Player.TitleBar && !IsFullscreen)
                {
                    var nccalcsizeParams = Marshal.PtrToStructure<NCCALCSIZE_PARAMS>(m.LParam);
                    var rects            = nccalcsizeParams.rgrc;
                    var h                = GetTitleBarHeight(Handle, GetDpi(Handle));
                    rects[0].Top = rects[0].Top - h;
                    Marshal.StructureToPtr(nccalcsizeParams, m.LParam, false);
                }

                break;
            case 0x231 : // WM_ENTERSIZEMOVE
            case 0x005 : // WM_SIZE
                if (Player.SnapWindow)
                    SnapManager.OnSizeAndEnterSizeMove(this);
                break;
            case 0x214 : // WM_SIZING
                if (!Player.KeepAspectWindow) return;
                var rc = Marshal.PtrToStructure<RECT>(m.LParam);
                var r  = rc;
                SubtractWindowBorders(Handle, ref r, GetDpi(Handle), !Player.TitleBar);

                int cW        = r.Right - r.Left, c_h = r.Bottom - r.Top;
                var videoSize = Player.VideoSize;

                if (videoSize == Size.Empty)
                    videoSize = new Size(16, 9);

                var aspect = videoSize.Width / (double)videoSize.Height;
                var dW     = (int)Math.Ceiling(c_h * aspect - cW);
                var dH     = (int)Math.Floor(cW    / aspect - c_h);

                int[] dCorners = { dW, dH, -dW, -dH };
                int[] corners  = { rc.Left, rc.Top, rc.Right, rc.Bottom };
                var   corner   = GetResizeBorder(m.WParam.ToInt32());

                if (corner >= 0)
                    corners[corner] -= dCorners[corner];

                Marshal.StructureToPtr(new RECT(corners[0], corners[1], corners[2], corners[3]), m.LParam, false);
                m.Result = new IntPtr(1);

                return;
            case 0x84 : // WM_NCHITTEST
                // resize borderless window
                if ((!Player.Border || !Player.TitleBar) && !Player.Fullscreen)
                {
                    const int htClient      = 1;
                    const int htLeft        = 10;
                    const int htRight       = 11;
                    const int htTop         = 12;
                    const int htTopLeft     = 13;
                    const int htTopRight    = 14;
                    const int htBottom      = 15;
                    const int htBottomLeft  = 16;
                    const int htBottomRight = 17;

                    int x = (short)(m.LParam.ToInt32() & 0xFFFF); // LoWord
                    int y = (short)(m.LParam.ToInt32() >> 16);    // HiWord

                    var pt = PointToClient(new Point(x, y));
                    var cs = ClientSize;
                    m.Result = new IntPtr(htClient);
                    var distance = FontHeight / 3;

                    if (pt.X >= cs.Width - distance && pt.Y >= cs.Height - distance && cs.Height >= distance)
                        m.Result = new IntPtr(htBottomRight);
                    else if (pt.X <= distance && pt.Y >= cs.Height - distance && cs.Height >= distance)
                        m.Result = new IntPtr(htBottomLeft);
                    else if (pt.X <= distance && pt.Y <= distance && cs.Height >= distance)
                        m.Result = new IntPtr(htTopLeft);
                    else if (pt.X >= cs.Width - distance && pt.Y <= distance && cs.Height >= distance)
                        m.Result = new IntPtr(htTopRight);
                    else if (pt.Y <= distance && cs.Height >= distance)
                        m.Result = new IntPtr(htTop);
                    else if (pt.Y >= cs.Height - distance && cs.Height >= distance)
                        m.Result = new IntPtr(htBottom);
                    else if (pt.X <= distance && cs.Height >= distance)
                        m.Result = new IntPtr(htLeft);
                    else if (pt.X >= cs.Width - distance && cs.Height >= distance)
                        m.Result = new IntPtr(htRight);

                    return;
                }

                break;
            case 0x4A : // WM_COPYDATA
            {
                var copyData = (CopyDataStruct)m.GetLParam(typeof(CopyDataStruct))!;
                var args     = copyData.lpData.Split('\n');
                var mode     = args[0];
                args = args.Skip(1).ToArray();

                switch (mode)
                {
                    case "single" :
                        Player.LoadFiles(args, true, false);
                        break;
                    case "queue" :
                        foreach (var file in args)
                            Player.CommandV("loadfile", file, "append");
                        break;
                    case "command" :
                        Player.Command(args[0]);
                        break;
                }

                Activate();
            }
                return;
            case 0x216 : // WM_MOVING
                if (Player.SnapWindow)
                    SnapManager.OnMoving(ref m);
                break;
        }

        if (m.Msg == _taskBarButtonCreatedMessage && Player.TaskBarProgress)
        {
            _taskBar = new Taskbar(Handle);
            ProgressTimer.Start();
        }

        // beep sound when closed using taskbar due to exception
        if (!IsDisposed)
            base.WndProc(ref m);
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);

        if (_maxSizeSet)
        {
            TaskHelp.Run(() =>
            {
                Thread.Sleep(200);
                BeginInvoke(() =>
                {
                    if (IsDisposed || Disposing) return;
                    MaximumSize = new Size(int.MaxValue, int.MaxValue);
                    _maxSizeSet = false;
                });
            });
        }
    }

    private void CursorTimer_Tick(object sender, EventArgs e)
    {
        if (IsCursorPosDifferent(_lastCursorPosition))
        {
            _lastCursorPosition = MousePosition;
            _lastCursorChanged  = Environment.TickCount;
        }
        else if ((Environment.TickCount - _lastCursorChanged > _cursorAutohide) &&
                 ClientRectangle.Contains(PointToClient(MousePosition)) &&
                 ActiveForm == this && !ContextMenu.IsVisible && !IsMouseInOsc())

            HideCursor();
    }

    private void ProgressTimer_Tick(object sender, EventArgs e) => UpdateProgressBar();

    private void UpdateProgressBar()
    {
        if (Player.TaskBarProgress && _taskBar != null)
            _taskBar.SetValue(Player.GetPropertyDouble("time-pos", false), Player.Duration.TotalSeconds);
    }

    private void PropChangeOnTop(bool value) => BeginInvoke(() => TopMost = value);

    private static void PropChangeAid(string value) => Player.Aid = value;

    private static void PropChangeSid(string value) => Player.Sid = value;

    private static void PropChangeVid(string value) => Player.Vid = value;

    private void PropChangeTitle(string value)
    {
        Title = value;
        SetTitle();
    }

    private static void PropChangeEdition(int value) => Player.Edition = value;

    private void PropChangeWindowMaximized()
    {
        if (!WasShown)
            return;

        BeginInvoke(() =>
        {
            Player.WindowMaximized = Player.GetPropertyBool("window-maximized");

            WindowState = Player.WindowMaximized switch
            {
                true when WindowState  != FormWindowState.Maximized => FormWindowState.Maximized,
                false when WindowState == FormWindowState.Maximized => FormWindowState.Normal,
                _                                                   => WindowState
            };
        });
    }

    private void PropChangeWindowMinimized()
    {
        if (!WasShown)
            return;

        BeginInvoke(() =>
        {
            Player.WindowMinimized = Player.GetPropertyBool("window-minimized");

            WindowState = Player.WindowMinimized switch
            {
                true when WindowState  != FormWindowState.Minimized => FormWindowState.Minimized,
                false when WindowState == FormWindowState.Minimized => FormWindowState.Normal,
                _                                                   => WindowState
            };
        });
    }

    private void PropChangeCursorAutohide()
    {
        var strValue = Player.GetPropertyString("cursor-autohide");

        switch (strValue)
        {
            case "no" :
                _cursorAutohide = 0;
                break;
            case "always" :
                _cursorAutohide = -1;
                break;
            default :
            {
                if (int.TryParse(strValue, out var intValue))
                    _cursorAutohide = intValue;
                break;
            }
        }
    }

    private void PropChangeBorder(bool enabled)
    {
        Player.Border = enabled;

        BeginInvoke(() =>
        {
            if (IsFullscreen) return;
            FormBorderStyle = Player.Border switch
            {
                true when FormBorderStyle  == FormBorderStyle.None    => FormBorderStyle.Sizable,
                false when FormBorderStyle == FormBorderStyle.Sizable => FormBorderStyle.None,
                _                                                     => FormBorderStyle
            };
        });
    }

    private void PropChangeTitleBar(bool enabled)
    {
        if (enabled == Player.TitleBar)
            return;

        Player.TitleBar = enabled;

        BeginInvoke(() =>
        {
            SetSize(ClientSize.Width, ClientSize.Height, Screen.FromControl(this), false);
            Height += 1;
            Height -= 1;
        });
    }

    private void Player_Pause()
    {
        if (_taskBar != null && Player.TaskBarProgress)
            _taskBar.SetState(Player.Paused ? TaskbarStates.Paused : TaskbarStates.Normal);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        _lastCycleFullscreen = Environment.TickCount;
        SetFormPosAndSize(false, true, true);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        ShowCursor();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        if (WindowState == FormWindowState.Maximized)
            Player.SetPropertyBool("window-maximized", true);

        WpfApplication.Init();
        Theme.UpdateWpfColors();
        MessageBoxEx.MessageForeground = Theme.Current?.GetBrush("heading");
        MessageBoxEx.MessageBackground = Theme.Current?.GetBrush("background");
        MessageBoxEx.ButtonBackground  = Theme.Current?.GetBrush("highlight");
        InitAndBuildContextMenu();
        Cursor.Position = new Point(Cursor.Position.X + 1, Cursor.Position.Y);
        GlobalHotkey.RegisterGlobalHotkeys(Handle);
        StrongReferenceMessenger.Default.Send(new MainWindowIsLoadedMessage());
        WasShown = true;
    }

    private void ContextMenu_Closed(object sender, System.Windows.RoutedEventArgs e) => MenuAutoResetEvent.Set();

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        SaveWindowProperties();

        if (FormBorderStyle != FormBorderStyle.None)
        {
            _wasMaximized = WindowState switch
            {
                FormWindowState.Maximized => true,
                FormWindowState.Normal    => false,
                _                         => _wasMaximized
            };
        }

        if (!WasShown) return;
        switch (WindowState)
        {
            case FormWindowState.Minimized :
                Player.SetPropertyBool("window-minimized", true);
                break;
            case FormWindowState.Normal :
                Player.SetPropertyBool("window-maximized", false);
                Player.SetPropertyBool("window-minimized", false);
                break;
            case FormWindowState.Maximized :
                Player.SetPropertyBool("window-maximized", true);
                break;
            default :
                throw new ArgumentOutOfRangeException();
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);

        if (Player.IsQuitNeeded)
            Player.CommandV("quit");

        if (!Player.ShutdownAutoResetEvent.WaitOne(10000))
            Msg.ShowError(_("Shutdown thread failed to complete within 10 seconds."));

        Player.Destroy();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        _mouseDownLocation = PointToScreen(e.Location);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (!IsCursorPosDifferent(_mouseDownLocation) ||
            WindowState != FormWindowState.Normal     ||
            e.Button    != MouseButtons.Left          || IsMouseInOsc() ||
            !Player.GetPropertyBool("window-dragging")) return;
        var       htCaption       = new IntPtr(2);
        const int wmNclButtonDown = 0xA1;
        ReleaseCapture();
        PostMessage(Handle, wmNclButtonDown, htCaption, IntPtr.Zero);
    }

    protected override void OnMove(EventArgs e)
    {
        base.OnMove(e);
        SaveWindowProperties();
    }

    protected override void OnDragEnter(DragEventArgs e)
    {
        base.OnDragEnter(e);

        if (e.Data!.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(DataFormats.Text))
            e.Effect = DragDropEffects.Copy;
    }

    protected override void OnDragDrop(DragEventArgs e)
    {
        base.OnDragDrop(e);

        var append = ModifierKeys == Keys.Shift;

        if (e.Data!.GetDataPresent(DataFormats.FileDrop))
            Player.LoadFiles(e.Data.GetData(DataFormats.FileDrop) as string[], true, append);
        else if (e.Data.GetDataPresent(DataFormats.Text))
            Player.LoadFiles(new[] { e.Data.GetData(DataFormats.Text)!.ToString()! }, true, append);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        // prevent annoying beep using alt key
        if (ModifierKeys == Keys.Alt)
            e.SuppressKeyPress = true;

        base.OnKeyDown(e);
    }

    private void ShowCursor()
    {
        if (_isCursorVisible || _cursorAutohide == -1) return;
        Cursor.Show();
        _isCursorVisible = true;
    }

    private void HideCursor()
    {
        if (!_isCursorVisible || _cursorAutohide == 0) return;
        Cursor.Hide();
        _isCursorVisible = false;
    }

    private bool IsCursorPosDifferent(Point screenPos)
    {
        var len = 5 * (GetDpi(Handle) / 96f);
        return Math.Abs(screenPos.X - MousePosition.X) > len || Math.Abs(screenPos.Y - MousePosition.Y) > len;
    }

    public static int GetDpi(IntPtr hwnd)
    {
        if (Environment.OSVersion.Version >= WindowsTen1607 && hwnd != IntPtr.Zero)
            return GetDpiForWindow(hwnd);
        else
            using (var gx = Graphics.FromHwnd(hwnd))
                return GetDeviceCaps(gx.GetHdc(), 88 /*LOGPIXELSX*/);
    }

    [DllImport("DwmApi")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, int[] attrValue, int attrSize);
}
