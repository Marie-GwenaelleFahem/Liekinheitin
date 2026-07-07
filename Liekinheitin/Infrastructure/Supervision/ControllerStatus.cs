namespace Liekinheitin.Infrastructure.Supervision;

/// <summary>
/// État de santé connu d'un contrôleur à un instant donné.
/// </summary>
/// <remarks>
/// Produit par <see cref="ControllerHealthChecker"/>, affiché par
/// <c>PatchVisualizationViewModel</c> (RoutingHost) pour repérer rapidement une panne sur
/// le terrain.
/// </remarks>
public class ControllerStatus
{
    /// <summary>Identifiant du contrôleur (recopié de <c>Controller.Id</c>).</summary>
    public string ControllerId { get; set; } = string.Empty;

    /// <summary>Adresse IP du contrôleur (recopiée de <c>Controller.IpAddress</c>).</summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>Indique si le contrôleur a répondu lors de la dernière vérification.</summary>
    public bool IsReachable { get; set; }

    /// <summary>Moment de la dernière vérification effectuée.</summary>
    public DateTime LastChecked { get; set; }
}