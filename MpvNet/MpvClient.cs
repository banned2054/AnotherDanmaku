using System.Runtime.InteropServices;
using static MpvNet.Native.LibMpv;

namespace MpvNet;

public class MpvClient
{
    public event Action<string[]>?            ClientMessage;    // client-message      MPV_EVENT_CLIENT_MESSAGE
    public event Action<MpvLogLevel, string>? LogMessage;       // log-message         MPV_EVENT_LOG_MESSAGE
    public event Action<MpvEndFileReason>?    EndFile;          // end-file            MPV_EVENT_END_FILE
    public event Action?                      Shutdown;         // shutdown            MPV_EVENT_SHUTDOWN
    public event Action?                      GetPropertyReply; // get-property-reply  MPV_EVENT_GET_PROPERTY_REPLY
    public event Action?                      SetPropertyReply; // set-property-reply  MPV_EVENT_SET_PROPERTY_REPLY
    public event Action?                      CommandReply;     // command-reply       MPV_EVENT_COMMAND_REPLY
    public event Action?                      StartFile;        // start-file          MPV_EVENT_START_FILE
    public event Action?                      FileLoaded;       // file-loaded         MPV_EVENT_FILE_LOADED
    public event Action?                      VideoReconfig;    // video-reconfig      MPV_EVENT_VIDEO_RECONFIG
    public event Action?                      AudioReconfig;    // audio-reconfig      MPV_EVENT_AUDIO_RECONFIG
    public event Action?                      Seek;             // seek                MPV_EVENT_SEEK
    public event Action?                      PlaybackRestart;  // playback-restart    MPV_EVENT_PLAYBACK_RESTART

    public Dictionary<string, List<Action>> PropChangeActions { get; set; } = new Dictionary<string, List<Action>>();

    public Dictionary<string, List<Action<int>>> IntPropChangeActions { get; set; } = new();

    public Dictionary<string, List<Action<bool>>> BoolPropChangeActions { get; set; } = new();

    public Dictionary<string, List<Action<double>>> DoublePropChangeActions { get; set; } = new();

    public Dictionary<string, List<Action<string>>> StringPropChangeActions { get; set; } = new();

    public nint Handle { get; set; }

