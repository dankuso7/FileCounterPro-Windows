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
                                    _threats.Add(new ThreatResult
                                    {
                                        FileName = Path.GetFileName(file),
                                        FilePath = file,
                                        Reason = $"Matched known malicious signature: {sig}"
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
