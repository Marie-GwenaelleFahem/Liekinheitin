using System;
using System.Collections.Generic;
using System.Linq;
using Liekinheitin.CreativeTool.Models;

namespace Liekinheitin.CreativeTool.Services
{
    internal static class CinematicEffectsRenderer
    {
        public static bool TryApply(IDictionary<int, RgbwColor> colors, TimelineClip clip, double localTime, int width, int height)
        {
            var progress = Math.Clamp(localTime / Math.Max(0.001, clip.Duration), 0, 1);
            switch (clip.EffectType)
            {
                case EffectType.SweetsBite: DrawSweets(colors, width, height, progress); return true;
                case EffectType.RedDiagonalLines: DrawDiagonalLines(colors, width, height, progress); return true;
                case EffectType.RoseBloomExplosion: DrawRose(colors, width, height, progress); return true;
                case EffectType.MoonKissSilhouettes: DrawMoonKiss(colors, width, height, progress); return true;
                case EffectType.BloodText: DrawBloodText(colors, width, height, progress); return true;
                case EffectType.BittenHeart: DrawBittenHeart(colors, width, height, progress); return true;
                case EffectType.BlackDrip: DrawBlackDrip(colors, width, height, progress); return true;
                case EffectType.FlameParticleBurst: DrawFlameParticleBurst(colors, clip, localTime, width, height); return true;
                case EffectType.PersistentRedStars: DrawPersistentRedStars(colors, clip, localTime, width, height); return true;
                case EffectType.PixelText: DrawPixelText(colors, clip, progress, width, height); return true;
                case EffectType.AppleSnakeFinale: DrawWhiteAppleFormation(colors, clip, localTime, width, height); return true;
                case EffectType.AppleSnakeApproach: DrawGreenSnakeApproach(colors, localTime, width, height); return true;
                case EffectType.AppleBiteJuice: DrawSnakeCoilAndBite(colors, localTime, width, height); return true;
                case EffectType.WhiteRisingLines: DrawWhiteAppleFormation(colors, clip, localTime, width, height); return true;
                case EffectType.WhiteAppleBounce: DrawWhiteAppleBounce(colors, clip, localTime, width, height); return true;
                case EffectType.FallingSmallApples: DrawFallingSmallApples(colors, clip, localTime, width, height); return true;
                case EffectType.GreenSnakeCrawl: DrawGreenSnakeCrawl(colors, clip, localTime, width, height); return true;
                case EffectType.SnakeCoilApple: DrawSnakeCoilApple(colors, clip, localTime, width, height); return true;
                case EffectType.WhiteJuiceCurtain: DrawWhiteJuiceCurtain(colors, clip, localTime, width, height); return true;
                case EffectType.SpiralBloodStairs: DrawSpiralBloodStairs(colors, clip, localTime, width, height); return true;
                case EffectType.CollapsingBloodStairs: DrawCollapsingBloodStairs(colors, clip, localTime, width, height); return true;
                case EffectType.GrowingThornVines: DrawGrowingThornVines(colors, clip, localTime, width, height); return true;
                case EffectType.BloomingRedRoses: DrawBloomingRedRoses(colors, clip, localTime, width, height); return true;
                case EffectType.RedRoseLiquidFade: DrawRedRoseLiquidFade(colors, clip, localTime, width, height); return true;
                case EffectType.BloodRectanglesToStairs: DrawBloodRectanglesToStairs(colors, clip, localTime, width, height); return true;
                case EffectType.FallingRosePetals: DrawFallingRosePetals(colors, clip, localTime, width, height); return true;
                default: return false;
            }
        }

        private static void DrawSweets(IDictionary<int, RgbwColor> colors, int width, int height, double progress)
        {
            var candies = new (int X, int Y, int R, RgbwColor Color, int Kind)[]
            {
                (18, 27, 8, new(156, 76, 28, 0), 0), (42, 20, 7, new(255, 78, 132, 0), 1),
                (68, 30, 9, new(118, 62, 22, 0), 2), (96, 20, 7, new(255, 184, 38, 0), 1),
                (113, 43, 8, new(184, 86, 34, 0), 0), (29, 65, 9, new(248, 102, 62, 0), 2),
                (58, 57, 7, new(255, 212, 74, 0), 1), (88, 67, 10, new(138, 66, 28, 0), 0),
                (110, 86, 7, new(244, 72, 116, 0), 1), (48, 94, 9, new(172, 78, 30, 0), 2),
                (76, 103, 7, new(255, 156, 36, 0), 1), (18, 105, 8, new(124, 58, 22, 0), 0)
            };
            var reveal = Math.Clamp(progress / 0.26, 0, 1);
            var bite = Math.Clamp((progress - 0.34) / 0.66, 0, 1);
            for (var index = 0; index < candies.Length; index++)
            {
                if (index / (double)candies.Length > reveal) continue;
                var candy = candies[index];
                for (var y = candy.Y - candy.R - 3; y <= candy.Y + candy.R + 3; y++)
                for (var x = candy.X - candy.R - 4; x <= candy.X + candy.R + 4; x++)
                {
                    var dx = x - candy.X; var dy = y - candy.Y;
                    var body = candy.Kind == 0 ? Math.Abs(dx) <= candy.R && Math.Abs(dy) <= candy.R * 0.62
                        : candy.Kind == 1 ? (dx * dx) + (dy * dy) <= candy.R * candy.R
                        : Math.Pow(dx / (double)candy.R, 2) + Math.Pow(dy / (double)(candy.R * 0.68), 2) <= 1;
                    var wrapper = candy.Kind == 1 && Math.Abs(dx) > candy.R && Math.Abs(dx) <= candy.R + 4 && Math.Abs(dy) <= 3;
                    if (!body && !wrapper) continue;
                    var biteRadius = 2.2 + (bite * candy.R * 1.25);
                    var biteX = candy.X + candy.R * 0.72; var biteY = candy.Y - candy.R * 0.18;
                    if (bite > 0 && Math.Pow(x - biteX, 2) + Math.Pow(y - biteY, 2) < biteRadius * biteRadius) continue;
                    Set(colors, width, height, x, y, wrapper ? new RgbwColor(255, 212, 110, 0) : candy.Color);
                }
            }
        }

        private static void DrawDiagonalLines(IDictionary<int, RgbwColor> colors, int width, int height, double progress)
        {
            var red = new RgbwColor(180, 12, 28, 0);
            for (var line = -2; line < 9; line++)
            {
                var offset = (int)Math.Round((line * 22) + (progress * 48));
                for (var y = 0; y < height; y++)
                {
                    var x = offset + (y / 2);
                    Set(colors, width, height, x, y, red);
                }
            }
        }

        private static void DrawRose(IDictionary<int, RgbwColor> colors, int width, int height, double progress)
        {
            var cx = width / 2.0; var cy = height * 0.53;
            if (progress < 0.82)
            {
                var bloom = 0.14 + (0.86 * Smooth(progress / 0.82));
                for (var petal = 0; petal < 9; petal++)
                {
                    var angle = petal * Math.PI * 2 / 9;
                    var px = cx + Math.Cos(angle) * 18 * bloom;
                    var py = cy + Math.Sin(angle) * 14 * bloom;
                    DrawEllipse(colors, width, height, px, py, 11 * bloom, 7 * bloom, new(214, 12, 44, 0));
                }
                DrawEllipse(colors, width, height, cx, cy, 10 * bloom, 10 * bloom, new(255, 38, 62, 0));
            }
            else
            {
                var explosion = (progress - 0.82) / 0.18;
                for (var particle = 0; particle < 72; particle++)
                {
                    var angle = particle * 2.399963;
                    var radius = (8 + (particle % 13)) * (1 + explosion * 4.2);
                    Set(colors, width, height, (int)(cx + Math.Cos(angle) * radius), (int)(cy + Math.Sin(angle) * radius), particle % 3 == 0 ? new(255, 54, 68, 0) : new(166, 6, 32, 0));
                }
            }
        }

        private static void DrawMoonKiss(IDictionary<int, RgbwColor> colors, int width, int height, double progress)
        {
            var moonX = width / 2; var moonY = (int)(height * 0.40); var moonR = Math.Min(width, height) * 0.27;
            for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
            {
                var d = Math.Sqrt(Math.Pow(x - moonX, 2) + Math.Pow(y - moonY, 2));
                if (d <= moonR) Set(colors, width, height, x, y, d > moonR - 2 ? new(132, 148, 172, 0) : new(224, 219, 177, 20));
            }
            var approach = Smooth(progress);
            var leftX = 39 + (19 * approach); var rightX = 89 - (19 * approach);
            DrawPerson(colors, width, height, leftX, 55, true, progress);
            DrawPerson(colors, width, height, rightX, 55, false, progress);
        }

