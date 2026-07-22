using Liekinheitin.CreativeTool.Models;

namespace Liekinheitin.CreativeTool.Services;

public static class ShowTemplateService
{
    public static ShowProject CreateFinlandFortySeconds()
    {
        return new ShowProject
        {
            Name = "Liekinheitin — Fire, Ice & Toxic Love — 40s",
            Duration = 40,
            WallWidth = 128,
            WallHeight = 128,
            AudioPlaybackDuration = 40,
            AudioFadeOutDuration = 2,
            Tracks =
            {
                new Track
                {
                    Name = "Musique · 143,55 BPM",
                    Clips =
                    {
                        new TimelineClip
                        {
                            Name = "Finland — extrait 40 secondes",
                            StartTime = 0,
                            Duration = 40,
                            IsAudio = true,
                            Target = new TargetSelection { Type = TargetType.Selection },
                            Intensity = 0
                        }
                    }
                },
                new Track
                {
                    Name = "01 · Introduction neige",
                    Clips =
                    {
                        Clip("Entrée des premiers flocons", 1.16, 9.08, EffectType.Snowfall, new RgbwColor(200, 235, 255, 100), 0.9, 0.5),
                        Clip("Tourbillon sur la montée", 7.31, 2.93, EffectType.Snowfall, new RgbwColor(230, 248, 255, 125), 1, 1.35)
                    }
                },
                new Track
                {
                    Name = "02 · Silence et fracture de glace",
                    Clips =
                    {
                        Clip("Givre dans le creux musical", 10.24, 4.27, EffectType.Frost, new RgbwColor(78, 178, 255, 80), 0.94, 0.78),
                        Clip("Première fissure", 13.91, 0.32, EffectType.Fade, new RgbwColor(190, 235, 255, 120), 1, 1),
                        Clip("Fracture principale", 14.51, 0.38, EffectType.Fade, new RgbwColor(230, 250, 255, 170), 1, 1)
                    }
                },
                new Track
                {
                    Name = "03 · Montée du feu",
                    Clips =
                    {
                        Clip("Étincelle après la fracture", 14.51, 0.55, EffectType.Fade, new RgbwColor(255, 210, 80, 70), 1, 1),
                        Clip("Flammes en tension", 14.51, 6.49, EffectType.Fire, new RgbwColor(255, 70, 18, 0), 0.92, 1.05),
                        Clip("Déflagration vocale", 18.00, 3.00, EffectType.Fire, new RgbwColor(255, 105, 22, 0), 1, 1.55)
                    }
                },
                new Track
                {
                    Name = "04 · Amour toxique pulsé",
                    Clips =
                    {
                        Clip("Apparition du cœur", 19.64, 10.29, EffectType.ToxicHeart, new RgbwColor(255, 25, 130, 0), 0.88, 1.20),
                        Clip("Contamination sur les beats", 22.31, 7.62, EffectType.ToxicHeart, new RgbwColor(125, 255, 40, 0), 1, 2.39),
                        Clip("Pulsations rapprochées", 24.98, 4.95, EffectType.ToxicHeart, new RgbwColor(255, 32, 145, 0), 1, 2.39)
                    }
                },
                new Track
                {
                    Name = "05 · Rafale finale Eurovision",
                    Clips =
                    {
                        Clip("Collision feu contre glace", 29.51, 10.49, EffectType.FireIce, new RgbwColor(255, 255, 255, 0), 1, 1.42)
                    }
                },
                new Track
                {
                    Name = "06 · Accents mesurés dans la musique",
                    Clips =
                    {
                        Impact("Accent neige", 8.36, new RgbwColor(205, 242, 255, 120)),
                        Impact("Fissure 1", 13.91, new RgbwColor(205, 242, 255, 140)),
                        Impact("Fissure 2", 14.51, new RgbwColor(255, 225, 145, 110)),
                        Impact("Impact feu", 18.00, new RgbwColor(255, 115, 35, 90)),
                        Impact("Réponse feu", 18.41, new RgbwColor(255, 185, 65, 80)),
                        Impact("Relance", 20.22, new RgbwColor(255, 85, 35, 70)),
                        Impact("Entrée toxique", 22.31, new RgbwColor(255, 30, 135, 80)),
                        Impact("Pulsation 1", 24.98, new RgbwColor(130, 255, 55, 70)),
                        Impact("Pulsation 2", 25.82, new RgbwColor(255, 30, 135, 70)),
                        Impact("Pulsation 3", 27.05, new RgbwColor(130, 255, 55, 70)),
                        Impact("Départ finale", 29.51, new RgbwColor(255, 255, 255, 160)),
                        Impact("Finale 1", 31.97, new RgbwColor(180, 225, 255, 120)),
                        Impact("Finale 2", 34.44, new RgbwColor(255, 125, 45, 100)),
                        Impact("Finale 3", 36.08, new RgbwColor(200, 235, 255, 120)),
                        Impact("Finale 4", 38.36, new RgbwColor(255, 100, 35, 110)),
                        Impact("Flash final", 38.96, new RgbwColor(255, 255, 255, 190), 0.38)
                    }
                }
            }
        };
    }

    public static TimelineClip CreateMotif(string motifId, double startTime, int width, int height, out string trackName)
    {
        startTime = Math.Max(0, startTime);
        switch (motifId)
        {
            case "Snowfall":
                trackName = "Neige & glace";
                return Clip("Chute de flocons", startTime, 7, EffectType.Snowfall, new RgbwColor(205, 240, 255, 110), 1, 0.8);
            case "Frost":
                trackName = "Neige & glace";
                return Clip("Givre cristallin", startTime, 5, EffectType.Frost, new RgbwColor(85, 190, 255, 80), 1, 1);
            case "Fire":
                trackName = "Feu";
                return Clip("Flammes vivantes", startTime, 6, EffectType.Fire, new RgbwColor(255, 75, 18, 0), 1, 1);
            case "ToxicLove":
                trackName = "Amour toxique";
                return Clip("Cœur toxique", startTime, 5, EffectType.ToxicHeart, new RgbwColor(255, 25, 132, 0), 1, 1.1);
            case "FireIce":
                trackName = "Finale";
                return Clip("Collision feu-glace", startTime, 6, EffectType.FireIce, new RgbwColor(255, 255, 255, 0), 1, 1.15);
            case "Impact":
                trackName = "Impacts";
                return Impact("Impact Eurovision", startTime, new RgbwColor(255, 255, 255, 150));
            default:
                throw new ArgumentOutOfRangeException(nameof(motifId), motifId, "Scène artistique inconnue.");
        }
    }

    private static TimelineClip Impact(string name, double start, RgbwColor color, double duration = 0.28)
        => Clip(name, start, duration, EffectType.Fade, color, 1, 1);

    private static TimelineClip Clip(string name, double start, double duration, EffectType effect, RgbwColor color, double intensity, double speed)
        => new()
        {
            Name = name,
            StartTime = start,
            Duration = duration,
            EffectType = effect,
            Color = color,
            Intensity = intensity,
            Speed = speed
        };
}
