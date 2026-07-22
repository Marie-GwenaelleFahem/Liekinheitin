using System.Windows.Media;

namespace Liekinheitin.CreativeTool.Models
{
    public readonly record struct RgbwColor(byte R, byte G, byte B, byte W)
    {
        public static RgbwColor White => new(255, 255, 255, 0);

        public Color ToColor()
        {
            var red = ClampToByte(R + W);
            var green = ClampToByte(G + W);
            var blue = ClampToByte(B + W);

            return Color.FromRgb(red, green, blue);
        }

        private static byte ClampToByte(int value) => (byte)Math.Clamp(value, byte.MinValue, byte.MaxValue);
    }
}
