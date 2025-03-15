using MpvNet.ExtensionMethod;
using MpvNet.Help;
using MpvNet.Native;
using MpvNet.Windows.Help;
using MpvNet.Windows.WinForms;
using MpvNet.Windows.WPF;
using MpvNet.Windows.WPF.MsgBox;
using MpvNet.Windows.WPF.Views;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

namespace MpvNet.Windows;

public class GuiCommand
{
    Dictionary<string, Action<IList<string>>>? _commands;

    public event Action<float>?  ScaleWindow;
    public event Action<string>? MoveWindow;
    public event Action<float>?  WindowScaleNet;
    public event Action?         ShowMenu;

    public static GuiCommand Current { get; } = new();

    public Dictionary<string, Action<IList<string>>> Commands => _commands ??= new()
    {
        ["add-to-path"] = _ => AddToPath(),
        ["edit-conf-file"] = EditCongFile,
        ["install-command-palette"] = _ => InstallCommandPalette(),
        ["load-audio"] = LoadAudio,
        ["load-sub"] = LoadSubtitle,
        ["move-window"] = args => MoveWindow?.Invoke(args[0]),
        ["open-clipboard"] = OpenFromClipboard,
        ["open-files"] = OpenFiles,
        ["open-optical-media"] = Open_DVD_Or_BD_Folder,
        ["reg-file-assoc"] = RegisterFileAssociations,
        ["remove-from-path"] = _ => RemoveFromPath(),
        ["scale-window"] = args => ScaleWindow?.Invoke(float.Parse(args[0], CultureInfo.InvariantCulture)),
        ["show-about"] = _ => ShowDialog(typeof(AboutWindow)),
        ["show-bindings"] = _ => ShowBindings(),
        ["show-commands"] = _ => ShowCommands(),
        ["show-conf-editor"] = _ => ShowDialog(typeof(ConfWindow)),
        ["show-decoders"] = _ => ShowDecoders(),
        ["show-demuxers"] = _ => ShowDemuxers(),
        ["show-info"] = _ => ShowMediaInfo(new[] { "osd" }),
        ["show-input-editor"] = _ => ShowDialog(typeof(InputWindow)),
        ["show-keys"] = _ => ShowKeys(),
        ["show-media-info"] = ShowMediaInfo,
        ["show-menu"] = _ => ShowMenu?.Invoke(),
        ["show-profiles"] = _ => Msg.ShowInfo(Player.GetProfiles()),
        ["show-properties"] = _ => Player.Command("script-binding select/show-properties"),
        ["show-protocols"] = _ => ShowProtocols(),
        ["show-recent-in-command-palette"] = _ => ShowRecentFilesInCommandPalette(),
        ["stream-quality"] = _ => StreamQuality(),
        ["window-scale"] = args => WindowScaleNet?.Invoke(float.Parse(args[0], CultureInfo.InvariantCulture)),
    };

    private static void ShowDialog(Type winType)
    {
        var win = Activator.CreateInstance(winType) as Window;
        new WindowInteropHelper(win!).Owner = MainForm.Instance!.Handle;
        win?.ShowDialog();
    }

    private static void LoadSubtitle(IList<string> args)
    {
        using var dialog = new OpenFileDialog();
        var       path   = Player.GetPropertyString("path");

        if (File.Exists(path))
            dialog.InitialDirectory = Path.GetDirectoryName(path);

        dialog.Multiselect = true;

        if (dialog.ShowDialog() != DialogResult.OK) return;
        foreach (var filename in dialog.FileNames)
            Player.CommandV("sub-add", filename);
    }

    private static void OpenFiles(IList<string> args)
    {
        var append = false;

        foreach (var arg in args)
            if (arg == "append")
                append = true;

        using var dialog = new OpenFileDialog();
        dialog.Multiselect = true;

        if (dialog.ShowDialog() == DialogResult.OK)
            Player.LoadFiles(dialog.FileNames, true, append);
    }

