using HFAuthenticator.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;

namespace HFAuthenticator
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private HttpClient _httpClient = new HttpClient();
        private Login _loginService;
        private Utils.ConfigManager.AppConfig _config;

        private System.Timers.Timer _autoLoginTimer;
        private int _autoLoginRunning = 0; // 0 not running, 1 running
        private readonly string _logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HFAuthenticator", "autolog.log");
        private bool _suppressToggleEvent = false;
        private bool _suppressFrequencySliderEvent = true;

        // Track last and next run times
        private DateTime? _lastAutoLoginTime = null;
        private DateTime? _nextAutoLoginTime = null;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                _suppressFrequencySliderEvent = false;
                UpdateRequestFrequencySliderLabel();
            };
            AppendLog("Initializing MainWindow");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            UpdateLog.Text = "HF Authenticator Version 1.1.0 By -Windows-11-\n" +
                "一个用于自动登录 HFBZ 上网认证系统的应用\n\n" +
                "1.1.0  2026-03-21 00:15\n" +
                "\t1. 添加登录间隔配置，将固定的10分钟改为2分钟~2小时自由配置\n" +
                "\t2. 修改 UI，添加上次登录、下次登录时间显示\n" +
                "\t3. 添加热点功能，支持操控系统热点启停并可以设置打开应用时自动启动系统热点\n\n" +
                "1.0.0  2026-01-07 00:06\n" +
                "\t1. 添加基础登录功能\n" +
                "\t2. 添加用户名、密码、自动登录、IP配置\n";

            // Load config and populate UI
            try
            {
                _config = Utils.ConfigManager.Load();
                if (_config != null)
                {
                    IPEndpointBox.Text = _config.IPEndpoint;
                    UsernameTextBox.Text = _config.Username;
                    Password.Password = _config.Password;
                    RequestFrequencySlider.Value = _config.RequestFrequency > 0 ? _config.RequestFrequency : 600;

                    // temporarily suppress toggle event while initializing UI
                    _suppressToggleEvent = true;
                    AutoLoginToggle.IsOn = _config.AutoStart;
                    AutoHotSpotToggleSwitch.IsOn = _config.AutoHotspot;
                    if (_config.AutoHotspot)
                    {
                        Task.Run(async () => {
                            await HotspotUtils.TurnOnHotspotAsync();
                            await Dispatcher.Invoke(() => InitializeHotspotToggleAsync());
                        });

                    }
                    _suppressToggleEvent = false;
                }
            }
            catch { }

            // show initial time info
            UpdateTimeInfo();

            // initialize hotspot toggle state asynchronously (fire-and-forget safe)
            _ = InitializeHotspotToggleAsync();

            // If auto start enabled, start timer and run once immediately
            if (_config != null && _config.AutoStart)
            {
                StartAutoLoginTimer(immediate: true);
            }
        }

        private async Task InitializeHotspotToggleAsync()
        {
            try
            {
                var state = await HotspotUtils.IsHotspotOnAsync();
                Dispatcher.Invoke(() =>
                {
                    if (state.HasValue)
                    {
                        HotSpotToggle.IsChecked = state.Value;
                        HotSpotToggle.Content = state.Value ? "关闭热点" : "打开热点";
                    }
                    else
                    {
                        HotSpotToggle.IsChecked = false;
                        HotSpotToggle.Content = "热点状态未知";
                    }
                });
            }
            catch (Exception ex)
            {
                AppendLog($"初始化热点状态失败: {ex.Message}");
            }
        }

        private void SaveConfig()
        {
            if (_config == null) _config = new Utils.ConfigManager.AppConfig();
            _config.AutoStart = AutoLoginToggle.IsOn;
            _config.IPEndpoint = IPEndpointBox.Text;
            _config.Username = UsernameTextBox.Text;
            _config.Password = Password.Password;
            _config.RequestFrequency = (int)RequestFrequencySlider.Value;
            Utils.ConfigManager.Save(_config);

            // If timer is running, update its interval to the new value
            try
            {
                if (_autoLoginTimer != null)
                {
                    var intervalMs = Math.Max(1, _config.RequestFrequency) * 1000.0;
                    _autoLoginTimer.Interval = intervalMs;

                    // Immediately reset scheduling: next run = now + new interval
                    if (_autoLoginTimer.Enabled)
                    {
                        _nextAutoLoginTime = DateTime.Now.AddMilliseconds(_autoLoginTimer.Interval);
                    }
                    else
                    {
                        _nextAutoLoginTime = null;
                    }

                    UpdateTimeInfo();
                }
            }
            catch { }
        }

        private void StartAutoLoginTimer(bool immediate = false)
        {
            var intervalSeconds = (_config != null && _config.RequestFrequency > 0) ? _config.RequestFrequency : (int)RequestFrequencySlider.Value;
            var intervalMs = Math.Max(1, intervalSeconds) * 1000.0;

            if (_autoLoginTimer == null)
            {
                _autoLoginTimer = new System.Timers.Timer(intervalMs);
                _autoLoginTimer.AutoReset = true;
                _autoLoginTimer.Elapsed += async (s, e) => await AutoLoginCallback();
            }
            else
            {
                _autoLoginTimer.Interval = intervalMs;
            }

            _autoLoginTimer.Stop();
            _autoLoginTimer.Start();

            // set next run time
            _nextAutoLoginTime = DateTime.Now.AddMilliseconds(_autoLoginTimer.Interval);
            UpdateTimeInfo();

            if (immediate)
            {
                AppendLog("immediate");
                // run once immediately
                Task.Run(async () => await AutoLoginCallback());
            }
        }

        private void StopAutoLoginTimer()
        {
            if (_autoLoginTimer != null)
            {
                _autoLoginTimer.Stop();
            }
            _nextAutoLoginTime = null;
            UpdateTimeInfo();
        }

        private async Task AutoLoginCallback()
        {
            // record last run time at start
            _lastAutoLoginTime = DateTime.Now;
            UpdateTimeInfo();

            // ensure single run
            if (Interlocked.Exchange(ref _autoLoginRunning, 1) == 1) return;
            try
            {

                // Read UI fields safely on UI thread with timeout
                string ip = null, user = null, pwd = null;
                try
                {
                    var readTask = Dispatcher.InvokeAsync(() => Tuple.Create(IPEndpointBox?.Text ?? string.Empty, UsernameTextBox?.Text ?? string.Empty, Password?.Password ?? string.Empty)).Task;
                    var completed = await Task.WhenAny(readTask, Task.Delay(2000));
                    if (completed == readTask)
                    {
                        var tup = readTask.Result;
                        ip = tup.Item1;
                        user = tup.Item2;
                        pwd = tup.Item3;
                    }
                    else
                    {
                        AppendLog("Timeout reading UI fields; falling back to config values");
                        ip = _config?.IPEndpoint ?? string.Empty;
                        user = _config?.Username ?? string.Empty;
                        pwd = _config?.Password ?? string.Empty;
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"Error reading UI fields: {ex.Message}");
                    ip = _config?.IPEndpoint ?? string.Empty;
                    user = _config?.Username ?? string.Empty;
                    pwd = _config?.Password ?? string.Empty;
                }

                AppendLog("Auto-Login initialized");

                if (string.IsNullOrWhiteSpace(ip) || string.IsNullOrWhiteSpace(user))
                {
                    AppendLog("Skipped auto-login: missing IP or username");
                    return;
                }

                _loginService = new Login(_httpClient, new Uri($"http://{ip}"));
                try
                {
                    var result = await _loginService.PasswordLoginAsync(user, pwd, true);
                    if (result != null && result.Success)
                    {
                        AppendLog("Auto-login success");
                        // save config on successful login
                        SaveConfig();
                    }
                    else
                    {
                        AppendLog("Auto-login failed");
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"Auto-login error: {ex.Message}");
                }
            }
            finally
            {
                Interlocked.Exchange(ref _autoLoginRunning, 0);

                // calculate next run time if timer running
                try
                {
                    if (_autoLoginTimer != null && _autoLoginTimer.Enabled)
                    {
                        _nextAutoLoginTime = _lastAutoLoginTime?.AddMilliseconds(_autoLoginTimer.Interval);
                    }
                    else
                    {
                        _nextAutoLoginTime = null;
                    }
                }
                catch { _nextAutoLoginTime = null; }

                UpdateTimeInfo();
            }
        }

        private void AppendLog(string text)
        {
            try
            {
                var dir = System.IO.Path.GetDirectoryName(_logPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {text}{Environment.NewLine}";
                System.IO.File.AppendAllText(_logPath, line, Encoding.UTF8);
            }
            catch { }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveConfig();
        }

        private void ViewLogButton_Click(object sender, RoutedEventArgs e)
        {
            // open a simple dialog window to show logs
            var dlg = new LogDialog(_logPath);
            dlg.ShowAsync();
        }

        private void AutoLoginToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (_suppressToggleEvent) return;
            SaveConfig();
            AppendLog("Toggle changed");
            if (AutoLoginToggle.IsOn)
            {
                StartAutoLoginTimer(true);
            }
            else
            {
                StopAutoLoginTimer();
            }
        }

        // Update the TimeInfo TextBlock on UI thread
        private void UpdateTimeInfo()
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    var last = _lastAutoLoginTime.HasValue ? _lastAutoLoginTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "未执行";
                    var next = _nextAutoLoginTime.HasValue ? _nextAutoLoginTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "未计划";
                    TimeInfo.Text = $"上次执行: {last}\n下次执行: {next}";
                });
            }
            catch { }
        }

        private async void HotSpotToggle_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                HotSpotToggle.IsEnabled = false;
                AppendLog("尝试打开热点...");
                var ok = await HotspotUtils.TurnOnHotspotAsync();
                AppendLog(ok ? "热点已打开" : "打开热点失败");
                HotSpotToggle.Content = ok ? "关闭热点" : "打开热点";
            }
            catch (Exception ex)
            {
                AppendLog($"打开热点异常: {ex.Message}");
                HotSpotToggle.Content = "打开失败";
            }
            finally
            {
                HotSpotToggle.IsEnabled = true;
            }
        }
        
        private async void HotSpotToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                HotSpotToggle.IsEnabled = false;
                AppendLog("尝试关闭热点...");
                var ok = await HotspotUtils.TurnOffHotspotAsync();
                AppendLog(ok ? "热点已关闭" : "关闭热点失败");
                HotSpotToggle.Content = ok ? "打开热点" : "关闭失败";
            }
            catch (Exception ex)
            {
                AppendLog($"关闭热点异常: {ex.Message}");
                HotSpotToggle.Content = "关闭失败";
            }
            finally
            {
                HotSpotToggle.IsEnabled = true;
            }
        }

        private void AutoHotSpotToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            bool isOn = AutoHotSpotToggleSwitch.IsOn;
            if (_config == null) _config = new Utils.ConfigManager.AppConfig();
            _config.AutoHotspot = isOn;
            Utils.ConfigManager.Save(_config);
        }
        private void UpdateRequestFrequencySliderLabel()
        {
            double durationSecond = RequestFrequencySlider.Value;
            // 转换为中文时分秒格式
            TimeSpan timeSpan = TimeSpan.FromSeconds(durationSecond);
            string timeStr = "";
            if (timeSpan.Hours > 0)
            {
                timeStr += $"{timeSpan.Hours} 小时 ";
            }
            if (timeSpan.Minutes > 0)
            {
                timeStr += $"{timeSpan.Minutes} 分钟 ";
            }
            if (timeSpan.Seconds > 0 || timeStr == "")
            {
                timeStr += $"{timeSpan.Seconds} 秒 ";
            }
            try
            {
                RequestFrequencyLabel.Text = $"{timeStr} (={(int)durationSecond}s)";
            }
            catch { }
        }

        private void RequestFrequencySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_suppressFrequencySliderEvent) return;
            UpdateRequestFrequencySliderLabel();
        }
    }
}
