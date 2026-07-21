using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Liekinheitin.CreativeTool.Domain;

namespace Liekinheitin.CreativeTool.Infrastructure
{
    public sealed class PixelGridRenderer
    {
        private readonly WallLayout _layout;
        public WriteableBitmap Bitmap { get; }

        public PixelGridRenderer(WallLayout layout)
        {
            _layout = layout;
            Bitmap = new WriteableBitmap(layout.Columns, layout.Rows, 96, 96, PixelFormats.Bgra32, null);
        }

        private int RowToBitmapY(int row) => _layout.Rows - 1 - row;

        /// <summary>Redessine tout (ouverture, test orientation) — un seul verrouillage pour
        /// les 16 384 cases, malgré le volume.</summary>
        public void DrawAll(PixelCanvas canvas)
        {
            var cells = new List<(int Col, int Row, Color Color)>(_layout.Columns * _layout.Rows);
            for (int c = 0; c < _layout.Columns; c++)
                for (int r = 0; r < _layout.Rows; r++)
                    cells.Add((c, r, _layout.HasLed(c, r) ? canvas.GetPixel(c, r) : Colors.Black));

            DrawPixels(cells);
        }

        /// <summary>Redessine uniquement les cases indiquées — un seul verrouillage pour
        /// tout le lot, quel que soit le nombre de cases.</summary>
        public void DrawPixels(IReadOnlyCollection<(int Col, int Row, Color Color)> cells)
        {
            if (cells.Count == 0) return;

            Bitmap.Lock();
            try
            {
                IntPtr backBuffer = Bitmap.BackBuffer;
                int stride = Bitmap.BackBufferStride;

                int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;

                foreach (var (col, row, color) in cells)
                {
                    int y = RowToBitmapY(row);
                    int offset = y * stride + col * 4;
                    int packed = color.B | (color.G << 8) | (color.R << 16) | (color.A << 24);
                    Marshal.WriteInt32(backBuffer, offset, packed);

                    if (col < minX) minX = col;
                    if (col > maxX) maxX = col;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }

                Bitmap.AddDirtyRect(new Int32Rect(minX, minY, maxX - minX + 1, maxY - minY + 1));
            }
            finally
            {
                Bitmap.Unlock();
            }
        }

        /// <summary>Conservé pour compatibilité ponctuelle (un seul pixel) — préférer DrawPixels
        /// pour tout ce qui touche plusieurs cases à la fois.</summary>
        public void DrawPixel(int col, int row, Color color) => DrawPixels(new[] { (col, row, color) });
    }
}