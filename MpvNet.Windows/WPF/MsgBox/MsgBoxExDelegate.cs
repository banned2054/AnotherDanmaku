using System.Windows;

namespace MpvNet.Windows.WPF.MsgBox;

public abstract class MsgBoxExDelegate
{
    public virtual MessageBoxResult PerformAction(string message, string? details = null)
    {
        throw new NotImplementedException();
    }
}
