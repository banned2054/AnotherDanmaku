using CommunityToolkit.Mvvm.Input;
using MpvNet.Help;
using MpvNet.Windows.UI;
using MpvNet.Windows.WPF.Controls;
using MpvNet.Windows.WPF.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace MpvNet.Windows.WPF;

public partial class ConfWindow : INotifyPropertyChanged
{
    private readonly List<Setting>        _settings  = Conf.LoadConf(Properties.Resources.editor_conf.TrimEnd());
    private readonly List<ConfItem>       _confItems = new();
    private readonly string               _initialContent;
    private readonly string               _themeConf = GetThemeConf();
    private          string?              _searchText;
    private          List<NodeViewModel>? _nodes;
    private          bool                 _shown;
    private          int                  _useSpace;
    private          int                  _useNoSpace;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ConfWindow()
    {
        InitializeComponent();
        DataContext = this;
        LoadConf(Player.ConfPath);
        LoadConf(App.ConfPath);
        LoadLibplaceboConf();
        LoadSettings();
        _initialContent = GetCompareString();

        SearchText = string.IsNullOrEmpty(App.Settings.ConfigEditorSearch)
            ? "General:"
            : App.Settings.ConfigEditorSearch;

        foreach (var node in Nodes)
            SelectNodeFromSearchText(node);

        foreach (var node in Nodes)
            node.IsExpanded = true;
    }

    public ObservableCollection<string> FilterStrings { get; } = new();

    public string SearchText
    {
        get => _searchText ?? "";
        set
        {
            _searchText = value;
            SearchTextChanged();
            OnPropertyChanged();
        }
    }

    public List<NodeViewModel> Nodes
    {
        get
        {
            if (_nodes != null) return _nodes;
            var rootNode = new TreeNode();

            foreach (var setting in _settings)
                AddNode(rootNode.Children, setting.Directory!);

            _nodes = new NodeViewModel(rootNode).Children;

            return _nodes;
        }
    }

    public static TreeNode? AddNode(IList<TreeNode> nodes, string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        for (var x = 0; x < parts.Length; x++)
        {
            var found = false;

            foreach (var node in nodes)
            {
                if (x < parts.Length - 1)
                {
                    if (node.Name != parts[x]) continue;
                    found = true;
                    nodes = node.Children;
                }
                else if (x == parts.Length - 1 && node.Name == parts[x])
                {
                    found = true;
                }
            }

            if (found) continue;
            if (x != parts.Length - 1) continue;
            var item = new TreeNode() { Name = parts[x] };
            nodes?.Add(item);
            return item;
        }

        return null;
    }

    private void LoadSettings()
    {
        foreach (var setting in _settings)
        {
            setting.StartValue = setting.Value;

            if (!FilterStrings.Contains(setting.Directory!))
                FilterStrings.Add(setting.Directory!);

            foreach (var item in _confItems.Where(item => setting.Name == item.Name &&
                                                          setting.File == item.File &&
                                                          item is { Section: "", IsSectionItem: false }))
            {
                setting.Value      = item.Value;
                setting.StartValue = setting.Value;
                setting.ConfItem   = item;
                item.SettingBase   = setting;
            }

            switch (setting)
            {
                case StringSetting s :
                    MainStackPanel.Children.Add(new StringSettingControl(s) { Visibility = Visibility.Collapsed });
                    break;
                case OptionSetting s :
                    if (s.Options.Count > 3)
                        MainStackPanel.Children.Add(new ComboBoxSettingControl(s)
                                                        { Visibility = Visibility.Collapsed });
                    else
                        MainStackPanel.Children.Add(new OptionSettingControl(s) { Visibility = Visibility.Collapsed });
                    break;
            }
        }
    }

    private static string GetThemeConf() => Theme.DarkMode + App.DarkTheme + App.LightTheme;

    private string GetCompareString() => string.Join("", _settings.Select(item => item.Name + item.Value).ToArray());

