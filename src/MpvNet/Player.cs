using MpvNet.ExtensionMethod;
using MpvNet.Help;
using MpvNet.Native;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using static MpvNet.Native.LibMpv;

namespace MpvNet;

public class MainPlayer : MpvClient
{
    public string ConfPath => ConfigFolder + "mpv.conf";

    public string GpuApi               { get; set; } = "auto";
    public string Path                 { get; set; } = "";
    public string Vo                   { get; set; } = "gpu";
    public string UsedInputConfContent { get; set; } = "";

    public string Vid { get; set; } = string.Empty;
    public string Aid { get; set; } = string.Empty;
    public string Sid { get; set; } = string.Empty;

    public bool Border           { get; set; } = true;
    public bool FileEnded        { get; set; }
    public bool Fullscreen       { get; set; }
    public bool IsQuitNeeded     { set; get; } = true;
    public bool KeepAspectWindow { get; set; }
    public bool Paused           { get; set; }
    public bool SnapWindow       { get; set; }
    public bool TaskBarProgress  { get; set; } = true;
    public bool TitleBar         { get; set; } = true;
    public bool WasInitialSizeSet;
    public bool WindowMaximized { get; set; }
    public bool WindowMinimized { get; set; }

    public int Edition     { get; set; }
    public int PlaylistPos { get; set; } = -1;
    public int Screen      { get; set; } = -1;
    public int VideoRotate { get; set; }

    public float Autofit        { get; set; } = 0.6f;
    public float AutofitSmaller { get; set; } = 0.3f;
    public float AutofitLarger  { get; set; } = 0.8f;

    public AutoResetEvent   ShutdownAutoResetEvent { get; } = new(false);
    public nint             MainHandle             { get; set; }
    public List<MediaTrack> MediaTracks            { get; set; } = new();
    public List<TimeSpan>   BluRayTitles           { get; }      = new();
    public object           MediaTracksLock        { get; }      = new();
    public Size             VideoSize              { get; set; }
    public TimeSpan         Duration;
    public List<MpvClient>  Clients { get; } = new();

    private List<StringPair>? _audioDevices;

    public event Action?       Initialized;
    public event Action?       Pause;
    public event Action<int>?  PlaylistPosChanged;
    public event Action<Size>? VideoSizeChanged;

    public void Init(IntPtr formHandle, bool processCommandLine)
    {
        App.ApplyShowMenuFix();

        MainHandle = mpv_create();
        Handle     = MainHandle;

        var events = Enum.GetValues(typeof(MpvEventId)).Cast<MpvEventId>();

        foreach (var i in events)
            mpv_request_event(MainHandle, i, 0);

        mpv_request_log_messages(MainHandle, "no");

        if (formHandle != IntPtr.Zero)
            TaskHelp.Run(MainEventLoop);

        if (MainHandle == IntPtr.Zero)
            throw new Exception("error mpv_create");

        if (App.IsTerminalAttached)
        {
            SetPropertyString("terminal", "yes");
            SetPropertyString("input-terminal", "yes");
        }

        if (formHandle != IntPtr.Zero)
        {
            SetPropertyString("force-window", "yes");
            SetPropertyLong("wid", formHandle.ToInt64());
        }

        SetPropertyInt("osd-duration", 2000);

        SetPropertyBool("input-default-bindings", true);
        SetPropertyBool("input-builtin-bindings", false);

        SetPropertyString("idle", "yes");
        SetPropertyString("screenshot-directory", "~~desktop/");
        SetPropertyString("osd-playing-msg", "${media-title}");
        SetPropertyString("osc", "yes");
        SetPropertyString("config-dir", ConfigFolder);
        SetPropertyString("config", "yes");

        UsedInputConfContent = App.InputConf.GetContent();

        if (!string.IsNullOrEmpty(UsedInputConfContent))
            SetPropertyString("input-conf", @"memory://" + UsedInputConfContent);

        if (processCommandLine)
            CommandLine.ProcessCommandLineArgsPreInit();

        if (CommandLine.Contains("config-dir"))
        {
            var configDir = CommandLine.GetValue("config-dir");
            var fullPath  = System.IO.Path.GetFullPath(configDir);
            App.InputConf.Path = fullPath.AddSep() + "input.conf";
            var content = App.InputConf.GetContent();

            if (!string.IsNullOrEmpty(content))
                SetPropertyString("input-conf", "memory://" + content);
        }

        var err = mpv_initialize(MainHandle);

        if (err < 0)
            throw new Exception("mpv_initialize error" + Br2 + GetError(err) + Br);

        var idle = GetPropertyString("idle");
        App.Exit = idle is "no" or "once";

        Handle = mpv_create_client(MainHandle, "mpvnet");

        if (Handle == IntPtr.Zero)
            throw new Exception("mpv_create_client error");

        mpv_request_log_messages(Handle, "info");

        if (formHandle != IntPtr.Zero)
            TaskHelp.Run(EventLoop);

        // otherwise shutdown is raised before media files are loaded,
        // this means Lua scripts that use idle might not work correctly
        SetPropertyString("idle", "yes");

        SetPropertyString("user-data/frontend/name", "mpv.net");
        SetPropertyString("user-data/frontend/version", AppInfo.Version.ToString());
        SetPropertyString("user-data/frontend/process-path", Environment.ProcessPath!);

        ObservePropertyBool("pause", value =>
        {
            Paused = value;
            Pause?.Invoke();
        });

        VideoRotate = GetPropertyInt("video-rotate");

        ObservePropertyInt("video-rotate", value =>
        {
            if (VideoRotate == value) return;
            VideoRotate = value;
            UpdateVideoSize("dwidth", "dheight");
        });

        ObservePropertyInt("playlist-pos", value =>
        {
            PlaylistPos = value;
            PlaylistPosChanged?.Invoke(value);

            if (!FileEnded || value != -1) return;
            if (GetPropertyString("keep-open") == "no" && App.Exit)
                CommandV("quit");
        });

        Initialized?.Invoke();
    }

