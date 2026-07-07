using System.Net.Sockets;
using System.Text;
using Liekinheitin.Infrastructure.Supervision;

namespace Liekinheitin.Infrastructure.Network;

/// <summary>
/// Émet et détecte un signal de présence (« ping ») entre CreativeTool et RoutingHost.
/// </summary>
/// <remarks>
/// Utilisée à l'identique des deux côtés, avec des rôles symétriques une fois instanciée.
/// <see cref="StartSending"/> envoie un petit message UDP de présence à intervalle régulier
/// vers l'autre application. <see cref="StartListening"/> écoute ces messages : si aucun
/// n'est reçu pendant <c>peerTimeout</c>, <see cref="PeerLost"/> est déclenché ; dès qu'un
/// message est reçu après une absence, <see cref="PeerDetected"/> est déclenché.
///
/// Côté RoutingHost, <see cref="PeerDetected"/> déclenche l'envoi du State initial via
/// <see cref="UdpEntityListPublisher"/> ; côté CreativeTool, <see cref="PeerDetected"/>
/// déclenche le démarrage de l'envoi continu du State. Le paramètre <c>logService</c> est
/// optionnel : RoutingHost fournit une vraie instance pour tracer les pertes de connexion,
/// CreativeTool peut simplement passer <c>null</c>.
/// </remarks>
public class HeartbeatService : IDisposable
{
    private static readonly byte[] PingMessage = Encoding.ASCII.GetBytes("SONETLUMIERE_PING");

    private readonly LogService? _logService;
    private readonly TimeSpan _peerTimeout;

    private UdpClient? _sendClient;
    private UdpClient? _listenClient;
    private CancellationTokenSource? _cts;

    private DateTime _lastPingReceived = DateTime.MinValue;
    private bool _peerCurrentlyPresent;

    /// <summary>Déclenché quand l'autre application est détectée (premier ping, ou retour après absence).</summary>
    public event Action? PeerDetected;

    /// <summary>Déclenché quand aucun ping n'a été reçu depuis plus de <c>peerTimeout</c>.</summary>
    public event Action? PeerLost;

    /// <param name="peerTimeout">
    /// Délai de tolérance avant de considérer l'autre application comme perdue
    /// (3 secondes par défaut, pour éviter des resynchronisations inutiles au moindre
    /// ralentissement réseau).
    /// </param>
    /// <param name="logService">Journal optionnel, pour tracer les pertes de connexion.</param>
    public HeartbeatService(TimeSpan? peerTimeout = null, LogService? logService = null)
    {
        _peerTimeout = peerTimeout ?? TimeSpan.FromSeconds(3);
        _logService = logService;
    }

    /// <summary>Démarre l'envoi périodique du signal de présence.</summary>
    /// <param name="targetIp">Adresse IP de l'autre application.</param>
    /// <param name="targetPort">Port UDP sur lequel l'autre application écoute les pings.</param>
    /// <param name="interval">Intervalle entre deux envois (1 seconde par défaut).</param>
    public void StartSending(string targetIp, int targetPort, TimeSpan? interval = null)
    {
        _sendClient = new UdpClient();
        var period = interval ?? TimeSpan.FromSeconds(1);
        var cts = _cts ??= new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    _sendClient.Send(PingMessage, PingMessage.Length, targetIp, targetPort);
                }
                catch
                {
                    // Un échec d'envoi ponctuel du ping n'est pas critique : on retentera au tour suivant.
                }

                await Task.Delay(period, cts.Token).ContinueWith(_ => { });
            }
        }, cts.Token);
    }

    /// <summary>Démarre l'écoute des signaux de présence de l'autre application.</summary>
    /// <param name="listenPort">Port UDP local sur lequel écouter les pings.</param>
    public void StartListening(int listenPort)
    {
        _listenClient = new UdpClient(listenPort);
        var cts = _cts ??= new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    await _listenClient.ReceiveAsync(cts.Token);
                    _lastPingReceived = DateTime.UtcNow;

                    if (!_peerCurrentlyPresent)
                    {
                        _peerCurrentlyPresent = true;
                        PeerDetected?.Invoke();
                    }
                }
                catch (OperationCanceledException)
                {
                    // Arrêt normal.
                }
            }
        }, cts.Token);

        _ = Task.Run(async () =>
        {
            while (!cts.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500), cts.Token).ContinueWith(_ => { });

                bool depasseLeDelai = DateTime.UtcNow - _lastPingReceived > _peerTimeout;
                if (_peerCurrentlyPresent && depasseLeDelai)
                {
                    _peerCurrentlyPresent = false;
                    var dureeEcoulee = DateTime.UtcNow - _lastPingReceived;
                    _logService?.Log(
                        LogLevel.Warning,
                        nameof(HeartbeatService),
                        $"Pair non détecté depuis {dureeEcoulee.TotalSeconds:F1}s (aucun ping reçu).");
                    PeerLost?.Invoke();
                }
            }
        }, cts.Token);
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _sendClient?.Dispose();
        _listenClient?.Dispose();
    }
}