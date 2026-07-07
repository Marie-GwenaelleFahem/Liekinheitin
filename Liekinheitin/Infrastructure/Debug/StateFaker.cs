using Liekinheitin.Domain.Entities;

namespace Liekinheitin.Infrastructure.Debug;

/// <summary>
/// Génère un <see cref="State"/> factice, pour tester tout le pipeline de routage (calcul
/// d'adresse, construction ArtNet, envoi) sans dépendre de CreativeTool ni d'une vraie
/// animation.
/// </summary>
/// <remarks>
/// C'est l'outil de debug demandé par l'exigence P8. Déclenché manuellement depuis
/// <c>MonitorViewModel.TriggerFaker()</c>, côté RoutingHost.
/// </remarks>
public class StateFaker
{
    private readonly Random _random = new();

    /// <summary>
    /// Construit un State avec des couleurs aléatoires pour les identifiants d'entité fournis.
    /// </summary>
    /// <param name="entityIds">Identifiants des entités à faire varier.</param>
    /// <param name="channelsPerEntity">
    /// Nombre de canaux à générer par entité (3 par défaut, pour du RGB simple).
    /// </param>
    /// <returns>Un State factice, prêt à être injecté dans RoutingEngine pour test.</returns>
    public State GenerateRandomState(List<int> entityIds, int channelsPerEntity = 3)
    {
        var state = new State();

        foreach (int id in entityIds)
        {
            var channels = new byte[channelsPerEntity];
            _random.NextBytes(channels);

            state.Entities.Add(new Entity { Id = id, Channels = channels });
        }

        return state;
    }
}