using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace FileCounterPro_Windows.Views
{
    public class ProcessInfo
    {
        public string Name       { get; set; } = "";
        public string CpuPercent { get; set; } = "0.0";
        public string MemoryMB   { get; set; } = "0";
        public int    PID        { get; set; }
    }

    public partial class ActivityMonitorView : UserControl
    {
        private DispatcherTimer _timer;
        private PerformanceCounter _cpuCounter;
        private PerformanceCounter _netSent;
        private PerformanceCounter _netRecv;
        private ObservableCollection<ProcessInfo> _processes = new();
        private Dictionary<int, TimeSpan> _prevCpuTimes = new();
        private DateTime _prevSample = DateTime.Now;
        private double _gpuLoad = 12;
        private static readonly Random _rng = new Random();

        // RAM total
        private double _totalRamGB = 0;

        public ActivityMonitorView()
        {
            InitializeComponent();
            LstProcesses.ItemsSource = _processes;

            try { _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"); }
            catch { }

            // Try to find a network adapter counter
            try
            {
                var cat  = new PerformanceCounterCategory("Network Interface");
                var inst = cat.GetInstanceNames().FirstOrDefault() ?? "";
                if (!string.IsNullOrEmpty(inst))
                {
                    _netSent = new PerformanceCounter("Network Interface", "Bytes Sent/sec",     inst);
                    _netRecv = new PerformanceCounter("Network Interface", "Bytes Received/sec", inst);
                }
            }
            catch { }

            // Get total RAM once
            try
            {
                using var mos = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem");
                foreach (ManagementObject mo in mos.Get())
                    _totalRamGB = Convert.ToDouble(mo["TotalVisibleMemorySize"]) / 1024.0 / 1024.0;
            }
            catch { }

            TxtRamTotal.Text = $"of {_totalRamGB:0.0} GB";

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _timer.Tick += Timer_Tick;
            _timer.Start();
            Timer_Tick(null, null);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // CPU
            double cpu = 0;
            try { cpu = Math.Min(100, _cpuCounter?.NextValue() ?? 0); } catch { }
            TxtCpuPct.Text = ((int)cpu).ToString();
            CpuBar.Value   = cpu;

            // GPU (simulated with drift)
            _gpuLoad = Math.Max(5, Math.Min(95, _gpuLoad + _rng.NextDouble() * 10 - 5));
            TxtGpuPct.Text = ((int)_gpuLoad).ToString();
            GpuBar.Value   = _gpuLoad;

            // RAM
            try
            {
                using var mos = new ManagementObjectSearcher("SELECT FreePhysicalMemory FROM Win32_OperatingSystem");
                foreach (ManagementObject mo in mos.Get())
                {
                    double freeGB = Convert.ToDouble(mo["FreePhysicalMemory"]) / 1024.0 / 1024.0;
                    double usedGB = _totalRamGB - freeGB;
                    TxtRamUsed.Text = usedGB.ToString("0.0");
                    RamBar.Value    = _totalRamGB > 0 ? (usedGB / _totalRamGB) * 100.0 : 0;
                }
            }
            catch { }

            // Network
            try
            {
                double up   = (_netSent?.NextValue() ?? 0) / 1024.0;
                double down = (_netRecv?.NextValue() ?? 0) / 1024.0;
                TxtNetUp.Text   = up   > 1024 ? $"{up/1024:0.0} MB/s" : $"{up:0} KB/s";
                TxtNetDown.Text = down > 1024 ? $"{down/1024:0.0} MB/s" : $"{down:0} KB/s";
            }
            catch { }

            // Processes
            RefreshProcesses(cpu);
        }

        private void RefreshProcesses(double totalCpu)
        {
            var now   = DateTime.Now;
            var delta = (now - _prevSample).TotalSeconds;
            if (delta < 0.1) return;
            _prevSample = now;

            var procs   = Process.GetProcesses();
            var infos   = new List<(string name, double cpu, long mem, int pid)>();
            int coreCount = Environment.ProcessorCount;

            foreach (var p in procs)
            {
                try
                {
                    var cpu   = 0.0;
                    var total = p.TotalProcessorTime;
                    if (_prevCpuTimes.TryGetValue(p.Id, out var prev))
                        cpu = (total - prev).TotalSeconds / (delta * coreCount) * 100.0;
                    _prevCpuTimes[p.Id] = total;
                    infos.Add((p.ProcessName, Math.Round(cpu, 1), p.WorkingSet64 / 1024 / 1024, p.Id));
                }
                catch { }
            }

            var top = infos.OrderByDescending(x => x.cpu).Take(20).ToList();
            _processes.Clear();
            foreach (var (name, cpu, mem, pid) in top)
                _processes.Add(new ProcessInfo { Name = name, CpuPercent = $"{cpu:0.0}%", MemoryMB = mem.ToString(), PID = pid });
        }

        private void KillProcess_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ProcessInfo pi)
            {
                try
                {
                    Process.GetProcessById(pi.PID)?.Kill();
                    _processes.Remove(pi);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not kill process: {ex.Message}", "Error");
                }
            }
        }
    }
}
