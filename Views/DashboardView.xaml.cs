using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FileCounterPro_Windows.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
        }

        private async void BtnCount_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                TxtStatus.Text = "SCANNING...";
                TxtCount.Text = "0";
                string path = dialog.FolderName;

                try
                {
                    int count = await Task.Run(() => CountFilesRecursively(path));
                    TxtCount.Text = count.ToString();
                    TxtStatus.Text = "SCAN COMPLETE";
                }
                catch (Exception ex)
                {
                    TxtStatus.Text = "ERROR DETECTED";
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private async void BtnZip_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                string sourcePath = dialog.FolderName;
                string destPath = sourcePath + "_Archive.zip";

                TxtStatus.Text = "COMPRESSING...";
                TxtCount.Text = "ZIP";

                try
                {
                    await Task.Run(() => 
                    {
                        if (File.Exists(destPath)) File.Delete(destPath);
                        ZipFile.CreateFromDirectory(sourcePath, destPath, CompressionLevel.Optimal, false);
                    });
                    TxtStatus.Text = "ZIP CREATED SUCCESSFULLY";
                }
                catch (Exception ex)
                {
                    TxtStatus.Text = "ERROR CREATING ZIP";
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private int CountFilesRecursively(string path)
        {
            int count = 0;
            try
            {
                count += Directory.GetFiles(path).Length;
                foreach (string dir in Directory.GetDirectories(path))
                {
                    count += CountFilesRecursively(dir);
                }
            }
            catch (UnauthorizedAccessException) { /* Ignore inaccessible folders */ }
            return count;
        }
    }
}
