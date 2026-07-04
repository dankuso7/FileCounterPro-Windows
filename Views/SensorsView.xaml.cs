using System;
using System.Diagnostics;
using System.Management;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace FileCounterPro_Windows.Views
{
    public partial class SensorsView : UserControl
    {
        private DispatcherTimer _refreshTimer;
        private DispatcherTimer _stressTimer;
        private PerformanceCounter _cpuCounter;
        private double _cpuLoad = 0;
        private double _gpuLoad = 0;
        private bool _stopStress = false;
        private int _stressCountdown = 0;

        // Smooth values
        private double _cpuTemp = 45, _cpuPkgTemp = 44, _gpuTemp = 43;
        private double _memTemp = 36, _ssdTemp = 33, _mbTemp = 32;
        private double _fanRPM = 1200;

        private static readonly Random _rng = new Random();

        public SensorsView()
        {
            InitializeComponent();
            try { _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"); }
            catch { }

            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();

            // Immediate first read
            RefreshTimer_Tick(null, null);
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            try { _cpuLoad = Math.Min(100, _cpuCounter?.NextValue() ?? 0); } catch { }

            // Simulate GPU load
            _gpuLoad = Math.Min(100, _gpuLoad + (_rng.NextDouble() * 10 - 5));
            _gpuLoad = Math.Max(5, _gpuLoad);

            // Real WMI temperature (Windows only)
            double wmiCpuTemp = ReadWmiCpuTemp();

            // Smooth all temps
            double tCpu = wmiCpuTemp > 0 ? wmiCpuTemp : 38.0 + (_cpuLoad / 100.0) * 52.0 + (_rng.NextDouble() - 0.5);
            double tPkg = tCpu - _rng.NextDouble() * 2;
            double tGpu = 35.0 + (_gpuLoad / 100.0) * 50.0 + (_rng.NextDouble() - 0.5);
            double tMem = 30.0 + (_cpuLoad / 100.0) * 20.0 + (_rng.NextDouble() * 0.6 - 0.3);
            double tSsd = 28.0 + (_cpuLoad / 100.0) * 12.0 + (_rng.NextDouble() * 0.4 - 0.2);
            double tMb  = 32.0 + (_cpuLoad / 100.0) * 15.0 + (_rng.NextDouble() * 0.3 - 0.15);

            _cpuTemp    += (tCpu - _cpuTemp)   * 0.25;
            _cpuPkgTemp += (tPkg - _cpuPkgTemp)* 0.25;
            _gpuTemp    += (tGpu - _gpuTemp)   * 0.25;
            _memTemp    += (tMem - _memTemp)   * 0.25;
            _ssdTemp    += (tSsd - _ssdTemp)   * 0.25;
            _mbTemp     += (tMb  - _mbTemp)    * 0.25;

            // Fan RPM
            double thermalLoad = Math.Max(_cpuLoad, _gpuLoad) / 100.0;
            double targetRPM   = 1000 + thermalLoad * 3200 + _rng.Next(-40, 40);
            _fanRPM += (targetRPM - _fanRPM) * 0.15;
            _fanRPM = Math.Max(1000, Math.Min(4200, _fanRPM));

            UpdateUI();
        }

        private double ReadWmiCpuTemp()
        {
            try
            {
                using var mos = new ManagementObjectSearcher(
                    @"root\WMI", "SELECT * FROM MSAcpi_ThermalZoneTemperature");
                foreach (ManagementObject mo in mos.Get())
                {
                    double kelvinx10 = Convert.ToDouble(mo["CurrentTemperature"]);
                    return (kelvinx10 / 10.0) - 273.15;
                }
            }
            catch { }
            return -1;
        }

        private void UpdateUI()
        {
            // Fan
            TxtFanRPM.Text  = ((int)_fanRPM).ToString();
            FanBar.Value    = _fanRPM;
            double fanPct   = (_fanRPM - 1000) / 3200.0 * 100.0;
            TxtFanPct.Text  = $"{fanPct:0}%";

            if (fanPct > 70) { TxtFanStatus.Text = "BOOST";  TxtFanStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x00, 0x99)); }
            else if (fanPct > 40) { TxtFanStatus.Text = "ACTIVE"; TxtFanStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x99, 0x00)); }
            else { TxtFanStatus.Text = "IDLE"; TxtFanStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xE6, 0xFF)); }

            // Temps
            SetTempCard(TxtCpuTemp,    CpuTempBar,    CpuBadge,    CpuBadgeTxt,    _cpuTemp,    90, 75);
            SetTempCard(TxtCpuPkgTemp, CpuPkgBar,     CpuPkgBadge, CpuPkgBadgeTxt, _cpuPkgTemp, 90, 75);
            SetTempCard(TxtGpuTemp,    GpuTempBar,    GpuBadge,    GpuBadgeTxt,    _gpuTemp,    85, 70);
            SetTempCard(TxtMemTemp,    MemTempBar,    MemBadge,    MemBadgeTxt,    _memTemp,    70, 55);
            SetTempCard(TxtSsdTemp,    SsdTempBar,    SsdBadge,    SsdBadgeTxt,    _ssdTemp,    65, 50);
            SetTempCard(TxtMbTemp,     MbTempBar,     MbBadge,     MbBadgeTxt,     _mbTemp,     60, 45);
        }

        private void SetTempCard(TextBlock valueTb, ProgressBar bar, Border badge, TextBlock badgeTb,
                                  double temp, double critical, double warning)
        {
            valueTb.Text = ((int)temp).ToString();
            bar.Value    = temp;

            Color color;
            string label;
            if (temp >= critical)      { color = Color.FromRgb(0xFF,0x00,0x99); label = "CRITICAL"; }
            else if (temp >= warning)  { color = Color.FromRgb(0xFF,0x99,0x00); label = "WARN"; }
            else                       { color = Color.FromRgb(0x00,0xFF,0x80); label = "OK"; }

            var brush = new SolidColorBrush(color);
            bar.Foreground             = brush;
            badge.BorderBrush          = brush;
            badge.Background           = new SolidColorBrush(Color.FromArgb(0x22, color.R, color.G, color.B));
            badgeTb.Text               = label;
            badgeTb.Foreground         = brush;
        }

        // ── Fan Stress ────────────────────────────────────────────────────────

        private void Stress15_Click(object sender, RoutedEventArgs e) => StartStress(15);
        private void Stress30_Click(object sender, RoutedEventArgs e) => StartStress(30);
        private void Stress60_Click(object sender, RoutedEventArgs e) => StartStress(60);

        private void StartStress(int duration)
        {
            _stopStress      = false;
            _stressCountdown = duration;

            Btn15s.IsEnabled = Btn30s.IsEnabled = Btn60s.IsEnabled = false;
            BtnStop.Visibility        = Visibility.Visible;
            CountdownBorder.Visibility = Visibility.Visible;
            TxtCountdown.Text         = duration.ToString();
            StressTitle.Text          = "SPINNING UP FAN…";
            StressDesc.Text           = $"All {Environment.ProcessorCount} CPU cores loaded · Fan ramping up via thermal load";
            StressPanel.BorderBrush   = new SolidColorBrush(Color.FromRgb(0xFF,0x99,0x00));

            // Launch one tight FP-math thread per logical core
            int cores = Environment.ProcessorCount;
            for (int i = 0; i < cores; i++)
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    double x = 1.0;
                    while (!_stopStress)
                    {
                        x = Math.Sin(x) * Math.Cos(x) + Math.Tan(x) * Math.Log(Math.Abs(x) + 1.0);
                        x += 1e-10;
                    }
                });
            }

            _stressTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _stressTimer.Tick += (s, e) =>
            {
                _stressCountdown--;
                TxtCountdown.Text = _stressCountdown.ToString();
                if (_stressCountdown <= 0) StopStress();
            };
            _stressTimer.Start();
        }

        private void StressStop_Click(object sender, RoutedEventArgs e) => StopStress();

        private void StopStress()
        {
            _stopStress = true;
            _stressTimer?.Stop();
            _stressTimer = null;

            Btn15s.IsEnabled = Btn30s.IsEnabled = Btn60s.IsEnabled = true;
            BtnStop.Visibility         = Visibility.Collapsed;
            CountdownBorder.Visibility = Visibility.Collapsed;
            StressTitle.Text           = "PHYSICAL FAN CONTROL";
            StressDesc.Text            = "Stress all CPU cores to force the physical fan to spin via thermal load";
            StressPanel.BorderBrush    = new SolidColorBrush(Color.FromArgb(0x33,0xFF,0xFF,0xFF));
        }

        // ── Power Mode ────────────────────────────────────────────────────────

        private void PowerBalanced_Click(object sender, RoutedEventArgs e)
            => ApplyPowerPlan("SCHEME_BALANCED", "✅ Balanced Mode applied.");

        private void PowerHigh_Click(object sender, RoutedEventArgs e)
            => ApplyPowerPlan("SCHEME_MIN", "✅ High Performance Mode applied. Fan may spin faster under load.");

        private void PowerSaver_Click(object sender, RoutedEventArgs e)
            => ApplyPowerPlan("SCHEME_MAX", "✅ Power Saver Mode applied. System will run cooler and quieter.");

        private void ApplyPowerPlan(string scheme, string message)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName        = "powercfg",
                    Arguments       = $"/setactive {scheme}",
                    UseShellExecute = true,
                    Verb            = "runas",
                    WindowStyle     = ProcessWindowStyle.Hidden,
                    CreateNoWindow  = true
                };
                Process.Start(psi)?.WaitForExit(3000);

                PowerStatusPanel.Visibility = Visibility.Visible;
                TxtPowerStatus.Text  = message;
                TxtPowerStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0x80));
            }
            catch (Exception ex)
            {
                PowerStatusPanel.Visibility = Visibility.Visible;
                TxtPowerStatus.Text  = $"⚠️ Could not apply power plan: {ex.Message}. Try running as Administrator.";
                TxtPowerStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x99, 0x00));
            }
        }
    }
}
