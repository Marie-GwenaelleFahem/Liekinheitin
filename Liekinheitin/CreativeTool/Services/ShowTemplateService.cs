using Liekinheitin.CreativeTool.Models;

namespace Liekinheitin.CreativeTool.Services;

public static class ShowTemplateService
{
    public static ShowProject CreateFinlandThirtySeconds()
    {
        return new ShowProject
        {
            Name = "Liekinheitin — Fire, Ice & Toxic Love",
            Duration = 30,
            WallWidth = 128,
            WallHeight = 128,
            AudioFadeOutDuration = 2,
            Tracks =
            {
                new Track { Name = "Musique" },
                new Track
                {
                    Name = "01 · Le froid tombe",
                    Clips =
                    {
                        Clip("Premiers flocons", 0, 8, EffectType.Snowfall, new RgbwColor(200, 235, 255, 100), 1, 0.72),
                        Clip("Tempête qui s'accumule", 5.8, 4.2, EffectType.Snowfall, new RgbwColor(225, 245, 255, 120), 0.88, 1.35)
                    }
                },
                new Track
                {
                    Name = "02 · La glace enferme",
                    Clips =
                    {
                        Clip("Givre depuis les bords", 8, 4.5, EffectType.Frost, new RgbwColor(80, 180, 255, 80), 1, 1),
                        Clip("Éclat de glace", 12.2, 0.32, EffectType.Fade, new RgbwColor(210, 245, 255, 140), 1, 1)
                    }
                },
                new Track
                {
                    Name = "03 · Liekinheitin allume le feu",
                    Clips =
                    {
                        Clip("Étincelle", 12.5, 0.45, EffectType.Fade, new RgbwColor(255, 210, 80, 60), 1, 1),
                        Clip("Flammes montantes", 12.7, 6.1, EffectType.Fire, new RgbwColor(255, 70, 18, 0), 1, 0.9)
                    }
                },
                new Track
                {
                    Name = "04 · Amour toxique",
                    Clips =
                    {
                        Clip("Cœur empoisonné", 18.4, 5.8, EffectType.ToxicHeart, new RgbwColor(255, 25, 130, 0), 1, 1.05),
                        Clip("Pulsation dangereuse", 21.2, 3, EffectType.ToxicHeart, new RgbwColor(125, 255, 40, 0), 0.86, 1.7)
                    }
                },
                new Track
                {
                    Name = "05 · Finale Eurovision",
                    Clips =
                    {
                        Clip("Feu contre glace", 24, 6, EffectType.FireIce, new RgbwColor(255, 255, 255, 0), 1, 1.15)
                    }
                },
                new Track
                {
                    Name = "06 · Accents",
                    Clips =
                    {
                        Clip("Impact glace", 8, 0.22, EffectType.Fade, new RgbwColor(210, 245, 255, 120), 1, 1),
                        Clip("Impact feu", 12.5, 0.22, EffectType.Fade, new RgbwColor(255, 190, 65, 80), 1, 1),
                        Clip("Impact cœur", 18.4, 0.18, EffectType.Fade, new RgbwColor(255, 30, 125, 60), 1, 1),
                        Clip("Flash final", 29.35, 0.28, EffectType.Fade, new RgbwColor(255, 255, 255, 180), 1, 1)
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
                return Clip("Impact Eurovision", startTime, 0.28, EffectType.Fade, new RgbwColor(255, 255, 255, 150), 1, 1);
            default:
                throw new ArgumentOutOfRangeException(nameof(motifId), motifId, "Scène artistique inconnue.");
        }
    }

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
