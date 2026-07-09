using System.Collections.ObjectModel;
using Liekinheitin.Application.Interfaces;
using Liekinheitin.Application.Services;
using Liekinheitin.Domain.Entities;
using Liekinheitin.Infrastructure.Debug;
using Liekinheitin.Infrastructure.Supervision;

namespace Liekinheitin.RoutingHost.ViewModels;

/// <summary>
/// Affiche une représentation visuelle du contenu de patch.json : navigation en trois niveaux
/// (contrôleurs → univers → LED) via un fil d'Ariane, chaque contrôleur étant accompagné de son
/// état de santé (joignable / injoignable / inconnu). Permet aussi d'envoyer une couleur de test
/// réelle en ArtNet vers les entités couvertes par une carte, pour vérifier le câblage sur le
/// terrain sans dépendre de CreativeTool.
/// </summary>
/// <remarks>
/// Reconstruit sa liste de cartes à partir de <see cref="PatchService.Controllers"/> et des
/// dernières valeurs connues via <see cref="ControllerHealthChecker.ControllerStatusChanged"/>,
/// auquel cette classe est abonnée. Un contrôleur signalé injoignable est mis en évidence
/// visuellement, pour repérer rapidement une panne sur le terrain.
/// </remarks>
public class PatchVisualizationViewModel
{
    private readonly PatchService _patchService;
    private readonly IPacketSender _packetSender;
    private readonly UniverseSnapshotStore _snapshotStore;
    private readonly Dictionary<string, bool> _reachability = new();
    private readonly Dictionary<string, CardSwatch> _selectedSwatches = new();
    private List<string> _path = new();

    public ObservableCollection<BreadcrumbItemViewModel> Breadcrumb { get; } = new();
    public ObservableCollection<PatchCardViewModel> Cards { get; } = new();

    public PatchVisualizationViewModel(
        PatchService patchService,
        ControllerHealthChecker healthChecker,
        IPacketSender packetSender,
        UniverseSnapshotStore snapshotStore)
    {
        _patchService = patchService;
        _packetSender = packetSender;
        _snapshotStore = snapshotStore;
        healthChecker.ControllerStatusChanged += OnControllerStatusChanged;
        Render();
    }