    private static void Open_DVD_Or_BD_Folder(IList<string> args)
    {
        var dialog = new FolderBrowserDialog();

        if (dialog.ShowDialog() == DialogResult.OK)
            Player.LoadDiskFolder(dialog.SelectedPath);
    }

    private static void EditCongFile(IList<string> args)
    {
        var file = Player.ConfigFolder + args[0];

        if (!File.Exists(file))
        {
            var msg = $"{args[0]} does not exist. Would you like to create it?";

            if (Msg.ShowQuestion(msg) == MessageBoxResult.OK)
                File.WriteAllText(file, "");
        }

        if (File.Exists(file))
            ProcessHelp.ShellExecute(WinApiHelp.GetAppPathForExtension("txt"), "\"" + file + "\"");
    }

    private static void ShowTextWithEditor(string name, string text)
    {
        var file = Path.Combine(Path.GetTempPath(), name + ".txt");
        App.TempFiles.Add(file);
        File.WriteAllText(file, Br                                              + text.Trim() + Br);
        ProcessHelp.ShellExecute(WinApiHelp.GetAppPathForExtension("txt"), "\"" + file        + "\"");
    }

    private static void ShowCommands()
    {
        var json       = Core.GetPropertyString("command-list");
        var enumerator = JsonDocument.Parse(json).RootElement.EnumerateArray();
        var commands   = enumerator.OrderBy(it => it.GetProperty("name").GetString());
        var sb         = new StringBuilder();

        foreach (var cmd in commands)
        {
            sb.AppendLine();
            sb.AppendLine(cmd.GetProperty("name").GetString());

            foreach (var args in cmd.GetProperty("args").EnumerateArray())
            {
                var value = args.GetProperty("name").GetString()            + " <" +
                            args.GetProperty("type").GetString()!.ToLower() + ">";

                if (args.GetProperty("optional").GetBoolean())
                    value = "[" + value + "]";

                sb.AppendLine("    " + value);
            }
        }

        var header = Br                                                      +
                     "https://mpv.io/manual/master/#list-of-input-commands"  + Br2 +
                     "https://github.com/stax76/mpv-scripts#command_palette" + Br;

        ShowTextWithEditor("Input Commands", header + sb.ToString());
    }

    private static void ShowKeys() =>
        ShowTextWithEditor("Keys", Core.GetPropertyString("input-key-list").Replace(",", Br));

    private static void ShowProtocols() =>
        ShowTextWithEditor("Protocols", Core.GetPropertyString("protocol-list").Replace(",", Br));

    private static void ShowDecoders() =>
        ShowTextWithEditor("Decoders", Core.GetPropertyOsdString("decoder-list").Replace(",", Br));

    private static void ShowDemuxers() =>
        ShowTextWithEditor("Demuxers", Core.GetPropertyOsdString("demuxer-lavf-list").Replace(",", Br));