        private static void DrawPerson(IDictionary<int, RgbwColor> colors, int width, int height, double x, int headY, bool left, double progress)
        {
            var black = new RgbwColor(2, 2, 4, 0); var lean = (left ? 1 : -1) * progress * 3;
            FillCircle(colors, width, height, x + lean, headY, 7, black);
            for (var y = headY + 6; y < height; y++)
            {
                var half = 4 + ((y - headY) * 0.12);
                for (var px = (int)(x - half); px <= x + half; px++) Set(colors, width, height, px, y, black);
            }
        }

        private static void DrawBloodText(IDictionary<int, RgbwColor> colors, int width, int height, double progress)
        {
            const string text = "OH, DEAR LORD";
            var startX = (width - (text.Length * 6)) / 2; var startY = height / 2 - 4;
            var visible = (int)Math.Ceiling(text.Length * Math.Clamp(progress / 0.62, 0, 1));
            for (var i = 0; i < visible; i++) DrawGlyph(colors, width, height, text[i], startX + (i * 6), startY, new(148, 0, 20, 0));
            var drip = Math.Clamp((progress - 0.62) / 0.38, 0, 1);
            for (var i = 0; i < text.Length; i += 2)
            {
                var length = (int)(drip * (3 + (i % 5)));
                for (var y = 0; y < length; y++) Set(colors, width, height, startX + (i * 6) + 2, startY + 8 + y, new(112, 0, 14, 0));
            }
        }

        private static void DrawBittenHeart(IDictionary<int, RgbwColor> colors, int width, int height, double progress)
        {
            var cx = width / 2.0; var cy = height / 2.0; var bite = Math.Clamp((progress - 0.18) / 0.82, 0, 1);
            for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
            {
                var nx = (x - cx) / 25.0; var ny = (cy - y) / 25.0;
                var heart = Math.Pow((nx * nx) + (ny * ny) - 1, 3) - (nx * nx * ny * ny * ny) <= 0;
                if (!heart) continue;
                var biteR = 3 + (bite * 25); var bx = cx + 20; var by = cy - 12;
                if (bite > 0 && Math.Pow(x - bx, 2) + Math.Pow(y - by, 2) < biteR * biteR) continue;
                Set(colors, width, height, x, y, new(206, 8, 34, 0));
            }
        }

        private static void DrawBlackDrip(IDictionary<int, RgbwColor> colors, int width, int height, double progress)
        {
            var black = new RgbwColor(0, 0, 0, 0);
            for (var x = 0; x < width; x++)
            {
                var variation = 0.70 + (0.30 * ((Math.Sin(x * 1.73) + 1) * 0.5));
                var drip = progress * progress * height * variation;
                if (x % 11 < 3) drip += progress * 22;
                for (var y = 0; y <= Math.Min(height - 1, drip); y++) Set(colors, width, height, x, y, black);
            }
        }

        private static void DrawAppleSnakeFinale(
            IDictionary<int, RgbwColor> colors,
            double localTime,
            double duration,
            int width,
            int height)
        {
            var cx = width * 0.5;
            var cy = height * 0.48;
            var white = new RgbwColor(238, 246, 255, 48);
            var red = new RgbwColor(232, 12, 34, 0);
            var darkRed = new RgbwColor(142, 0, 18, 0);

            // 45-48 s : le liquide blanc remonte, puis se resserre vers le centre.
            if (localTime < 3)
            {
                var rise = Smooth(Math.Clamp(localTime / 1.85, 0, 1));
                var gather = Smooth(Math.Clamp((localTime - 1.35) / 1.65, 0, 1));
                for (var x = 0; x < width; x++)
                {
                    var wave = (Math.Sin((x * 0.19) + (localTime * 4.2)) * 4)
                               + (Math.Sin((x * 0.053) - (localTime * 2.1)) * 3);
                    var top = height - (rise * height * (0.78 + (0.08 * Math.Sin(x * 1.17)))) + wave;
                    var distanceFromCenter = Math.Abs(x - cx) / Math.Max(1, width * 0.5);
                    top += gather * height * 0.76 * Math.Pow(distanceFromCenter, 0.72);
                    for (var y = Math.Max(0, (int)Math.Round(top)); y < height; y++)
                    {
                        var centerPull = 1 - gather + (gather * Math.Max(0, 1 - distanceFromCenter));
                        if (centerPull > 0.16) Set(colors, width, height, x, y, white);
                    }
                }

                for (var stream = -2; stream <= 2; stream++)
                {
                    var streamX = cx + (stream * 7 * (1 - gather));
                    DrawLine(colors, width, height, streamX, height - 1, cx + (stream * 2), cy + 16, white);
                }
            }

            var appleReveal = Smooth(Math.Clamp((localTime - 1.8) / 1.2, 0, 1));
            var biteTime = 8.5; // 53,5 s dans un clip qui commence a 45 s.
            var afterBite = Math.Clamp((localTime - biteTime) / Math.Max(0.001, duration - biteTime), 0, 1);
            var finaleFade = 1 - Smooth(Math.Clamp((afterBite - 0.38) / 0.62, 0, 1));

            if (appleReveal > 0)
            {
                var appleColor = ScaleColor(red, appleReveal * finaleFade);
                var appleShadow = ScaleColor(darkRed, appleReveal * finaleFade);
                var appleRadius = 18 * appleReveal;
                DrawEllipse(colors, width, height, cx - 8, cy + 3, appleRadius * 0.72, appleRadius, appleColor);
                DrawEllipse(colors, width, height, cx + 8, cy + 3, appleRadius * 0.72, appleRadius, appleColor);
                DrawEllipse(colors, width, height, cx, cy + 11, appleRadius * 0.86, appleRadius * 0.68, appleShadow);
                DrawLine(colors, width, height, cx, cy - 14, cx + 3, cy - 25, ScaleColor(new RgbwColor(112, 52, 12, 0), finaleFade));
                DrawEllipse(colors, width, height, cx + 9, cy - 21, 8, 3.5, ScaleColor(new RgbwColor(48, 184, 72, 0), finaleFade));

                if (localTime >= biteTime)
                {
                    FillCircle(colors, width, height, cx - 17, cy - 2, 7, new RgbwColor(0, 0, 0, 0));
                    FillCircle(colors, width, height, cx - 14, cy - 9, 5, new RgbwColor(0, 0, 0, 0));
                }
            }

            // 48-53,5 s : le serpent entre a gauche et revele progressivement ses anneaux.
            var snakeProgress = Smooth(Math.Clamp((localTime - 3) / 5.5, 0, 1));
            if (snakeProgress > 0 && finaleFade > 0)
            {
                const int segments = 150;
                var visibleSegments = Math.Max(2, (int)Math.Round(segments * snakeProgress));
                (double X, double Y) Point(int index)
                {
                    var t = index / (double)(segments - 1);
                    if (t < 0.48)
                    {
                        var approach = t / 0.48;
                        return (-12 + ((cx - 30 + 12) * approach), cy + 26 + (Math.Sin(approach * Math.PI * 3) * 8));
                    }
                    var coil = (t - 0.48) / 0.52;
                    var angle = Math.PI * (0.62 + (coil * 2.15));
                    var radiusX = 29 - (coil * 7);
                    var radiusY = 24 - (coil * 5);
                    return (cx + (Math.Cos(angle) * radiusX), cy + 4 + (Math.Sin(angle) * radiusY));
                }

                var snakeColor = ScaleColor(white, finaleFade);
                var previous = Point(0);
                for (var segment = 1; segment < visibleSegments; segment++)
                {
                    var current = Point(segment);
                    DrawLine(colors, width, height, previous.X, previous.Y, current.X, current.Y, snakeColor);
                    DrawLine(colors, width, height, previous.X, previous.Y + 1, current.X, current.Y + 1, snakeColor);
                    DrawLine(colors, width, height, previous.X, previous.Y - 1, current.X, current.Y - 1, snakeColor);
                    previous = current;
                }
                FillCircle(colors, width, height, previous.X, previous.Y, 4, snakeColor);
                var eye = ScaleColor(new RgbwColor(220, 0, 20, 0), finaleFade);
                Set(colors, width, height, (int)Math.Round(previous.X - 1), (int)Math.Round(previous.Y - 2), eye);
            }

            // Apres la morsure : le jus rouge tombe et toutes les lumieres s'eteignent vers le noir.
            if (localTime >= biteTime)
            {
                var fall = Smooth(afterBite);
                for (var drop = 0; drop < 13; drop++)
                {
                    var delay = drop * 0.045;
                    var dropProgress = Math.Clamp((fall - delay) / Math.Max(0.08, 1 - delay), 0, 1);
                    var x = cx - 16 + (drop * 2.7) + (Math.Sin(drop * 2.3) * 3);
                    var y = cy + 12 + (dropProgress * height * (0.62 + ((drop % 4) * 0.06)));
                    var length = 3 + (int)Math.Round(dropProgress * (8 + (drop % 5)));
                    DrawLine(colors, width, height, x, y - length, x, y, ScaleColor(drop % 3 == 0 ? red : darkRed, finaleFade));
                }
            }
        }