    private void OnControllerStatusChanged(ControllerStatus status)
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            _reachability[status.ControllerId] = status.IsReachable;
            Render();
        });
    }

    private void NavigateTo(List<string> newPath)
    {
        _path = newPath;
        Render();
    }

    private void Render()
    {
        RenderBreadcrumb();
        RenderCards();
    }

    private void RenderBreadcrumb()
    {
        Breadcrumb.Clear();

        var crumbs = new List<string> { "Contrôleurs" };
        crumbs.AddRange(_path);

        for (int i = 0; i < crumbs.Count; i++)
        {
            bool isCurrent = i == crumbs.Count - 1;
            int truncateAt = i;

            var navigateCommand = isCurrent
                ? null
                : new RelayCommand(_ => NavigateTo(_path.Take(truncateAt).ToList()));

            Breadcrumb.Add(new BreadcrumbItemViewModel(crumbs[i], isCurrent, navigateCommand));
        }
    }

    private void RenderCards()
    {
        Cards.Clear();

        if (_path.Count == 0)
        {
            foreach (var controller in _patchService.Controllers)
            {
                string controllerId = controller.Id;
                string key = $"ctrl:{controllerId}";
                var entityIds = GetControllerEntityIds(controllerId);

                Cards.Add(new PatchCardViewModel(
                    key,
                    controller.Id,
                    controller.IpAddress,
                    GetStatus(controller.Id),
                    entityIds,
                    GetSelectedSwatch(key),
                    new RelayCommand(_ => NavigateTo(new List<string> { controllerId })),
                    ApplySwatch));
            }
        }
        else if (_path.Count == 1)
        {
            string controllerId = _path[0];
            StatusDot status = GetStatus(controllerId);

            foreach (int universe in _patchService.GetUniverses(controllerId))
            {
                var entityIds = _patchService.GetEntityIds(controllerId, universe);
                string key = $"uni:{controllerId}:{universe}";

                Cards.Add(new PatchCardViewModel(
                    key,
                    $"Univers {universe}",
                    $"{entityIds.Count} LED",
                    status,
                    entityIds,
                    GetSelectedSwatch(key),
                    new RelayCommand(_ => NavigateTo(new List<string> { controllerId, universe.ToString() })),
                    ApplySwatch));
            }
        }
        else
        {
            string controllerId = _path[0];
            int universe = int.Parse(_path[1]);
            StatusDot status = GetStatus(controllerId);

            foreach (int entityId in _patchService.GetEntityIds(controllerId, universe))
            {
                string key = $"led:{entityId}";

                Cards.Add(new PatchCardViewModel(
                    key,
                    $"LED {entityId}",
                    string.Empty,
                    status,
                    new List<int> { entityId },
                    GetSelectedSwatch(key),
                    navigateCommand: null,
                    ApplySwatch));
            }
        }
    }

    private List<int> GetControllerEntityIds(string controllerId)
    {
        var ids = new List<int>();
        foreach (int universe in _patchService.GetUniverses(controllerId))
            ids.AddRange(_patchService.GetEntityIds(controllerId, universe));
        return ids;
    }

    private CardSwatch GetSelectedSwatch(string key)
        => _selectedSwatches.TryGetValue(key, out var swatch) ? swatch : CardSwatch.None;

    private StatusDot GetStatus(string controllerId)
    {
        if (!_reachability.TryGetValue(controllerId, out bool reachable))
            return StatusDot.Off;

        return reachable ? StatusDot.Ok : StatusDot.Err;
    }

    /// <summary>
    /// Envoie réellement, en ArtNet, la couleur de test choisie vers toutes les entités couvertes
    /// par la carte cliquée, en préservant les autres canaux déjà en vigueur sur l'univers
    /// concerné (lus depuis <see cref="UniverseSnapshotStore"/>).
    /// </summary>
    private void ApplySwatch(PatchCardViewModel card, CardSwatch swatch)
    {
        _selectedSwatches[card.Key] = swatch;
        card.SelectedSwatch = swatch;

        (byte r, byte g, byte b) = swatch switch
        {
            CardSwatch.Red => ((byte)0xE5, (byte)0x48, (byte)0x4D),
            CardSwatch.Blue => ((byte)0x3B, (byte)0x82, (byte)0xF6),
            CardSwatch.Green => ((byte)0x22, (byte)0xC5, (byte)0x5E),
            _ => ((byte)0, (byte)0, (byte)0),
        };

        var groups = card.EntityIds
            .Select(id => (Id: id, Range: _patchService.FindAddress(id)))
            .Where(x => x.Range is not null)
            .GroupBy(x => (x.Range!.ControllerId, x.Range.Universe));

        foreach (var group in groups)
        {
            var controller = _patchService.Controllers.FirstOrDefault(c => c.Id == group.Key.ControllerId);
            if (controller is null)
                continue;

            byte[] snapshot = _snapshotStore.GetSnapshot(group.Key.Universe);
            byte[] data = snapshot.Length == 512 ? (byte[])snapshot.Clone() : new byte[512];

            foreach (var (id, range) in group)
            {
                int offset = range!.ChannelStart - 1 + (id - range.EntityIdStart) * range.ChannelsPerEntity;
                if (offset < 0 || offset >= data.Length)
                    continue;

                data[offset] = r;
                if (range.ChannelsPerEntity > 1 && offset + 1 < data.Length)
                    data[offset + 1] = g;
                if (range.ChannelsPerEntity > 2 && offset + 2 < data.Length)
                    data[offset + 2] = b;
            }

            _packetSender.Send(new DmxFrame
            {
                TargetIp = controller.IpAddress,
                Universe = group.Key.Universe,
                Data = data,
            });
        }
    }
}
