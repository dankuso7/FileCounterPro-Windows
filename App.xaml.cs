using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace FileCounterPro_Windows
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Force Software Rendering for Virtual Machines (UTM/Parallels) to prevent DirectX crashes
            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
            base.OnStartup(e);
        }
    }
}
