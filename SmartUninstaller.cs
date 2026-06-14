using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace FileCounterPro_Windows
{
    public static class SmartUninstaller
    {
        public static List<string> GetInstalledApps()
        {
            var apps = new List<string>();

            string registry_key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            
            // Check 64-bit Registry
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registry_key))
            {
                if (key != null)
                {
                    foreach (string subkey_name in key.GetSubKeyNames())
                    {
                        using (RegistryKey subkey = key.OpenSubKey(subkey_name))
                        {
                            if (subkey?.GetValue("DisplayName") != null)
                            {
                                apps.Add((string)subkey.GetValue("DisplayName"));
                            }
                        }
                    }
                }
            }

            // Check 32-bit Registry
            string registry_key_32 = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registry_key_32))
            {
                if (key != null)
                {
                    foreach (string subkey_name in key.GetSubKeyNames())
                    {
                        using (RegistryKey subkey = key.OpenSubKey(subkey_name))
                        {
                            if (subkey?.GetValue("DisplayName") != null)
                            {
                                string name = (string)subkey.GetValue("DisplayName");
                                if (!apps.Contains(name))
                                    apps.Add(name);
                            }
                        }
                    }
                }
            }
            
            // Check Current User
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(registry_key))
            {
                if (key != null)
                {
                    foreach (string subkey_name in key.GetSubKeyNames())
                    {
                        using (RegistryKey subkey = key.OpenSubKey(subkey_name))
                        {
                            if (subkey?.GetValue("DisplayName") != null)
                            {
                                string name = (string)subkey.GetValue("DisplayName");
                                if (!apps.Contains(name))
                                    apps.Add(name);
                            }
                        }
                    }
                }
            }

            apps.Sort();
            return apps;
        }
    }
}
