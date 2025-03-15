namespace MpvNet.Windows.WPF.MsgBox;

public enum MessageBoxButtonEx
{
    Ok = 0,
    OkCancel,
    AbortRetryIgnore,
    YesNoCancel,
    YesNo,
    RetryCancel
}

public enum MessageBoxResultEx
{
    Ok,
    Cancel,
    Abort,
    Retry,
    Ignore,
    Yes,
    No
}

public enum MessageBoxButtonDefault
{
    Ok,
    Cancel,
    Yes,
    No,
    Abort,
    Retry,
    Ignore, // specific button
    Button1,
    Button2,
    Button3, // button by ordinal left-to-right position
    MostPositive,
    LeastPositive, // button by positivity
    Forms,         // button according to the Windows.Forms standard messagebox
    None           // no default button
}
