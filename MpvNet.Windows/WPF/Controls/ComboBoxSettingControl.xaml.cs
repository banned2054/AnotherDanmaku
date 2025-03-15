using System.Windows;
using System.Windows.Controls;

namespace MpvNet.Windows.WPF.Controls;

public partial class ComboBoxSettingControl : ISettingControl
{
    private readonly OptionSetting _optionSetting;

    public ComboBoxSettingControl(OptionSetting optionSetting)
    {
        _optionSetting = optionSetting;
        InitializeComponent();
        DataContext       = this;
        TitleTextBox.Text = optionSetting.Name;

        if (string.IsNullOrEmpty(optionSetting.Help))
            HelpTextBox.Visibility = Visibility.Collapsed;

        HelpTextBox.Text            = optionSetting.Help;
        ComboBoxControl.ItemsSource = optionSetting.Options;

        foreach (var item in optionSetting.Options.Where(item => item.Name == optionSetting.Value))
            ComboBoxControl.SelectedItem = item;

        if (string.IsNullOrEmpty(optionSetting.Url))
            LinkTextBlock.Visibility = Visibility.Collapsed;

        Link.SetURL(optionSetting.Url);
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

    private void ComboBoxControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _optionSetting.Value = (ComboBoxControl.SelectedItem as OptionSettingOption)?.Name;
    }
}
