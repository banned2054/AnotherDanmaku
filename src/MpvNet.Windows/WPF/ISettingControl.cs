namespace MpvNet.Windows.WPF;

internal interface ISettingControl
{
    bool    Contains(string searchString);
    Setting Setting { get; }
}
