using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;

namespace FileCounterPro_Windows.Views
{
    public class AppEntry
    {
        public string DisplayName    { get; set; } = "";
        public string Publisher      { get; set; } = "";
        public string Version        { get; set; } = "";
        public string SizeMB         { get; set; } = "";
        public string InstallDate    { get; set; } = "";
        public string UninstallString { get; set; } = "";
    }

    public partial class SmartUninstallerView : UserControl
    {
        private ObservableCollection<AppEntry> _apps = new();
        private ICollectionView _view;

        public SmartUninstallerView()
        {
            InitializeComponent();
            _view = CollectionViewSource.GetDefaultView(_apps);
            _view.Filter = FilterApp;
            LstApps.ItemsSource = _view;
        }

        private bool FilterApp(object obj)
        {
            if (obj is AppEntry app)
                return string.IsNullOrEmpty(TxtSearch.Text) ||
                       app.DisplayName.Contains(TxtSearch.Text, StringComparison.OrdinalIgnoreCase) ||
                       app.Publisher.Contains(TxtSearch.Text, StringComparison.OrdinalIgnoreCase);
            return true;
        }

        private async void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            BtnLoad.IsEnabled      = false;
            Progress.Visibility    = Visibility.Visible;
            StatusPanel.Visibility = Visibility.Collapsed;
            _apps.Clear();

            var entries = await Task.Run(() => ReadInstalledApps());
            foreach (var app in entries.OrderBy(a => a.DisplayName))
                _apps.Add(app);

            Progress.Visibility = Visibility.Collapsed;
            BtnLoad.IsEnabled   = true;
            TxtSelected.Text    = $"Loaded {_apps.Count} installed applications. Select apps to uninstall.";
        }

        private List<AppEntry> ReadInstalledApps()
        {
            var results = new List<AppEntry>();
            var keys = new[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            foreach (var key in keys)
            {
                try
                {
                    using var root = Registry.LocalMachine.OpenSubKey(key);
                    if (root == null) continue;
                    foreach (var sub in root.GetSubKeyNames())
                    {
                        try
                        {
                            using var sk = root.OpenSubKey(sub);
                            if (sk == null) continue;
                            var name = sk.GetValue("DisplayName") as string;
                            if (string.IsNullOrWhiteSpace(name)) continue;
                            var sizeKb = sk.GetValue("EstimatedSize") as int? ?? 0;
                            results.Add(new AppEntry
                            {
                                DisplayName     = name,
                                Publisher       = sk.GetValue("Publisher") as string ?? "",
                                Version         = sk.GetValue("DisplayVersion") as string ?? "",
                                SizeMB          = sizeKb > 0 ? $"{sizeKb / 1024.0:0.0}" : "–",
                                InstallDate     = sk.GetValue("InstallDate") as string ?? "",
                                UninstallString = sk.GetValue("UninstallString") as string ?? ""
                            });
                        }
                        catch { }
                    }
                }
                catch { }
            }

            // Also check HKCU
            try
            {
                using var root = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                if (root != null)
                {
                    foreach (var sub in root.GetSubKeyNames())
                    {
                        try
                        {
                            using var sk = root.OpenSubKey(sub);
                            if (sk == null) continue;
                            var name = sk.GetValue("DisplayName") as string;
                            if (string.IsNullOrWhiteSpace(name)) continue;
                            results.Add(new AppEntry
                            {
                                DisplayName     = name,
                                Publisher       = sk.GetValue("Publisher") as string ?? "",
                                Version         = sk.GetValue("DisplayVersion") as string ?? "",
                                UninstallString = sk.GetValue("UninstallString") as string ?? ""
                            });
                        }
                        catch { }
                    }
                }
            }
            catch { }

            return results.DistinctBy(a => a.DisplayName).ToList();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
            => _view?.Refresh();

        private void LstApps_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int count = LstApps.SelectedItems.Count;
            BtnUninstall.IsEnabled = count > 0;
            TxtSelected.Text = count > 0
                ? $"{count} app{(count > 1 ? "s" : "")} selected for uninstall"
                : "Select apps to uninstall (use Ctrl+Click for multiple)";
        }

        private void BtnUninstall_Click(object sender, RoutedEventArgs e)
        {
            var selected = LstApps.SelectedItems.Cast<AppEntry>().ToList();
            if (!selected.Any()) return;

            var confirm = MessageBox.Show(
                $"Uninstall {selected.Count} app(s)?\n\n{string.Join("\n", selected.Take(5).Select(a => "• " + a.DisplayName))}" +
                (selected.Count > 5 ? $"\n…and {selected.Count - 5} more" : ""),
                "Confirm Uninstall", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            foreach (var app in selected)
            {
                try
                {
                    if (string.IsNullOrEmpty(app.UninstallString)) continue;
                    var cmd = app.UninstallString.Trim();
                    if (cmd.StartsWith("MsiExec", StringComparison.OrdinalIgnoreCase))
                        Process.Start(new ProcessStartInfo("msiexec", cmd.Replace("MsiExec.exe", "").Trim())
                            { UseShellExecute = true });
                    else
                        Process.Start(new ProcessStartInfo("cmd", $"/c \"{cmd}\"")
                            { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to uninstall {app.DisplayName}: {ex.Message}");
                }
            }

            StatusPanel.Visibility = Visibility.Visible;
            TxtStatus.Text = $"✅ Uninstall launched for {selected.Count} app(s). Follow any prompts that appear.";
        }

        private async void BtnFindLeftovers_Click(object sender, RoutedEventArgs e)
        {
            var selected = LstApps.SelectedItems.Cast<AppEntry>().ToList();
            if (!selected.Any())
            {
                MessageBox.Show("Select one or more apps from the list first, then click Find Leftovers.");
                return;
            }

            Progress.Visibility    = Visibility.Visible;
            StatusPanel.Visibility = Visibility.Collapsed;

            var leftovers = await Task.Run(() =>
            {
                var found = new List<string>();
                var searchDirs = new[]
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                };

                foreach (var app in selected)
                {
                    var keywords = new[] { app.DisplayName, app.Publisher }
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s.Split(new[] { ' ', '.', '-' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "")
                        .Where(s => s.Length > 3)
                        .Distinct()
                        .ToList();

                    foreach (var dir in searchDirs)
                    {
                        if (!Directory.Exists(dir)) continue;
                        try
                        {
                            foreach (var sub in Directory.GetDirectories(dir))
                            {
                                var dirName = Path.GetFileName(sub).ToLower();
                                if (keywords.Any(k => dirName.Contains(k.ToLower())))
                                    found.Add($"[FOLDER] {sub}");
                            }
                        }
                        catch { }
                    }
                }
                return found;
            });

            Progress.Visibility    = Visibility.Collapsed;
            StatusPanel.Visibility = Visibility.Visible;

            if (leftovers.Any())
                TxtStatus.Text = $"⚠️ Found {leftovers.Count} leftover location(s):\n" +
                                  string.Join("\n", leftovers.Take(10)) +
                                  (leftovers.Count > 10 ? $"\n…and {leftovers.Count - 10} more" : "");
            else
                TxtStatus.Text = "✅ No leftover folders found for selected apps.";
        }
    }
}