    public void Destroy()
    {
        mpv_destroy(MainHandle);
        mpv_destroy(Handle);

        foreach (var client in Clients)
            mpv_destroy(client.Handle);
    }

    public void ProcessProperty(string? name, string? value)
    {
        switch (name)
        {
            case "autofit" :
            {
                if (int.TryParse(value?.Trim('%'), out var result))
                    Autofit = result / 100f;
            }
                break;
            case "autofit-smaller" :
            {
                if (int.TryParse(value?.Trim('%'), out var result))
                    AutofitSmaller = result / 100f;
            }
                break;
            case "autofit-larger" :
            {
                if (int.TryParse(value?.Trim('%'), out var result))
                    AutofitLarger = result / 100f;
            }
                break;
            case "border" :
                Border = value == "yes";
                break;
            case "fs" :
            case "fullscreen" :
                Fullscreen = value == "yes";
                break;
            case "gpu-api" :
                GpuApi = value!;
                break;
            case "keepaspect-window" :
                KeepAspectWindow = value == "yes";
                break;
            case "screen" :
                Screen = Convert.ToInt32(value);
                break;
            case "snap-window" :
                SnapWindow = value == "yes";
                break;
            case "taskbar-progress" :
                TaskBarProgress = value == "yes";
                break;
            case "vo" :
                Vo = value!;
                break;
            case "window-maximized" :
                WindowMaximized = value == "yes";
                break;
            case "window-minimized" :
                WindowMinimized = value == "yes";
                break;
            case "title-bar" :
                TitleBar = value == "yes";
                break;
        }

        if (AutofitLarger > 1)
            AutofitLarger = 1;
    }

    private string? _configFolder;

    public string ConfigFolder
    {
        get
        {
            if (_configFolder != null) return _configFolder;
            var mpvnetHome = Environment.GetEnvironmentVariable("MPVNET_HOME");

            if (Directory.Exists(mpvnetHome))
                return _configFolder = mpvnetHome.AddSep();

            _configFolder = Folder.Startup + "portable_config";

            if (!Directory.Exists(_configFolder))
                _configFolder = Folder.AppData + "mpv.net";

            if (!Directory.Exists(_configFolder))
            {
                try
                {
                    using var proc = new Process();
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.CreateNoWindow  = true;
                    proc.StartInfo.FileName        = "powershell.exe";
                    proc.StartInfo.Arguments       = $@"-Command New-Item -Path '{_configFolder}' -ItemType Directory";
                    proc.Start();
                    proc.WaitForExit();
                }
                catch (Exception)
                {
                    // ignored
                }

                if (!Directory.Exists(_configFolder))
                    Directory.CreateDirectory(_configFolder);
            }

            _configFolder = _configFolder.AddSep();

            return _configFolder;
        }
    }

