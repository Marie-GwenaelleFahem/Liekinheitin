using Liekinheitin.Domain.Entities;
using System.Net.NetworkInformation;

namespace Liekinheitin.Infrastructure.Supervision;

/// <summary>
/// Vérifie périodiquement si les contrôleurs connus répondent bien sur le réseau.
/// </summary>
/// <remarks>
/// <see cref="CheckAllAsync"/> envoie un ping ICMP à chaque contrôleur fourni (une vérification
/// simple et suffisante pour détecter un contrôleur injoignable ; une requête ArtPoll ciblée
/// serait une alternative plus proche du protocole ArtNet, envisageable en évolution) et
/// déclenche <see cref="ControllerStatusChanged"/> pour chaque changement d'état détecté.
/// C'est ce mécanisme qui alimente <c>PatchVisualizationViewModel</c> (RoutingHost) pour
/// savoir quel contrôleur afficher comme étant en panne.
/// </remarks>
public class ControllerHealthChecker
{
    private const int TimeoutMilliseconds = 500;

    private readonly Dictionary<string, bool> _lastKnownState = new();

    /// <summary>Déclenché à chaque changement d'état de santé détecté pour un contrôleur.</summary>
    public event Action<ControllerStatus>? ControllerStatusChanged;

    /// <summary>Vérifie tous les contrôleurs fournis et notifie les changements d'état.</summary>
    /// <param name="controllers">Contrôleurs à vérifier (typiquement <c>PatchService.Controllers</c>).</param>
    public async Task CheckAllAsync(List<Controller> controllers)
    {
        foreach (var controller in controllers)
        {
            bool isReachable = await PingAsync(controller.IpAddress);

            bool changed = !_lastKnownState.TryGetValue(controller.Id, out bool wasReachable)
                           || wasReachable != isReachable;

            _lastKnownState[controller.Id] = isReachable;

            if (changed)
            {
                ControllerStatusChanged?.Invoke(new ControllerStatus
                {
                    ControllerId = controller.Id,
                    IpAddress = controller.IpAddress,
                    IsReachable = isReachable,
                    LastChecked = DateTime.Now,
                });
            }
        }
    }

    private static async Task<bool> PingAsync(string ipAddress)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(ipAddress, TimeoutMilliseconds);
            return reply.Status == IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }
}