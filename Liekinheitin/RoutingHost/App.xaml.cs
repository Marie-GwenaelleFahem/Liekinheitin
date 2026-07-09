using System.IO;
using System.Windows;
using System.Windows.Threading;
using Liekinheitin.Application.Services;
using Liekinheitin.Infrastructure.Config;
using Liekinheitin.Infrastructure.Debug;
using Liekinheitin.Infrastructure.Network;
using Liekinheitin.Infrastructure.Supervision;
using Liekinheitin.RoutingHost.ViewModels;

namespace Liekinheitin.RoutingHost
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private static readonly TimeSpan HealthCheckInterval = TimeSpan.FromSeconds(3);

        // Conservé en champ pour éviter que le ramasse-miettes n'arrête le timer.
        private DispatcherTimer? _healthCheckTimer;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string patchPath = Path.Combine(AppContext.BaseDirectory, "patch.json");

            var logService = new LogService();
            var patchService = new PatchService(new JsonPatchLoader());
            patchService.LoadPatch(patchPath);

            var healthChecker = new ControllerHealthChecker();
            StartHealthCheckLoop(healthChecker, patchService, logService);

            var snapshotStore = new UniverseSnapshotStore();
            var packetSender = new ArtNetSender(logService, snapshotStore);

            var patchVisualizationViewModel = new PatchVisualizationViewModel(
                patchService, healthChecker, packetSender, snapshotStore);

            var mainWindow = new MainWindow(patchVisualizationViewModel);
            mainWindow.Show();
        }

        private void StartHealthCheckLoop(ControllerHealthChecker healthChecker, PatchService patchService, LogService logService)
        {
            _healthCheckTimer = new DispatcherTimer { Interval = HealthCheckInterval };
            _healthCheckTimer.Tick += async (_, _) =>
            {
                try
                {
                    await healthChecker.CheckAllAsync(patchService.Controllers);
                }
                catch (Exception ex)
                {
                    logService.Log(LogLevel.Error, nameof(ControllerHealthChecker), ex.Message);
                }
            };
            _healthCheckTimer.Start();
        }
    }

}