    private Dictionary<string, string>? _conf;

    public Dictionary<string, string> Conf
    {
        get
        {
            if (_conf != null)
                return _conf;

            App.ApplyInputDefaultBindingsFix();

            _conf = new Dictionary<string, string>();

            if (File.Exists(ConfPath))
            {
                foreach (var it in File.ReadAllLines(ConfPath))
                {
                    var line = it.TrimStart(' ', '-').TrimEnd();

                    if (line.StartsWith("#"))
                        continue;

                    if (!line.Contains('='))
                    {
                        if (Regex.Match(line, "^[\\w-]+$").Success)
                            line += "=yes";
                        else
                            continue;
                    }

                    var key   = line[..line.IndexOf("=", StringComparison.Ordinal)].Trim();
                    var value = line[(line.IndexOf("=", StringComparison.Ordinal) + 1)..].Trim();

                    if (value.Contains('#')     && !value.StartsWith("#") &&
                        !value.StartsWith("'#") && !value.StartsWith("\"#"))

                        value = value[..value.IndexOf("#", StringComparison.Ordinal)].Trim();

                    _conf[key] = value;
                }
            }

            foreach (var i in _conf)
                ProcessProperty(i.Key, i.Value);

            return _conf;
        }
    }

    private void UpdateVideoSize(string w, string h)
    {
        if (string.IsNullOrEmpty(Path))
            return;

        var size = new Size(GetPropertyInt(w), GetPropertyInt(h));

        if (VideoRotate is 90 or 270)
            size = new Size(size.Height, size.Width);

        if (size == VideoSize || size == Size.Empty) return;
        VideoSize = size;
        VideoSizeChanged?.Invoke(size);
    }

    public void MainEventLoop()
    {
        while (true)
            mpv_wait_event(MainHandle, -1);
    }

    protected override void OnShutdown()
    {
        IsQuitNeeded = false;
        base.OnShutdown();
        ShutdownAutoResetEvent.Set();
    }

    protected override void OnLogMessage(mpv_event_log_message data)
    {
        if (data.log_level == mpv_log_level.MPV_LOG_LEVEL_INFO)
        {
            var prefix = ConvertFromUtf8(data.prefix);

            if (prefix == "bd")
                ProcessBluRayLogMessage(ConvertFromUtf8(data.text));
        }

        base.OnLogMessage(data);
    }

    protected override void OnEndFile(mpv_event_end_file data)
    {
        base.OnEndFile(data);
        FileEnded = true;
    }

    protected override void OnVideoReconfig()
    {
        UpdateVideoSize("dwidth", "dheight");
        base.OnVideoReconfig();
    }

    // executed before OnFileLoaded
    protected override void OnStartFile()
    {
        Path = GetPropertyString("path");
        base.OnStartFile();
        TaskHelp.Run(LoadFolder);
    }

    // executed after OnStartFile
    protected override void OnFileLoaded()
    {
        Duration = TimeSpan.FromSeconds(GetPropertyDouble("duration"));

        if (App.StartSize == "video")
            WasInitialSizeSet = false;

        TaskHelp.Run(UpdateTracks);

        base.OnFileLoaded();
    }

    private void ProcessBluRayLogMessage(string msg)
    {
        lock (BluRayTitles)
        {
            if (msg.Contains(" 0 duration: "))
                BluRayTitles.Clear();

            if (!msg.Contains(" duration: ")) return;
            var start = msg.IndexOf(" duration: ", StringComparison.Ordinal) + 11;
            BluRayTitles.Add(new TimeSpan(
                                          msg.Substring(start, 2).ToInt(),
                                          msg.Substring(start + 3, 2).ToInt(),
                                          msg.Substring(start + 6, 2).ToInt()));
        }
    }

    public void SetBluRayTitle(int id) => LoadFiles(new[] { "bd://" + id }, false, false);

    public DateTime LastLoad;

