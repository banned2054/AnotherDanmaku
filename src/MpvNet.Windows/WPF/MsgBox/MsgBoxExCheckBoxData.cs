using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MpvNet.Windows.WPF.MsgBox;

public class MsgBoxExCheckBoxData : INotifyPropertyChanged
{
    private bool _isModified;

    public bool IsModified
    {
        get => _isModified;
        set
        {
            if (value == _isModified) return;
            _isModified = true;
            NotifyPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        if (PropertyChanged == null) return;
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));

        if (propertyName != "IsModified")
            IsModified = true;
    }

    private string? _checkBoxText;
    private bool    _checkBoxIsChecked;

    public string? CheckBoxText
    {
        get => _checkBoxText;
        set
        {
            if (_checkBoxText != null && value != _checkBoxText)
                _checkBoxText = value;
            NotifyPropertyChanged();
        }
    }

    public bool CheckBoxIsChecked
    {
        get => _checkBoxIsChecked;
        set
        {
            if (value == _checkBoxIsChecked) return;
            _checkBoxIsChecked = value;
            NotifyPropertyChanged();
        }
    }
}
