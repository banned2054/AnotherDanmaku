using System.Windows;

namespace MpvNet.Windows;

public abstract class Setting
{
    public string? Default    { get; set; }
    public string? File       { get; set; }
    public string? Directory  { get; set; }
    public string? Help       { get; set; }
    public string? Name       { get; set; }
    public string? StartValue { get; set; }
    public string? Type       { get; set; }
    public string? Url        { get; set; }
    public string? Value      { get; set; }

    public int Width           { get; set; }
    public int OptionNameWidth { get; set; } = 100;

    public ConfItem? ConfItem { get; set; }
}

public class StringSetting : Setting
{
}

public class OptionSetting : Setting
{
    public List<OptionSettingOption> Options { get; } = new();
}

public class OptionSettingOption
{
    private string? _text;

    public string? Name { get; set; }
    public string? Help { get; set; }

    public int Width => OptionSetting!.OptionNameWidth;

    public OptionSetting? OptionSetting { get; set; }

    public string? Text
    {
        get => _text ?? Name;
        set => _text = value;
    }

    public bool Checked
    {
        get => OptionSetting?.Value == Name;
        set
        {
            if (value)
                OptionSetting!.Value = Name;
        }
    }

    public Visibility Visibility => string.IsNullOrEmpty(Help) ? Visibility.Collapsed : Visibility.Visible;
}
