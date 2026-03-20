using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Windows;
using WinForms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace HFAuthenticator
{
    public partial class App : System.Windows.Application
    {
        private WinForms.NotifyIcon _notifyIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            bool silent = e.Args != null && e.Args.Any(a => string.Equals(a, "-silent", StringComparison.OrdinalIgnoreCase));

            // Initialize NotifyIcon
            _notifyIcon = new WinForms.NotifyIcon();

            try
            {
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
                if (File.Exists(iconPath))
                {
                    _notifyIcon.Icon = new Drawing.Icon(iconPath);
                }
                else
                {
                    var asm = Assembly.GetEntryAssembly();
                    if (asm != null)
                    {
                        var asmIcon = Drawing.Icon.ExtractAssociatedIcon(asm.Location);
                        if (asmIcon != null)
                            _notifyIcon.Icon = asmIcon;
                    }

                    if (_notifyIcon.Icon == null)
                        _notifyIcon.Icon = Drawing.SystemIcons.Application;
                }
            }
            catch
            {
                _notifyIcon.Icon = Drawing.SystemIcons.Application;
            }

            _notifyIcon.Text = "HFAuthenticator";

            // Context menu for tray icon
            var ctx = new WinForms.ContextMenuStrip();

            var showItem = new WinForms.ToolStripMenuItem("Show");
            showItem.Click += (s, args) => Dispatcher.Invoke(ShowMainWindow);

            var hideItem = new WinForms.ToolStripMenuItem("Hide");
            hideItem.Click += (s, args) => Dispatcher.Invoke(HideMainWindow);

            var exitItem = new WinForms.ToolStripMenuItem("Exit");
            exitItem.Click += (s, args) => Dispatcher.Invoke(ExitApplication);

            ctx.Items.Add(showItem);
            ctx.Items.Add(hideItem);
            ctx.Items.Add(new WinForms.ToolStripSeparator());
            ctx.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = ctx;
            _notifyIcon.DoubleClick += (s, args) => Dispatcher.Invoke(ShowMainWindow);
            _notifyIcon.Visible = true;

            // Create main window but only show when not silent
            var main = new MainWindow();
            this.MainWindow = main;

            if (!silent)
                main.Show();
            else
                main.Hide();
        }

        private void ShowMainWindow()
        {
            if (this.MainWindow == null)
                this.MainWindow = new MainWindow();

            var wnd = this.MainWindow;
            if (!wnd.IsVisible)
                wnd.Show();

            if (wnd.WindowState == WindowState.Minimized)
                wnd.WindowState = WindowState.Normal;

            wnd.Activate();
        }

        private void HideMainWindow()
        {
            if (this.MainWindow != null && this.MainWindow.IsVisible)
                this.MainWindow.Hide();
        }

        private void ExitApplication()
        {
            try
            {
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                    _notifyIcon = null;
                }
            }
            catch { }

            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                    _notifyIcon = null;
                }
            }
            catch { }

            base.OnExit(e);
        }
    }
}
