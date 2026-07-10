namespace Liekinheitin.Infrastructure.Supervision;

/// <summary>
/// Point central de journalisation de RoutingHost.
/// </summary>
/// <remarks>
/// N'importe quelle classe d'Infrastructure qui rencontre un problème l'appelle directement :
/// <see cref="ArtNetSender"/> en cas d'échec d'envoi, <see cref="HeartbeatService"/> en cas de
/// perte de connexion, etc. Chaque appel à <see cref="Log"/> crée un <see cref="LogEntry"/>
/// horodaté et déclenche <see cref="LogEntryAdded"/>, auquel <c>LogViewModel</c> (RoutingHost)
/// est abonné pour afficher les messages à l'écran en temps réel.
/// </remarks>
public class LogService
{
    /// <summary>
    /// Instance partagée par toute l'application RoutingHost, pour que les producteurs de logs
    /// (ArtNetSender, HeartbeatService, ControllerHealthChecker...) et l'écran de logs
    /// (LogView) utilisent tous le même journal sans avoir à se passer l'instance explicitement.
    /// </summary>
    public static LogService Instance { get; } = new();

    private readonly List<LogEntry> _history = new();
    private readonly object _lock = new();

    /// <summary>Déclenché à chaque nouveau message journalisé.</summary>
    public event Action<LogEntry>? LogEntryAdded;

    /// <summary>Enregistre un nouveau message de journal et notifie les abonnés.</summary>
    /// <param name="level">Gravité du message.</param>
    /// <param name="source">Classe à l'origine du message.</param>
    /// <param name="message">Message lisible.</param>
    public void Log(LogLevel level, string source, string message)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            Source = source,
            Message = message,
        };

        lock (_lock)
        {
            _history.Add(entry);
        }

        LogEntryAdded?.Invoke(entry);
    }

    /// <summary>Renvoie une copie de l'historique complet des messages journalisés.</summary>
    public List<LogEntry> GetHistory()
    {
        lock (_lock)
        {
            return new List<LogEntry>(_history);
        }
    }
}