        private static void DrawWhiteAppleFormation(
            IDictionary<int, RgbwColor> colors, TimelineClip clip, double localTime, int width, int height)
        {
            var cx = width * 0.5;
            var cy = height * 0.48;
            // Lorsque ce clip termine le spectacle, ses deux dernieres secondes servent au
            // fondu general. La formation elle-meme garde donc sa duree courte d'origine,
            // puis la pomme achevee reste en place pendant le fondu.
            var formationDuration = clip.Duration > 2.5 ? clip.Duration - 2.0 : clip.Duration;
            var progress = Math.Clamp(localTime / Math.Max(0.001, formationDuration), 0, 1);
            var whiteCore = ScaleColor(clip.Color, clip.Intensity);
            // Un point unique au centre grossit continuellement jusqu'a devenir la pomme.
            // La courbe douce evite tout saut de taille ou effet de flash.
            DrawSolidWhiteApple(colors, width, height, cx, cy, Smooth(progress), whiteCore);
        }

        private static void DrawSolidWhiteApple(
            IDictionary<int, RgbwColor> colors, int width, int height,
            double cx, double cy, double reveal, RgbwColor white)
        {
            if (reveal <= 0.015)
            {
                FillCircle(colors, width, height, cx, cy, 1, white);
                return;
            }

            var scale = 0.025 + (0.975 * reveal);
            var color = white;
            var leftX = cx - (8 * scale);
            var rightX = cx + (8 * scale);
            var bodyY = cy + (5 * scale);
            FillEllipse(colors, width, height, leftX, bodyY, 16 * scale, 23 * scale, color);
            FillEllipse(colors, width, height, rightX, bodyY, 16 * scale, 23 * scale, color);
            FillEllipse(colors, width, height, cx, cy + (14 * scale), 21 * scale, 15 * scale, color);

            // Petite encoche superieure, puis tige et feuille elles aussi entierement blanches.
            FillCircle(colors, width, height, cx, cy - (17 * scale), Math.Max(1, (int)Math.Round(4 * scale)), new RgbwColor(0, 0, 0, 0));
            DrawLine(colors, width, height, cx, cy - (14 * scale), cx + (2 * scale), cy - (26 * scale), color);
            FillEllipse(colors, width, height, cx + (10 * scale), cy - (23 * scale), 9 * scale, 4 * scale, color);
        }

        private static void DrawWhiteAppleBounce(
            IDictionary<int, RgbwColor> colors, TimelineClip clip,
            double localTime, int width, int height)
        {
            var cx = width * 0.5;
            var groundY = height - 30.0;
            var startY = -32.0;
            var motionDuration = clip.Duration > 2.5 ? clip.Duration - 2.0 : clip.Duration;
            var progress = Math.Clamp(localTime / Math.Max(0.001, motionDuration), 0, 1);

            double cy;
            if (progress < 0.42)
            {
                // Chute acceleree depuis le haut jusqu'au premier contact avec le sol.
                var fall = progress / 0.42;
                cy = startY + ((groundY - startY) * fall * fall);
            }
            else if (progress < 0.70)
            {
                // Premier rebond, large et souple.
                var bounce = (progress - 0.42) / 0.28;
                cy = groundY - (34 * 4 * bounce * (1 - bounce));
            }
            else if (progress < 0.88)
            {
                // Deuxieme rebond, plus court et moins haut.
                var bounce = (progress - 0.70) / 0.18;
                cy = groundY - (16 * 4 * bounce * (1 - bounce));
            }
            else
            {
                cy = groundY;
            }

            var appearance = Smooth(Math.Clamp(progress / 0.10, 0, 1));
            var white = ScaleColor(clip.Color, clip.Intensity * appearance);
            DrawSolidWhiteApple(colors, width, height, cx, cy, 1, white);
        }

        private static void DrawFallingSmallApples(
            IDictionary<int, RgbwColor> colors, TimelineClip clip,
            double localTime, int width, int height)
        {
            var progress = Math.Clamp(localTime / Math.Max(0.001, clip.Duration), 0, 1);
            var red = new RgbwColor(232, 12, 38, 0);
            var darkRed = new RgbwColor(134, 0, 18, 0);

            // Pommes secondaires : vitesses, departs et derives differents.
            const int appleCount = 17;
            for (var apple = 0; apple < appleCount; apple++)
            {
                var phase = Noise(apple, 20) * 0.34;
                var fall = Math.Clamp((progress - phase) / Math.Max(0.18, 1 - phase), 0, 1);
                var easedFall = fall * fall;
                var startX = 5 + (Noise(apple, 21) * (width - 10));
                var drift = (Noise(apple, 22) - 0.5) * 46;
                var x = startX + (drift * easedFall) + (Math.Sin((fall * Math.PI * 3) + apple) * 3);
                var y = -10 - (Noise(apple, 23) * 42) + (easedFall * (height + 62));
                DrawMiniApple(colors, width, height, x, y, 3 + (apple % 3), red, darkRed);
            }

            // Pomme principale : elle termine sa chute exactement au centre a 48,5 s.
            var mainFall = Smooth(progress);
            var centerX = width * 0.5;
            var centerY = height * 0.50;
            var mainY = -14 + ((centerY + 14) * mainFall);
            DrawMiniApple(colors, width, height, centerX, mainY, 12, red, darkRed);
        }

        private static void DrawGreenSnakeCrawl(
            IDictionary<int, RgbwColor> colors, TimelineClip clip,
            double localTime, int width, int height)
        {
            var progress = Smooth(Math.Clamp(localTime / Math.Max(0.001, clip.Duration), 0, 1));
            var cx = width * 0.5;
            var cy = height * 0.50;

            var headX = -8 + ((cx - 18 + 8) * progress);
            var headY = cy + 16 + (Math.Sin(progress * Math.PI * 4) * 5);
            DrawCrawlingSnake(colors, width, height, headX, headY, progress);
        }

        private static void DrawSnakeCoilApple(
            IDictionary<int, RgbwColor> colors, TimelineClip clip,
            double localTime, int width, int height)
        {
            var coilDuration = Math.Min(5.0, Math.Max(0.001, clip.Duration));
            var progress = Smooth(Math.Clamp(localTime / coilDuration, 0, 1));
            var cx = width * 0.5;
            var cy = height * 0.50;
            DrawMiniApple(colors, width, height, cx, cy, 12, new RgbwColor(232, 12, 38, 0), new RgbwColor(134, 0, 18, 0));
            DrawCoiledSnake(colors, width, height, cx, cy, progress, false);
        }

        private static void DrawWhiteJuiceCurtain(
            IDictionary<int, RgbwColor> colors, TimelineClip clip,
            double localTime, int width, int height)
        {
            var progress = Math.Clamp(localTime / Math.Max(0.001, clip.Duration), 0, 1);
            var cx = width * 0.5;
            var cy = height * 0.50;
            DrawMiniApple(colors, width, height, cx, cy, 12, new RgbwColor(232, 12, 38, 0), new RgbwColor(134, 0, 18, 0), bitten: true);
            DrawCoiledSnake(colors, width, height, cx, cy, 1, true);

            var white = new RgbwColor(248, 252, 255, 92);
            var juice = Smooth(Math.Clamp(progress / 0.66, 0, 1));
            var spread = 7 + (juice * width * 0.62);
            for (var stream = 0; stream < 39; stream++)
            {
                var normalized = (stream / 38.0) - 0.5;
                if (Math.Abs(normalized) * width > spread) continue;
                var delay = Math.Abs(normalized) * 0.32 + ((stream % 5) * 0.012);
                var fall = Smooth(Math.Clamp((juice - delay) / Math.Max(0.08, 1 - delay), 0, 1));
                var x = cx + (normalized * width) + (Math.Sin(stream * 1.73) * 2.5);
                var startY = cy - 3 + (Math.Abs(normalized) * 30);
                var endY = startY + (fall * (height - startY + 12));
                var thickness = 1 + (stream % 3);
                for (var offset = -thickness; offset <= thickness; offset++)
                    DrawLine(colors, width, height, x + offset, startY, x + offset, endY, ScaleColor(white, 0.65 + (0.35 * fall)));
            }

            // Le noir descend comme un rideau apres que le jus a envahi le mur.
            var blackout = Smooth(Math.Clamp((progress - 0.62) / 0.38, 0, 1));
            var blackBottom = (int)Math.Round(blackout * height);
            for (var y = 0; y < blackBottom; y++)
            for (var x = 0; x < width; x++)
                Set(colors, width, height, x, y, new RgbwColor(0, 0, 0, 0));
        }