    private void LoadConf(string file)
    {
        if (!File.Exists(file))
            return;

        var comment = string.Empty;
        var section = string.Empty;

        var isSectionItem = false;

        foreach (var it in File.ReadAllLines(file))
        {
            var line = it.Trim();

            if (line.StartsWith("-"))
                line = line.TrimStart('-');

            if (line == "")
                comment += "\r\n";
            else if (line.StartsWith("#"))
                comment += line.Trim() + "\r\n";
            else if (line.StartsWith("[") && line.Contains(']'))
            {
                if (!isSectionItem && comment != "" && comment != "\r\n")
                    _confItems.Add(new ConfItem()
                    {
                        Comment = comment, File = Path.GetFileNameWithoutExtension(file)
                    });

                section       = line[..(line.IndexOf("]", StringComparison.Ordinal) + 1)];
                comment       = "";
                isSectionItem = true;
            }
            else if (line.Contains('=') || Regex.Match(line, "^[\\w-]+$").Success)
            {
                if (!line.Contains('='))
                {
                    if (line.StartsWith("no-"))
                    {
                        line =  line[3..];
                        line += "=no";
                    }
                    else
                        line += "=yes";
                }

                if (line.Contains(" =") || line.Contains("= "))
                    _useSpace += 1;
                else
                    _useNoSpace += 1;

                ConfItem item = new()
                {
                    File          = Path.GetFileNameWithoutExtension(file),
                    IsSectionItem = isSectionItem,
                    Comment       = comment
                };
                comment      = "";
                item.Section = section;
                section      = "";

                if (line.Contains('#') && !line.Contains('\'') && !line.Contains('"'))
                {
                    item.LineComment = line[line.IndexOf("#", StringComparison.Ordinal)..].Trim();
                    line             = line[..line.IndexOf("#", StringComparison.Ordinal)].Trim();
                }

                var pos   = line.IndexOf("=", StringComparison.Ordinal);
                var left  = line[..pos].Trim().ToLower().TrimStart('-');
                var right = line[(pos + 1)..].Trim();

                if (right.StartsWith('\'') && right.EndsWith('\''))
                    right = right.Trim('\'');

                if (right.StartsWith('"') && right.EndsWith('"'))
                    right = right.Trim('"');

                if (left == "fs")
                    left = "fullscreen";

                if (left == "loop")
                    left = "loop-file";

                item.Name  = left;
                item.Value = right;
                _confItems.Add(item);
            }
        }
    }

    private string GetKeyValueContent(string filename)
    {
        List<string> pairs = (from setting in _settings
                              where filename              == setting.File
                              where (setting.Value ?? "") != setting.Default
                              select setting.Name + "=" + EscapeValue(setting.Value!)).ToList();

        return string.Join(',', pairs);
    }

    private void LoadLibplaceboConf()
    {
        foreach (var item in _confItems.ToArray())
            if (item.Name == "libplacebo-opts")
                LoadKeyValueList(item.Value, "libplacebo");
    }

    private void LoadKeyValueList(string options, string file)
    {
        var optionStrings = options.Split(",", StringSplitOptions.RemoveEmptyEntries);

        foreach (var pair in optionStrings)
        {
            if (!pair.Contains('='))
                continue;

            var pos   = pair.IndexOf("=", StringComparison.Ordinal);
            var left  = pair[..pos].Trim().ToLower();
            var right = pair[(pos + 1)..].Trim();

            ConfItem item = new()
            {
                Name  = left,
                Value = right,
                File  = file
            };
            _confItems.Add(item);
        }
    }

    private static string EscapeValue(string value)
    {
        if (value.Contains('\''))
            return '"' + value + '"';

        if (value.Contains('"'))
            return '\'' + value + '\'';

        if (value.Contains('"')   || value.Contains('#') || value.StartsWith("%") ||
            value.StartsWith(" ") || value.EndsWith(" "))
        {
            return '\'' + value + '\'';
        }

        return value;
    }

