using System;
using System.Management;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace FileCounterPro_Windows.Views
{
    public partial class HardwareAnalyzerView : UserControl
    {
        public HardwareAnalyzerView()
        {
            InitializeComponent();
            _ = LoadHardwareDataAsync();
        }

        private async Task LoadHardwareDataAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    // CPU Info
                    using (var searcher = new ManagementObjectSearcher("select Name, NumberOfCores from Win32_Processor"))
                    {
                        foreach (var item in searcher.Get())
                        {
                            Dispatcher.Invoke(() =>
                            {
                                TxtCPUName.Text = item["Name"]?.ToString() ?? "Unknown CPU";
                                TxtCPUCores.Text = $"{item["NumberOfCores"]} Cores";
                            });
                        }
                    }

                    // RAM Info
                    using (var searcher = new ManagementObjectSearcher("select TotalPhysicalMemory from Win32_ComputerSystem"))
                    {
                        foreach (var item in searcher.Get())
                        {
                            if (item["TotalPhysicalMemory"] != null)
                            {
                                ulong ramBytes = (ulong)item["TotalPhysicalMemory"];
                                double ramGB = ramBytes / (1024.0 * 1024.0 * 1024.0);
                                Dispatcher.Invoke(() =>
                                {
                                    TxtRAM.Text = $"{Math.Round(ramGB, 1)} GB";
                                });
                            }
                        }
                    }

                    // GPU Info
                    using (var searcher = new ManagementObjectSearcher("select Name, DriverVersion from Win32_VideoController"))
                    {
                        foreach (var item in searcher.Get())
                        {
                            Dispatcher.Invoke(() =>
                            {
                                TxtGPU.Text = item["Name"]?.ToString() ?? "Unknown GPU";
                                TxtGPUDriver.Text = $"Driver: {item["DriverVersion"]}";
                            });
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    TxtCPUName.Text = "Error loading data";
                    TxtCPUCores.Text = ex.Message;
                });
            }
        }
    }
}
