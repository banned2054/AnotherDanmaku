namespace MpvNet.Windows.WPF.HandyControl.Data
{
    internal static class ValueBoxes
    {
        internal static object TrueBox    = true;
        internal static object FalseBox   = false;
        internal static object Double0Box = .0;
        internal static object BooleanBox(bool value) => value ? TrueBox : FalseBox;
    }
}
