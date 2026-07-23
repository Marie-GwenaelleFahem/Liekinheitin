using System.Collections.Generic;
using Liekinheitin.CreativeTool.Models;

namespace Liekinheitin.CreativeTool.Services
{
    public static class EffectPresetFactory
    {
        public static IReadOnlyList<EffectPreset> CreateAll() => new List<EffectPreset>
        {
            Preset("Couleur fixe", "Lumière", EffectType.SolidColor, new(255, 255, 255, 0)),
            Preset("Fondu lumineux", "Lumière", EffectType.Fade, new(255, 255, 255, 0)),
            Preset("Vague", "Lumière", EffectType.Wave, new(60, 150, 255, 0)),
            Preset("Pulsation", "Lumière", EffectType.Pulse, new(255, 42, 80, 0)),
            Preset("Respiration", "Lumière", EffectType.Breath, new(100, 170, 255, 0), 3),
            Preset("Stroboscope", "Rythme", EffectType.Strobe, new(255, 255, 255, 0), 1),
            Preset("Chaser DJ", "Rythme", EffectType.Chase, new(30, 220, 190, 0)),
            Preset("Égaliseur", "Rythme", EffectType.Equalizer, new(30, 220, 120, 0), 3),
            new EffectPreset { DisplayName = "Étincelles", Category = "Particules", EffectType = EffectType.Sparkle, Duration = 3, Color = new(255, 220, 100, 0), ParticleCount = 96 },
            Preset("Braises descendantes", "Particules", EffectType.FallingEmbers, new(255, 78, 8, 0), 3),
            Preset("Lignes descendantes", "Particules", EffectType.WhiteFallingLines, new(240, 245, 255, 0), 2),
            new EffectPreset { DisplayName = "Étoiles rouges persistantes", Category = "Particules", EffectType = EffectType.PersistentRedStars, Duration = 4, Color = new(198, 5, 22, 0), Speed = 1.2, UsesScatteredSelection = true },
            Preset("Ondes concentriques", "Ondulations", EffectType.Ripple, new(70, 150, 255, 0), 2),
            Preset("Anneau d’impact", "Ondulations", EffectType.ClickRipple, new(255, 80, 100, 0), 1.1),
            Preset("Contraction et explosion", "Ondulations", EffectType.ContractExplodeRipple, new(44, 126, 224, 0), 2),
            Preset("Tracé cardiaque", "Ondulations", EffectType.HeartbeatTrace, new(255, 30, 70, 0), 2),
            Preset("Sucreries croquées", "Scènes", EffectType.SweetsBite, new(234, 143, 72, 0), 3),
            Preset("Lignes rouges diagonales", "Scènes", EffectType.RedDiagonalLines, new(178, 28, 38, 0), 2),
            Preset("Rose — ouverture et explosion", "Scènes", EffectType.RoseBloomExplosion, new(218, 24, 52, 0), 3),
            Preset("Silhouettes sous la lune", "Scènes", EffectType.MoonKissSilhouettes, new(224, 220, 184, 20), 5),
            Preset("Texte rouge sang — OH, DEAR LORD", "Textes", EffectType.BloodText, new(148, 0, 20, 0), 2.2),
            new EffectPreset { DisplayName = "Texte libre avec fondu", Category = "Textes", EffectType = EffectType.PixelText, Duration = 2, Color = new(255, 255, 255, 0), TextContent = "VOTRE TEXTE" },
            Preset("Cœur croqué", "Scènes", EffectType.BittenHeart, new(206, 8, 34, 0), 3),
            Preset("Explosion jaune en particules", "Scènes", EffectType.FlameParticleBurst, new(255, 220, 24, 0), 4),
            Preset("Liquide noir final", "Scènes", EffectType.BlackDrip, new(0, 0, 0, 0), 1.5)
        };

        private static EffectPreset Preset(string name, string category, EffectType effectType, RgbwColor color, double duration = 2)
            => new()
            {
                DisplayName = name,
                Category = category,
                EffectType = effectType,
                Color = color,
                Duration = duration
            };
    }
}
