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
    public static extern MpvError mpv_get_property(nint mpvHandle, byte[] name, MpvFormat format, out nint data);

    [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern MpvError mpv_get_property(nint mpvHandle, byte[] name, MpvFormat format, out double data);

    [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern MpvError mpv_set_property(nint mpvHandle, byte[] name, MpvFormat format, ref byte[] data);

    [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern MpvError mpv_set_property(nint mpvHandle, byte[] name, MpvFormat format, ref long data);

    [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern MpvError mpv_observe_property(nint mpvHandle, ulong reply_userdata,
                                                       [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
                                                       MpvFormat format);

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
        Nomem               = -2,
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
        Shutdown         = 1,
        LogMessage       = 2,
        GetPropertyReply = 3,
        SetPropertyReply = 4,
        CommandReply     = 5,
        StartFile        = 6,
        EndFile          = 7,
        FileLoaded       = 8,
        ClientMessage    = 16,
        VideoReconfig    = 17,
        AudioReconfig    = 18,
        Seek             = 20,
        PlaybackRestart  = 21,
        PropertyChange   = 22,
        QueueOverflow    = 24,
        Hook             = 25
    }

    public enum MpvFormat
    {
        None      = 0,
        String    = 1,
        OsdString = 2,
        Flag      = 3,
        Int64     = 4,
        Double    = 5,
        Node      = 6,
        NodeArray = 7,
        NodeMap   = 8,
        ByteArray = 9
    }

    public enum MpvLogLevel
    {
        None  = 0,
        Fatal = 10,
        Error = 20,
        Warn  = 30,
        Info  = 40,
        V     = 50,
        Debug = 60,
        Trace = 70,
    }

    public enum MpvEndFileReason
    {
        Eof      = 0,
        Stop     = 2,
        Quit     = 3,
        Error    = 4,
        Redirect = 5
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MpvEventLogMessage
    {
        public nint        prefix;
        public nint        level;
        public nint        text;
        public MpvLogLevel log_level;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MpvEvent
    {
        public MpvEventId event_id;
        public int        error;
        public ulong      reply_userData;
        public nint       data;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MpvEventClientMessage
    {
        public int  num_args;
        public nint args;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MpvEventProperty
    {
        public string    name;
        public MpvFormat format;
        public nint      data;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MpvEventEndFile
    {
        public int reason;
        public int error;
    }

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct MpvNode
    {
        [FieldOffset(0)] public nint      str;
        [FieldOffset(0)] public int       flag;
        [FieldOffset(0)] public long      int64;
        [FieldOffset(0)] public double    dbl;
        [FieldOffset(0)] public nint      list;
        [FieldOffset(0)] public nint      ba;
        [FieldOffset(8)] public MpvFormat format;
    }

    public static string[] ConvertFromUtf8Strings(nint utf8StringArray, int stringCount)
    {
        var intPtrArray = new nint[stringCount];
        var stringArray = new string[stringCount];
        Marshal.Copy(utf8StringArray, intPtrArray, 0, stringCount);

        for (var i = 0; i < stringCount; i++)
            stringArray[i] = ConvertFromUtf8(intPtrArray[i]);

        return stringArray;
    }

    public static string ConvertFromUtf8(nint nativeUtf8)
    {
        var len = 0;

        while (Marshal.ReadByte(nativeUtf8, len) != 0)
            ++len;

        var buffer = new byte[len];
        Marshal.Copy(nativeUtf8, buffer, 0, buffer.Length);
        return Encoding.UTF8.GetString(buffer);
    }

    public static string GetError(MpvError err) => ConvertFromUtf8(mpv_error_string(err));

    public static byte[] GetUtf8Bytes(string s) => Encoding.UTF8.GetBytes(s + "\0");
}
