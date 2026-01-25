using System;
using System.Diagnostics;
using System.Drawing;
using System.ServiceProcess;
using System.Windows.Forms;
using System.IO;

namespace DNSAgent.Tray
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new TrayApplicationContext());
        }
    }

    public class TrayApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private readonly System.Windows.Forms.Timer _statusTimer;
        private const string ServiceName = "DNSAgent";
        private const string DashboardUrl = "http://localhost:5123";

        public TrayApplicationContext()
        {
            // Create context menu
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Open Dashboard", null, (s, e) => OpenDashboard());
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Start Service", null, (s, e) => ControlService(ServiceControllerStatus.Running));
            contextMenu.Items.Add("Stop Service", null, (s, e) => ControlService(ServiceControllerStatus.Stopped));
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Exit", null, (s, e) => Exit());

            // Initialize Tray Icon
            _trayIcon = new NotifyIcon()
            {
                Text = "DNS Agent v1.2",
                ContextMenuStrip = contextMenu,
                Visible = true
            };

            // Double click to open dashboard
            _trayIcon.DoubleClick += (s, e) => OpenDashboard();

            // Set up icon refresh timer
            _statusTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            _statusTimer.Tick += (s, e) => UpdateStatus();
            _statusTimer.Start();

            UpdateStatus(); // Initial status
        }

        private void UpdateStatus()
        {
            try
            {
                using var sc = new ServiceController(ServiceName);
                var status = sc.Status;
                
                string statusText = $"DNS Agent - {status}";
                _trayIcon.Text = statusText;

                // For now, use simple colored circles or dots as "creative" dynamic icons
                _trayIcon.Icon = GetStatusIcon(status);
            }
            catch
            {
                _trayIcon.Text = "DNS Agent - Service Not Found";
                _trayIcon.Icon = SystemIcons.Error;
            }
        }

        private Icon GetStatusIcon(ServiceControllerStatus status)
        {
            try
            {
                string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.png");
                using var baseImage = File.Exists(imagePath) ? Image.FromFile(imagePath) : null;
                
                using var bitmap = new Bitmap(32, 32);
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                    if (baseImage != null)
                    {
                        g.DrawImage(baseImage, 0, 0, 32, 32);
                    }
                    else
                    {
                        // Fallback if image missing
                        g.FillEllipse(Brushes.DodgerBlue, 2, 2, 28, 28);
                        using var font = new Font("Arial", 7, FontStyle.Bold);
                        g.DrawString("DNS", font, Brushes.White, 2, 10);
                    }

                    // High-visibility status badge in corner
                    Color statusColor = status == ServiceControllerStatus.Running ? Color.LimeGreen : Color.Red;
                    using var brush = new SolidBrush(statusColor);
                    g.FillEllipse(brush, 20, 20, 10, 10);
                    g.DrawEllipse(new Pen(Color.White, 1.5f), 20, 20, 10, 10);
                }
                return Icon.FromHandle(bitmap.GetHicon());
            }
            catch
            {
                return SystemIcons.Application;
            }
        }

        private void OpenDashboard()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = DashboardUrl,
                UseShellExecute = true
            });
        }

        private void ControlService(ServiceControllerStatus desiredStatus)
        {
            try
            {
                using var sc = new ServiceController(ServiceName);
                if (desiredStatus == ServiceControllerStatus.Running && sc.Status == ServiceControllerStatus.Stopped)
                {
                    sc.Start();
                }
                else if (desiredStatus == ServiceControllerStatus.Stopped && sc.Status == ServiceControllerStatus.Running)
                {
                    sc.Stop();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to control service: {ex.Message}", "DNS Agent", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Exit()
        {
            _trayIcon.Visible = false;
            _statusTimer.Stop();
            Application.Exit();
        }
    }
}