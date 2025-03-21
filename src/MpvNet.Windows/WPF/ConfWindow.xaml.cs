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

public partial class ConfWindow : Window, INotifyPropertyChanged
{
    private readonly List<Setting>  _settings  = Conf.LoadConf(Properties.Resources.editor_conf.TrimEnd());
    private readonly List<ConfItem> _confItems = new();
    private readonly string         _initialContent;
    private readonly string         _themeConf = GetThemeConf();

    private string?              _searchText;
    private List<NodeViewModel>? _nodes;

    private bool _shown;
    private int  _useSpace;
    private int  _useNoSpace;

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

    public Theme? Theme => Theme.Current;

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

        var comment = "";
        var section = "";

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

    void LoadKeyValueList(string options, string file)
    {
        string[] optionStrings = options.Split(",", StringSplitOptions.RemoveEmptyEntries);

        foreach (string pair in optionStrings)
        {
            if (!pair.Contains('='))
                continue;

            int    pos   = pair.IndexOf("=");
            string left  = pair.Substring(0, pos).Trim().ToLower();
            string right = pair.Substring(pos + 1).Trim();

            ConfItem item = new();
            item.Name  = left;
            item.Value = right;
            item.File  = file;
            _confItems.Add(item);
        }
    }

    string EscapeValue(string value)
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

    string GetContent(string filename)
    {
        StringBuilder sb           = new StringBuilder();
        List<string>  namesWritten = new List<string>();
        string        equalString  = _useSpace > _useNoSpace ? " = " : "=";

        foreach (ConfItem item in _confItems)
        {
            if (filename != item.File || item.Section != "" || item.IsSectionItem)
                continue;

            if (item.Comment != "")
                sb.Append(item.Comment);

            if (item.SettingBase == null)
            {
                if (item.Name != "")
                {
                    sb.Append(item.Name + equalString + EscapeValue(item.Value));

                    if (item.LineComment != "")
                        sb.Append(" " + item.LineComment);

                    sb.AppendLine();
                    namesWritten.Add(item.Name);
                }
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

        foreach (Setting setting in _settings)
        {
            if (filename != setting.File || namesWritten.Contains(setting.Name!))
                continue;

            if ((setting.Value ?? "") != setting.Default)
                sb.AppendLine(setting.Name + equalString + EscapeValue(setting.Value!));
        }

        foreach (ConfItem item in _confItems)
        {
            if (filename != item.File || (item.Section == "" && !item.IsSectionItem))
                continue;

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

    void SearchTextChanged()
    {
        string activeFilter = "";

        foreach (string i in FilterStrings)
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
                if ((i as ISettingControl)!.Setting.Directory == activeFilter)
                    i.Visibility = Visibility.Visible;
                else
                    i.Visibility = Visibility.Collapsed;

        MainScrollViewer.ScrollToTop();
    }

    void ConfWindow1_Loaded(object sender, RoutedEventArgs e)
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

        foreach (Setting setting in _settings)
        {
            if (setting.Name == "libplacebo-opts")
            {
                setting.Value = GetKeyValueContent("libplacebo");
                break;
            }
        }

        File.WriteAllText(Player.ConfPath, GetContent("mpv"));
        File.WriteAllText(App.ConfPath, GetContent("mpvnet"));

        foreach (Setting it in _settings)
        {
            if (it.Value != it.StartValue)
            {
                if (it.File == "mpv")
                {
                    Player.ProcessProperty(it.Name, it.Value);
                    Player.SetPropertyString(it.Name!, it.Value!);
                }
                else if (it.File == "mpvnet")
                    App.ProcessProperty(it.Name ?? "", it.Value ?? "", true);
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

        if (e.Key == Key.Escape)
            Close();

        if (e.Key == Key.F3 || e.Key == Key.F6 || (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control))
        {
            Keyboard.Focus(SearchControl.SearchTextBox);
            SearchControl.SearchTextBox.SelectAll();
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

    void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        var node = TreeView.SelectedItem as NodeViewModel;

        if (node == null)
            return;

        Application.Current.Dispatcher.BeginInvoke(() => { SearchText = node!.Path + ":"; },
                                                   DispatcherPriority.Background);
    }

    void SelectNodeFromSearchText(NodeViewModel node)
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

    void UnselectNode(NodeViewModel node)
    {
        if (node.IsSelected)
            node.IsSelected = false;

        foreach (var it in node.Children)
            UnselectNode(it);
    }

    [RelayCommand]
    void ShowMpvNetSpecificSettings() => SearchText = "mpv.net";

    [RelayCommand]
    void PreviewMpvConfFile() => Msg.ShowInfo(GetContent("mpv"));

    [RelayCommand]
    void PreviewMpvNetConfFile() => Msg.ShowInfo(GetContent("mpvnet"));

    [RelayCommand]
    void ShowMpvManual() => ProcessHelp.ShellExecute("https://mpv.io/manual/master/");

    [RelayCommand]
    void ShowMpvNetManual() =>
        ProcessHelp.ShellExecute("https://github.com/mpvnet-player/mpv.net/blob/main/docs/manual.md");
}
