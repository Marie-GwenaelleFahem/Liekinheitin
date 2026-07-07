namespace Liekinheitin.Infrastructure.Debug;

/// <summary>
/// Conserve en mémoire les 512 derniers octets réellement envoyés pour chaque univers.
/// </summary>
/// <remarks>
/// <see cref="ArtNetSender"/> (ou le point d'appel juste après un envoi réussi) appelle
/// <see cref="Store"/> ; <c>MonitorViewModel</c> (RoutingHost) appelle <see cref="GetSnapshot"/>
/// pour afficher, univers par univers, la grille de valeurs actuellement en vigueur sur le
/// matériel.
/// </remarks>
public class UniverseSnapshotStore
{
    private readonly Dictionary<int, byte[]> _lastSentByUniverse = new();
    private readonly object _lock = new();

    /// <summary>Enregistre les dernières valeurs envoyées pour un univers.</summary>
    /// <param name="universe">Univers concerné.</param>
    /// <param name="data">Les 512 octets envoyés.</param>
    public void Store(int universe, byte[] data)
    {
        lock (_lock)
        {
            _lastSentByUniverse[universe] = (byte[])data.Clone();
        }
    }

    /// <summary>Récupère les dernières valeurs connues pour un univers.</summary>
    /// <param name="universe">Univers demandé.</param>
    /// <returns>Les 512 derniers octets envoyés, ou un tableau vide si l'univers est inconnu.</returns>
    public byte[] GetSnapshot(int universe)
    {
        lock (_lock)
        {
            return _lastSentByUniverse.TryGetValue(universe, out var data)
                ? (byte[])data.Clone()
                : Array.Empty<byte>();
        }
    }

    /// <summary>Liste les univers actuellement connus (déjà vus au moins une fois).</summary>
    public List<int> GetKnownUniverses()
    {
        lock (_lock)
        {
            return _lastSentByUniverse.Keys.OrderBy(u => u).ToList();
        }
    }
}