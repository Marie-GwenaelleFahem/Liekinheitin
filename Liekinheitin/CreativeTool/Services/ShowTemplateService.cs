using Liekinheitin.CreativeTool.Models;

namespace Liekinheitin.CreativeTool.Services;

public static class ShowTemplateService
{
    public static ShowProject CreateFinlandThirtySeconds()
    {
        const int width = 128;
        const int height = 128;
        return new ShowProject
        {
            Name = "Finland — Voyage boréal 30s",
            Duration = 30,
            WallWidth = width,
            WallHeight = height,
            AudioFadeOutDuration = 2,
            Tracks =
            {
                new Track { Name = "Musique" },
                new Track
                {
                    Name = "01 · Atmosphère boréale",
                    Clips =
                    {
                        Clip("Nuit profonde", 0, 30, EffectType.Breath, new RgbwColor(5, 18, 48, 0), 0.38, 0.28),
                        Clip("Aurore froide", 4, 10, EffectType.Wave, new RgbwColor(20, 125, 175, 0), 0.54, 0.42),
                        Clip("Aurore violette", 14, 10, EffectType.Wave, new RgbwColor(92, 32, 155, 0), 0.48, 0.34),
                        Clip("Retour au silence", 24, 6, EffectType.Fade, new RgbwColor(18, 45, 88, 0), 0.45, 0.25)
                    }
                },
                new Track
                {
                    Name = "02 · Voix — Cercles de glace",
                    Clips =
                    {
                        ShapeClip("Premier souffle", 2, 5, EffectType.Ripple, new RgbwColor(105, 225, 255, 0), 0.78, 0.52, Rings(width, height, 22, 3)),
                        ShapeClip("Réponse lointaine", 8, 5, EffectType.Ripple, new RgbwColor(172, 125, 255, 0), 0.72, 0.65, Rings(width, height, 37, 4)),
                        ShapeClip("Chœur boréal", 17, 7, EffectType.Breath, new RgbwColor(115, 235, 215, 0), 0.82, 0.48, Rings(width, height, 30, 5))
                    }
                },
                new Track
                {
                    Name = "03 · Violon — Fil vivant",
                    Clips =
                    {
                        ShapeClip("Archet ascendant", 5, 7, EffectType.Wave, new RgbwColor(245, 178, 82, 0), 0.86, 0.72, Ribbon(width, height, 0.2)),
                        ShapeClip("Archet suspendu", 12, 6, EffectType.Breath, new RgbwColor(255, 214, 142, 0), 0.72, 0.36, Ribbon(width, height, 1.6)),
                        ShapeClip("Dernière vibration", 21, 7, EffectType.Wave, new RgbwColor(235, 112, 62, 0), 0.84, 0.58, Ribbon(width, height, 2.8))
                    }
                },
                new Track
                {
                    Name = "04 · Cœur et impacts",
                    Clips =
                    {
                        ShapeClip("Battement humain", 10, 8, EffectType.Pulse, new RgbwColor(225, 42, 65, 0), 0.9, 1.15, Heartbeat(width, height)),
                        Clip("Impact blanc", 18, 0.22, EffectType.Fade, new RgbwColor(255, 255, 255, 80), 1, 1),
                        ShapeClip("Battement final", 24, 4, EffectType.Pulse, new RgbwColor(255, 70, 42, 0), 0.94, 0.9, Heartbeat(width, height))
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
            case "Aurora":
                trackName = "Ambiances";
                return Clip("Aurore boréale", startTime, 6, EffectType.Wave, new RgbwColor(22, 155, 190, 0), 0.62, 0.45);
            case "Voice":
                trackName = "Voix";
                return ShapeClip("Cercles de voix", startTime, 5, EffectType.Ripple, new RgbwColor(130, 225, 255, 0), 0.82, 0.58, Rings(width, height, Math.Min(width, height) * 0.18, 4));
            case "Violin":
                trackName = "Violon";
                return ShapeClip("Trait de violon", startTime, 6, EffectType.Wave, new RgbwColor(250, 176, 76, 0), 0.88, 0.7, Ribbon(width, height, startTime * 0.2));
            case "Heartbeat":
                trackName = "Cœur";
                return ShapeClip("Battement vivant", startTime, 4, EffectType.Pulse, new RgbwColor(235, 42, 68, 0), 0.94, 1.1, Heartbeat(width, height));
            case "Impact":
                trackName = "Impacts";
                return Clip("Impact lumineux", startTime, 0.35, EffectType.Fade, new RgbwColor(255, 255, 255, 90), 1, 1);
            case "Finale":
                trackName = "Finale";
                return Clip("Finale incandescent", startTime, 5, EffectType.Sparkle, new RgbwColor(255, 86, 32, 0), 0.95, 0.78);
            default:
                throw new ArgumentOutOfRangeException(nameof(motifId), motifId, "Motif artistique inconnu.");
        }
    }
    private static TimelineClip Clip(string name, double start, double duration, EffectType effect, RgbwColor color, double intensity, double speed)
        => new() { Name = name, StartTime = start, Duration = duration, EffectType = effect, Color = color, Intensity = intensity, Speed = speed };

    private static TimelineClip ShapeClip(string name, double start, double duration, EffectType effect, RgbwColor color, double intensity, double speed, List<int> ids)
        => new()
        {
            Name = name, StartTime = start, Duration = duration, EffectType = effect, Color = color,
            Intensity = intensity, Speed = speed, Target = new TargetSelection { Type = TargetType.Selection, EntityIds = ids }
        };

    private static List<int> Rings(int width, int height, double radius, int count)
    {
        var ids = new List<int>();
        var cx = (width - 1) / 2.0;
        var cy = (height - 1) / 2.0;
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var distance = Math.Sqrt(Math.Pow(x - cx, 2) + Math.Pow(y - cy, 2));
            for (var ring = 0; ring < count; ring++)
            {
                if (Math.Abs(distance - radius - (ring * 7)) <= 1.8) { ids.Add((y * width) + x); break; }
            }
        }
        return ids;
    }

    private static List<int> Ribbon(int width, int height, double phase)
    {
        var ids = new List<int>();
        for (var x = 3; x < width - 3; x++)
        {
            var y = (height / 2.0) + (Math.Sin((x / 11.0) + phase) * 20) + (Math.Sin((x / 4.7) - phase) * 4);
            for (var thickness = -2; thickness <= 2; thickness++)
            {
                var py = Math.Clamp((int)Math.Round(y) + thickness, 0, height - 1);
                ids.Add((py * width) + x);
            }
        }
        return ids.Distinct().ToList();
    }

    private static List<int> Heartbeat(int width, int height)
    {
        var ids = new List<int>();
        var baseline = height / 2;
        for (var x = 4; x < width - 4; x++)
        {
            var normalized = (x - 4.0) / (width - 8.0);
            var pulse = normalized switch
            {
                < 0.28 => Math.Sin(normalized * 28) * 2,
                < 0.38 => -(normalized - 0.28) * 70,
                < 0.46 => 7 - ((normalized - 0.38) * 330),
                < 0.54 => -19 + ((normalized - 0.46) * 520),
                < 0.62 => 23 - ((normalized - 0.54) * 290),
                _ => Math.Sin(normalized * 24) * 2
            };
            var y = Math.Clamp((int)Math.Round(baseline - pulse), 0, height - 1);
            for (var thickness = -1; thickness <= 1; thickness++) ids.Add((Math.Clamp(y + thickness, 0, height - 1) * width) + x);
        }
        return ids.Distinct().ToList();
    }
}