    public void EventLoop()
    {
        while (true)
        {
            var ptr = mpv_wait_event(Handle, -1);
            var evt = (MpvEvent)Marshal.PtrToStructure(ptr, typeof(MpvEvent))!;

            try
            {
                switch (evt.event_id)
                {
                    case MpvEventId.Shutdown :
                        OnShutdown();
                        return;
                    case MpvEventId.LogMessage :
                    {
                        var data =
                            (MpvEventLogMessage)Marshal.PtrToStructure(evt.data, typeof(MpvEventLogMessage))!;
                        OnLogMessage(data);
                    }
                        break;
                    case MpvEventId.ClientMessage :
                    {
                        var data =
                            (MpvEventClientMessage)
                            Marshal.PtrToStructure(evt.data, typeof(MpvEventClientMessage))!;
                        OnClientMessage(data);
                    }
                        break;
                    case MpvEventId.VideoReconfig :
                        OnVideoReconfig();
                        break;
                    case MpvEventId.EndFile :
                    {
                        var data = (MpvEventEndFile)Marshal.PtrToStructure(evt.data, typeof(MpvEventEndFile))!;
                        OnEndFile(data);
                    }
                        break;
                    case MpvEventId.FileLoaded : // triggered after MPV_EVENT_START_FILE
                        OnFileLoaded();
                        break;
                    case MpvEventId.PropertyChange :
                    {
                        var data = (MpvEventProperty)Marshal.PtrToStructure(evt.data, typeof(MpvEventProperty))!;
                        OnPropertyChange(data);
                    }
                        break;
                    case MpvEventId.GetPropertyReply :
                        OnGetPropertyReply();
                        break;
                    case MpvEventId.SetPropertyReply :
                        OnSetPropertyReply();
                        break;
                    case MpvEventId.CommandReply :
                        OnCommandReply();
                        break;
                    case MpvEventId.StartFile : // triggered before MPV_EVENT_FILE_LOADED
                        OnStartFile();
                        break;
                    case MpvEventId.AudioReconfig :
                        OnAudioReconfig();
                        break;
                    case MpvEventId.Seek :
                        OnSeek();
                        break;
                    case MpvEventId.PlaybackRestart :
                        OnPlaybackRestart();
                        break;
                    default :
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception ex)
            {
                Terminal.WriteError(ex);
            }
        }
    }

    protected virtual void OnClientMessage(MpvEventClientMessage data) =>
        ClientMessage?.Invoke(ConvertFromUtf8Strings(data.args, data.num_args));

    protected virtual void OnLogMessage(MpvEventLogMessage data)
    {
        if (LogMessage == null) return;
        var msg = $"[{ConvertFromUtf8(data.prefix)}] {ConvertFromUtf8(data.text)}";
        LogMessage.Invoke(data.log_level, msg);
    }

    protected virtual void OnPropertyChange(MpvEventProperty data)
    {
        switch (data.format)
        {
            case MpvFormat.MpvFormatFlag :
            {
                lock (BoolPropChangeActions)
                    foreach (var pair in BoolPropChangeActions)
                        if (pair.Key == data.name)
                        {
                            var value = Marshal.PtrToStructure<int>(data.data) == 1;

                            foreach (var action in pair.Value)
                                action.Invoke(value);
                        }

                break;
            }
            case MpvFormat.MpvFormatString :
            {
                lock (StringPropChangeActions)
                    foreach (var pair in StringPropChangeActions)
                        if (pair.Key == data.name)
                        {
                            var value = ConvertFromUtf8(Marshal.PtrToStructure<IntPtr>(data.data));

                            foreach (var action in pair.Value)
                                action.Invoke(value);
                        }

                break;
            }
            case MpvFormat.MpvFormatInt64 :
            {
                lock (IntPropChangeActions)
                    foreach (var pair in IntPropChangeActions)
                        if (pair.Key == data.name)
                        {
                            var value = Marshal.PtrToStructure<int>(data.data);

                            foreach (var action in pair.Value)
                                action.Invoke(value);
                        }

                break;
            }
            case MpvFormat.MpvFormatNone :
            {
                lock (PropChangeActions)
                    foreach (var action in PropChangeActions.Where(pair => pair.Key == data.name)
                                                            .SelectMany(pair => pair.Value))
                        action.Invoke();
                break;
            }
            case MpvFormat.MpvFormatDouble :
            {
                lock (DoublePropChangeActions)
                    foreach (var pair in DoublePropChangeActions)
                        if (pair.Key == data.name)
                        {
                            var value = Marshal.PtrToStructure<double>(data.data);

                            foreach (var action in pair.Value)
                                action.Invoke(value);
                        }

                break;
            }
        }
    }

    protected virtual void OnEndFile(MpvEventEndFile data) => EndFile?.Invoke((MpvEndFileReason)data.reason);
    protected virtual void OnFileLoaded()                  => FileLoaded?.Invoke();
    protected virtual void OnShutdown()                    => Shutdown?.Invoke();
    protected virtual void OnGetPropertyReply()            => GetPropertyReply?.Invoke();
    protected virtual void OnSetPropertyReply()            => SetPropertyReply?.Invoke();
    protected virtual void OnCommandReply()                => CommandReply?.Invoke();
    protected virtual void OnStartFile()                   => StartFile?.Invoke();
    protected virtual void OnVideoReconfig()               => VideoReconfig?.Invoke();
    protected virtual void OnAudioReconfig()               => AudioReconfig?.Invoke();
    protected virtual void OnSeek()                        => Seek?.Invoke();
    protected virtual void OnPlaybackRestart()             => PlaybackRestart?.Invoke();

    public void Command(string command)
    {
        var err = mpv_command_string(Handle, command);

        if (err < 0)
            HandleError(err, "error executing command: " + command);
    }

    public void CommandV(params string[] args)
    {
        var count    = args.Length + 1;
        var pointers = new IntPtr[count];
        var rootPtr  = Marshal.AllocHGlobal(IntPtr.Size * count);

        for (var index = 0; index < args.Length; index++)
        {
            var bytes = GetUtf8Bytes(args[index]);
            var ptr   = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, ptr, bytes.Length);
            pointers[index] = ptr;
        }

        Marshal.Copy(pointers, 0, rootPtr, count);
        var err = mpv_command(Handle, rootPtr);

        foreach (var ptr in pointers)
            Marshal.FreeHGlobal(ptr);

        Marshal.FreeHGlobal(rootPtr);

        if (err < 0)
            HandleError(err, "error executing command: " + string.Join("\n", args));
    }