        private static void DrawMiniApple(
            IDictionary<int, RgbwColor> colors, int width, int height,
            double cx, double cy, int radius, RgbwColor red, RgbwColor shade, bool bitten = false)
        {
            FillEllipse(colors, width, height, cx - (radius * 0.34), cy, radius * 0.72, radius, red);
            FillEllipse(colors, width, height, cx + (radius * 0.34), cy, radius * 0.72, radius, red);
            FillEllipse(colors, width, height, cx, cy + (radius * 0.42), radius * 0.82, radius * 0.62, shade);
            DrawLine(colors, width, height, cx, cy - (radius * 0.72), cx + 1, cy - (radius * 1.28), new RgbwColor(102, 54, 18, 0));
            if (radius >= 6) FillEllipse(colors, width, height, cx + (radius * 0.45), cy - (radius * 1.08), radius * 0.42, radius * 0.18, new RgbwColor(42, 188, 70, 0));
            if (!bitten) return;
            FillCircle(colors, width, height, cx - (radius * 0.72), cy - (radius * 0.18), Math.Max(2, radius / 3), new RgbwColor(0, 0, 0, 0));
        }

        private static void DrawCrawlingSnake(
            IDictionary<int, RgbwColor> colors, int width, int height,
            double headX, double headY, double progress)
        {
            var green = new RgbwColor(34, 214, 78, 0);
            var dark = new RgbwColor(7, 105, 36, 0);
            var previousX = -15.0;
            var previousY = headY;
            const int segments = 46;
            for (var segment = 1; segment <= segments; segment++)
            {
                var t = segment / (double)segments;
                var x = -15 + ((headX + 15) * t);
                var y = headY + (Math.Sin((t * Math.PI * 5) - (progress * Math.PI * 4)) * 5 * (1 - (t * 0.35)));
                DrawLine(colors, width, height, previousX, previousY, x, y, dark);
                DrawLine(colors, width, height, previousX, previousY - 1, x, y - 1, green);
                previousX = x;
                previousY = y;
            }
            FillCircle(colors, width, height, headX, headY, 4, green);
            Set(colors, width, height, (int)Math.Round(headX + 1), (int)Math.Round(headY - 2), new RgbwColor(245, 235, 52, 0));
        }

        private static void DrawCoiledSnake(
            IDictionary<int, RgbwColor> colors, int width, int height,
            double cx, double cy, double progress, bool biting)
        {
            var green = new RgbwColor(34, 214, 78, 0);
            var dark = new RgbwColor(7, 105, 36, 0);
            const int segments = 132;
            var visible = Math.Max(3, (int)Math.Round(segments * progress));
            (double X, double Y) Point(int index)
            {
                var t = index / (double)(segments - 1);
                var angle = Math.PI * (0.12 + (t * 2.25));
                var rx = 28 - (t * 8);
                var ry = 22 - (t * 6);
                return (cx + (Math.Cos(angle) * rx), cy + 3 + (Math.Sin(angle) * ry));
            }
            var previous = Point(0);
            for (var segment = 1; segment < visible; segment++)
            {
                var current = Point(segment);
                DrawLine(colors, width, height, previous.X, previous.Y, current.X, current.Y, dark);
                DrawLine(colors, width, height, previous.X, previous.Y - 1, current.X, current.Y - 1, green);
                previous = current;
            }
            if (biting) previous = (cx - 11, cy - 4);
            FillCircle(colors, width, height, previous.X, previous.Y, 4, green);
            Set(colors, width, height, (int)Math.Round(previous.X + 1), (int)Math.Round(previous.Y - 2), new RgbwColor(245, 235, 52, 0));
        }

        private static void DrawSpiralBloodStairs(
            IDictionary<int, RgbwColor> colors, TimelineClip clip,
            double localTime, int width, int height)
        {
            var progress = Math.Clamp(localTime / Math.Max(0.001, clip.Duration), 0, 1);
            var cx = width * 0.5;
            var cameraRotation = Smooth(progress) * Math.PI * 1.45;
            var cameraBob = Math.Sin(progress * Math.PI * 14) * 2.8;

            // Peu de marches visibles a la fois : elles naissent devant la camera,
            // grossissent tres vite, puis passent sous elle comme dans une course subjective.
            const int steps = 18;
            for (var step = steps - 1; step >= 0; step--)
            {
                var depth = ((step / (double)steps) + (progress * 1.85)) % 1.0;
                var perspective = 0.16 + (3.25 * depth * depth * depth);
                var angle = (step * 0.58) - cameraRotation;
                var x = cx + (Math.Sin(angle) * 15 * perspective);
                var y = 22 + (Math.Pow(depth, 1.85) * (height + 24)) + cameraBob;
                var stepWidth = 7 + (int)Math.Round(24 * perspective);
                var stepHeight = 1 + (int)Math.Round(3.2 * perspective);
                var color = step % 3 == 0
                    ? new RgbwColor(194, 6, 30, 0)
                    : new RgbwColor(118, 0, 18, 0);
                var tiltX = Math.Cos(angle) * stepWidth * 0.48;
                var tiltY = Math.Sin(angle) * stepWidth * 0.13;
                for (var thickness = 0; thickness <= stepHeight; thickness++)
                    DrawLine(colors, width, height, x - tiltX, y - tiltY + thickness, x + tiltX, y + tiltY + thickness, color);
                DrawLine(colors, width, height, x - tiltX, y - tiltY, x + tiltX, y + tiltY, new RgbwColor(244, 24, 48, 0));

                // Contremarche sombre : elle donne du volume lorsque la marche passe pres de la camera.
                if (depth > 0.34)
                    DrawLine(colors, width, height, x - tiltX, y - tiltY + stepHeight, x + tiltX, y + tiltY + stepHeight, new RgbwColor(58, 0, 14, 0));
            }
        }

        private static void DrawBloodRectanglesToStairs(
            IDictionary<int, RgbwColor> colors, TimelineClip clip,
            double localTime, int width, int height)
        {
            var progress = Smooth(Math.Clamp(localTime / Math.Max(0.001, clip.Duration), 0, 1));
            var cx = width * 0.5;
            const int rectangles = 24;
            for (var index = 0; index < rectangles; index++)
            {
                var startX = 5 + ((index * 37) % (width - 10));
                var startY = -8 + ((index % 7) * 7);
                var depth = index / (double)(rectangles - 1);
                var perspective = 0.22 + (2.25 * depth * depth);
                var angle = index * 0.58;
                var targetX = cx + (Math.Sin(angle) * 18 * perspective);
                var targetY = 13 + (Math.Pow(depth, 1.55) * (height + 10));
                var x = startX + ((targetX - startX) * progress);
                var y = startY + ((targetY - startY) * progress);
                var startWidth = 3 + (index % 5);
                var targetWidth = 6 + (21 * perspective);
                var rectangleWidth = startWidth + ((targetWidth - startWidth) * progress);
                var tiltX = Math.Cos(angle * progress) * rectangleWidth * 0.48;
                var tiltY = Math.Sin(angle * progress) * rectangleWidth * 0.13;
                DrawLine(colors, width, height, x - tiltX, y - tiltY, x + tiltX, y + tiltY,
                    index % 3 == 0 ? new RgbwColor(218, 10, 38, 0) : new RgbwColor(132, 0, 22, 0));
            }
        }

        private static void DrawCollapsingBloodStairs(
            IDictionary<int, RgbwColor> colors, TimelineClip clip,
            double localTime, int width, int height)
        {
            var progress = Math.Clamp(localTime / Math.Max(0.001, clip.Duration), 0, 1);
            var cx = width * 0.5;
            const double previousCameraProgress = 1.0;
            var cameraRotation = Smooth(previousCameraProgress) * Math.PI * 1.45;
            var cameraBob = Math.Sin(previousCameraProgress * Math.PI * 14) * 2.8;
            const int steps = 18;
            for (var step = steps - 1; step >= 0; step--)
            {
                var depth = ((step / (double)steps) + (previousCameraProgress * 1.85)) % 1.0;
                var perspective = 0.16 + (3.25 * depth * depth * depth);
                var angle = (step * 0.58) - cameraRotation;
                var baseX = cx + (Math.Sin(angle) * 15 * perspective);
                var baseY = 22 + (Math.Pow(depth, 1.85) * (height + 24)) + cameraBob;
                var delay = ((steps - 1 - step) % 8) * 0.038;
                var fall = Math.Clamp((progress - delay) / Math.Max(0.08, 1 - delay), 0, 1);
                var x = baseX + (Math.Sin(step * 2.1) * fall * 11);
                var y = baseY + (fall * fall * (height - baseY + 28));
                var stepWidth = 7 + (int)Math.Round(24 * perspective);
                var stepHeight = 1 + (int)Math.Round(3.2 * perspective);
                var tiltX = Math.Cos(angle) * stepWidth * 0.48;
                var tiltY = Math.Sin(angle) * stepWidth * 0.13;
                var color = step % 3 == 0 ? new RgbwColor(194, 6, 30, 0) : new RgbwColor(118, 0, 18, 0);
                for (var thickness = 0; thickness <= stepHeight; thickness++)
                    DrawLine(colors, width, height, x - tiltX, y - tiltY + thickness, x + tiltX, y + tiltY + thickness, color);
                DrawLine(colors, width, height, x - tiltX, y - tiltY, x + tiltX, y + tiltY, new RgbwColor(244, 24, 48, 0));
                if (depth > 0.34)
                    DrawLine(colors, width, height, x - tiltX, y - tiltY + stepHeight, x + tiltX, y + tiltY + stepHeight, new RgbwColor(58, 0, 14, 0));
            }
        }

