using Liekinheitin.Application.Interfaces;
using Liekinheitin.Application.Services;
using Liekinheitin.Infrastructure.Config;
using Liekinheitin.Infrastructure.Debug;
using Liekinheitin.Infrastructure.Network;
using Liekinheitin.Infrastructure.Supervision;
using System.IO;
using System.Windows;

namespace Liekinheitin.RoutingHost
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var logService = new LogService();
            var snapshotStore = new UniverseSnapshotStore();

            var patchService = new PatchService(new JsonPatchLoader());
            string patchPath = Path.GetFullPath(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "patch.json"));
            patchService.LoadPatch(patchPath);

            IPacketSender packetSender = new ArtNetSender(logService, snapshotStore);
            var routingEngine = new RoutingEngine(patchService, packetSender);

            // Écoute en continu le flux temps réel envoyé par CreativeTool (port 9001,
            // cohérent avec UdpStatePublisher côté CreativeTool) et route chaque State
            // reçu vers ArtNet automatiquement, dès qu'il arrive.
            var stateReceiver = new UdpStateReceiver(listenPort: 9001);
            routingEngine.Start(stateReceiver);
            stateReceiver.StartListening();

            var mainWindow = new MainWindow();
            mainWindow.InitializePatchVisualization(patchService, packetSender, routingEngine);
            mainWindow.Show();
        }
    }
}