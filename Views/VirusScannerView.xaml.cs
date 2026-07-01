using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FileCounterPro_Windows.Views
{
    public class ThreatResult
    {
        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string Reason { get; set; } = "";
        public string AIExplanation { get; set; } = "";
    }

    public partial class VirusScannerView : UserControl
    {
        private ObservableCollection<ThreatResult> _threats = new ObservableCollection<ThreatResult>();

        public VirusScannerView()
        {
            InitializeComponent();
            LstThreats.ItemsSource = _threats;
        }

        private async void BtnScan_Click(object sender, RoutedEventArgs e)
        {
            BtnFullScan.Content = "SCANNING SYSTEM...";
            BtnFullScan.IsEnabled = false;
            PnlReady.Visibility = Visibility.Collapsed;
            LstThreats.Visibility = Visibility.Visible;
            _threats.Clear();

            try
            {
                await Task.Run(() => ScanCriticalPaths());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            BtnFullScan.Content = "🚨 INITIATE FULL SYSTEM SCAN 🚨";
            BtnFullScan.IsEnabled = true;

            if (_threats.Count == 0)
            {
                LstThreats.Visibility = Visibility.Collapsed;
                PnlReady.Visibility = Visibility.Visible;
            }
        }

        private void ScanCriticalPaths()
        {
            string[] pathsToScan = {
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                Environment.GetEnvironmentVariable("TEMP"),
                Environment.GetFolderPath(Environment.SpecialFolder.System)
            };

            string[] badSignatures = { "minerd", "xmrig", "nc.exe", "trojan", "keylogger" };

            foreach (var path in pathsToScan)
            {
                if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) continue;

                try
                {
                    var files = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly);
                    foreach (var file in files)
                    {
                        string lowerName = Path.GetFileName(file).ToLower();
                        foreach (var sig in badSignatures)
                        {
                            if (lowerName.Contains(sig))
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    string aiExp = "⚠️ **DELETION IMPACT ANALYSIS:**\n";
                                    string lowerPath = file.ToLower();
                                    if (lowerPath.Contains("windows\\system32") || lowerPath.Contains("windows\\syswow64")) {
                                        aiExp += "CRITICAL WARNING: This is a core Windows system folder. Deleting this may result in a Blue Screen of Death (BSOD) or prevent Windows from booting. Do not delete unless absolutely certain.";
                                    } else if (lowerPath.Contains("steamapps\\common") || lowerPath.Contains("epic games") || lowerPath.Contains("program files")) {
                                        aiExp += "This appears to belong to a Game or Application installation. Deleting it will likely break the game, requiring you to verify game files or reinstall. False positives occur often with game anti-cheat or DRM systems.";
                                    } else if (lowerPath.Contains("temp") || lowerPath.Contains("appdata\\local\\temp")) {
                                        aiExp += "This file is in a temporary directory. It is generally safe to delete and will not break core system functionality.";
                                    } else if (lowerPath.Contains("appdata\\roaming") || lowerPath.Contains("appdata\\local")) {
                                        aiExp += "This file is in your AppData directory. Deleting it might reset settings or break a specific application installed for your user account.";
                                    } else {
                                        aiExp += "Deleting this file will permanently remove it from your system. If it belongs to an app you use, that app may stop working.";
                                    }

                                    _threats.Add(new ThreatResult
                                    {
                                        FileName = Path.GetFileName(file),
                                        FilePath = file,
                                        Reason = $"Matched known malicious signature: {sig}",
                                        AIExplanation = aiExp
                                    });
                                });
                            }
                        }
                    }
                }
                catch { /* Ignore access denied */ }
            }
        }

        private void BtnDeleteThreat_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ThreatResult threat)
            {
                try
                {
                    if (File.Exists(threat.FilePath))
                    {
                        File.Delete(threat.FilePath);
                        _threats.Remove(threat);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete file: {ex.Message}");
                }

                if (_threats.Count == 0)
                {
                    LstThreats.Visibility = Visibility.Collapsed;
                    PnlReady.Visibility = Visibility.Visible;
                }
            }
        }
    }
}
