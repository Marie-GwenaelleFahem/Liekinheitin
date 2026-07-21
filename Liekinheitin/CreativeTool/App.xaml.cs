using System.Windows;
using Liekinheitin.CreativeTool.Domain;
using Liekinheitin.CreativeTool.ViewModels;
using Liekinheitin.Infrastructure.Network;

namespace Liekinheitin.CreativeTool
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var statePublisher = new UdpStatePublisher(targetIp: "127.0.0.1", targetPort: 9001);
            var layout = new WallLayout(columns: 128, rows: 128);
            var scene = new SceneManager(layout);
            var brush = new BrushTool(scene);

            var mainViewModel = new MainViewModel(scene, layout, brush, statePublisher);

            var mainWindow = new MainWindow { DataContext = mainViewModel };
            mainWindow.Show();
        }
    }
}