using MpvNet.Help;

namespace MpvNet;

public class Command
{
    Dictionary<string, Action<IList<string>>>? _commands;

    public static Command Current { get; } = new();

    public Dictionary<string, Action<IList<string>>> Commands => _commands ??= new()
    {
        ["open-conf-folder"] = args => ProcessHelp.ShellExecute(Player.ConfigFolder),
        ["play-pause"]       = PlayPause,
        ["shell-execute"]    = args => ProcessHelp.ShellExecute(args[0]),
        ["show-text"]        = args => ShowText(args[0], Convert.ToInt32(args[1]), Convert.ToInt32(args[2])),
        ["cycle-audio"]      = args => CycleAudio(),
        ["cycle-subtitles"]  = args => CycleSubtitles(),
        ["playlist-first"]   = args => PlaylistFirst(),
        ["playlist-last"]    = args => PlaylistLast(),
    };

    private static void PlayPause(IList<string> args)
    {
        var count = Player.GetPropertyInt("playlist-count");

        if (count > 0)
            Player.Command("cycle pause");
        else if (App.Settings.RecentFiles.Count > 0)
        {
            foreach (var i in App.Settings.RecentFiles.Where(i => i.Contains("://") || File.Exists(i)))
            {
                Player.LoadFiles(new[] { i }, true, false);
                break;
            }
        }
    }

    public static void ShowText(string text, int duration = 0, int fontSize = 0)
    {
        if (string.IsNullOrEmpty(text))
            return;

        if (duration == 0)
            duration = Player.GetPropertyInt("osd-duration");

        if (fontSize == 0)
            fontSize = Player.GetPropertyInt("osd-font-size");

        Player.Command("show-text \"${osd-ass-cc/0}{\\\\fs" + fontSize +
                       "}${osd-ass-cc/1}"                   + text     + "\" " + duration);
    }

    private static void CycleAudio()
    {
        Player.UpdateExternalTracks();

        lock (Player.MediaTracksLock)
        {
            var tracks = Player.MediaTracks.Where(track => track.Type == "a").ToArray();

            if (tracks.Length < 1)
            {
                Player.CommandV("show-text", "No audio tracks");
                return;
            }

            var aid = Player.GetPropertyInt("aid");

            if (tracks.Length > 1)
            {
                if (++aid > tracks.Length)
                    aid = 1;

                Player.SetPropertyInt("aid", aid);
            }

            Player.CommandV("show-text", aid + "/" + tracks.Length + ": " + tracks[aid - 1].Text[3..], "5000");
        }
    }

    private static void CycleSubtitles()
    {
        Player.UpdateExternalTracks();

        lock (Player.MediaTracksLock)
        {
            var tracks = Player.MediaTracks.Where(track => track.Type == "s").ToArray();

            if (tracks.Length < 1)
            {
                Player.CommandV("show-text", "No subtitles");
                return;
            }

            var sid = Player.GetPropertyInt("sid");

            if (tracks.Length > 1)
            {
                if (++sid > tracks.Length)
                    sid = 0;

                Player.SetPropertyInt("sid", sid);
            }

            if (sid == 0)
                Player.CommandV("show-text", "No subtitle");
            else
                Player.CommandV("show-text", sid + "/" + tracks.Length + ": " + tracks[sid - 1].Text[3..], "5000");
        }
    }

    private static void PlaylistFirst()
    {
        if (Player.PlaylistPos != 0)
            Player.SetPropertyInt("playlist-pos", 0);
    }

    private static void PlaylistLast()
    {
        var count = Player.GetPropertyInt("playlist-count");

        if (Player.PlaylistPos < count - 1)
            Player.SetPropertyInt("playlist-pos", count - 1);
    }
}
