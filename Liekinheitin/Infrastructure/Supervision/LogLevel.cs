namespace Liekinheitin.Infrastructure.Supervision;

/// <summary>Gravité d'un message de journal.</summary>
public enum LogLevel
{
    /// <summary>Information générale, sans problème associé.</summary>
    Info,

    /// <summary>Situation anormale mais non bloquante (par exemple une perte de ping temporaire).</summary>
    Warning,

    /// <summary>Échec réel d'une opération (par exemple un envoi de paquet qui a échoué).</summary>
    Error,
}