        private static void DrawGrowingThornVines(
            IDictionary<int, RgbwColor> colors, TimelineClip clip,
            double localTime, int width, int height)
        {
            var progress = Smooth(Math.Clamp(localTime / Math.Max(0.001, clip.Duration), 0, 1));
            DrawReferenceVineGarden(colors, width, height, progress, drawBuds: false);
        }

        private static void DrawBloomingRedRoses(
            IDictionary<int, RgbwColor> colors, TimelineClip clip,
            double localTime, int width, int height)
        {
            var progress = Smooth(Math.Clamp(localTime / Math.Max(0.001, clip.Duration), 0, 1));
            DrawReferenceVineGarden(colors, width, height, 1, drawBuds: false);
            DrawRoseHead(colors, width, height, 104, 37, 13, progress);
            DrawRoseHead(colors, width, height, 24, 48, 8, Smooth(Math.Clamp((progress - 0.12) / 0.88, 0, 1)));
            DrawRoseHead(colors, width, height, 64, 28, 9, Smooth(Math.Clamp((progress - 0.22) / 0.78, 0, 1)));
            DrawRoseHead(colors, width, height, 84, 77, 6, Smooth(Math.Clamp((progress - 0.34) / 0.66, 0, 1)));
        }

        private static void DrawFallingRosePetals(
            IDictionary<int, RgbwColor> colors, TimelineClip clip,
            double localTime, int width, int height)
        {
            var progress = Math.Clamp(localTime / Math.Max(0.001, clip.Duration), 0, 1);
            DrawReferenceVineGarden(colors, width, height, 1, drawBuds: false);
            var roses = new (double X, double Y, double Radius, double Delay)[]
            {
                (104, 37, 13, 0.00), (24, 48, 8, 0.10), (64, 28, 9, 0.18), (84, 77, 6, 0.28)
            };

            var roseIndex = 0;
            foreach (var rose in roses)
            {
                var shedding = Smooth(Math.Clamp((progress - rose.Delay) / Math.Max(0.1, 1 - rose.Delay), 0, 1));
                var fallenCount = Math.Clamp((int)Math.Floor(shedding * 13), 0, 12);
                DrawSheddingRoseHead(colors, width, height, rose.X, rose.Y, rose.Radius, fallenCount);

                for (var petal = 0; petal < fallenCount; petal++)
                {
                    var petalDelay = rose.Delay + (petal * 0.055);
                    var fall = Smooth(Math.Clamp((progress - petalDelay) / Math.Max(0.08, 1 - petalDelay), 0, 1));
                    var direction = petal % 2 == 0 ? -1 : 1;
                    var x = rose.X + (direction * fall * (8 + (petal * 2.4))) + (Math.Sin((fall * Math.PI * 4) + petal) * 5);
                    var y = rose.Y + 4 + (fall * fall * (height - rose.Y + 12));
                    FillEllipse(colors, width, height, x, y,
                        Math.Max(1.2, rose.Radius * 0.28), Math.Max(1.0, rose.Radius * 0.16),
                        petal % 3 == 0 ? new RgbwColor(240, 18, 48, 0) : new RgbwColor(168, 4, 32, 0));
                }
                roseIndex++;
            }
        }

        private static void DrawReferenceVineGarden(
            IDictionary<int, RgbwColor> colors, int width, int height, double progress, bool drawBuds)
        {
            // Grandes courbes croisees inspirees du dessin de reference fourni.
            DrawBezierVine(colors, width, height, 126, 132, 111, 92, 126, 63, 104, 37, 1.35, progress, drawBuds);
            DrawBezierVine(colors, width, height, -8, 18, 18, 55, 50, 94, 24, 48, 0.82, Smooth(Math.Clamp((progress - 0.08) / 0.92, 0, 1)), drawBuds);
            DrawBezierVine(colors, width, height, 18, 132, 14, 83, 49, 48, 64, 28, 0.96, Smooth(Math.Clamp((progress - 0.16) / 0.84, 0, 1)), drawBuds);
            DrawBezierVine(colors, width, height, 73, 132, 96, 103, 91, 89, 84, 77, 0.62, Smooth(Math.Clamp((progress - 0.28) / 0.72, 0, 1)), drawBuds);

            // Les tiges traversent les fleurs et continuent jusqu'au plafond, comme dans
            // la reference animee, au lieu de s'arreter brutalement sous chaque bouton.
            var extension = Smooth(Math.Clamp((progress - 0.62) / 0.38, 0, 1));
            DrawBezierVine(colors, width, height, 104, 37, 112, 24, 119, 8, 128, -5, 1.05, extension, false);
            DrawBezierVine(colors, width, height, 24, 48, 15, 37, 6, 20, -5, 5, 0.75, extension, false);
            DrawBezierVine(colors, width, height, 64, 28, 71, 20, 84, 7, 96, -5, 0.86, extension, false);
            DrawBezierVine(colors, width, height, 84, 77, 91, 56, 105, 23, 116, -5, 0.58, extension, false);
        }

        private static void DrawBezierVine(
            IDictionary<int, RgbwColor> colors, int width, int height,
            double x0, double y0, double x1, double y1, double x2, double y2, double x3, double y3,
            double scale, double progress, bool drawBud)
        {
            var red = new RgbwColor(218, 8, 38, 0);
            var dark = new RgbwColor(112, 0, 24, 0);
            const int segments = 72;
            var visible = Math.Max(1, (int)Math.Round(segments * progress));
            (double X, double Y) Point(int index)
            {
                var t = index / (double)segments;
                var u = 1 - t;
                return (
                    (u * u * u * x0) + (3 * u * u * t * x1) + (3 * u * t * t * x2) + (t * t * t * x3),
                    (u * u * u * y0) + (3 * u * u * t * y1) + (3 * u * t * t * y2) + (t * t * t * y3));
            }
            var previous = Point(0);
            for (var segment = 1; segment <= visible; segment++)
            {
                var current = Point(segment);
                DrawLine(colors, width, height, previous.X, previous.Y, current.X, current.Y, red);
                if (scale > 1.1) DrawLine(colors, width, height, previous.X + 1, previous.Y, current.X + 1, current.Y, dark);
                if (segment > 9 && segment % 11 == 0)
                {
                    var side = segment % 22 == 0 ? -1 : 1;
                    DrawLine(colors, width, height, current.X, current.Y,
                        current.X + (side * 4.5 * scale), current.Y - (3.5 * scale), red);
                }
                previous = current;
            }

            if (!drawBud || progress < 0.78) return;
            var bud = Smooth(Math.Clamp((progress - 0.78) / 0.22, 0, 1));
            var radius = (3 + (6 * bud)) * scale;
            // Bouton volontairement irregulier, comme une boucle dessinee a la main.
            DrawEllipse(colors, width, height, x3 - (radius * 0.18), y3, radius, radius * 0.62, red);
            DrawEllipse(colors, width, height, x3 + (radius * 0.22), y3 + 1, radius * 0.82, radius * 0.72, red);
            DrawLine(colors, width, height, x3 - radius, y3 - 1, x3 + (radius * 0.72), y3 + (radius * 0.45), red);
        }

        private static void DrawSheddingRoseHead(
            IDictionary<int, RgbwColor> colors, int width, int height,
            double cx, double cy, double radius, int fallenCount)
        {
            DrawLayeredRose(colors, width, height, cx, cy, radius, 1, fallenCount);
        }

        private static void DrawRedRoseLiquidFade(
            IDictionary<int, RgbwColor> colors, TimelineClip clip,
            double localTime, int width, int height)
        {
            var progress = Math.Clamp(localTime / Math.Max(0.001, clip.Duration), 0, 1);
            DrawThornGarden(colors, width, height, 1);
            DrawRoseHead(colors, width, height, 103, 31, 13, 1);
            DrawRoseHead(colors, width, height, 29, 46, 8, 1);
            DrawRoseHead(colors, width, height, 78, 52, 6, 1);

            var liquid = Smooth(Math.Clamp(progress / 0.72, 0, 1));
            var sources = new (double X, double Y, int Streams)[] { (103, 35, 17), (29, 49, 11), (78, 55, 8) };
            var streamIndex = 0;
            foreach (var source in sources)
            {
                for (var stream = 0; stream < source.Streams; stream++, streamIndex++)
                {
                    var delay = (stream % 7) * 0.035;
                    var fall = Smooth(Math.Clamp((liquid - delay) / Math.Max(0.08, 1 - delay), 0, 1));
                    var x = source.X + ((stream - ((source.Streams - 1) / 2.0)) * 2.2) + (Math.Sin(streamIndex * 1.9) * 2);
                    var y = source.Y + (fall * (height - source.Y + 10));
                    DrawLine(colors, width, height, x, source.Y, x + (Math.Sin(streamIndex) * 4), y,
                        streamIndex % 4 == 0 ? new RgbwColor(244, 18, 44, 0) : new RgbwColor(148, 0, 22, 0));
                }
            }

            var blackout = Smooth(Math.Clamp((progress - 0.62) / 0.38, 0, 1));
            var blackBottom = (int)Math.Round(blackout * height);
            for (var y = 0; y < blackBottom; y++)
            for (var x = 0; x < width; x++)
                Set(colors, width, height, x, y, new RgbwColor(0, 0, 0, 0));
        }

