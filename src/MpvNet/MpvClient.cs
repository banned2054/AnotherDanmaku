using System.Runtime.InteropServices;
using static MpvNet.Native.LibMpv;

namespace MpvNet;

public class MpvClient
{
    public event Action<string[]>?              ClientMessage;    // client-message      MPV_EVENT_CLIENT_MESSAGE
    public event Action<mpv_log_level, string>? LogMessage;       // log-message         MPV_EVENT_LOG_MESSAGE
    public event Action<mpv_end_file_reason>?   EndFile;          // end-file            MPV_EVENT_END_FILE
    public event Action?                        Shutdown;         // shutdown            MPV_EVENT_SHUTDOWN
    public event Action?                        GetPropertyReply; // get-property-reply  MPV_EVENT_GET_PROPERTY_REPLY
    public event Action?                        SetPropertyReply; // set-property-reply  MPV_EVENT_SET_PROPERTY_REPLY
    public event Action?                        CommandReply;     // command-reply       MPV_EVENT_COMMAND_REPLY
    public event Action?                        StartFile;        // start-file          MPV_EVENT_START_FILE
    public event Action?                        FileLoaded;       // file-loaded         MPV_EVENT_FILE_LOADED
    public event Action?                        VideoReconfig;    // video-reconfig      MPV_EVENT_VIDEO_RECONFIG
    public event Action?                        AudioReconfig;    // audio-reconfig      MPV_EVENT_AUDIO_RECONFIG
    public event Action?                        Seek;             // seek                MPV_EVENT_SEEK
    public event Action?                        PlaybackRestart;  // playback-restart    MPV_EVENT_PLAYBACK_RESTART

    public Dictionary<string, List<Action>>         PropChangeActions       { get; set; } = new();
    public Dictionary<string, List<Action<int>>>    IntPropChangeActions    { get; set; } = new();
    public Dictionary<string, List<Action<bool>>>   BoolPropChangeActions   { get; set; } = new();
    public Dictionary<string, List<Action<double>>> DoublePropChangeActions { get; set; } = new();
    public Dictionary<string, List<Action<string>>> StringPropChangeActions { get; set; } = new();

    public nint Handle { get; set; }

