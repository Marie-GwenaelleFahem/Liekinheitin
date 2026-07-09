using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Liekinheitin.CreativeTool.Views
{
    /// <summary>
    /// High-throughput preview for the logical LED wall.
    /// </summary>
    public partial class PixelGridView : UserControl
    {
        public const int DefaultWidth = 128;
        public const int DefaultHeight = 128;

        private const int BytesPerPixel = 4;
        private readonly byte[] _pixels;
        private readonly WriteableBitmap _bitmap;

        public PixelGridView()
        {
            InitializeComponent();

            WallWidth = DefaultWidth;
            WallHeight = DefaultHeight;
            _pixels = new byte[WallWidth * WallHeight * BytesPerPixel];
            _bitmap = new WriteableBitmap(WallWidth, WallHeight, 96, 96, PixelFormats.Bgra32, null);
            PreviewImage.Source = _bitmap;

            Clear();
        }

        public int WallWidth { get; }

        public int WallHeight { get; }

        public void Clear()
        {
            Array.Clear(_pixels, 0, _pixels.Length);
            CommitPixels();
        }

        public void Fill(Color color)
        {
            for (var index = 0; index < _pixels.Length; index += BytesPerPixel)
            {
                WritePixel(index, color);
            }

            CommitPixels();
        }

        public void RenderWave(double time)
        {
            for (var y = 0; y < WallHeight; y++)
            {
                for (var x = 0; x < WallWidth; x++)
                {
                    var phase = (x * 0.09) + (y * 0.045) + (time * 4.0);
                    var wave = (Math.Sin(phase) + 1.0) * 0.5;
                    var red = (byte)(24 + (wave * 180));
                    var green = (byte)(30 + ((1.0 - wave) * 90));
                    var blue = (byte)(80 + (wave * 175));
                    var index = ((y * WallWidth) + x) * BytesPerPixel;

                    WritePixel(index, Color.FromRgb(red, green, blue));
                }
            }

            CommitPixels();
        }

        private void WritePixel(int index, Color color)
        {
            _pixels[index] = color.B;
            _pixels[index + 1] = color.G;
            _pixels[index + 2] = color.R;
            _pixels[index + 3] = color.A;
        }

        private void CommitPixels()
        {
            _bitmap.WritePixels(
                new System.Windows.Int32Rect(0, 0, WallWidth, WallHeight),
                _pixels,
                WallWidth * BytesPerPixel,
                0);
        }
    }
}
