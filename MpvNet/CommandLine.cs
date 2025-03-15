namespace MpvNet;

public class CommandLine
{
    private static List<StringPair>? _arguments;

    private static string[] PreInitProperties { get; } =
    {
        "input-terminal", "terminal", "input-file", "config", "o",
        "config-dir", "input-conf", "load-scripts", "scripts", "player-operation-mode",
        "idle", "log-file", "msg-color", "dump-stats", "msg-level", "really-quiet"
    };

    public static List<StringPair> Arguments
    {
        get
        {
            if (_arguments != null)
                return _arguments;

            _arguments = new List<StringPair>();

            foreach (var i in Environment.GetCommandLineArgs().Skip(1))
            {
                var arg = i;

                if (!arg.StartsWith("--"))
                    continue;

                if (!arg.Contains('='))
                {
                    if (arg.Contains("--no-"))
                    {
                        arg =  arg.Replace("--no-", "--");
                        arg += "=no";
                    }
                    else
                        arg += "=yes";
                }

                var left  = arg[2..arg.IndexOf("=", StringComparison.Ordinal)];
                var right = arg[(left.Length + 3)..];

                if (string.IsNullOrEmpty(left))
                    continue;

                left = left switch
                {
                    "script"        => "scripts",
                    "audio-file"    => "audio-files",
                    "sub-file"      => "sub-files",
                    "external-file" => "external-files",
                    _               => left
                };

                _arguments.Add(new StringPair(left, right));
            }

            return _arguments;
        }
    }

    public static void ProcessCommandLineArgsPreInit()
    {
        foreach (var pair in Arguments.Where(pair => !pair.Name.EndsWith("-add")    &&
                                                     !pair.Name.EndsWith("-set")    &&
                                                     !pair.Name.EndsWith("-pre")    &&
                                                     !pair.Name.EndsWith("-clr")    &&
                                                     !pair.Name.EndsWith("-append") &&
                                                     !pair.Name.EndsWith("-remove") &&
                                                     !pair.Name.EndsWith("-toggle")))
        {
            Player.ProcessProperty(pair.Name, pair.Value);

            if (!App.ProcessProperty(pair.Name, pair.Value))
                Player.SetPropertyString(pair.Name, pair.Value);
        }
    }

    public static void ProcessCommandLineArgsPostInit()
    {
        foreach (var pair in Arguments.Where(pair => !PreInitProperties.Contains(pair.Name)))
        {
            if (pair.Name.EndsWith("-add"))
                Player.CommandV("change-list", pair.Name[..^4], "add", pair.Value);
            else if (pair.Name.EndsWith("-set"))
                Player.CommandV("change-list", pair.Name[..^4], "set", pair.Value);
            else if (pair.Name.EndsWith("-append"))
                Player.CommandV("change-list", pair.Name[..^7], "append", pair.Value);
            else if (pair.Name.EndsWith("-pre"))
                Player.CommandV("change-list", pair.Name[..^4], "pre", pair.Value);
            else if (pair.Name.EndsWith("-clr"))
                Player.CommandV("change-list", pair.Name[..^4], "clr", "");
            else if (pair.Name.EndsWith("-remove"))
                Player.CommandV("change-list", pair.Name[..^7], "remove", pair.Value);
            else if (pair.Name.EndsWith("-toggle"))
                Player.CommandV("change-list", pair.Name[..^7], "toggle", pair.Value);
            else
            {
                Player.ProcessProperty(pair.Name, pair.Value);

                if (!App.ProcessProperty(pair.Name, pair.Value))
                    Player.SetPropertyString(pair.Name, pair.Value);
            }
        }
    }

    public static void ProcessCommandLineFiles()
    {
        Player.LoadFiles(Environment.GetCommandLineArgs().Skip(1).Where(arg => !arg.StartsWith("--") && (arg == "-" || arg.Contains("://") || arg.Contains(":\\") || arg.StartsWith("\\\\") || arg.StartsWith(".") || File.Exists(arg))).ToArray(),
                         !App.Queue, App.Queue);

        if (!App.CommandLine.Contains("--shuffle")) return;
        Player.Command("playlist-shuffle");
        Player.SetPropertyInt("playlist-pos", 0);
    }

    public static bool Contains(string name)
    {
        return Arguments.Any(pair => pair.Name == name);
    }

    public static string GetValue(string name)
    {
        foreach (var pair in Arguments.Where(pair => pair.Name == name))
            return pair.Value;

        return "";
    }
}
