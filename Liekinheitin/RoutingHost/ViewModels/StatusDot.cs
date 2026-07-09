namespace Liekinheitin.RoutingHost.ViewModels;

/// <summary>Statut de santé affiché sous forme de pastille de couleur sur une carte.</summary>
public enum StatusDot
{
    /// <summary>Contrôleur joignable.</summary>
    Ok,

    /// <summary>Contrôleur injoignable (dernier ping en échec).</summary>
    Err,

    /// <summary>Aucune vérification effectuée pour le moment.</summary>
    Off,
}
