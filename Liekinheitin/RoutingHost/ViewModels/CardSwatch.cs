namespace Liekinheitin.RoutingHost.ViewModels;

/// <summary>Couleur de test sélectionnée sur une carte (contrôleur, univers ou LED).</summary>
public enum CardSwatch
{
    /// <summary>Aucune couleur de test envoyée depuis cette carte pour l'instant.</summary>
    None,

    Red,
    Blue,
    Green,

    /// <summary>Éteint (0, 0, 0) — remet les entités de cette carte à zéro.</summary>
    Off,
}