    public string Expand(string? value)
    {
        if (value == null)
            return "";

        if (!value.Contains("${"))
            return value;

        string[] args     = { "expand-text", value };
        var      count    = args.Length + 1;
        var      pointers = new IntPtr[count];
        var      rootPtr  = Marshal.AllocHGlobal(IntPtr.Size * count);

        for (var index = 0; index < args.Length; index++)
        {
            var bytes = GetUtf8Bytes(args[index]);
            var ptr   = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, ptr, bytes.Length);
            pointers[index] = ptr;
        }

        Marshal.Copy(pointers, 0, rootPtr, count);
        var resultNodePtr = Marshal.AllocHGlobal(16);
        var err           = mpv_command_ret(Handle, rootPtr, resultNodePtr);

        foreach (var ptr in pointers)
            Marshal.FreeHGlobal(ptr);

        Marshal.FreeHGlobal(rootPtr);

        if (err < 0)
        {
            HandleError(err, "error executing command: " + string.Join("\n", args));
            Marshal.FreeHGlobal(resultNodePtr);
            return "property expansion error";
        }

        var resultNode = Marshal.PtrToStructure<MpvNode>(resultNodePtr);
        var ret        = ConvertFromUtf8(resultNode.str);
        mpv_free_node_contents(resultNodePtr);
        Marshal.FreeHGlobal(resultNodePtr);
        return ret;
    }

    public bool GetPropertyBool(string name)
    {
        var err = mpv_get_property(Handle, GetUtf8Bytes(name),
                                   MpvFormat.MpvFormatFlag, out IntPtr lpBuffer);

        if (err < 0)
            HandleError(err, "error getting property: " + name);

        return lpBuffer.ToInt32() != 0;
    }

    public void SetPropertyBool(string name, bool value)
    {
        long val = value ? 1 : 0;
        var  err = mpv_set_property(Handle, GetUtf8Bytes(name), MpvFormat.MpvFormatFlag, ref val);

        if (err < 0)
            HandleError(err, $"error setting property: {name} = {value}");
    }

    public int GetPropertyInt(string name)
    {
        var err = mpv_get_property(Handle, GetUtf8Bytes(name),
                                   MpvFormat.MpvFormatInt64, out IntPtr lpBuffer);

        if (err < 0 && App.DebugMode)
            HandleError(err, "error getting property: " + name);

        return lpBuffer.ToInt32();
    }

    public void SetPropertyInt(string name, int value)
    {
        long val = value;
        var  err = mpv_set_property(Handle, GetUtf8Bytes(name), MpvFormat.MpvFormatInt64, ref val);

        if (err < 0)
            HandleError(err, $"error setting property: {name} = {value}");
    }

    public void SetPropertyLong(string name, long value)
    {
        var err = mpv_set_property(Handle, GetUtf8Bytes(name), MpvFormat.MpvFormatInt64, ref value);

        if (err < 0)
            HandleError(err, $"error setting property: {name} = {value}");
    }

    public double GetPropertyDouble(string name, bool handleError = true)
    {
        var err = mpv_get_property(Handle, GetUtf8Bytes(name),
                                   MpvFormat.MpvFormatDouble, out double value);

        if (err < 0 && handleError && App.DebugMode)
            HandleError(err, "error getting property: " + name);

        return value;
    }

    public string GetPropertyString(string name)
    {
        if (Handle == IntPtr.Zero)
            return "";

        var err = mpv_get_property(Handle, GetUtf8Bytes(name),
                                   MpvFormat.MpvFormatString, out IntPtr lpBuffer);

        switch (err)
        {
            case 0 :
            {
                var ret = ConvertFromUtf8(lpBuffer);
                mpv_free(lpBuffer);
                return ret;
            }
            case < 0 when App.DebugMode :
                HandleError(err, "error getting property: " + name);
                break;
        }

        return "";
    }

    public void SetPropertyString(string name, string value)
    {
        if (Handle == IntPtr.Zero)
        {
            Terminal.WriteError($"error setting property: {name} = {value}");
            return;
        }

        var   bytes = GetUtf8Bytes(value);
        var err   = mpv_set_property(Handle, GetUtf8Bytes(name), MpvFormat.MpvFormatString, ref bytes);

        if (err < 0)
            HandleError(err, $"error setting property: {name} = {value}");
    }

    public string GetPropertyOsdString(string name)
    {
        var err = mpv_get_property(Handle, GetUtf8Bytes(name),
                                   MpvFormat.MpvFormatOsdString, out IntPtr lpBuffer);

        switch (err)
        {
            case 0 :
            {
                var ret = ConvertFromUtf8(lpBuffer);
                mpv_free(lpBuffer);
                return ret;
            }
            case < 0 :
                HandleError(err, "error getting property: " + name);
                break;
        }

        return "";
    }

    public void ObservePropertyInt(string name, Action<int> action)
    {
        lock (IntPropChangeActions)
        {
            if (!IntPropChangeActions.ContainsKey(name))
            {
                var err = mpv_observe_property(Handle, 0, name, MpvFormat.MpvFormatInt64);

                if (err < 0)
                    HandleError(err, "error observing property: " + name);
                else
                    IntPropChangeActions[name] = new List<Action<int>>();
            }

            if (IntPropChangeActions.ContainsKey(name))
                IntPropChangeActions[name].Add(action);
        }
    }

    public void ObservePropertyDouble(string name, Action<double> action)
    {
        lock (DoublePropChangeActions)
        {
            if (!DoublePropChangeActions.ContainsKey(name))
            {
                var err = mpv_observe_property(Handle, 0, name, MpvFormat.MpvFormatDouble);

                if (err < 0)
                    HandleError(err, "error observing property: " + name);
                else
                    DoublePropChangeActions[name] = new List<Action<double>>();
            }

            if (DoublePropChangeActions.ContainsKey(name))
                DoublePropChangeActions[name].Add(action);
        }
    }

    public void ObservePropertyBool(string name, Action<bool> action)
    {
        lock (BoolPropChangeActions)
        {
            if (!BoolPropChangeActions.ContainsKey(name))
            {
                var err = mpv_observe_property(Handle, 0, name, MpvFormat.MpvFormatFlag);

                if (err < 0)
                    HandleError(err, "error observing property: " + name);
                else
                    BoolPropChangeActions[name] = new List<Action<bool>>();
            }

            if (BoolPropChangeActions.ContainsKey(name))
                BoolPropChangeActions[name].Add(action);
        }
    }

    public void ObservePropertyString(string name, Action<string> action)
    {
        lock (StringPropChangeActions)
        {
            if (!StringPropChangeActions.ContainsKey(name))
            {
                var err = mpv_observe_property(Handle, 0, name, MpvFormat.MpvFormatString);

                if (err < 0)
                    HandleError(err, "error observing property: " + name);
                else
                    StringPropChangeActions[name] = new List<Action<string>>();
            }

            if (StringPropChangeActions.ContainsKey(name))
                StringPropChangeActions[name].Add(action);
        }
    }

    public void ObserveProperty(string name, Action action)
    {
        lock (PropChangeActions)
        {
            if (!PropChangeActions.ContainsKey(name))
            {
                var err = mpv_observe_property(Handle, 0, name, MpvFormat.MpvFormatNone);

                if (err < 0)
                    HandleError(err, "error observing property: " + name);
                else
                    PropChangeActions[name] = new List<Action>();
            }

            if (PropChangeActions.ContainsKey(name))
                PropChangeActions[name].Add(action);
        }
    }

    private static void HandleError(MpvError err, string msg)
    {
        Terminal.WriteError(msg);
        Terminal.WriteError(GetError(err));
    }
}
