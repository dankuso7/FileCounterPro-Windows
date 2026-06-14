using System;
using System.Windows;
using System.Threading.Tasks;

namespace FileCounterPro_Windows
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void NavDashboard_Click(object sender, RoutedEventArgs e)
        {
            ContentTitle.Text = "Dashboard";
            MainContentText.Text = "System Dashboard coming soon. This will show your PC's general health.";
        }

        private async void NavUninstaller_Click(object sender, RoutedEventArgs e)
        {
            ContentTitle.Text = "Smart Uninstaller";
            MainContentText.Text = "Scanning Windows Registry for installed software...\n\n";

            // Run on background thread
            var apps = await Task.Run(() => SmartUninstaller.GetInstalledApps());
            
            foreach (var app in apps)
            {
                MainContentText.Text += $"- {app}\n";
            }
        }

        private async void NavHardware_Click(object sender, RoutedEventArgs e)
        {
            ContentTitle.Text = "Hardware Analyzer";
            MainContentText.Text = "Polling WMI for hardware specs...\n\n";

            // Run on background thread
            var hwInfo = await Task.Run(() => HardwareAnalyzer.GetSystemInfo());
            MainContentText.Text += hwInfo;
        }
    }
}
