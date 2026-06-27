using System.Windows;
using System.Windows.Input;
using FileCounterPro_Windows.Views;

namespace FileCounterPro_Windows
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainContent.Content = new DashboardView();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void NavDashboard_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new DashboardView();
        }

        private void NavVirusScanner_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new VirusScannerView();
        }

        private void NavHardware_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new HardwareAnalyzerView();
        }

        // Placeholders for remaining modules
        private void NavUninstaller_Click(object sender, RoutedEventArgs e) { }
        private void NavPower_Click(object sender, RoutedEventArgs e) { }
        private void NavNetwork_Click(object sender, RoutedEventArgs e) { }
        private void NavJunk_Click(object sender, RoutedEventArgs e) { }
        private void NavDuplicate_Click(object sender, RoutedEventArgs e) { }
    }
}
