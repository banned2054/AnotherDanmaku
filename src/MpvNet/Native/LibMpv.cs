#pragma warning disable IDE1006 // type name starts with underscore
#pragma warning disable CA1401  // P/Invokes should not be visible
#pragma warning disable CA2101  // Specify marshaling for P/Invoke string arguments

using System.Runtime.InteropServices;
using System.Text;

namespace MpvNet.Native;

public static class LibMpv
{
    [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint mpv_create();

    [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint mpv_create_client(nint mpvHandle, [MarshalAs(UnmanagedType.LPUTF8Str)] string command);

    [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern MpvError mpv_initialize(nint mpvHandle);

    [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void mpv_destroy(nint mpvHandle);

    [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern MpvError mpv_command(nint mpvHandle, nint strings);

    [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern MpvError mpv_command_string(nint                                        mpvHandle,
                                                     [MarshalAs(UnmanagedType.LPUTF8Str)] string command);

    [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern MpvError mpv_command_ret(nint mpvHandle, nint strings, nint node);

    [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void mpv_free_node_contents(nint node);

    [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint mpv_error_string(MpvError error);

    [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern MpvError mpv_request_log_messages(nint                                        mpvHandle,
                                                           [MarshalAs(UnmanagedType.LPUTF8Str)] string minLevel);

    [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_set_option(nint mpvHandle, byte[] name, mpv_format format, ref long data);

    [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_set_option_string(nint mpvHandle, byte[] name, byte[] value);

    [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern MpvError mpv_get_property(nint mpvHandle, byte[] name, mpv_format format, out nint data);

    [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern MpvError mpv_get_property(nint mpvHandle, byte[] name, mpv_format format, out double data);

    [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern MpvError mpv_set_property(nint mpvHandle, byte[] name, mpv_format format, ref byte[] data);

    [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern MpvError mpv_set_property(nint mpvHandle, byte[] name, mpv_format format, ref long data);

    [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern MpvError mpv_set_property(nint mpvHandle, byte[] name, mpv_format format, ref double data);

    [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern MpvError mpv_observe_property(nint mpvHandle, ulong replyUserdata,
                                                       [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
                                                       mpv_format format);

    [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_unobserve_property(nint mpvHandle, ulong registeredReplyUserdata);

    [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void mpv_free(nint data);

    [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint mpv_wait_event(nint mpvHandle, double timeout);

    [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern MpvError mpv_request_event(nint mpvHandle, MpvEventId id, int enable);

    public enum MpvError
    {
        Success             = 0,
        EventQueueFull      = -1,
        NoMemory            = -2,
        Uninitialized       = -3,
        InvalidParameter    = -4,
        OptionNotFound      = -5,
        OptionFormat        = -6,
        OptionError         = -7,
        PropertyNotFound    = -8,
        PropertyFormat      = -9,
        PropertyUnavailable = -10,
        PropertyError       = -11,
        Command             = -12,
        LoadingFailed       = -13,
        AoInitFailed        = -14,
        VoInitFailed        = -15,
        NothingToPlay       = -16,
        UnknownFormat       = -17,
        Unsupported         = -18,
        NotImplemented      = -19,
        Generic             = -20
    }

    public enum MpvEventId
    {
        None                = 0,
        Shutdown            = 1,
        LogMessage          = 2,
        GetPropertyReply    = 3,
        SetPropertyReply    = 4,
        CommandReply        = 5,
        StartFile           = 6,
        EndFile             = 7,
        FileLoaded          = 8,
        ScriptInputDispatch = 15,
        ClientMessage       = 16,
        VideoReconfig       = 17,
        AudioReconfig       = 18,
        Seek                = 20,
        PlaybackRestart     = 21,
        PropertyChange      = 22,
        QueueOverflow       = 24,
        Hook                = 25
    }

    public enum mpv_format
    {
        MPV_FORMAT_NONE       = 0,
        MPV_FORMAT_STRING     = 1,
        MPV_FORMAT_OSD_STRING = 2,
        MPV_FORMAT_FLAG       = 3,
        MPV_FORMAT_INT64      = 4,
        MPV_FORMAT_DOUBLE     = 5,
        MPV_FORMAT_NODE       = 6,
        MPV_FORMAT_NODE_ARRAY = 7,
        MPV_FORMAT_NODE_MAP   = 8,
        MPV_FORMAT_BYTE_ARRAY = 9
    }

    public enum mpv_log_level
    {
        MPV_LOG_LEVEL_NONE  = 0,
        MPV_LOG_LEVEL_FATAL = 10,
        MPV_LOG_LEVEL_ERROR = 20,
        MPV_LOG_LEVEL_WARN  = 30,
        MPV_LOG_LEVEL_INFO  = 40,
        MPV_LOG_LEVEL_V     = 50,
        MPV_LOG_LEVEL_DEBUG = 60,
        MPV_LOG_LEVEL_TRACE = 70,
    }

    public enum mpv_end_file_reason
    {
        MPV_END_FILE_REASON_EOF      = 0,
        MPV_END_FILE_REASON_STOP     = 2,
        MPV_END_FILE_REASON_QUIT     = 3,
        MPV_END_FILE_REASON_ERROR    = 4,
        MPV_END_FILE_REASON_REDIRECT = 5
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct mpv_event_log_message
    {
        public nint          prefix;
        public nint          level;
        public nint          text;
        public mpv_log_level log_level;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct mpv_event
    {
        public MpvEventId event_id;
        public int        error;
        public ulong      reply_userdata;
        public nint       data;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct mpv_event_client_message
    {
        public int  num_args;
        public nint args;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct mpv_event_property
    {
        public string     name;
        public mpv_format format;
        public nint       data;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct mpv_event_end_file
    {
        public int reason;
        public int error;
    }

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct mpv_node
    {
        [FieldOffset(0)]
        public nint str;

        [FieldOffset(0)]
        public int flag;

        [FieldOffset(0)]
        public long int64;

        [FieldOffset(0)]
        public double dbl;

        [FieldOffset(0)]
        public nint list;

        [FieldOffset(0)]
        public nint ba;

        [FieldOffset(8)]
        public mpv_format format;
    }

    public static string[] ConvertFromUtf8Strings(nint utf8StringArray, int stringCount)
    {
        nint[]   intPtrArray = new nint[stringCount];
        string[] stringArray = new string[stringCount];
        Marshal.Copy(utf8StringArray, intPtrArray, 0, stringCount);

        for (int i = 0; i < stringCount; i++)
            stringArray[i] = ConvertFromUtf8(intPtrArray[i]);

        return stringArray;
    }

    public static string ConvertFromUtf8(nint nativeUtf8)
    {
        int len = 0;

        while (Marshal.ReadByte(nativeUtf8, len) != 0)
            ++len;

        byte[] buffer = new byte[len];
        Marshal.Copy(nativeUtf8, buffer, 0, buffer.Length);
        return Encoding.UTF8.GetString(buffer);
    }

    public static string GetError(MpvError err) => ConvertFromUtf8(mpv_error_string(err));

    public static byte[] GetUtf8Bytes(string s) => Encoding.UTF8.GetBytes(s + "\0");
}
