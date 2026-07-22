using Liekinheitin.Application.Interfaces;
using Liekinheitin.Application.Services;
using Liekinheitin.Domain.Entities;
using Liekinheitin.Infrastructure.Config;
using Liekinheitin.Infrastructure.Debug;
using Liekinheitin.Infrastructure.Network;
using Liekinheitin.Infrastructure.Supervision;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Liekinheitin.RoutingHost.Views
{
    /// <summary>
    /// Représente une carte affichée à l'écran (contrôleur, univers ou LED selon <see cref="Kind"/>).
    /// Implémente INotifyPropertyChanged uniquement pour StatusColor : c'est la seule valeur
    /// qui doit pouvoir changer après que la carte soit déjà affichée (ping en direct).
    /// </summary>
    public class CardItem : INotifyPropertyChanged
    {
        public string Name { get; set; } = "";
        public string Body { get; set; } = "";
        public string Kind { get; set; } = ""; // "Controller", "Universe" ou "Led"
        public string ControllerId { get; set; } = "";
        public int? Universe { get; set; }
        public int EntityId { get; set; } // uniquement rempli pour Kind == "Led"

        private string _statusColor = "Gray";
        public string StatusColor
        {
            get => _statusColor;
            set
            {
                _statusColor = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusColor)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    /// <summary>
    /// Logique d'interaction pour PatchVisualizationView.xaml
    /// </summary>
    public partial class PatchVisualizationView : UserControl
    {
        // Port UDP local sur lequel CreativeTool publie son State en continu (voir
        // RoutingHostStatePort dans CreativeTool/Views/MainWindow.xaml.cs).
        private const int StatePort = 5000;

        private PatchService _patchService = null!;
        private readonly List<string> _path = new();

        private readonly ControllerHealthChecker _healthChecker = new();
        private readonly Dictionary<string, string> _statusByController = new();
        private readonly DispatcherTimer _healthTimer = new() { Interval = TimeSpan.FromSeconds(5) };

        private readonly LogService _logService = LogService.Instance;
        private readonly UniverseSnapshotStore _snapshotStore = new();
        private IPacketSender _packetSender = null!;
        private RoutingEngine _routingEngine = null!;
        private UdpStateReceiver _stateReceiver = null!;

        public PatchVisualizationView()
        {
            InitializeComponent();

            _patchService = new PatchService(new JsonPatchLoader());

            string patchPath = Path.GetFullPath(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "patch.json"));
            _patchService.LoadPatch(patchPath);

            _packetSender = new ArtNetSender(_logService, _snapshotStore);
            _routingEngine = new RoutingEngine(_patchService, _packetSender);

            // Reçoit en continu l'État envoyé par CreativeTool (~40x/seconde) et le route vers
            // ArtNet via _routingEngine : sans ça, l'animation jouée dans CreativeTool n'atteint
            // jamais le matériel, même si l'envoi manuel par pastille de couleur fonctionne (lui
            // ne passe pas par le réseau CreativeTool -> RoutingHost).
            _stateReceiver = new UdpStateReceiver(StatePort);
            _routingEngine.Start(_stateReceiver);
            _stateReceiver.StartListening();
            Unloaded += (_, _) => _stateReceiver.StopListening();

            _healthChecker.ControllerStatusChanged += OnControllerStatusChanged;
            _healthTimer.Tick += async (s, e) => await _healthChecker.CheckAllAsync(_patchService.Controllers);
            _healthTimer.Start();

            ShowControllers();
        }

        private string GetKnownColor(string controllerId) =>
            _statusByController.TryGetValue(controllerId, out var color) ? color : "Gray";

        // ----- Les 3 niveaux d'affichage -----

        private void ShowControllers()
        {
            _path.Clear();
            RenderBreadcrumb();

            CardList.ItemsSource = _patchService.Controllers.Select(c => new CardItem
            {
                Name = c.Id,
                Body = c.IpAddress,
                StatusColor = GetKnownColor(c.Id),
                Kind = "Controller",
            }).ToList();
        }

        private void ShowUniverses(string controllerId)
        {
            _path.Clear();
            _path.Add(controllerId);
            RenderBreadcrumb();

            CardList.ItemsSource = _patchService.GetUniverses(controllerId).Select(u => new CardItem
            {
                Name = "Univers " + u,
                Body = "",
                StatusColor = GetKnownColor(controllerId),
                Kind = "Universe",
                ControllerId = controllerId,
                Universe = u,
            }).ToList();
        }

        private void ShowLeds(string controllerId, int universe)
        {
            _path.Clear();
            _path.Add(controllerId);
            _path.Add(universe.ToString());
            RenderBreadcrumb();

            CardList.ItemsSource = _patchService.GetEntityIds(controllerId, universe).Select(id => new CardItem
            {
                Name = "LED " + id,
                Body = "",
                StatusColor = GetKnownColor(controllerId),
                Kind = "Led",
                ControllerId = controllerId,
                Universe = universe,
                EntityId = id,
            }).ToList();
        }

        // ----- Navigation (clic sur une carte, fil d'Ariane) -----

        private void Card_Click(object sender, MouseButtonEventArgs e)
        {
            var item = (CardItem)((FrameworkElement)sender).DataContext;

            if (item.Kind == "Controller")
                ShowUniverses(item.Name);
            else if (item.Kind == "Universe")
                ShowLeds(item.ControllerId, item.Universe!.Value);
        }

        private void RenderBreadcrumb()
        {
            Breadcrumb.Children.Clear();

            var labels = new List<string> { "Contrôleurs" };
            labels.AddRange(_path);

            for (int i = 0; i < labels.Count; i++)
            {
                bool isLast = i == labels.Count - 1;
                int levelToRestore = i;

                var crumb = new TextBlock
                {
                    Text = labels[i],
                    Foreground = isLast ? Brushes.Gray : Brushes.White,
                    Cursor = isLast ? Cursors.Arrow : Cursors.Hand,
                    Margin = new Thickness(4, 0, 4, 0),
                };

                if (!isLast)
                {
                    crumb.MouseLeftButtonUp += (s, e) => GoToLevel(levelToRestore);
                }

                Breadcrumb.Children.Add(crumb);

                if (!isLast)
                {
                    Breadcrumb.Children.Add(new TextBlock { Text = "›", Foreground = Brushes.Gray, Margin = new Thickness(0, 0, 4, 0) });
                }
            }
        }

        private void GoToLevel(int level)
        {
            _path.RemoveRange(level, _path.Count - level);

            if (_path.Count == 0) ShowControllers();
            else if (_path.Count == 1) ShowUniverses(_path[0]);
            else if (_path.Count == 2) ShowLeds(_path[0], int.Parse(_path[1]));

            RenderBreadcrumb();
        }

        // ----- Statut de santé des contrôleurs (ping en direct) -----

        private void OnControllerStatusChanged(ControllerStatus status)
        {
            string color = status.IsReachable ? "LimeGreen" : "Red";
            _statusByController[status.ControllerId] = color;

            // Met à jour toutes les cartes actuellement affichées qui appartiennent à ce
            // contrôleur (que ce soit la carte du contrôleur lui-même, ou des cartes
            // univers/LED en dessous) : c'est ce qui garantit que l'indicateur reste le
            // même partout, quel que soit le niveau affiché au moment du changement.
            foreach (var card in CardList.ItemsSource?.Cast<CardItem>() ?? Enumerable.Empty<CardItem>())
            {
                if (card.ControllerId == status.ControllerId || (card.Kind == "Controller" && card.Name == status.ControllerId))
                {
                    card.StatusColor = color;
                }
            }
        }

        // ----- Clic sur une couleur : construit un State et envoie de vraies trames ArtNet -----

        private void Swatch_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var card = (CardItem)button.DataContext;
            string colorName = (string)button.Tag;

            List<int> targetIds = card.Kind switch
            {
                "Controller" => _patchService.GetAllEntityIds(card.Name),
                "Universe" => _patchService.GetEntityIds(card.ControllerId, card.Universe!.Value),
                "Led" => new List<int> { card.EntityId },
                _ => new List<int>(),
            };

            State state = BuildColorState(targetIds, colorName);

            foreach (var frame in _routingEngine.BuildFrames(state))
            {
                _packetSender.Send(frame);
            }
        }

        private State BuildColorState(List<int> entityIds, string colorName)
        {
            var state = new State();

            foreach (int id in entityIds)
            {
                var range = _patchService.FindAddress(id);
                if (range is null) continue; // entité inconnue du patch, on l'ignore

                var channels = new byte[range.ChannelsPerEntity];

                if (colorName == "Red") channels[0] = 255;
                else if (colorName == "Green" && channels.Length > 1) channels[1] = 255;
                else if (colorName == "Blue" && channels.Length > 2) channels[2] = 255;
                // "Off" : le tableau reste à zéro, déjà le cas par défaut en C#

                state.Entities.Add(new Entity { Id = id, Channels = channels });
            }

            return state;
        }
    }
}