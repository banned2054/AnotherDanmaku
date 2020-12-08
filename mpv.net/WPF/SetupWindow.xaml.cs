﻿
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows;

using WinForms = System.Windows.Forms;

using static StockIcon;

namespace mpvnet
{
    public partial class SetupWindow : Window
    {
        public SetupWindow() => InitializeComponent();

        static BitmapSource _ShieldIcon;

        public static BitmapSource ShieldIcon {
            get {
                if (_ShieldIcon == null)
                {
                    IntPtr icon = GetIcon(SHSTOCKICONID.Shield, SHSTOCKICONFLAGS.SHGSI_ICON);
                    _ShieldIcon = Imaging.CreateBitmapSourceFromHIcon(
                        icon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    DestroyIcon(icon);
                }
                return _ShieldIcon;
            }
        }

        void RegisterFileAssociations(string value)
        {
            try
            {
                using (Process proc = new Process())
                {
                    proc.StartInfo.FileName = WinForms.Application.ExecutablePath;
                    proc.StartInfo.Arguments = "--reg-file-assoc " + value;
                    proc.StartInfo.Verb = "runas";
                    proc.StartInfo.UseShellExecute = true;
                    proc.Start();
                }

                Msg.Show(value[0].ToString().ToUpper() + value.Substring(1) +
                         " file associations successfully created.");
            } catch {}
        }

        void AddVideo_Click(object sender, RoutedEventArgs e) => RegisterFileAssociations("video");
        void AddAudio_Click(object sender, RoutedEventArgs e) => RegisterFileAssociations("audio");
        void AddImage_Click(object sender, RoutedEventArgs e) => RegisterFileAssociations("image");

        void RemoveFileAssociations_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (Process proc = new Process())
                {
                    proc.StartInfo.FileName = "powershell.exe";
                    proc.StartInfo.Arguments = "-NoLogo -NoExit -ExecutionPolicy Unrestricted -File \"" +
                        Folder.Startup + "Setup\\uninstall.ps1\"";
                    proc.StartInfo.Verb = "runas";
                    proc.StartInfo.UseShellExecute = true;
                    proc.Start();
                }
            } catch { }
        }

        void AddToPathEnvVar_Click(object sender, RoutedEventArgs e)
        {
            string var = Folder.Startup.TrimEnd(Path.DirectorySeparatorChar) + ";";
            string path = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User);

            if (path.Contains(var))
                Msg.ShowWarning("Path was already containing mpv.net.");
            else
            {
                Environment.SetEnvironmentVariable("Path", var + path, EnvironmentVariableTarget.User);
                Msg.Show("mpv.net was successfully added to Path.", (var + path).Replace(";","\n"));
            }
        }

        void RemoveFromPathEnvVar_Click(object sender, RoutedEventArgs e)
        {
            string var = Folder.Startup.TrimEnd(Path.DirectorySeparatorChar) + ";";
            string path = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User);

            if (path.Contains(var))
            {
                Environment.SetEnvironmentVariable("Path", path.Replace(var, ""), EnvironmentVariableTarget.User);
                Msg.Show("mpv.net was successfully removed from Path.");
            }
            else
                Msg.ShowWarning("Path was not containing mpv.net.");
        }

        void AddStartMenuShortcut_Click(object sender, RoutedEventArgs e)
        {
            ExecutePowerShellScript(Folder.Startup + "Setup\\create start menu shortcut.ps1");
        }

        void RemoveStartMenuShortcut_Click(object sender, RoutedEventArgs e)
        {
            ExecutePowerShellScript(Folder.Startup + "Setup\\remove start menu shortcut.ps1");
        }

        void ShowEnvVarEditor_Click(object sender, RoutedEventArgs e)
        {
            ProcessHelp.Execute("rundll32.exe", "sysdm.cpl,EditEnvironmentVariables");
        }

        void ExecutePowerShellScript(string file)
        {
            ProcessHelp.Execute("powershell.exe", "-NoLogo -NoExit -ExecutionPolicy Unrestricted -File \"" + file + "\"");
        }

        private void EditDefaultApp_Click(object sender, RoutedEventArgs e)
        {
            ProcessHelp.ShellExecute("ms-settings:defaultapps");
        }
    }
}
