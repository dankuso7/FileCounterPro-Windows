using System;
using System.Text;
using System.Management;

namespace FileCounterPro_Windows
{
    public static class HardwareAnalyzer
    {
        public static string GetSystemInfo()
        {
            StringBuilder sb = new StringBuilder();

            try
            {
                sb.AppendLine("=== CPU INFO ===");
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_Processor"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        sb.AppendLine($"Name: {obj["Name"]}");
                        sb.AppendLine($"Cores: {obj["NumberOfCores"]}");
                        sb.AppendLine($"Logical Processors: {obj["NumberOfLogicalProcessors"]}");
                    }
                }

                sb.AppendLine("\n=== GPU INFO ===");
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_VideoController"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        sb.AppendLine($"Name: {obj["Name"]}");
                        sb.AppendLine($"Driver Version: {obj["DriverVersion"]}");
                        
                        var adapterRam = obj["AdapterRAM"];
                        if (adapterRam != null)
                        {
                            long bytes = Convert.ToInt64(adapterRam);
                            sb.AppendLine($"VRAM: {bytes / 1024 / 1024} MB");
                        }
                    }
                }

                sb.AppendLine("\n=== RAM INFO ===");
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_PhysicalMemory"))
                {
                    long totalCapacity = 0;
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        totalCapacity += Convert.ToInt64(obj["Capacity"]);
                        sb.AppendLine($"Speed: {obj["Speed"]} MHz");
                    }
                    sb.AppendLine($"Total RAM: {totalCapacity / 1024 / 1024 / 1024} GB");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Error querying WMI: {ex.Message}");
            }

            return sb.ToString();
        }
    }
}