    public void EventLoop()
    {
        while (true)
        {
            IntPtr ptr = mpv_wait_event(Handle, -1);
            var    evt = (mpv_event)Marshal.PtrToStructure(ptr, typeof(mpv_event))!;

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
                            (mpv_event_log_message)Marshal.PtrToStructure(evt.data, typeof(mpv_event_log_message))!;
                        OnLogMessage(data);
                    }
                        break;
                    case MpvEventId.ClientMessage :
                    {
                        var data =
                            (mpv_event_client_message)
                            Marshal.PtrToStructure(evt.data, typeof(mpv_event_client_message))!;
                        OnClientMessage(data);
                    }
                        break;
                    case MpvEventId.VideoReconfig :
                        OnVideoReconfig();
                        break;
                    case MpvEventId.EndFile :
                    {
                        var data = (mpv_event_end_file)Marshal.PtrToStructure(evt.data, typeof(mpv_event_end_file))!;
                        OnEndFile(data);
                    }
                        break;
                    case MpvEventId.FileLoaded : // triggered after MPV_EVENT_START_FILE
                        OnFileLoaded();
                        break;
                    case MpvEventId.PropertyChange :
                    {
                        var data = (mpv_event_property)Marshal.PtrToStructure(evt.data, typeof(mpv_event_property))!;
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
                    case MpvEventId.None :
                        break;
                    case MpvEventId.ScriptInputDispatch :
                        break;
                    case MpvEventId.QueueOverflow :
                        break;
                    case MpvEventId.Hook :
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

    protected virtual void OnClientMessage(mpv_event_client_message data) =>
        ClientMessage?.Invoke(ConvertFromUtf8Strings(data.args, data.num_args));

    protected virtual void OnLogMessage(mpv_event_log_message data)
    {
        if (LogMessage == null) return;
        var msg = $"[{ConvertFromUtf8(data.prefix)}] {ConvertFromUtf8(data.text)}";
        LogMessage.Invoke(data.log_level, msg);
    }

    protected virtual void OnPropertyChange(mpv_event_property data)
    {
        switch (data.format)
        {
            case mpv_format.MPV_FORMAT_FLAG :
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
            case mpv_format.MPV_FORMAT_STRING :
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
            case mpv_format.MPV_FORMAT_INT64 :
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
            case mpv_format.MPV_FORMAT_NONE :
            {
                lock (PropChangeActions)
                    foreach (var action in PropChangeActions.Where(pair => pair.Key == data.name)
                                                            .SelectMany(pair => pair.Value))
                        action.Invoke();
                break;
            }
            case mpv_format.MPV_FORMAT_DOUBLE :
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
            case mpv_format.MPV_FORMAT_OSD_STRING :
                break;
            case mpv_format.MPV_FORMAT_NODE :
                break;
            case mpv_format.MPV_FORMAT_NODE_ARRAY :
                break;
            case mpv_format.MPV_FORMAT_NODE_MAP :
                break;
            case mpv_format.MPV_FORMAT_BYTE_ARRAY :
                break;
            default :
                throw new ArgumentOutOfRangeException();
        }
    }

    protected virtual void OnEndFile(mpv_event_end_file data) => EndFile?.Invoke((mpv_end_file_reason)data.reason);
    protected virtual void OnFileLoaded()                     => FileLoaded?.Invoke();
    protected virtual void OnShutdown()                       => Shutdown?.Invoke();
    protected virtual void OnGetPropertyReply()               => GetPropertyReply?.Invoke();
    protected virtual void OnSetPropertyReply()               => SetPropertyReply?.Invoke();
    protected virtual void OnCommandReply()                   => CommandReply?.Invoke();
    protected virtual void OnStartFile()                      => StartFile?.Invoke();
    protected virtual void OnVideoReconfig()                  => VideoReconfig?.Invoke();
    protected virtual void OnAudioReconfig()                  => AudioReconfig?.Invoke();
    protected virtual void OnSeek()                           => Seek?.Invoke();
    protected virtual void OnPlaybackRestart()                => PlaybackRestart?.Invoke();

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

        foreach (IntPtr ptr in pointers)
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

        var resultNode = Marshal.PtrToStructure<mpv_node>(resultNodePtr);
        var ret        = ConvertFromUtf8(resultNode.str);
        mpv_free_node_contents(resultNodePtr);
        Marshal.FreeHGlobal(resultNodePtr);
        return ret;
    }

    public bool GetPropertyBool(string name)
    {
        var err = mpv_get_property(Handle, GetUtf8Bytes(name),
                                   mpv_format.MPV_FORMAT_FLAG, out IntPtr lpBuffer);

        if (err < 0)
            HandleError(err, "error getting property: " + name);

        return lpBuffer.ToInt32() != 0;
    }

    public void SetPropertyBool(string name, bool value)
    {
        long val = value ? 1 : 0;
        var  err = mpv_set_property(Handle, GetUtf8Bytes(name), mpv_format.MPV_FORMAT_FLAG, ref val);

        if (err < 0)
            HandleError(err, $"error setting property: {name} = {value}");
    }

    public int GetPropertyInt(string name)
    {
        var err = mpv_get_property(Handle, GetUtf8Bytes(name),
                                   mpv_format.MPV_FORMAT_INT64, out IntPtr lpBuffer);

        if (err < 0 && App.DebugMode)
            HandleError(err, "error getting property: " + name);

        return lpBuffer.ToInt32();
    }

    public void SetPropertyInt(string name, int value)
    {
        long val = value;
        var  err = mpv_set_property(Handle, GetUtf8Bytes(name), mpv_format.MPV_FORMAT_INT64, ref val);

        if (err < 0)
            HandleError(err, $"error setting property: {name} = {value}");
    }

    public void SetPropertyLong(string name, long value)
    {
        var err = mpv_set_property(Handle, GetUtf8Bytes(name), mpv_format.MPV_FORMAT_INT64, ref value);

        if (err < 0)
            HandleError(err, $"error setting property: {name} = {value}");
    }

    public long GetPropertyLong(string name)
    {
        var err = mpv_get_property(Handle, GetUtf8Bytes(name),
                                   mpv_format.MPV_FORMAT_INT64, out IntPtr lpBuffer);

        if (err < 0)
            HandleError(err, "error getting property: " + name);

        return lpBuffer.ToInt64();
    }

    public double GetPropertyDouble(string name, bool handleError = true)
    {
        var err = mpv_get_property(Handle, GetUtf8Bytes(name),
                                   mpv_format.MPV_FORMAT_DOUBLE, out double value);

        if (err < 0 && handleError && App.DebugMode)
            HandleError(err, "error getting property: " + name);

        return value;
    }

    public void SetPropertyDouble(string name, double value)
    {
        var val = value;
        var err = mpv_set_property(Handle, GetUtf8Bytes(name), mpv_format.MPV_FORMAT_DOUBLE, ref val);

        if (err < 0)
            HandleError(err, $"error setting property: {name} = {value}");
    }

    public string GetPropertyString(string name)
    {
        var err = mpv_get_property(Handle, GetUtf8Bytes(name),
                                   mpv_format.MPV_FORMAT_STRING, out IntPtr lpBuffer);

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
        var bytes = GetUtf8Bytes(value);
        var err   = mpv_set_property(Handle, GetUtf8Bytes(name), mpv_format.MPV_FORMAT_STRING, ref bytes);

        if (err < 0)
            HandleError(err, $"error setting property: {name} = {value}");
    }

    public string GetPropertyOsdString(string name)
    {
        var err = mpv_get_property(Handle, GetUtf8Bytes(name),
                                   mpv_format.MPV_FORMAT_OSD_STRING, out IntPtr lpBuffer);

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
                var err = mpv_observe_property(Handle, 0, name, mpv_format.MPV_FORMAT_INT64);

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
                var err = mpv_observe_property(Handle, 0, name, mpv_format.MPV_FORMAT_DOUBLE);

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
                var err = mpv_observe_property(Handle, 0, name, mpv_format.MPV_FORMAT_FLAG);

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
                var err = mpv_observe_property(Handle, 0, name, mpv_format.MPV_FORMAT_STRING);

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
                var err = mpv_observe_property(Handle, 0, name, mpv_format.MPV_FORMAT_NONE);

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
