namespace MpvNet;

public static class Terminal
{
    private static int Padding => 60;

    public static void WriteError(object obj, string module = "mpv.net") =>
        Write(obj, module, ConsoleColor.DarkRed, false);

    public static void Write(object obj, string module = "mpv.net") => Write(obj, module, ConsoleColor.Black, true);

    public static void Write(object obj, string module, ConsoleColor color, bool useDefaultColor)
    {
        var text = obj + "";

        if (text == "")
            return;

        if (!string.IsNullOrEmpty(module))
            module = "[" + module + "] ";

        if (useDefaultColor)
            Console.ResetColor();
        else
            Console.ForegroundColor = color;

        text = module + text;

        if (text.Length < Padding)
            text = text.PadRight(Padding);

        if (color is ConsoleColor.Red or ConsoleColor.DarkRed)
            Console.Error.WriteLine(text);
        else
            Console.WriteLine(text);

        Console.ResetColor();
        Trace.WriteLine(obj);
    }
}
