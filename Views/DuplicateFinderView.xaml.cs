using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace FileCounterPro_Windows.Views
{
    public class DuplicateGroup
    {
        public string       FileName     { get; set; } = "";
        public long         SizeBytes    { get; set; }
        public int          Count        { get; set; }
        public List<string> Paths        { get; set; } = new();
        public string       SizeText     => $"{SizeBytes / 1024.0:0.0} KB";
        public string       WastedText   => $"{(SizeBytes * (Count - 1)) / 1024.0 / 1024.0:0.00} MB";
        public string       PathsSummary => string.Join(" | ", Paths.Take(2)) + (Paths.Count > 2 ? $" +{Paths.Count - 2} more" : "");
    }

    public partial class DuplicateFinderView : UserControl
    {
        private ObservableCollection<DuplicateGroup> _groups = new();
        private List<DuplicateGroup> _allGroups = new();

        public DuplicateFinderView()
        {
            InitializeComponent();
            LstDupes.ItemsSource = _groups;
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title            = "Select any file in the folder you want to scan",
                CheckFileExists  = false,
                FileName         = "Select Folder",
            };
            if (dlg.ShowDialog() == true)
                TxtFolder.Text = Path.GetDirectoryName(dlg.FileName) ?? "";
        }

        private async void BtnScan_Click(object sender, RoutedEventArgs e)
        {
            var folder = TxtFolder.Text.Trim();
            if (!Directory.Exists(folder)) { MessageBox.Show("Please select a valid folder."); return; }

            BtnScan.IsEnabled       = false;
            ScanProgress.Visibility = Visibility.Visible;
            StatsBar.Visibility     = Visibility.Collapsed;
            StatusPanel.Visibility  = Visibility.Collapsed;
            _groups.Clear();
            _allGroups.Clear();

            var progress = new Progress<int>(pct => ScanProgress.Value = pct);

            _allGroups = await Task.Run(() => FindDuplicates(folder, progress));

            foreach (var g in _allGroups)
                _groups.Add(g);

            long wasted = _allGroups.Sum(g => g.SizeBytes * (g.Count - 1));
            TxtStats.Text = $"Found {_allGroups.Count} duplicate groups · {_allGroups.Sum(g => g.Count)} files · Wasted: {wasted / 1024.0 / 1024.0:0.00} MB";

            BtnScan.IsEnabled       = true;
            ScanProgress.Visibility = Visibility.Collapsed;
            StatsBar.Visibility     = _allGroups.Any() ? Visibility.Visible : Visibility.Collapsed;
        }

        private List<DuplicateGroup> FindDuplicates(string folder, IProgress<int> progress)
        {
            var all = Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).ToList();
            var hashMap = new Dictionary<string, List<FileInfo>>();
            int done = 0;

            foreach (var f in all)
            {
                try
                {
                    var info = new FileInfo(f);
                    var hash = ComputeMd5(f);
                    if (!hashMap.ContainsKey(hash)) hashMap[hash] = new List<FileInfo>();
                    hashMap[hash].Add(info);
                }
                catch { }
                done++;
                progress?.Report((int)(done * 100.0 / all.Count));
            }

            return hashMap
                .Where(kv => kv.Value.Count > 1)
                .Select(kv => new DuplicateGroup
                {
                    FileName  = kv.Value[0].Name,
                    SizeBytes = kv.Value[0].Length,
                    Count     = kv.Value.Count,
                    Paths     = kv.Value.Select(f => f.FullName).ToList()
                })
                .OrderByDescending(g => g.SizeBytes * g.Count)
                .ToList();
        }

        private string ComputeMd5(string path)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(path);
            return BitConverter.ToString(md5.ComputeHash(stream));
        }

        private async void BtnKeepNewest_Click(object sender, RoutedEventArgs e)
            => await DeleteDuplicates(keepNewest: true);

        private async void BtnKeepOldest_Click(object sender, RoutedEventArgs e)
            => await DeleteDuplicates(keepNewest: false);

        private async Task DeleteDuplicates(bool keepNewest)
        {
            if (!_allGroups.Any()) return;
            var confirm = MessageBox.Show($"This will delete all duplicate files keeping only the {(keepNewest ? "newest" : "oldest")} copy.\nContinue?",
                "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            int deleted = 0;
            long freed  = 0;

            await Task.Run(() =>
            {
                foreach (var g in _allGroups)
                {
                    var sorted = keepNewest
                        ? g.Paths.OrderByDescending(p => File.GetLastWriteTime(p)).ToList()
                        : g.Paths.OrderBy(p => File.GetLastWriteTime(p)).ToList();

                    foreach (var path in sorted.Skip(1))
                    {
                        try
                        {
                            freed += new FileInfo(path).Length;
                            File.Delete(path);
                            deleted++;
                        }
                        catch { }
                    }
                }
            });

            _groups.Clear();
            _allGroups.Clear();
            StatsBar.Visibility = Visibility.Collapsed;

            StatusPanel.Visibility = Visibility.Visible;
            TxtStatus.Text = $"✅ Deleted {deleted} files · Freed {freed / 1024.0 / 1024.0:0.00} MB";
        }
    }
}
