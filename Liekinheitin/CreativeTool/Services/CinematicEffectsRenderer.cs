using System;
using System.Collections.Generic;
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

        private static void DrawEllipse(IDictionary<int, RgbwColor> colors, int width, int height, double cx, double cy, double rx, double ry, RgbwColor color)
        {
            for (var a = 0; a < 48; a++) { var angle = a * Math.PI * 2 / 48; Set(colors, width, height, (int)Math.Round(cx + Math.Cos(angle) * rx), (int)Math.Round(cy + Math.Sin(angle) * ry), color); }
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

        private static readonly Dictionary<char, int[]> Font = new()
        {
            [' '] = new[]{0,0,0,0,0,0,0}, ['O'] = new[]{14,17,17,17,17,17,14}, ['H'] = new[]{17,17,17,31,17,17,17},
            [','] = new[]{0,0,0,0,0,4,8}, ['D'] = new[]{30,17,17,17,17,17,30}, ['E'] = new[]{31,16,16,30,16,16,31},
            ['A'] = new[]{14,17,17,31,17,17,17}, ['R'] = new[]{30,17,17,30,20,18,17}, ['L'] = new[]{16,16,16,16,16,16,31}
        };

        private static double Smooth(double value) { value = Math.Clamp(value, 0, 1); return value * value * (3 - (2 * value)); }
        private static void Set(IDictionary<int, RgbwColor> colors, int width, int height, int x, int y, RgbwColor color) { if (x >= 0 && x < width && y >= 0 && y < height) colors[(y * width) + x] = color; }
    }
}
