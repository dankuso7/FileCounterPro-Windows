using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FileCounterPro_Windows.Views
{
    public class JunkCategory : INotifyPropertyChanged
    {
        private bool _isSelected = true;
        public string Category  { get; set; } = "";
        public string Path      { get; set; } = "";
        public long   SizeBytes { get; set; }
        public int    FileCount { get; set; }
        public string Status    { get; set; } = "Found";

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public string SizeText => SizeBytes > 1024 * 1024 * 1024
            ? $"{SizeBytes / 1024.0 / 1024.0 / 1024.0:0.00} GB"
            : $"{SizeBytes / 1024.0 / 1024.0:0.0} MB";

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public partial class SystemJunkView : UserControl
    {
        private ObservableCollection<JunkCategory> _junk = new();

        public SystemJunkView()
        {
            InitializeComponent();
            LstJunk.ItemsSource = _junk;
        }

        private async void BtnScan_Click(object sender, RoutedEventArgs e)
        {
            BtnScan.IsEnabled      = false;
            BtnClean.IsEnabled     = false;
            ScanProgress.Visibility = Visibility.Visible;
            StatusPanel.Visibility  = Visibility.Collapsed;
            _junk.Clear();
            TxtTotal.Text = "Scanning…";

            var categories = new[]
            {
                ("Temp Files",          Environment.GetEnvironmentVariable("TEMP") ?? ""),
                ("Windows Temp",        @"C:\Windows\Temp"),
                ("Chrome Cache",        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\User Data\Default\Cache")),
                ("Edge Cache",          Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Edge\User Data\Default\Cache")),
                ("Windows Update Cache",@"C:\Windows\SoftwareDistribution\Download"),
                ("Thumbnail Cache",     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Windows\Explorer")),
                ("Log Files",           @"C:\Windows\Logs"),
            };

            foreach (var (cat, path) in categories)
            {
                if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) continue;

                var (size, count) = await Task.Run(() => GetFolderStats(path));
                if (size > 0)
                    _junk.Add(new JunkCategory { Category = cat, Path = path, SizeBytes = size, FileCount = count });
            }

            long total = _junk.Sum(j => j.SizeBytes);
            TxtTotal.Text = $"FOUND: {total / 1024.0 / 1024.0 / 1024.0:0.00} GB of junk ({_junk.Count} categories)";

            BtnScan.IsEnabled       = true;
            BtnClean.IsEnabled      = _junk.Count > 0;
            ScanProgress.Visibility = Visibility.Collapsed;
        }

        private (long size, int count) GetFolderStats(string path)
        {
            long total = 0;
            int  count = 0;
            try
            {
                foreach (var f in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    try { total += new FileInfo(f).Length; count++; } catch { }
                }
            }
            catch { }
            return (total, count);
        }

        private async void BtnClean_Click(object sender, RoutedEventArgs e)
        {
            var selected = _junk.Where(j => j.IsSelected).ToList();
            if (!selected.Any()) return;

            var confirm = MessageBox.Show($"Delete {selected.Count} junk categories?\nThis cannot be undone.",
                "Confirm Clean", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            BtnScan.IsEnabled = BtnClean.IsEnabled = false;
            ScanProgress.Visibility = Visibility.Visible;

            long cleaned = 0;
            int  deletedFiles = 0;

            foreach (var cat in selected)
            {
                cat.Status = "Cleaning…";
                (long freed, int files) = await Task.Run(() => CleanFolder(cat.Path));
                cleaned      += freed;
                deletedFiles += files;
                cat.Status    = "✅ Cleaned";
                cat.SizeBytes = 0;
            }

            ScanProgress.Visibility = Visibility.Collapsed;
            BtnScan.IsEnabled = BtnClean.IsEnabled = true;

            StatusPanel.Visibility = Visibility.Visible;
            TxtStatus.Text = $"✅ Cleaned {deletedFiles} files · Freed {cleaned / 1024.0 / 1024.0:0.0} MB";

            TxtTotal.Text = $"REMAINING: {_junk.Where(j => !j.IsSelected).Sum(j => j.SizeBytes) / 1024.0 / 1024.0:0.0} MB";
        }

        private (long freed, int count) CleanFolder(string path)
        {
            long freed = 0;
            int  count = 0;
            try
            {
                foreach (var f in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        var info = new FileInfo(f);
                        freed += info.Length;
                        File.Delete(f);
                        count++;
                    }
                    catch { }
                }
            }
            catch { }
            return (freed, count);
        }
    }
}
