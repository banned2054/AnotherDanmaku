namespace MpvNet;

public static class Global
{
    public static readonly string     Br     = Environment.NewLine;
    public static readonly string     Br2    = Environment.NewLine + Environment.NewLine;
    public static readonly MainPlayer Player = new();
    public static readonly MainPlayer Core   = Player; // deprecated
    public static readonly AppClass   App    = new();

    public static string _(string  value)                 => Translator.Current!.Gettext(value);
    public static string _p(string context, string value) => Translator.Current!.GetParticularString(context, value);
}