    public void LoadFiles(string[]? files, bool loadFolder, bool append)
    {
        if (files == null || files.Length == 0)
            return;

        if ((DateTime.Now - LastLoad).TotalMilliseconds < 1000)
            append = true;

        LastLoad = DateTime.Now;

        for (var i = 0; i < files.Length; i++)
        {
            var file = files[i];

            if (string.IsNullOrEmpty(file))
                continue;

            if (file.Contains('|'))
                file = file[..file.IndexOf("|", StringComparison.Ordinal)];

            file = ConvertFilePath(file);

            var ext = file.Ext();

            if (OperatingSystem.IsWindows())
            {
                switch (ext)
                {
                    case "avs" :
                        LoadAviSynth();
                        break;
                    case "lnk" :
                        file = GetShortcutTarget(file);
                        break;
                }
            }

            if (ext == "iso")
                LoadBluRayIso(file);
            else if (FileTypes.Subtitle.Contains(ext))
                CommandV("sub-add", file);
            else if (!FileTypes.IsMedia(ext) && !file.Contains("://") && Directory.Exists(file) &&
                     File.Exists(System.IO.Path.Combine(file, "BDMV\\index.bdmv")))
            {
                Command("stop");
                Thread.Sleep(500);
                SetPropertyString("bluray-device", file);
                CommandV("loadfile", @"bd://");
            }
            else
            {
                if (i == 0 && !append)
                    CommandV("loadfile", file);
                else
                    CommandV("loadfile", file, "append");
            }
        }

        if (string.IsNullOrEmpty(GetPropertyString("path")))
            SetPropertyInt("playlist-pos", 0);
    }

    public static string ConvertFilePath(string path)
    {
        if ((path.Contains(":/") && !path.Contains("://")) || (path.Contains(":\\") && path.Contains('/')))
            path = path.Replace("/", "\\");

        if (!path.Contains(':') && !path.StartsWith(@"\\") && File.Exists(path))
            path = System.IO.Path.GetFullPath(path);

        return path;
    }

    public void LoadBluRayIso(string path)
    {
        Command("stop");
        Thread.Sleep(500);
        SetPropertyString("bluray-device", path);
        LoadFiles(new[] { @"bd://" }, false, false);
    }

    public void LoadDiskFolder(string path)
    {
        Command("stop");
        Thread.Sleep(500);

        if (Directory.Exists(path + "\\BDMV"))
        {
            SetPropertyString("bluray-device", path);
            LoadFiles(new[] { @"bd://" }, false, false);
        }
        else
        {
            SetPropertyString("dvd-device", path);
            LoadFiles(new[] { "dvd://" }, false, false);
        }
    }

    private static readonly object LoadFolderLockObject = new();

    public void LoadFolder()
    {
        if (!App.AutoLoadFolder)
            return;

        Thread.Sleep(1000);

        lock (LoadFolderLockObject)
        {
            var path = GetPropertyString("path");

            if (!File.Exists(path) || GetPropertyInt("playlist-count") != 1)
                return;

            var dir = Environment.CurrentDirectory;

            if (path.Contains(":/") && !path.Contains("://"))
                path = path.Replace("/", "\\");

            if (path.Contains('\\'))
                dir = System.IO.Path.GetDirectoryName(path)!;

            var files = FileTypes.GetMediaFiles(Directory.GetFiles(dir)).ToList();

            if (OperatingSystem.IsWindows())
                files.Sort(new StringLogicalComparer());

            var index = files.IndexOf(path);
            files.Remove(path);

            foreach (var file in files)
                CommandV("loadfile", file, "append");

            if (index > 0)
                CommandV("playlist-move", "0", (index + 1).ToString());
        }
    }

    private bool _wasAviSynthLoaded;

