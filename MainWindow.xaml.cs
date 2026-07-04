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
            // Default to Sensors view
            MainContent.Content = new SensorsView();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
            => this.WindowState = WindowState.Minimized;

        private void BtnClose_Click(object sender, RoutedEventArgs e)
            => Application.Current.Shutdown();

        // ── Navigation ──────────────────────────────────────────────────────

        private void NavDashboard_Click(object sender, RoutedEventArgs e)
            => NavigateTo(new DashboardView());

        private void NavSensors_Click(object sender, RoutedEventArgs e)
            => NavigateTo(new SensorsView());

        private void NavActivity_Click(object sender, RoutedEventArgs e)
            => NavigateTo(new ActivityMonitorView());

        private void NavVirusScanner_Click(object sender, RoutedEventArgs e)
            => NavigateTo(new VirusScannerView());

        private void NavUninstaller_Click(object sender, RoutedEventArgs e)
            => NavigateTo(new SmartUninstallerView());

        private void NavJunk_Click(object sender, RoutedEventArgs e)
            => NavigateTo(new SystemJunkView());

        private void NavDuplicate_Click(object sender, RoutedEventArgs e)
            => NavigateTo(new DuplicateFinderView());

        private void NavHardware_Click(object sender, RoutedEventArgs e)
            => NavigateTo(new HardwareAnalyzerView());

        private void NavNetwork_Click(object sender, RoutedEventArgs e)
            => NavigateTo(new HardwareAnalyzerView()); // Network tab placeholder using Hardware

        // ── Helpers ─────────────────────────────────────────────────────────

        private void NavigateTo(object view)
        {
            MainContent.Content = null;
            MainContent.Content = view;     // Triggers the opacity animation
        }
    }
}
