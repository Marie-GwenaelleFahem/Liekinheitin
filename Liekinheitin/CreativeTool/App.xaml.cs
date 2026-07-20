using System.Windows;
using System.Windows.Media;
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
            var canvas = new PixelCanvas(layout.Columns, layout.Rows, Colors.Black);
            var brush = new BrushTool(canvas, layout);
            var shapeController = new ShapePlacementController(canvas, layout);
            var mainViewModel = new MainViewModel(canvas, layout, brush, statePublisher, shapeController);

            var mainWindow = new MainWindow { DataContext = mainViewModel };
            mainWindow.Show();
        }
    }
}