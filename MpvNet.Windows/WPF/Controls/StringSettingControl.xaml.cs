using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace MpvNet.Windows.WPF.Controls;

public partial class StringSettingControl : ISettingControl
{
    private readonly StringSetting _stringSetting;

    public StringSettingControl(StringSetting stringSetting)
    {
        _stringSetting = stringSetting;
        InitializeComponent();
        DataContext       = this;
        TitleTextBox.Text = stringSetting.Name;
        HelpTextBox.Text  = stringSetting.Help;
        ValueTextBox.Text = _stringSetting.Value;

        if (_stringSetting.Width > 0)
            ValueTextBox.Width = _stringSetting.Width;

        if (_stringSetting.Type != "folder" && _stringSetting.Type != "color")
            Button.Visibility = Visibility.Hidden;

        Link.SetUrl(_stringSetting.Url);

        if (string.IsNullOrEmpty(stringSetting.Url))
            LinkTextBlock.Visibility = Visibility.Collapsed;

        if (string.IsNullOrEmpty(stringSetting.Help))
            HelpTextBox.Visibility = Visibility.Collapsed;
    }

    public bool Contains(string search)
    {
        if (TitleTextBox.Text.IndexOf(search, StringComparison.InvariantCultureIgnoreCase) > -1)
            return true;

        if (HelpTextBox.Text.IndexOf(search, StringComparison.InvariantCultureIgnoreCase) > -1)
            return true;

        return ValueTextBox.Text.IndexOf(search, StringComparison.InvariantCultureIgnoreCase) > -1;
    }

    public Setting Setting => _stringSetting;

    public string? Text
    {
        get => _stringSetting.Value;
        set => _stringSetting.Value = value;
    }

    private void ButtonClick(object sender, RoutedEventArgs e)
    {
        switch (_stringSetting.Type)
        {
            case "folder" :
            {
                var dialog = new Forms.FolderBrowserDialog { InitialDirectory = ValueTextBox.Text };

                if (dialog.ShowDialog() == Forms.DialogResult.OK)
                    ValueTextBox.Text = dialog.SelectedPath;
            }
                break;
            case "color" :
                using (var dialog = new Forms.ColorDialog())
                {
                    dialog.FullOpen = true;

                    try
                    {
                        if (!string.IsNullOrEmpty(ValueTextBox.Text))
                        {
                            var col = GetColor(ValueTextBox.Text);
                            dialog.Color = System.Drawing.Color.FromArgb(col.A, col.R, col.G, col.B);
                        }
                    }
                    catch
                    {
                        // ignored
                    }

                    if (dialog.ShowDialog() == Forms.DialogResult.OK)
                        ValueTextBox.Text = "#" + dialog.Color.ToArgb().ToString("X8");
                }

                break;
        }
    }

    private void ValueTextBox_TextChanged(object sender, TextChangedEventArgs e) => Update();

    private static Color GetColor(string value)
    {
        if (!value.Contains('/')) return (Color)ColorConverter.ConvertFromString(value);
        var a = value.Split('/');

        return a.Length switch
        {
            3 => Color.FromRgb(ToByte(a[0]), ToByte(a[1]), ToByte(a[2])),
            4 => Color.FromArgb(ToByte(a[3]), ToByte(a[0]), ToByte(a[1]), ToByte(a[2])),
            _ => (Color)ColorConverter.ConvertFromString(value)
        };

        byte ToByte(string val) => Convert.ToByte(Convert.ToSingle(val, CultureInfo.InvariantCulture) * 255);
    }

    public void Update()
    {
        if (_stringSetting.Type != "color") return;
        var color = Colors.Transparent;

        if (ValueTextBox.Text != "")
            try
            {
                color = GetColor(ValueTextBox.Text);
            }
            catch
            {
                // ignored
            }

        ValueTextBox.Background = new SolidColorBrush(color);
    }
}
