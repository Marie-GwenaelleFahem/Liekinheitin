namespace Liekinheitin.Infrastructure.Supervision;

/// <summary>
/// Un message de journal individuel : le moment exact, sa gravité, la classe à l'origine du
/// message, et le message lisible expliquant ce qui s'est passé.
/// </summary>
/// <remarks>
/// C'est la brique de base affichée dans l'écran de logs de RoutingHost
/// (<c>LogViewModel</c>), sans transformation : le même objet transite jusqu'à l'écran via
/// l'abonnement à <see cref="LogService.LogEntryAdded"/>.
/// </remarks>
public class LogEntry
{
    /// <summary>Moment exact où le message a été journalisé.</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>Gravité du message.</summary>
    public LogLevel Level { get; set; }

    /// <summary>Classe à l'origine du message (par exemple "ArtNetSender").</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Message lisible expliquant ce qui s'est passé.</summary>
    public string Message { get; set; } = string.Empty;

    public override string ToString() => $"[{Timestamp:HH:mm:ss}] {Level} ({Source}) : {Message}";
}