    private static void OpenFromClipboard(IList<string> args)
    {
        var append = args is ["append"];

        if (System.Windows.Forms.Clipboard.ContainsFileDropList())
        {
            var files = System.Windows.Forms.Clipboard.GetFileDropList().Cast<string>().ToArray();
            Player.LoadFiles(files, false, append);

            if (append)
                Player.CommandV("show-text", _("Files/URLs were added to the playlist"));
        }
        else
        {
            var clipboard = System.Windows.Forms.Clipboard.GetText();
            var files = clipboard.Split(Br.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                                 .Where(i => i.Contains("://") || File.Exists(i)).ToList();

            if (files.Count == 0)
            {
                Terminal.WriteError(_("The clipboard does not contain a valid URL or file."));
                return;
            }

            Player.LoadFiles(files.ToArray(), false, append);

            if (append)
                Player.CommandV("show-text", _("Files/URLs were added to the playlist"));
        }
    }

    private static void LoadAudio(IList<string> args)
    {
        using var dialog = new OpenFileDialog();

        var path = Player.GetPropertyString("path");

        if (File.Exists(path))
            dialog.InitialDirectory = Path.GetDirectoryName(path);

        dialog.Multiselect = true;

        if (dialog.ShowDialog() != DialogResult.OK) return;
        foreach (var i in dialog.FileNames)
            Player.CommandV("audio-add", i);
    }

    private static void RegisterFileAssociations(IList<string> args)
    {
        var perceivedType = args[0];

        var extensions = perceivedType switch
        {
            "video" => FileTypes.GetVideoExtension(),
            "audio" => FileTypes.GetAudioExtension(),
            "image" => FileTypes.GetImgExtension(),
            _       => Array.Empty<string>()
        };

        try
        {
            using var proc = new Process();
            proc.StartInfo.FileName = Environment.ProcessPath;
            proc.StartInfo.Arguments = "--register-file-associations " +
                                       perceivedType                   + " " + string.Join(" ", extensions);
            proc.StartInfo.Verb            = "runas";
            proc.StartInfo.UseShellExecute = true;
            proc.Start();
            proc.WaitForExit();

            if (proc.ExitCode == 0)
            {
                var msgRestart = _("File Explorer icons will refresh after process restart.");

                if (perceivedType == "unreg")
                    Msg.ShowInfo(_("File associations were successfully removed.") + Br2 + msgRestart);
                else
                    Msg.ShowInfo(_("File associations were successfully created.") + Br2 + msgRestart);
            }
            else
                Msg.ShowError(_("Error creating file associations."));
        }
        catch
        {
            // ignored
        }
    }

    private static void InstallCommandPalette()
    {
        if (Msg.ShowQuestion("Install command palette?") != MessageBoxResult.OK)
            return;

        try
        {
            Environment.SetEnvironmentVariable("MPVNET_HOME", Player.ConfigFolder);
            using var proc = new Process();
            proc.StartInfo.FileName = "powershell";
            proc.StartInfo.Arguments =
                "-executionpolicy bypass -nologo -noexit -noprofile -command \"irm https://raw.githubusercontent.com/stax76/mpv-scripts/refs/heads/main/powershell/command_palette_installer.ps1 | iex\"";
            proc.Start();
        }
        catch
        {
        }
    }

    private static void StreamQuality()
    {
        var version = Player.GetPropertyInt("user-data/command-palette/version");

        if (version >= 2)
            Player.Command("script-message-to command_palette show-command-palette \"Stream Quality\"");
        else
        {
            var result = Msg.ShowQuestion("The Stream Quality feature requires the command palette to be installed." +
                                          Br2                                                                        +
                                          "Would you like to install the command palette now?");

            if (result == MessageBoxResult.OK)
                Player.Command("script-message-to mpvnet install-command-palette");
        }
    }

    private static void ShowRecentFilesInCommandPalette()
    {
        Obj o = new()
        {
            Title         = "Recent Files",
            SelectedIndex = 0
        };

        o.Items = App.Settings.RecentFiles.Select(file => new Item()
        {
            Title = Path.GetFileName(file), Value = new string[] { "loadfile", file }, Hint = file
        }).ToArray();
        var json = JsonSerializer.Serialize(o);
        Player.CommandV("script-message", "show-command-palette-json", json);
    }

    private class Obj
    {
        public string Title         { get; set; } = "";
        public int    SelectedIndex { get; set; }
        public Item[] Items         { get; set; } = Array.Empty<Item>();
    }

    private class Item
    {
        public string[] Value { get; set; } = Array.Empty<string>();
        public string   Title { get; set; } = string.Empty;
        public string   Hint  { get; set; } = string.Empty;
    }

    private void ShowMediaInfo(IList<string> args)
    {
        if (Player.PlaylistPos == -1)
            return;

        var full   = args.Contains("full");
        var raw    = args.Contains("raw");
        var editor = args.Contains("editor");
        var osd    = args.Contains("osd") || args.Count == 0;

        long fileSize = 0;

        string text;
        var    path = Player.GetPropertyString("path");

        if (File.Exists(path) && osd)
        {
            if (FileTypes.IsAudio(path.Ext()))
            {
                text = Player.GetPropertyOsdString("filtered-metadata");
                Player.CommandV("show-text", text, "5000");
                return;
            }
            else if (FileTypes.IsImage(path.Ext()))
            {
                fileSize = new FileInfo(path).Length;

                text = "Width: "  + Player.GetPropertyInt("width")     + "\n"    +
                       "Height: " + Player.GetPropertyInt("height")    + "\n"    +
                       "Size: "   + Convert.ToInt32(fileSize / 1024.0) + " KB\n" +
                       "Type: "   + path.Ext().ToUpper();

                Player.CommandV("show-text", text, "5000");
                return;
            }
        }

        if (path.Contains("://"))
        {
            if (path.Contains("://"))
                path = Player.GetPropertyString("media-title");
            var videoFormat = Player.GetPropertyString("video-format").ToUpper();
            var audioCodec  = Player.GetPropertyString("audio-codec-name").ToUpper();
            var width       = Player.GetPropertyInt("video-params/w");
            var height      = Player.GetPropertyInt("video-params/h");
            var len         = TimeSpan.FromSeconds(Player.GetPropertyDouble("duration"));
            text =  path.FileName()              + "\n";
            text += FormatTime(len.TotalMinutes) + ":" + FormatTime(len.Seconds) + "\n";
            text += $"{width} x {height}\n";
            text += $"{videoFormat}\n{audioCodec}";
            Player.CommandV("show-text", text, "5000");
            return;
        }

        if (App.MediaInfo && !osd && File.Exists(path) && !path.Contains(@"\\.\pipe\"))
            using (var mediaInfo = new MediaInfo(path))
                text = Regex.Replace(mediaInfo.GetSummary(full, raw), "Unique ID.+", "");
        else
        {
            Player.UpdateExternalTracks();
            text = "N: " + Player.GetPropertyString("filename") + Br;
            lock (Player.MediaTracksLock)
                text = Player.MediaTracks.Aggregate(text, (current, track) => current + (track.Text + Br));
        }

        text = text.TrimEx();

        if (editor)
            ShowTextWithEditor("media-info", text);
        else if (osd)
            Command.ShowText(text.Replace("\r", ""), 5000, 16);
        else
        {
            MessageBoxEx.SetFont("Consolas");
            Msg.ShowInfo(text);
            MessageBoxEx.SetFont("Segoe UI");
        }
    }

    private static string FormatTime(double value) => ((int)value).ToString("00");

    private static void ShowBindings() => ShowTextWithEditor("Bindings", Player.UsedInputConfContent);

    private static void AddToPath()
    {
        var path = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User)!;

        if (path.ToLower().Contains(Folder.Startup.TrimEnd(Path.DirectorySeparatorChar).ToLower()))
        {
            Msg.ShowWarning(_("mpv.net is already in the Path environment variable."));
            return;
        }

        Environment.SetEnvironmentVariable("Path",
                                           Folder.Startup.TrimEnd(Path.DirectorySeparatorChar) + ";" + path,
                                           EnvironmentVariableTarget.User);

        Msg.ShowInfo(_("mpv.net was successfully added to the Path environment variable."));
    }

    private static void RemoveFromPath()
    {
        var path = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User)!;

        if (!path.Contains(Folder.Startup.TrimEnd(Path.DirectorySeparatorChar)))
        {
            Msg.ShowWarning(_("mpv.net was not found in the Path environment variable."));
            return;
        }

        path = path.Replace(Folder.Startup.TrimEnd(Path.DirectorySeparatorChar), "");
        path = path.Replace(";;", ";").Trim(';');

        Environment.SetEnvironmentVariable("Path", path, EnvironmentVariableTarget.User);

        Msg.ShowInfo(_("mpv.net was successfully removed from the Path environment variable."));
    }
}
