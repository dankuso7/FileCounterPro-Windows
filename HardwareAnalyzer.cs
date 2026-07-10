using System;
using System.IO;

namespace FileCounterPro_Windows
{
    public static class HardwareAnalyzer
    {
        public static string FindHogwartsLegacy()
        {
            try
            {
                // Check common installation paths
                string[] commonPaths = {
                    @"C:\Program Files (x86)\Steam\steamapps\common\Hogwarts Legacy",
                    @"D:\SteamLibrary\steamapps\common\Hogwarts Legacy",
                    @"E:\SteamLibrary\steamapps\common\Hogwarts Legacy",
                    @"C:\Program Files\Epic Games\HogwartsLegacy",
                    @"D:\Epic Games\HogwartsLegacy",
                    @"E:\Epic Games\HogwartsLegacy",
                    @"F:\SteamLibrary\steamapps\common\Hogwarts Legacy",
                    @"F:\Epic Games\HogwartsLegacy",
                    @"G:\SteamLibrary\steamapps\common\Hogwarts Legacy",
                    @"G:\Epic Games\HogwartsLegacy"
                };

                // Check all logical drives for Steam/Epic libraries
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if (drive.IsReady)
                    {
                        string[] dynamicPaths = {
                            Path.Combine(drive.Name, @"SteamLibrary\steamapps\common\Hogwarts Legacy"),
                            Path.Combine(drive.Name, @"Steam\steamapps\common\Hogwarts Legacy"),
                            Path.Combine(drive.Name, @"Epic Games\HogwartsLegacy"),
                            Path.Combine(drive.Name, @"Program Files\Epic Games\HogwartsLegacy"),
                            Path.Combine(drive.Name, @"Program Files (x86)\Steam\steamapps\common\Hogwarts Legacy")
                        };

                        foreach(var path in dynamicPaths) {
                            if (Directory.Exists(path)) {
                                return path;
                            }
                        }
                    }
                }

                foreach (var path in commonPaths)
                {
                    if (Directory.Exists(path))
                    {
                        return path;
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
