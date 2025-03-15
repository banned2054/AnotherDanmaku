using CommunityToolkit.Mvvm.Input;

namespace MpvNet.Windows.WPF.ViewModels;

public partial class AboutViewModel : ViewModelBase
{
    public Action? CloseAction { get; set; }

    [RelayCommand]
    public void Close() => CloseAction!();
}
