namespace Liekinheitin.CreativeTool.Models
{
    public sealed class EffectPreset
    {
        public required string DisplayName { get; init; }
        public required string Category { get; init; }
        public EffectType EffectType { get; init; }
        public double Duration { get; init; } = 2;
        public RgbwColor Color { get; init; } = RgbwColor.White;
        public double Intensity { get; init; } = 1;
        public double Speed { get; init; } = 1;
        public string TextContent { get; init; } = string.Empty;
        public bool UsesScatteredSelection { get; init; }
        public int ParticleCount { get; init; } = 96;

        // Tous les préréglages naissent d'une forme modifiable. La toile organique
        // complète conserve le rendu historique tant que l'utilisateur ne la remplace pas.
        public string BaseShapeName { get; init; } = "Toile organique — mur complet";
    }
}
