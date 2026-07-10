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
                // Common root paths to check on ALL available drives
                string[] libraryRoots = {
                    @"SteamLibrary\steamapps\common\Hogwarts Legacy",
                    @"Steam\steamapps\common\Hogwarts Legacy",
                    @"Program Files (x86)\Steam\steamapps\common\Hogwarts Legacy",
                    @"Program Files\Steam\steamapps\common\Hogwarts Legacy",
                    @"Epic Games\HogwartsLegacy",
                    @"Program Files\Epic Games\HogwartsLegacy",
                    @"Program Files (x86)\Epic Games\HogwartsLegacy",
                    @"Games\Hogwarts Legacy",
                    @"Hogwarts Legacy"
                };

                // 1. Iterate through all system drives (C:\, D:\, E:\, etc)
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if (drive.IsReady)
                    {
                        foreach(var libRoot in libraryRoots) 
                        {
                            string fullPath = Path.Combine(drive.Name, libRoot);
                            if (Directory.Exists(fullPath)) 
                            {
                                return fullPath;
                            }
                        }
                    }
                }

                // 2. Check explicitly common hardcoded paths just in case DriveInfo fails
                string[] hardcodedFallbackPaths = {
                    @"C:\Program Files (x86)\Steam\steamapps\common\Hogwarts Legacy",
                    @"C:\Program Files\Epic Games\HogwartsLegacy",
                    @"D:\SteamLibrary\steamapps\common\Hogwarts Legacy",
                    @"D:\Epic Games\HogwartsLegacy",
                    @"E:\SteamLibrary\steamapps\common\Hogwarts Legacy",
                    @"E:\Epic Games\HogwartsLegacy"
                };

                foreach (var path in hardcodedFallbackPaths)
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