    private string GetContent(string filename)
    {
        var sb           = new StringBuilder();
        var namesWritten = new List<string>();
        var equalString  = _useSpace > _useNoSpace ? " = " : "=";

        foreach (var item in _confItems.Where(item => filename == item.File &&
                                                      item is { Section: "", IsSectionItem: false }))
        {
            if (item.Comment != "")
                sb.Append(item.Comment);

            if (item.SettingBase == null)
            {
                if (item.Name == "") continue;
                sb.Append(item.Name + equalString + EscapeValue(item.Value));

                if (item.LineComment != "")
                    sb.Append(" " + item.LineComment);

                sb.AppendLine();
                namesWritten.Add(item.Name);
            }
            else if ((item.SettingBase.Value ?? "") != item.SettingBase.Default)
            {
                sb.Append(item.Name + equalString + EscapeValue(item.SettingBase.Value!));

                if (item.LineComment != "")
                    sb.Append(" " + item.LineComment);

                sb.AppendLine();
                namesWritten.Add(item.Name);
            }
        }

        foreach (var setting in _settings
                               .Where(setting => filename == setting.File && !namesWritten.Contains(setting.Name!))
                               .Where(setting => (setting.Value ?? "") != setting.Default))
        {
            sb.AppendLine(setting.Name + equalString + EscapeValue(setting.Value!));
        }

        foreach (var item in _confItems.Where(item => filename == item.File &&
                                                      (item.Section != "" || item.IsSectionItem)))
        {
            if (item.Section != "")
            {
                if (!sb.ToString().EndsWith("\r\n\r\n"))
                    sb.AppendLine();

                sb.AppendLine(item.Section);
            }

            if (item.Comment != "")
                sb.Append(item.Comment);

            sb.Append(item.Name + equalString + EscapeValue(item.Value));

            if (item.LineComment != "")
                sb.Append(" " + item.LineComment);

            sb.AppendLine();
            namesWritten.Add(item.Name);
        }

        return "\r\n" + sb.ToString().Trim() + "\r\n";
    }

    private void SearchTextChanged()
    {
        var activeFilter = "";

        foreach (var i in FilterStrings)
            if (SearchText == i + ":")
                activeFilter = i;

        if (activeFilter == "")
        {
            foreach (UIElement i in MainStackPanel.Children)
                if ((i as ISettingControl)!.Contains(SearchText) && SearchText.Length > 1)
                    i.Visibility = Visibility.Visible;
                else
                    i.Visibility = Visibility.Collapsed;

            foreach (var node in Nodes)
                UnselectNode(node);
        }
        else
            foreach (UIElement i in MainStackPanel.Children)
                i.Visibility = (i as ISettingControl)!.Setting.Directory == activeFilter
                    ? Visibility.Visible
                    : Visibility.Collapsed;

        MainScrollViewer.ScrollToTop();
    }