        private static void DrawThornGarden(IDictionary<int, RgbwColor> colors, int width, int height, double progress)
        {
            DrawThornVine(colors, width, height, 112, height + 5, 103, 31, 1.35, progress);
            DrawThornVine(colors, width, height, 15, height + 4, 29, 46, 0.88, progress);
            DrawThornVine(colors, width, height, 87, height + 6, 78, 52, 0.62, progress);
        }

        private static void DrawThornVine(
            IDictionary<int, RgbwColor> colors, int width, int height,
            double startX, double startY, double endX, double endY, double scale, double progress)
        {
            var dark = new RgbwColor(82, 4, 20, 0);
            var light = new RgbwColor(154, 10, 32, 0);
            const int segments = 44;
            var visible = Math.Max(1, (int)Math.Round(segments * progress));
            (double X, double Y) Point(int index)
            {
                var t = index / (double)segments;
                return (startX + ((endX - startX) * t) + (Math.Sin(t * Math.PI * 3) * 7 * scale * (1 - t)),
                    startY + ((endY - startY) * t));
            }
            var previous = Point(0);
            for (var segment = 1; segment <= visible; segment++)
            {
                var current = Point(segment);
                DrawLine(colors, width, height, previous.X, previous.Y, current.X, current.Y, dark);
                if (scale > 1) DrawLine(colors, width, height, previous.X + 1, previous.Y, current.X + 1, current.Y, light);
                if (segment > 4 && segment % 5 == 0)
                {
                    var side = segment % 10 == 0 ? -1 : 1;
                    var thornLength = (4 + (segment % 3)) * scale;
                    DrawLine(colors, width, height, current.X, current.Y, current.X + (side * thornLength), current.Y - (thornLength * 0.72), light);
                }
                previous = current;
            }
        }

        private static void DrawRoseHead(
            IDictionary<int, RgbwColor> colors, int width, int height,
            double cx, double cy, double radius, double bloom)
        {
            DrawLayeredRose(colors, width, height, cx, cy, radius, bloom, 0);
        }

        private static void DrawLayeredRose(
            IDictionary<int, RgbwColor> colors, int width, int height,
            double cx, double cy, double radius, double bloom, int fallenOuterPetals)
        {
            if (bloom <= 0) return;
            var scale = 0.10 + (0.90 * Smooth(bloom));
            var outerDark = ScaleColor(new RgbwColor(86, 0, 24, 0), bloom);
            var outerRed = ScaleColor(new RgbwColor(156, 3, 38, 0), bloom);
            var middleRed = ScaleColor(new RgbwColor(214, 10, 48, 0), bloom);
            var innerRed = ScaleColor(new RgbwColor(250, 32, 66, 0), bloom);

            // Douze grands petales irreguliers, couches et orientes vers l'exterieur.
            const int outerCount = 12;
            for (var petal = fallenOuterPetals; petal < outerCount; petal++)
            {
                var angle = (-Math.PI / 2) + (petal * Math.PI * 2 / outerCount);
                var irregularity = 0.88 + ((petal % 4) * 0.055);
                var px = cx + (Math.Cos(angle) * radius * 0.48 * scale);
                var py = cy + (Math.Sin(angle) * radius * 0.39 * scale);
                FillRotatedEllipse(colors, width, height, px, py,
                    radius * 0.52 * scale * irregularity,
                    radius * 0.25 * scale,
                    angle, petal % 3 == 0 ? outerDark : outerRed);
            }

            // Couche moyenne decalee : elle cache les raccords et cree le volume du bouton.
            const int middleCount = 8;
            for (var petal = 0; petal < middleCount; petal++)
            {
                var angle = (-Math.PI / 2) + 0.34 + (petal * Math.PI * 2 / middleCount);
                var px = cx + (Math.Cos(angle) * radius * 0.27 * scale);
                var py = cy + (Math.Sin(angle) * radius * 0.22 * scale);
                FillRotatedEllipse(colors, width, height, px, py,
                    radius * 0.35 * scale, radius * 0.18 * scale, angle, middleRed);
            }

            // Petales centraux serres en spirale, plus lumineux.
            for (var petal = 0; petal < 5; petal++)
            {
                var angle = (petal * 1.26) + (bloom * 0.35);
                var distance = radius * scale * (0.05 + (petal * 0.025));
                FillRotatedEllipse(colors, width, height,
                    cx + (Math.Cos(angle) * distance), cy + (Math.Sin(angle) * distance),
                    radius * 0.20 * scale, radius * 0.10 * scale, angle, innerRed);
            }
            FillCircle(colors, width, height, cx, cy, Math.Max(1, (int)Math.Round(radius * 0.09 * scale)), new RgbwColor(255, 64, 86, 0));
        }

        private static void DrawRoseSpiral(
            IDictionary<int, RgbwColor> colors, int width, int height,
            double cx, double cy, double radius, RgbwColor color, double reveal)
        {
            const int segments = 58;
            var visible = Math.Max(2, (int)Math.Round(segments * reveal));
            var previousX = cx;
            var previousY = cy;
            for (var segment = 1; segment <= visible; segment++)
            {
                var t = segment / (double)segments;
                var angle = t * Math.PI * 4.4;
                var spiralRadius = radius * t;
                var x = cx + (Math.Cos(angle) * spiralRadius);
                var y = cy + (Math.Sin(angle) * spiralRadius * 0.82);
                DrawLine(colors, width, height, previousX, previousY, x, y, color);
                previousX = x;
                previousY = y;
            }
        }

        private static void FillRectangle(
            IDictionary<int, RgbwColor> colors, int width, int height,
            double x, double y, double rectangleWidth, double rectangleHeight, RgbwColor color)
        {
            for (var py = (int)Math.Floor(y); py <= Math.Ceiling(y + rectangleHeight); py++)
            for (var px = (int)Math.Floor(x); px <= Math.Ceiling(x + rectangleWidth); px++)
                Set(colors, width, height, px, py, color);
        }

        private static void DrawGreenSnakeApproach(
            IDictionary<int, RgbwColor> colors, double localTime, int width, int height)
        {
            var cx = width * 0.5;
            var cy = height * 0.48;
            DrawWhiteApple(colors, width, height, cx, cy, 1, false);

            // Le clip commence a 48,3 s : le serpent n'apparait qu'a 50 s (localTime 1,7).
            var progress = Smooth(Math.Clamp((localTime - 1.7) / 3.0, 0, 1));
            if (progress <= 0) return;
            var green = new RgbwColor(34, 210, 82, 0);
            var darkGreen = new RgbwColor(8, 112, 42, 0);
            var headX = width + 10 + ((cx + 25 - width - 10) * progress);
            var headY = cy + 21 + (Math.Sin(progress * Math.PI * 3) * 7);
            var tailX = headX + 34;
            DrawLine(colors, width, height, tailX, cy + 25, headX, headY, darkGreen);
            DrawLine(colors, width, height, tailX, cy + 24, headX, headY - 1, green);
            DrawLine(colors, width, height, tailX, cy + 26, headX, headY + 1, green);
            FillCircle(colors, width, height, headX, headY, 4, green);
            Set(colors, width, height, (int)Math.Round(headX - 2), (int)Math.Round(headY - 2), new RgbwColor(245, 30, 34, 0));
        }

