using System.Windows;
using System.Windows.Controls;

namespace MpvNet.Windows.WPF;

public partial class OptionSettingControl : ISettingControl
{
    private readonly OptionSetting _optionSetting;

    public OptionSettingControl(OptionSetting optionSetting)
    {
        _optionSetting = optionSetting;
        InitializeComponent();
        DataContext       = this;
        TitleTextBox.Text = optionSetting.Name;

        if (string.IsNullOrEmpty(optionSetting.Help))
            HelpTextBox.Visibility = Visibility.Collapsed;

        HelpTextBox.Text = optionSetting.Help;

        if (string.IsNullOrEmpty(optionSetting.Help))
            LinkTextBlock.Margin = new Thickness(2, 6, 0, 0);

        ItemsControl.ItemsSource = optionSetting.Options;

        if (string.IsNullOrEmpty(optionSetting.Url))
            LinkTextBlock.Visibility = Visibility.Collapsed;

        Link.SetUrl(optionSetting.Url);
    }

    public Setting Setting => _optionSetting;

    public bool Contains(string searchString) => ContainsInternal(searchString.ToLower());

    public bool ContainsInternal(string search)
    {
        if (TitleTextBox.Text.IndexOf(search, StringComparison.InvariantCultureIgnoreCase) > -1)
            return true;

        if (HelpTextBox.Text.IndexOf(search, StringComparison.InvariantCultureIgnoreCase) > -1)
            return true;

        foreach (var i in _optionSetting.Options)
        {
            if (i.Text?.IndexOf(search, StringComparison.InvariantCultureIgnoreCase) > -1)
                return true;

            if (i.Help?.IndexOf(search, StringComparison.InvariantCultureIgnoreCase) > -1)
                return true;

            if (i.Name?.IndexOf(search, StringComparison.InvariantCultureIgnoreCase) > -1)
                return true;
        }

        return false;
    }
}