    [SupportedOSPlatform("windows")]
    private void LoadAviSynth()
    {
        if (_wasAviSynthLoaded) return;
        var dll = Environment.GetEnvironmentVariable("AviSynthDLL"); // StaxRip sets it in portable mode
        LoadLibrary(File.Exists(dll) ? dll : "AviSynth.dll");
        _wasAviSynthLoaded = true;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibrary(string path);

    [SupportedOSPlatform("windows")]
    public static string GetShortcutTarget(string path)
    {
        var      t  = Type.GetTypeFromProgID("WScript.Shell");
        dynamic? sh = Activator.CreateInstance(t!);
        return sh?.CreateShortcut(path).TargetPath!;
    }

    private static string GetLanguage(string id)
    {
        foreach (var ci in CultureInfo.GetCultures(CultureTypes.NeutralCultures))
            if (ci.ThreeLetterISOLanguageName == id || Convert(ci.ThreeLetterISOLanguageName) == id)
                return ci.EnglishName;

        return id;

        static string Convert(string id2) => id2 switch
        {
            "bng" => "ben",
            "ces" => "cze",
            "deu" => "ger",
            "ell" => "gre",
            "eus" => "baq",
            "fra" => "fre",
            "hye" => "arm",
            "isl" => "ice",
            "kat" => "geo",
            "mya" => "bur",
            "nld" => "dut",
            "sqi" => "alb",
            "zho" => "chi",
            _     => id2,
        };
    }

    private static string GetNativeLanguage(string name)
    {
        foreach (var ci in CultureInfo.GetCultures(CultureTypes.NeutralCultures))
            if (ci.EnglishName == name)
                return ci.NativeName;

        return name;
    }

    public void UpdateTracks()
    {
        var path = GetPropertyString("path");

        if (!path.ToLowerEx().StartsWithEx("bd://"))
            lock (BluRayTitles)
                BluRayTitles.Clear();

        lock (MediaTracksLock)
        {
            if (App.MediaInfo && !path.Contains("://") && !path.Contains(@"\\.\pipe\") && File.Exists(path))
                MediaTracks = GetMediaInfoTracks(path);
            else
                MediaTracks = GetTracks();
        }
    }

    public List<StringPair> AudioDevices
    {
        get
        {
            if (_audioDevices != null)
                return _audioDevices;

            _audioDevices = new List<StringPair>();
            var json       = GetPropertyString("audio-device-list");
            var enumerator = JsonDocument.Parse(json).RootElement.EnumerateArray();

            foreach (var element in enumerator)
            {
                var name        = element.GetProperty("name").GetString()!;
                var description = element.GetProperty("description").GetString()!;
                _audioDevices.Add(new StringPair(name, description));
            }

            return _audioDevices;
        }
    }

    public List<Chapter> GetChapters()
    {
        var chapters = new List<Chapter>();
        var count    = GetPropertyInt("chapter-list/count");

        for (var x = 0; x < count; x++)
        {
            var title = GetPropertyString($"chapter-list/{x}/title");
            var time  = GetPropertyDouble($"chapter-list/{x}/time");

            if (string.IsNullOrEmpty(title) ||
                (title.Length == 12 && title.Contains(':') && title.Contains('.')))

                title = "Chapter " + (x + 1);

            chapters.Add(new Chapter() { Title = title, Time = time });
        }

        return chapters;
    }

    public void UpdateExternalTracks()
    {
        var trackListTrackCount = GetPropertyInt("track-list/count");
        var editionCount        = GetPropertyInt("edition-list/count");
        var count               = MediaTracks.Count(i => i.Type != "g");

        lock (MediaTracksLock)
        {
            if (count == (trackListTrackCount + editionCount)) return;
            MediaTracks = MediaTracks.Where(i => !i.External).ToList();
            MediaTracks.AddRange(GetTracks(false));
        }
    }

    public List<MediaTrack> GetTracks(bool includeInternal = true, bool includeExternal = true)
    {
        var tracks = new List<MediaTrack>();

        var trackCount = GetPropertyInt("track-list/count");

        for (var i = 0; i < trackCount; i++)
        {
            var external = GetPropertyBool($"track-list/{i}/external");

            if ((external && !includeExternal) || (!external && !includeInternal))
                continue;

            var type     = GetPropertyString($"track-list/{i}/type");
            var filename = GetPropertyString($"filename/no-ext");
            var title    = GetPropertyString($"track-list/{i}/title").Replace(filename, "");

            title = Regex.Replace(title, @"^[\._\-]", "");

            switch (type)
            {
                case "video" :
                {
                    var codec = GetPropertyString($"track-list/{i}/codec").ToUpperEx();
                    switch (codec)
                    {
                        case "MPEG2VIDEO" :
                            codec = "MPEG2";
                            break;
                        case "DVVIDEO" :
                            codec = "DV";
                            break;
                    }

                    var track = new MediaTrack();
                    Add(track, codec);
                    Add(track,
                        GetPropertyString($"track-list/{i}/demux-w") + "x" +
                        GetPropertyString($"track-list/{i}/demux-h"));
                    Add(track, GetPropertyString($"track-list/{i}/demux-fps").Replace(".000000", "") + " FPS");
                    Add(track, GetPropertyBool($"track-list/{i}/default") ? "Default" : null);
                    track.Text = "V: " + track.Text.Trim(' ', ',');
                    track.Type = "v";
                    track.Id   = GetPropertyInt($"track-list/{i}/id");
                    tracks.Add(track);
                    break;
                }
                case "audio" :
                {
                    var codec = GetPropertyString($"track-list/{i}/codec").ToUpperEx();
                    if (codec.Contains("PCM"))
                        codec = "PCM";
                    var track = new MediaTrack();
                    Add(track, GetLanguage(GetPropertyString($"track-list/{i}/lang")));
                    Add(track, codec);
                    Add(track, GetPropertyInt($"track-list/{i}/audio-channels")          + " ch");
                    Add(track, GetPropertyInt($"track-list/{i}/demux-samplerate") / 1000 + " kHz");
                    Add(track, GetPropertyBool($"track-list/{i}/forced") ? "Forced" : null);
                    Add(track, GetPropertyBool($"track-list/{i}/default") ? "Default" : null);
                    Add(track, GetPropertyBool($"track-list/{i}/external") ? "External" : null);
                    Add(track, title);
                    track.Text     = "A: " + track.Text.Trim(' ', ',');
                    track.Type     = "a";
                    track.Id       = GetPropertyInt($"track-list/{i}/id");
                    track.External = external;
                    tracks.Add(track);
                    break;
                }
                case "sub" :
                {
                    var codec = GetPropertyString($"track-list/{i}/codec").ToUpperEx();
                    if (codec.Contains("PGS"))
                        codec = "PGS";
                    else
                        codec = codec switch
                        {
                            "SUBRIP"       => "SRT",
                            "WEBVTT"       => "VTT",
                            "DVB_SUBTITLE" => "DVB",
                            "DVD_SUBTITLE" => "VOB",
                            _              => codec
                        };
                    var track = new MediaTrack();
                    Add(track, GetLanguage(GetPropertyString($"track-list/{i}/lang")));
                    Add(track, codec);
                    Add(track, GetPropertyBool($"track-list/{i}/forced") ? "Forced" : null);
                    Add(track, GetPropertyBool($"track-list/{i}/default") ? "Default" : null);
                    Add(track, GetPropertyBool($"track-list/{i}/external") ? "External" : null);
                    Add(track, title);
                    track.Text     = "S: " + track.Text.Trim(' ', ',');
                    track.Type     = "s";
                    track.Id       = GetPropertyInt($"track-list/{i}/id");
                    track.External = external;
                    tracks.Add(track);
                    break;
                }
            }
        }

        if (!includeInternal) return tracks;
        {
            var editionCount = GetPropertyInt("edition-list/count");

            for (var i = 0; i < editionCount; i++)
            {
                var title = GetPropertyString($"edition-list/{i}/title");

                if (string.IsNullOrEmpty(title))
                    title = "Edition " + i;

                var track = new MediaTrack
                {
                    Text = "E: " + title,
                    Type = "e",
                    Id   = i
                };

                tracks.Add(track);
            }
        }

        return tracks;

        static void Add(MediaTrack track, object? value)
        {
            var str = (value + "").Trim();

            if (str != "" && !track.Text.Contains(str))
                track.Text += " " + str + ",";
        }
    }

    public List<MediaTrack> GetMediaInfoTracks(string path)
    {
        var tracks = new List<MediaTrack>();

        using (var mi = new MediaInfo(path))
        {
            var track = new MediaTrack();
            Add(track, mi.GetGeneral("Format"));
            Add(track, mi.GetGeneral("FileSize/String"));
            Add(track, mi.GetGeneral("Duration/String"));
            Add(track, mi.GetGeneral("OverallBitRate/String"));
            track.Text = "G: " + track.Text.Trim(' ', ',');
            track.Type = "g";
            tracks.Add(track);

            var videoCount = mi.GetCount(MediaInfoStreamKind.Video);

            for (var i = 0; i < videoCount; i++)
            {
                var fps = mi.GetVideo(i, "FrameRate");

                if (float.TryParse(fps, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
                    fps = result.ToString(CultureInfo.InvariantCulture);

                track = new MediaTrack();
                Add(track, mi.GetVideo(i, "Format"));
                Add(track, mi.GetVideo(i, "Format_Profile"));
                Add(track, mi.GetVideo(i, "Width") + "x" + mi.GetVideo(i, "Height"));
                Add(track, mi.GetVideo(i, "BitRate/String"));
                Add(track, fps + " FPS");
                Add(track, (videoCount > 1 && mi.GetVideo(i, "Default") == "Yes") ? "Default" : "");
                track.Text = "V: " + track.Text.Trim(' ', ',');
                track.Type = "v";
                track.Id   = i + 1;
                tracks.Add(track);
            }

            var audioCount = mi.GetCount(MediaInfoStreamKind.Audio);

            for (var i = 0; i < audioCount; i++)
            {
                var lang       = mi.GetAudio(i, "Language/String");
                var nativeLang = GetNativeLanguage(lang);
                var title      = mi.GetAudio(i, "Title");
                var format     = mi.GetAudio(i, "Format");

                if (!string.IsNullOrEmpty(title))
                {
                    if (title.ContainsEx("DTS-HD MA"))
                        format = "DTS-MA";

                    if (title.ContainsEx("DTS-HD MA"))
                        title = title.Replace("DTS-HD MA", "");

                    if (title.ContainsEx("Blu-ray"))
                        title = title.Replace("Blu-ray", "");

                    if (title.ContainsEx("UHD "))
                        title = title.Replace("UHD ", "");

                    if (title.ContainsEx("EAC"))
                        title = title.Replace("EAC", "E-AC");

                    if (title.ContainsEx("AC3"))
                        title = title.Replace("AC3", "AC-3");

                    if (title.ContainsEx(lang))
                        title = title.Replace(lang, "").Trim();

                    if (title.ContainsEx(nativeLang))
                        title = title.Replace(nativeLang, "").Trim();

                    if (title.ContainsEx("Surround"))
                        title = title.Replace("Surround", "");

                    if (title.ContainsEx("Dolby Digital"))
                        title = title.Replace("Dolby Digital", "");

                    if (title.ContainsEx("Stereo"))
                        title = title.Replace("Stereo", "");

                    if (title.StartsWithEx(format + " "))
                        title = title.Replace(format + " ", "");

                    foreach (var i2 in new[] { "2.0", "5.1", "6.1", "7.1" })
                        if (title.ContainsEx(i2))
                            title = title.Replace(i2, "").Trim();

                    if (title.ContainsEx("@ "))
                        title = title.Replace("@ ", "");

                    if (title.ContainsEx(" @"))
                        title = title.Replace(" @", "");

                    if (title.ContainsEx("()"))
                        title = title.Replace("()", "");

                    if (title.ContainsEx("[]"))
                        title = title.Replace("[]", "");

                    if (title.TrimEx() == format)
                        title = null;

                    if (!string.IsNullOrEmpty(title))
                        title = title.Trim(" _-".ToCharArray());
                }

                track = new MediaTrack();
                Add(track, lang);
                Add(track, format);
                Add(track, mi.GetAudio(i, "Format_Profile"));
                Add(track, mi.GetAudio(i, "BitRate/String"));
                Add(track, mi.GetAudio(i, "Channel(s)") + " ch");
                Add(track, mi.GetAudio(i, "SamplingRate/String"));
                Add(track, mi.GetAudio(i, "Forced") == "Yes" ? "Forced" : "");
                Add(track, (audioCount > 1 && mi.GetAudio(i, "Default") == "Yes") ? "Default" : "");
                Add(track, title);

                if (track.Text.Contains("MPEG Audio, Layer 2"))
                    track.Text = track.Text.Replace("MPEG Audio, Layer 2", "MP2");

                if (track.Text.Contains("MPEG Audio, Layer 3"))
                    track.Text = track.Text.Replace("MPEG Audio, Layer 2", "MP3");

                track.Text = "A: " + track.Text.Trim(' ', ',');
                track.Type = "a";
                track.Id   = i + 1;
                tracks.Add(track);
            }

            var subCount = mi.GetCount(MediaInfoStreamKind.Text);

            for (var i = 0; i < subCount; i++)
            {
                var codec = mi.GetText(i, "Format").ToUpperEx();

                switch (codec)
                {
                    case "UTF-8" :
                        codec = "SRT";
                        break;
                    case "WEBVTT" :
                        codec = "VTT";
                        break;
                    case "VOBSUB" :
                        codec = "VOB";
                        break;
                }

                var lang       = mi.GetText(i, "Language/String");
                var nativeLang = GetNativeLanguage(lang);
                var title      = mi.GetText(i, "Title");
                var forced     = mi.GetText(i, "Forced") == "Yes";

                if (!string.IsNullOrEmpty(title))
                {
                    if (title.ContainsEx("VobSub"))
                        title = title.Replace("VobSub", "VOB");

                    if (title.ContainsEx(codec))
                        title = title.Replace(codec, "");

                    if (title.ContainsEx(lang.ToLowerEx()))
                        title = title.Replace(lang.ToLowerEx(), lang);

                    if (title.ContainsEx(nativeLang.ToLowerEx()))
                        title = title.Replace(nativeLang.ToLowerEx(), nativeLang).Trim();

                    if (title.ContainsEx(lang))
                        title = title.Replace(lang, "");

                    if (title.ContainsEx(nativeLang))
                        title = title.Replace(nativeLang, "").Trim();

                    if (title.ContainsEx("full"))
                        title = title.Replace("full", "").Trim();

                    if (title.ContainsEx("Full"))
                        title = title.Replace("Full", "").Trim();

                    if (title.ContainsEx("Subtitles"))
                        title = title.Replace("Subtitles", "").Trim();

                    if (title.ContainsEx("forced"))
                        title = title.Replace("forced", "Forced").Trim();

                    if (forced && title.ContainsEx("Forced"))
                        title = title.Replace("Forced", "").Trim();

                    if (title.ContainsEx("()"))
                        title = title.Replace("()", "");

                    if (title.ContainsEx("[]"))
                        title = title.Replace("[]", "");

                    if (!string.IsNullOrEmpty(title))
                        title = title.Trim(" _-".ToCharArray());
                }

                track = new MediaTrack();
                Add(track, lang);
                Add(track, codec);
                Add(track, mi.GetText(i, "Format_Profile"));
                Add(track, forced ? "Forced" : "");
                Add(track, (subCount > 1 && mi.GetText(i, "Default") == "Yes") ? "Default" : "");
                Add(track, title);
                track.Text = "S: " + track.Text.Trim(' ', ',');
                track.Type = "s";
                track.Id   = i + 1;
                tracks.Add(track);
            }
        }

        var editionCount = GetPropertyInt("edition-list/count");

        for (var i = 0; i < editionCount; i++)
        {
            var title = GetPropertyString($"edition-list/{i}/title");

            if (string.IsNullOrEmpty(title))
                title = "Edition " + i;

            var track = new MediaTrack
            {
                Text = "E: " + title,
                Type = "e",
                Id   = i
            };

            tracks.Add(track);
        }

        return tracks;

        static void Add(MediaTrack track, object? value)
        {
            var str = value?.ToStringEx().Trim() ?? "";

            if (str != "" && !(track.Text.Contains(str)))
                track.Text += " " + str + ",";
        }
    }

    private string[]? _profileNames;

    public string[] ProfileNames
    {
        get
        {
            if (_profileNames != null)
                return _profileNames;

            string[] ignore = { "builtin-pseudo-gui", "encoding", "libmpv", "pseudo-gui", "default" };
            var      json   = GetPropertyString("profile-list");
            return _profileNames = JsonDocument.Parse(json).RootElement.EnumerateArray()
                                               .Select(it => it.GetProperty("name").GetString())
                                               .Where(it => !ignore.Contains(it)).ToArray()!;
        }
    }

    public string GetProfiles()
    {
        var json = GetPropertyString("profile-list");
        var sb   = new StringBuilder();

        foreach (var profile in JsonDocument.Parse(json).RootElement.EnumerateArray())
        {
            sb.Append(profile.GetProperty("name").GetString() + Br2);

            foreach (var it in profile.GetProperty("options").EnumerateArray())
                sb.AppendLine($"    {it.GetProperty("key").GetString()} = {it.GetProperty("value").GetString()}");

            sb.Append(Br);
        }

        return sb.ToString();
    }

    public string GetDecoders()
    {
        var list = JsonDocument.Parse(GetPropertyString("decoder-list")).RootElement.EnumerateArray()
                               .Select(it =>
                                           $"{it.GetProperty("codec").GetString()} - {it.GetProperty("description").GetString()}")
                               .OrderBy(it => it);

        return string.Join(Br, list);
    }

    public string GetProtocols() => string.Join(Br, GetPropertyString("protocol-list").Split(',').OrderBy(i => i));

    public string GetDemuxers() => string.Join(Br, GetPropertyString("demuxer-lavf-list").Split(',').OrderBy(i => i));

    public MpvClient CreateNewPlayer(string name)
    {
        var client = new MpvClient { Handle = mpv_create_client(MainHandle, name) };

        if (client.Handle == IntPtr.Zero)
            throw new Exception("Error CreateNewPlayer");

        TaskHelp.Run(client.EventLoop);
        Clients.Add(client);
        return client;
    }
}