        private static void DrawSnakeCoilAndBite(
            IDictionary<int, RgbwColor> colors, double localTime, int width, int height)
        {
            var cx = width * 0.5;
            var cy = height * 0.48;
            var bitten = localTime >= 1; // Le clip commence a 53 s : morsure a 54 s.
            DrawWhiteApple(colors, width, height, cx, cy, 1, bitten);

            var green = new RgbwColor(28, 202, 72, 0);
            var darkGreen = new RgbwColor(6, 104, 34, 0);
            var coilProgress = Smooth(Math.Clamp(localTime / 1.0, 0, 1));
            const int segments = 120;
            var visible = Math.Max(3, (int)Math.Round(segments * coilProgress));
            (double X, double Y) CoilPoint(int index)
            {
                var t = index / (double)(segments - 1);
                var angle = (-0.18 + (t * 2.18)) * Math.PI;
                var radiusX = 30 - (t * 7);
                var radiusY = 24 - (t * 5);
                return (cx + (Math.Cos(angle) * radiusX), cy + 5 + (Math.Sin(angle) * radiusY));
            }
            var previous = CoilPoint(0);
            for (var segment = 1; segment < visible; segment++)
            {
                var current = CoilPoint(segment);
                DrawLine(colors, width, height, previous.X, previous.Y, current.X, current.Y, darkGreen);
                DrawLine(colors, width, height, previous.X, previous.Y - 1, current.X, current.Y - 1, green);
                DrawLine(colors, width, height, previous.X, previous.Y + 1, current.X, current.Y + 1, green);
                previous = current;
            }
            FillCircle(colors, width, height, previous.X, previous.Y, 4, green);
            Set(colors, width, height, (int)Math.Round(previous.X - 1), (int)Math.Round(previous.Y - 2), new RgbwColor(244, 28, 30, 0));

            if (!bitten) return;
            var juiceProgress = Math.Clamp((localTime - 1) / 3.648, 0, 1);
            var red = new RgbwColor(232, 4, 30, 0);
            var darkRed = new RgbwColor(130, 0, 14, 0);
            for (var drop = 0; drop < 11; drop++)
            {
                var delay = drop * 0.055;
                var fall = Smooth(Math.Clamp((juiceProgress - delay) / Math.Max(0.1, 1 - delay), 0, 1));
                var x = cx - 14 + (drop * 2.8) + (Math.Sin(drop * 1.7) * 2);
                var y = cy + 14 + (fall * height * (0.52 + ((drop % 3) * 0.08)));
                DrawLine(colors, width, height, x, y - 5 - (drop % 5), x, y, drop % 3 == 0 ? red : darkRed);
            }
        }

        private static void DrawWhiteApple(
            IDictionary<int, RgbwColor> colors, int width, int height,
            double cx, double cy, double reveal, bool bitten)
        {
            if (reveal <= 0) return;
            var white = ScaleColor(new RgbwColor(252, 252, 255, 100), reveal);
            var shade = ScaleColor(new RgbwColor(194, 210, 226, 32), reveal);
            var radius = 18 * reveal;
            DrawEllipse(colors, width, height, cx - 8, cy + 3, radius * 0.72, radius, white);
            DrawEllipse(colors, width, height, cx + 8, cy + 3, radius * 0.72, radius, white);
            DrawEllipse(colors, width, height, cx, cy + 12, radius * 0.84, radius * 0.64, shade);
            DrawLine(colors, width, height, cx, cy - 14, cx + 2, cy - 24, ScaleColor(new RgbwColor(116, 68, 24, 0), reveal));
            DrawEllipse(colors, width, height, cx + 9, cy - 21, 9 * reveal, 4 * reveal, ScaleColor(new RgbwColor(40, 198, 72, 0), reveal));
            if (!bitten) return;
            FillCircle(colors, width, height, cx + 17, cy - 3, 7, new RgbwColor(0, 0, 0, 0));
            FillCircle(colors, width, height, cx + 14, cy - 10, 5, new RgbwColor(0, 0, 0, 0));
        }

        private static void DrawFlameParticleBurst(
            IDictionary<int, RgbwColor> colors,
            TimelineClip clip,
            double localTime,
            int width,
            int height)
        {
            var centerY = clip.RippleCenterY ?? (height * 0.5);
            var scale = Math.Clamp(clip.VisualScale, 0.35, 3);
            if (localTime < 2)
            {
                var launch = Math.Clamp(localTime / 2, 0, 1);
                var easedLaunch = 1 - Math.Pow(1 - launch, 3);
                var destinationX = clip.RippleCenterX ?? (width * 0.4375);
                var centerX = 8 + ((destinationX - 8) * easedLaunch);
                var flicker = (int)Math.Round((Math.Sin(localTime * 19) + 1) * 0.8);

                FillCircle(colors, width, height, centerX - (9 * scale), centerY, Math.Max(1, (int)Math.Round(2 * scale)), new(255, 96, 4, 0));
                FillCircle(colors, width, height, centerX - (5 * scale), centerY, Math.Max(1, (int)Math.Round(4 * scale)), new(255, 152, 8, 0));
                FillCircle(colors, width, height, centerX, centerY, Math.Max(1, (int)Math.Round((7 + flicker) * scale)), new(255, 184, 18, 0));
                FillCircle(colors, width, height, centerX + scale, centerY, Math.Max(1, (int)Math.Round(5 * scale)), new(255, 244, 22, 0));
                FillCircle(colors, width, height, centerX + (2 * scale), centerY - scale, Math.Max(1, (int)Math.Round(2 * scale)), new(255, 255, 204, 0));
                return;
            }

            const double cycleDuration = 1.55;
            var elapsed = localTime - 2;
            var cycle = (int)Math.Floor(elapsed / cycleDuration);
            var phase = (elapsed % cycleDuration) / cycleDuration;
            var burstX = clip.RippleCenterX ?? (width * 0.4375);

            if (phase < 0.22)
            {
                var bloom = Smooth(phase / 0.22);
                var petalDistance = (2 + (bloom * 7)) * scale;
                var petalRadius = Math.Max(1, (int)Math.Round((2 + (bloom * 5)) * scale));
                for (var petal = 0; petal < 7; petal++)
                {
                    var angle = (petal * Math.PI * 2 / 7) + (cycle * 0.31);
                    var color = (petal % 3) switch
                    {
                        0 => new RgbwColor(255, 255, 218, 0),
                        1 => new RgbwColor(255, 246, 28, 0),
                        _ => new RgbwColor(255, 176, 20, 0)
                    };
                    FillCircle(colors, width, height,
                        burstX + (Math.Cos(angle) * petalDistance),
                        centerY + (Math.Sin(angle) * petalDistance),
                        petalRadius,
                        color);
                }
                FillCircle(colors, width, height, burstX, centerY, 3 + petalRadius / 2, new(255, 255, 224, 0));
                return;
            }

            var flight = Math.Clamp((phase - 0.22) / 0.78, 0, 1);
            var travel = (8 + (Smooth(flight) * Math.Min(width, height) * 0.43)) * scale;
            var fade = 1 - Smooth(Math.Clamp((flight - 0.52) / 0.48, 0, 1));
            for (var particle = 0; particle < 30; particle++)
            {
                var angle = (particle * 2.399963) + (cycle * 0.47) + ((Noise(particle, 1) - 0.5) * 0.24);
                var distance = travel * (0.58 + (Noise(particle, 2) * 0.54));
                var x = burstX + (Math.Cos(angle) * distance);
                var y = centerY + (Math.Sin(angle) * distance);
                var baseColor = (particle % 3) switch
                {
                    0 => new RgbwColor(255, 244, 18, 0),
                    1 => new RgbwColor(255, 172, 34, 0),
                    _ => new RgbwColor(255, 255, 214, 0)
                };
                var color = ScaleColor(baseColor, fade * clip.Intensity);
                var radius = particle % 5 == 0 ? 3 : particle % 2 == 0 ? 2 : 1;
                FillCircle(colors, width, height, x, y, radius, color);

                if (particle % 3 == 1)
                {
                    var trail = 3 + (int)Math.Round(Noise(particle, 3) * 6);
                    DrawLine(colors, width, height, x, y,
                        x - (Math.Cos(angle) * trail),
                        y - (Math.Sin(angle) * trail),
                        ScaleColor(baseColor, fade * 0.58 * clip.Intensity));
                }
            }
        }

        private static void DrawPersistentRedStars(
            IDictionary<int, RgbwColor> colors,
            TimelineClip clip,
            double localTime,
            int width,
            int height)
        {
            foreach (var entityId in clip.Target.EntityIds)
            {
                if (entityId < 0 || entityId >= width * height) continue;
                var phase = Noise(entityId, 4) * Math.PI * 2;
                var rate = 0.72 + (Noise(entityId, 5) * 0.96);
                var wave = (Math.Sin((localTime * Math.Max(0.1, clip.Speed) * rate * Math.PI * 2) + phase) + 1) * 0.5;
                var level = 0.32 + (0.68 * Math.Pow(wave, 7));
                colors[entityId] = ScaleColor(clip.Color, level * clip.Intensity);
            }
        }

        private static void DrawPixelText(
            IDictionary<int, RgbwColor> colors,
            TimelineClip clip,
            double progress,
            int width,
            int height)
        {
            var text = NormalizeText(string.IsNullOrWhiteSpace(clip.TextContent) ? clip.Name : clip.TextContent);
            var lines = text.Replace("\r", string.Empty).Split('\n');
            var scale = Math.Clamp((int)Math.Round(clip.VisualScale), 1, 4);
            var longestLine = Math.Max(1, lines.Max(line => line.Length));
            while (scale > 1 && (longestLine * 6 * scale) - scale > width - 4) scale--;

            var fadeIn = Smooth(Math.Clamp(progress / 0.22, 0, 1));
            var fadeOut = Smooth(Math.Clamp((1 - progress) / 0.22, 0, 1));
            var color = ScaleColor(clip.Color, Math.Min(fadeIn, fadeOut) * clip.Intensity);
            var lineHeight = 9 * scale;
            var totalHeight = (lines.Length * lineHeight) - (2 * scale);
            var centerX = clip.RippleCenterX ?? ((width - 1) / 2.0);
            var centerY = clip.RippleCenterY ?? ((height - 1) / 2.0);
            var startY = (int)Math.Round(centerY - (totalHeight / 2.0));

            for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                var line = lines[lineIndex];
                var lineWidth = Math.Max(0, (line.Length * 6 * scale) - scale);
                var startX = (int)Math.Round(centerX - (lineWidth / 2.0));
                for (var characterIndex = 0; characterIndex < line.Length; characterIndex++)
                {
                    DrawScaledGlyph(colors, width, height, line[characterIndex],
                        startX + (characterIndex * 6 * scale),
                        startY + (lineIndex * lineHeight),
                        scale,
                        color);
                }
            }
        }

