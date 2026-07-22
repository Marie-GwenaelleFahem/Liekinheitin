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
                    Name = "06 · Formes de la finale",
                    Clips =
                    {
                        ShapeClip("Éclat radial", 29.51, 2.46, EffectType.Pulse, new RgbwColor(210, 245, 255, 100), 1, 2.39, RadialBurst(128, 128)),
                        ShapeClip("Lances de glace", 31.97, 2.47, EffectType.Breath, new RgbwColor(80, 195, 255, 80), 1, 1.25, IceShards(128, 128)),
                        ShapeClip("Couronne de flammes", 34.44, 3.29, EffectType.Chase, new RgbwColor(255, 92, 24, 0), 1, 1.45, FlameCrown(128, 128)),
                        ShapeClip("Cœur toxique brisé", 36.08, 2.28, EffectType.Pulse, new RgbwColor(255, 25, 142, 0), 1, 2.39, BrokenHeart(128, 128)),
                        ShapeClip("Emblème Liekinheitin", 38.36, 1.64, EffectType.Fade, new RgbwColor(255, 255, 255, 180), 1, 1, FinalEmblem(128, 128))
                    }
                },
                new Track
                {
                    Name = "07 · Accents mesurés dans la musique",
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

    private static TimelineClip ShapeClip(string name, double start, double duration, EffectType effect, RgbwColor color, double intensity, double speed, List<int> entityIds)
    {
        var clip = Clip(name, start, duration, effect, color, intensity, speed);
        clip.Target = new TargetSelection { Type = TargetType.Selection, EntityIds = entityIds };
        return clip;
    }

    private static List<int> RadialBurst(int width, int height)
    {
        var ids = new HashSet<int>();
        var cx = (width - 1) / 2.0;
        var cy = (height - 1) / 2.0;
        for (var ray = 0; ray < 16; ray++)
        {
            var angle = ray * Math.PI * 2 / 16;
            AddLine(ids, cx + Math.Cos(angle) * 9, cy + Math.Sin(angle) * 9,
                cx + Math.Cos(angle) * 58, cy + Math.Sin(angle) * 58, width, height, 2);
        }
        return ids.ToList();
    }

    private static List<int> IceShards(int width, int height)
    {
        var ids = new HashSet<int>();
        for (var shard = 0; shard < 9; shard++)
        {
            var left = 3 + (shard * 15);
            var right = Math.Min(width - 2, left + 12);
            var tipX = left + 3 + ((shard * 7) % 7);
            var tipY = 30 + ((shard * 23) % 72);
            AddLine(ids, left, 0, tipX, tipY, width, height, 2);
            AddLine(ids, right, 0, tipX, tipY, width, height, 2);
            AddLine(ids, left, 0, right, 0, width, height, 1);
        }
        return ids.ToList();
    }

    private static List<int> FlameCrown(int width, int height)
    {
        var ids = new HashSet<int>();
        var baseline = height - 10;
        var flameWidth = width / 9.0;
        for (var flame = 0; flame < 9; flame++)
        {
            var left = flame * flameWidth;
            var right = (flame + 1) * flameWidth;
            var peakX = (left + right) / 2;
            var peakY = 35 + ((flame * 19) % 44);
            AddLine(ids, left, baseline, peakX, peakY, width, height, 3);
            AddLine(ids, peakX, peakY, right, baseline, width, height, 3);
        }
        AddLine(ids, 0, baseline, width - 1, baseline, width, height, 2);
        return ids.ToList();
    }

    private static List<int> BrokenHeart(int width, int height)
    {
        var ids = new HashSet<int>();
        for (var y = 12; y < height - 10; y++)
        for (var x = 12; x < width - 12; x++)
        {
            var nx = (x - width / 2.0) / (width * 0.29);
            var ny = (height / 2.0 - y) / (height * 0.29);
            var equation = Math.Pow((nx * nx) + (ny * ny) - 1, 3) - (nx * nx * ny * ny * ny);
            if (Math.Abs(equation) < 0.065) ids.Add((y * width) + x);
        }
        AddLine(ids, width * 0.52, height * 0.29, width * 0.45, height * 0.48, width, height, 2);
        AddLine(ids, width * 0.45, height * 0.48, width * 0.55, height * 0.61, width, height, 2);
        AddLine(ids, width * 0.55, height * 0.61, width * 0.48, height * 0.78, width, height, 2);
        return ids.ToList();
    }

    private static List<int> FinalEmblem(int width, int height)
    {
        var ids = new HashSet<int>();
        var cx = width / 2.0;
        var cy = height / 2.0;
        AddLine(ids, cx, 8, width - 16, cy, width, height, 3);
        AddLine(ids, width - 16, cy, cx, height - 8, width, height, 3);
        AddLine(ids, cx, height - 8, 16, cy, width, height, 3);
        AddLine(ids, 16, cy, cx, 8, width, height, 3);
        AddLine(ids, cx, 18, cx, height - 18, width, height, 2);
        AddLine(ids, 24, cy, width - 24, cy, width, height, 2);
        return ids.ToList();
    }

    private static void AddLine(HashSet<int> ids, double x1, double y1, double x2, double y2, int width, int height, int thickness)
    {
        var steps = Math.Max(1, (int)Math.Ceiling(Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1))));
        for (var step = 0; step <= steps; step++)
        {
            var progress = step / (double)steps;
            var x = (int)Math.Round(x1 + ((x2 - x1) * progress));
            var y = (int)Math.Round(y1 + ((y2 - y1) * progress));
            for (var dy = -thickness; dy <= thickness; dy++)
            for (var dx = -thickness; dx <= thickness; dx++)
            {
                var px = x + dx;
                var py = y + dy;
                if (px >= 0 && px < width && py >= 0 && py < height) ids.Add((py * width) + px);
            }
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