    private void ConfWindow1_Loaded(object sender, RoutedEventArgs e)
    {
        SearchControl.SearchTextBox.SelectAll();
        Keyboard.Focus(SearchControl.SearchTextBox);

        foreach (var i in MainStackPanel.Children.OfType<StringSettingControl>())
            i.Update();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        App.Settings.ConfigEditorSearch = SearchText;

        if (_initialContent == GetCompareString())
            return;

        foreach (var setting in _settings.Where(setting => setting.Name == "libplacebo-opts"))
        {
            setting.Value = GetKeyValueContent("libplacebo");
            break;
        }

        File.WriteAllText(Player.ConfPath, GetContent("mpv"));
        File.WriteAllText(App.ConfPath, GetContent("mpvnet"));

        foreach (var it in _settings.Where(it => it.Value != it.StartValue))
        {
            switch (it.File)
            {
                case "mpv" :
                    Player.ProcessProperty(it.Name, it.Value);
                    Player.SetPropertyString(it.Name!, it.Value!);
                    break;
                case "mpvnet" :
                    App.ProcessProperty(it.Name ?? "", it.Value ?? "", true);
                    break;
            }
        }

        Theme.Init();
        Theme.UpdateWpfColors();

        if (_themeConf != GetThemeConf())
            MessageBox.Show("Changed theme settings require mpv.net being restarted.", "Info");
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        switch (e.Key)
        {
            case Key.Escape :
                Close();
                break;
            case Key.F3 :
            case Key.F6 :
            case Key.F when Keyboard.Modifiers == ModifierKeys.Control :
                Keyboard.Focus(SearchControl.SearchTextBox);
                SearchControl.SearchTextBox.SelectAll();
                break;
            case Key.None :
                break;
            case Key.Cancel :
                break;
            case Key.Back :
                break;
            case Key.Tab :
                break;
            case Key.LineFeed :
                break;
            case Key.Clear :
                break;
            case Key.Enter :
                break;
            case Key.Pause :
                break;
            case Key.Capital :
                break;
            case Key.HangulMode :
                break;
            case Key.JunjaMode :
                break;
            case Key.FinalMode :
                break;
            case Key.HanjaMode :
                break;
            case Key.ImeConvert :
                break;
            case Key.ImeNonConvert :
                break;
            case Key.ImeAccept :
                break;
            case Key.ImeModeChange :
                break;
            case Key.Space :
                break;
            case Key.PageUp :
                break;
            case Key.Next :
                break;
            case Key.End :
                break;
            case Key.Home :
                break;
            case Key.Left :
                break;
            case Key.Up :
                break;
            case Key.Right :
                break;
            case Key.Down :
                break;
            case Key.Select :
                break;
            case Key.Print :
                break;
            case Key.Execute :
                break;
            case Key.PrintScreen :
                break;
            case Key.Insert :
                break;
            case Key.Delete :
                break;
            case Key.Help :
                break;
            case Key.D0 :
                break;
            case Key.D1 :
                break;
            case Key.D2 :
                break;
            case Key.D3 :
                break;
            case Key.D4 :
                break;
            case Key.D5 :
                break;
            case Key.D6 :
                break;
            case Key.D7 :
                break;
            case Key.D8 :
                break;
            case Key.D9 :
                break;
            case Key.A :
                break;
            case Key.B :
                break;
            case Key.C :
                break;
            case Key.D :
                break;
            case Key.E :
                break;
            case Key.G :
                break;
            case Key.H :
                break;
            case Key.I :
                break;
            case Key.J :
                break;
            case Key.K :
                break;
            case Key.L :
                break;
            case Key.M :
                break;
            case Key.N :
                break;
            case Key.O :
                break;
            case Key.P :
                break;
            case Key.Q :
                break;
            case Key.R :
                break;
            case Key.S :
                break;
            case Key.T :
                break;
            case Key.U :
                break;
            case Key.V :
                break;
            case Key.W :
                break;
            case Key.X :
                break;
            case Key.Y :
                break;
            case Key.Z :
                break;
            case Key.LWin :
                break;
            case Key.RWin :
                break;
            case Key.Apps :
                break;
            case Key.Sleep :
                break;
            case Key.NumPad0 :
                break;
            case Key.NumPad1 :
                break;
            case Key.NumPad2 :
                break;
            case Key.NumPad3 :
                break;
            case Key.NumPad4 :
                break;
            case Key.NumPad5 :
                break;
            case Key.NumPad6 :
                break;
            case Key.NumPad7 :
                break;
            case Key.NumPad8 :
                break;
            case Key.NumPad9 :
                break;
            case Key.Multiply :
                break;
            case Key.Add :
                break;
            case Key.Separator :
                break;
            case Key.Subtract :
                break;
            case Key.Decimal :
                break;
            case Key.Divide :
                break;
            case Key.F1 :
                break;
            case Key.F2 :
                break;
            case Key.F4 :
                break;
            case Key.F5 :
                break;
            case Key.F7 :
                break;
            case Key.F8 :
                break;
            case Key.F9 :
                break;
            case Key.F10 :
                break;
            case Key.F11 :
                break;
            case Key.F12 :
                break;
            case Key.F13 :
                break;
            case Key.F14 :
                break;
            case Key.F15 :
                break;
            case Key.F16 :
                break;
            case Key.F17 :
                break;
            case Key.F18 :
                break;
            case Key.F19 :
                break;
            case Key.F20 :
                break;
            case Key.F21 :
                break;
            case Key.F22 :
                break;
            case Key.F23 :
                break;
            case Key.F24 :
                break;
            case Key.NumLock :
                break;
            case Key.Scroll :
                break;
            case Key.LeftShift :
                break;
            case Key.RightShift :
                break;
            case Key.LeftCtrl :
                break;
            case Key.RightCtrl :
                break;
            case Key.LeftAlt :
                break;
            case Key.RightAlt :
                break;
            case Key.BrowserBack :
                break;
            case Key.BrowserForward :
                break;
            case Key.BrowserRefresh :
                break;
            case Key.BrowserStop :
                break;
            case Key.BrowserSearch :
                break;
            case Key.BrowserFavorites :
                break;
            case Key.BrowserHome :
                break;
            case Key.VolumeMute :
                break;
            case Key.VolumeDown :
                break;
            case Key.VolumeUp :
                break;
            case Key.MediaNextTrack :
                break;
            case Key.MediaPreviousTrack :
                break;
            case Key.MediaStop :
                break;
            case Key.MediaPlayPause :
                break;
            case Key.LaunchMail :
                break;
            case Key.SelectMedia :
                break;
            case Key.LaunchApplication1 :
                break;
            case Key.LaunchApplication2 :
                break;
            case Key.Oem1 :
                break;
            case Key.OemPlus :
                break;
            case Key.OemComma :
                break;
            case Key.OemMinus :
                break;
            case Key.OemPeriod :
                break;
            case Key.Oem2 :
                break;
            case Key.Oem3 :
                break;
            case Key.AbntC1 :
                break;
            case Key.AbntC2 :
                break;
            case Key.Oem4 :
                break;
            case Key.Oem5 :
                break;
            case Key.Oem6 :
                break;
            case Key.Oem7 :
                break;
            case Key.Oem8 :
                break;
            case Key.Oem102 :
                break;
            case Key.ImeProcessed :
                break;
            case Key.System :
                break;
            case Key.DbeAlphanumeric :
                break;
            case Key.DbeKatakana :
                break;
            case Key.DbeHiragana :
                break;
            case Key.DbeSbcsChar :
                break;
            case Key.DbeDbcsChar :
                break;
            case Key.DbeRoman :
                break;
            case Key.Attn :
                break;
            case Key.CrSel :
                break;
            case Key.DbeEnterImeConfigureMode :
                break;
            case Key.DbeFlushString :
                break;
            case Key.DbeCodeInput :
                break;
            case Key.DbeNoCodeInput :
                break;
            case Key.DbeDetermineString :
                break;
            case Key.DbeEnterDialogConversionMode :
                break;
            case Key.OemClear :
                break;
            case Key.DeadCharProcessed :
                break;
            default :
                throw new ArgumentOutOfRangeException();
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        if (_shown)
            return;

        _shown = true;

        Application.Current.Dispatcher.BeginInvoke(() => { SearchControl.SearchTextBox.SelectAll(); },
                                                   DispatcherPriority.Background);
    }

    private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (TreeView.SelectedItem is not NodeViewModel node)
            return;

        Application.Current.Dispatcher.BeginInvoke(() => { SearchText = node!.Path + ":"; },
                                                   DispatcherPriority.Background);
    }

    private void SelectNodeFromSearchText(NodeViewModel node)
    {
        if (node.Path + ":" == SearchText)
        {
            node.IsSelected = true;
            node.IsExpanded = true;
            return;
        }

        foreach (var it in node.Children)
            SelectNodeFromSearchText(it);
    }

    private static void UnselectNode(NodeViewModel node)
    {
        if (node.IsSelected)
            node.IsSelected = false;

        foreach (var it in node.Children)
            UnselectNode(it);
    }

    [RelayCommand]
    private void ShowMpvNetSpecificSettings() => SearchText = "mpv.net";

    [RelayCommand]
    private void PreviewMpvConfFile() => Msg.ShowInfo(GetContent("mpv"));

    [RelayCommand]
    private void PreviewMpvNetConfFile() => Msg.ShowInfo(GetContent("mpvnet"));

    [RelayCommand]
    private void ShowMpvManual() => ProcessHelp.ShellExecute("https://mpv.io/manual/master/");

    [RelayCommand]
    private void ShowMpvNetManual() =>
        ProcessHelp.ShellExecute("https://github.com/mpvnet-player/mpv.net/blob/main/docs/manual.md");
}