        private static void DrawLine(
            IDictionary<int, RgbwColor> colors,
            int width,
            int height,
            double startX,
            double startY,
            double endX,
            double endY,
            RgbwColor color)
        {
            var steps = Math.Max(1, (int)Math.Ceiling(Math.Max(Math.Abs(endX - startX), Math.Abs(endY - startY))));
            for (var step = 0; step <= steps; step++)
            {
                var amount = step / (double)steps;
                Set(colors, width, height,
                    (int)Math.Round(startX + ((endX - startX) * amount)),
                    (int)Math.Round(startY + ((endY - startY) * amount)),
                    color);
            }
        }

        private static RgbwColor ScaleColor(RgbwColor color, double level)
        {
            level = Math.Clamp(level, 0, 1);
            return new RgbwColor(
                (byte)Math.Round(color.R * level),
                (byte)Math.Round(color.G * level),
                (byte)Math.Round(color.B * level),
                (byte)Math.Round(color.W * level));
        }

        private static double Noise(int value, int channel)
        {
            var noise = Math.Sin((value * 12.9898) + (channel * 78.233)) * 43758.5453;
            return noise - Math.Floor(noise);
        }

        private static void DrawEllipse(IDictionary<int, RgbwColor> colors, int width, int height, double cx, double cy, double rx, double ry, RgbwColor color)
        {
            for (var a = 0; a < 48; a++) { var angle = a * Math.PI * 2 / 48; Set(colors, width, height, (int)Math.Round(cx + Math.Cos(angle) * rx), (int)Math.Round(cy + Math.Sin(angle) * ry), color); }
        }

        private static void FillEllipse(IDictionary<int, RgbwColor> colors, int width, int height, double cx, double cy, double rx, double ry, RgbwColor color)
        {
            if (rx <= 0 || ry <= 0) return;
            for (var y = (int)Math.Floor(cy - ry); y <= Math.Ceiling(cy + ry); y++)
            for (var x = (int)Math.Floor(cx - rx); x <= Math.Ceiling(cx + rx); x++)
            {
                var dx = (x - cx) / rx;
                var dy = (y - cy) / ry;
                if ((dx * dx) + (dy * dy) <= 1) Set(colors, width, height, x, y, color);
            }
        }

        private static void FillRotatedEllipse(
            IDictionary<int, RgbwColor> colors, int width, int height,
            double cx, double cy, double rx, double ry, double angle, RgbwColor color)
        {
            if (rx <= 0 || ry <= 0) return;
            var extent = Math.Ceiling(Math.Max(rx, ry));
            var cosine = Math.Cos(angle);
            var sine = Math.Sin(angle);
            for (var y = (int)Math.Floor(cy - extent); y <= Math.Ceiling(cy + extent); y++)
            for (var x = (int)Math.Floor(cx - extent); x <= Math.Ceiling(cx + extent); x++)
            {
                var dx = x - cx;
                var dy = y - cy;
                var localX = (dx * cosine) + (dy * sine);
                var localY = (-dx * sine) + (dy * cosine);
                if (((localX * localX) / (rx * rx)) + ((localY * localY) / (ry * ry)) <= 1)
                    Set(colors, width, height, x, y, color);
            }
        }

        private static void FillCircle(IDictionary<int, RgbwColor> colors, int width, int height, double cx, double cy, int radius, RgbwColor color)
        {
            for (var y = (int)cy - radius; y <= cy + radius; y++) for (var x = (int)cx - radius; x <= cx + radius; x++) if (Math.Pow(x - cx, 2) + Math.Pow(y - cy, 2) <= radius * radius) Set(colors, width, height, x, y, color);
        }

        private static void DrawGlyph(IDictionary<int, RgbwColor> colors, int width, int height, char c, int ox, int oy, RgbwColor color)
        {
            if (!Font.TryGetValue(c, out var rows)) return;
            for (var y = 0; y < rows.Length; y++) for (var x = 0; x < 5; x++) if ((rows[y] & (1 << (4 - x))) != 0) Set(colors, width, height, ox + x, oy + y, color);
        }

        private static void DrawScaledGlyph(IDictionary<int, RgbwColor> colors, int width, int height, char c, int ox, int oy, int scale, RgbwColor color)
        {
            if (!Font.TryGetValue(c, out var rows)) return;
            for (var y = 0; y < rows.Length; y++)
            for (var x = 0; x < 5; x++)
            {
                if ((rows[y] & (1 << (4 - x))) == 0) continue;
                for (var sy = 0; sy < scale; sy++)
                for (var sx = 0; sx < scale; sx++)
                    Set(colors, width, height, ox + (x * scale) + sx, oy + (y * scale) + sy, color);
            }
        }

        private static readonly Dictionary<char, int[]> Font = new()
        {
            [' '] = new[]{0,0,0,0,0,0,0}, ['A'] = new[]{14,17,17,31,17,17,17}, ['B'] = new[]{30,17,17,30,17,17,30},
            ['C'] = new[]{14,17,16,16,16,17,14}, ['D'] = new[]{30,17,17,17,17,17,30}, ['E'] = new[]{31,16,16,30,16,16,31},
            ['F'] = new[]{31,16,16,30,16,16,16}, ['G'] = new[]{14,17,16,23,17,17,15}, ['H'] = new[]{17,17,17,31,17,17,17},
            ['I'] = new[]{31,4,4,4,4,4,31}, ['J'] = new[]{7,2,2,2,18,18,12}, ['K'] = new[]{17,18,20,24,20,18,17},
            ['L'] = new[]{16,16,16,16,16,16,31}, ['M'] = new[]{17,27,21,21,17,17,17}, ['N'] = new[]{17,25,21,19,17,17,17},
            ['O'] = new[]{14,17,17,17,17,17,14}, ['P'] = new[]{30,17,17,30,16,16,16}, ['Q'] = new[]{14,17,17,17,21,18,13},
            ['R'] = new[]{30,17,17,30,20,18,17}, ['S'] = new[]{15,16,16,14,1,1,30}, ['T'] = new[]{31,4,4,4,4,4,4},
            ['U'] = new[]{17,17,17,17,17,17,14}, ['V'] = new[]{17,17,17,17,17,10,4}, ['W'] = new[]{17,17,17,21,21,21,10},
            ['X'] = new[]{17,17,10,4,10,17,17}, ['Y'] = new[]{17,17,10,4,4,4,4}, ['Z'] = new[]{31,1,2,4,8,16,31},
            ['0'] = new[]{14,17,19,21,25,17,14}, ['1'] = new[]{4,12,4,4,4,4,14}, ['2'] = new[]{14,17,1,2,4,8,31},
            ['3'] = new[]{30,1,1,14,1,1,30}, ['4'] = new[]{2,6,10,18,31,2,2}, ['5'] = new[]{31,16,16,30,1,1,30},
            ['6'] = new[]{14,16,16,30,17,17,14}, ['7'] = new[]{31,1,2,4,8,8,8}, ['8'] = new[]{14,17,17,14,17,17,14},
            ['9'] = new[]{14,17,17,15,1,1,14}, [','] = new[]{0,0,0,0,0,4,8}, ['.'] = new[]{0,0,0,0,0,12,12},
            [':'] = new[]{0,4,4,0,4,4,0}, ['-'] = new[]{0,0,0,31,0,0,0}, ['\''] = new[]{4,4,8,0,0,0,0}, ['!'] = new[]{4,4,4,4,4,0,4}
        };

        private static string NormalizeText(string value)
            => value.ToUpperInvariant()
                .Replace("Œ", "OE")
                .Replace('É', 'E').Replace('È', 'E').Replace('Ê', 'E').Replace('Ë', 'E')
                .Replace('À', 'A').Replace('Â', 'A').Replace('Ä', 'A')
                .Replace('Î', 'I').Replace('Ï', 'I')
                .Replace('Ô', 'O').Replace('Ö', 'O')
                .Replace('Ù', 'U').Replace('Û', 'U').Replace('Ü', 'U')
                .Replace('Ç', 'C').Replace('’', '\'');

        private static double Smooth(double value) { value = Math.Clamp(value, 0, 1); return value * value * (3 - (2 * value)); }
        private static void Set(IDictionary<int, RgbwColor> colors, int width, int height, int x, int y, RgbwColor color) { if (x >= 0 && x < width && y >= 0 && y < height) colors[(y * width) + x] = color; }
    }
}
