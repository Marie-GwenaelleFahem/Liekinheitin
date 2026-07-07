using Liekinheitin.Application.Interfaces;
using Liekinheitin.Domain.Entities;
using Liekinheitin.Infrastructure.Debug;
using Liekinheitin.Infrastructure.Supervision;
using System.Net.Sockets;

namespace Liekinheitin.Infrastructure.Network;

/// <summary>
/// Implémentation concrète d'<see cref="IPacketSender"/> : transforme une <see cref="DmxFrame"/>
/// en véritable paquet ArtNet et l'envoie réellement sur le réseau, au port standard 6454.
/// </summary>
/// <remarks>
/// Si l'envoi échoue (contrôleur injoignable, câble débranché...), l'exception est capturée
/// et transmise à <see cref="LogService"/> avec le détail exact et l'adresse IP visée,
/// plutôt que de faire planter silencieusement le routage. Si l'envoi réussit, la trame est
/// aussi enregistrée dans <see cref="UniverseSnapshotStore"/>, pour que l'écran de
/// supervision de RoutingHost puisse afficher les dernières valeurs réellement envoyées.
/// </remarks>
public class ArtNetSender : IPacketSender, IDisposable
{
    private const int ArtNetPort = 6454;

    private readonly UdpClient _udpClient = new();
    private readonly LogService _logService;
    private readonly UniverseSnapshotStore _snapshotStore;

    public ArtNetSender(LogService logService, UniverseSnapshotStore snapshotStore)
    {
        _logService = logService;
        _snapshotStore = snapshotStore;
    }

    /// <inheritdoc />
    public void Send(DmxFrame frame)
    {
        var packet = new ArtNetPacket
        {
            TargetIp = frame.TargetIp,
            Universe = frame.Universe,
            DmxData = frame.Data,
        };

        try
        {
            byte[] bytes = packet.ToBytes();
            _udpClient.Send(bytes, bytes.Length, frame.TargetIp, ArtNetPort);
            _snapshotStore.Store(frame.Universe, frame.Data);
        }
        catch (Exception ex)
        {
            _logService.Log(
                LogLevel.Error,
                nameof(ArtNetSender),
                $"Échec d'envoi vers {frame.TargetIp} (univers {frame.Universe}) : {ex.Message}");
        }
    }

    public void Dispose() => _udpClient.Dispose();
}