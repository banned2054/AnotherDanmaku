using MpvNet.Help;
using MpvNet.Windows.Help;
using MpvNet.Windows.Native;
using MpvNet.Windows.UI;
using MpvNet.Windows.WPF;
using System.Threading;
using System.Windows.Forms;

namespace MpvNet.Windows;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        try
        {
            RegistryHelp.ProductName = AppInfo.Product;
            Translator.Current       = new WpfTranslator();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            AppDomain.CurrentDomain.UnhandledException += (_, e) => Terminal.WriteError(e.ExceptionObject);
            Application.ThreadException                += (_, e) => Terminal.WriteError(e.Exception);

            if (App.IsTerminalAttached)
                WinApi.AttachConsole(-1 /*ATTACH_PARENT_PROCESS*/);

            var args = Environment.GetCommandLineArgs().Skip(1).ToArray();

            if (args.Length > 0 && args[0] == "--register-file-associations")
            {
                FileAssociation.Register(args[1], args.Skip(1).ToArray());
                return;
            }

            App.Init();
            Theme.Init();
            var mutex = new Mutex(true, StringHelp.GetMd5Hash(App.ConfPath), out var isFirst);

            if (Control.ModifierKeys == Keys.Shift                   ||
                App.CommandLine.Contains("--process-instance=multi") ||
                App.CommandLine.Contains("--o="))
            {
                App.ProcessInstance = "multi";
            }

            if (App.ProcessInstance is "single" or "queue" && !isFirst)
            {
                var args2 = new List<string> { App.ProcessInstance };

                foreach (var arg in args)
                {
                    if (!arg.StartsWith("--") && (arg == "-"          || arg.Contains("://") ||
                                                  arg.Contains(":\\") || arg.StartsWith("\\\\")))

                        args2.Add(arg);
                    else if (arg == "--queue")
                        args2[0] = "queue";
                    else if (arg.StartsWith("--command="))
                    {
                        args2[0] = "command";
                        args2.Add(arg[10..]);
                    }
                }

                var processList = Process.GetProcessesByName("mpvnet");

                for (var i = 0; i < 20; i++)
                {
                    foreach (var proc in processList)
                    {
                        if (proc.MainWindowHandle == IntPtr.Zero) continue;
                        WinApi.AllowSetForegroundWindow(proc.Id);
                        var data = new WinApi.CopyDataStruct
                        {
                            lpData = string.Join("\n", args2.ToArray())
                        };
                        data.cbData = data.lpData.Length * 2 + 1;
                        WinApi.SendMessage(proc.MainWindowHandle, 0x004A /*WM_COPYDATA*/, IntPtr.Zero, ref data);
                        mutex.Dispose();

                        if (App.IsTerminalAttached)
                            WinApi.FreeConsole();

                        return;
                    }

                    Thread.Sleep(50);
                }

                mutex.Dispose();
                return;
            }

            if (ProcessCommandLineArguments())
                Environment.GetCommandLineArgs();
            else if (App.CommandLine.Contains("--o="))
            {
                App.AutoLoadFolder = false;
                Player.Init(IntPtr.Zero, true);
                CommandLine.ProcessCommandLineArgsPostInit();
                CommandLine.ProcessCommandLineFiles();
                Player.SetPropertyString("idle", "no");
                Player.EventLoop();
                Player.Destroy();
            }
            else
                Application.Run(new WinForms.MainForm());

            if (App.IsTerminalAttached)
                WinApi.FreeConsole();

            mutex.Dispose();
        }
        catch (Exception ex)
        {
            Terminal.WriteError(ex);
        }
    }

    private static bool ProcessCommandLineArguments()
    {
        foreach (var arg in Environment.GetCommandLineArgs().Skip(1))
        {
            switch (arg)
            {
                case "--profile=help" :
                    Player.Init(IntPtr.Zero, false);
                    Console.WriteLine(Player.GetProfiles());
                    Player.Destroy();
                    return true;
                case "--vd=help" :
                case "--ad=help" :
                    Player.Init(IntPtr.Zero, false);
                    Console.WriteLine(Player.GetDecoders());
                    Player.Destroy();
                    return true;
                case "--audio-device=help" :
                    Player.Init(IntPtr.Zero, false);
                    Console.WriteLine(Player.GetPropertyOsdString("audio-device-list"));
                    Player.Destroy();
                    return true;
                case "--input-keylist" :
                    Player.Init(IntPtr.Zero, false);
                    Console.WriteLine(Player.GetPropertyString("input-key-list").Replace(",", Br));
                    Player.Destroy();
                    return true;
                case "--version" :
                    Player.Init(IntPtr.Zero, false);
                    Console.WriteLine(AppClass.About);
                    Player.Destroy();
                    return true;
            }
        }

        return false;
    }
}
