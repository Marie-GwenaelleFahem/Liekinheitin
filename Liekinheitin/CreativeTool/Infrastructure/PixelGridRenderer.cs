using Liekinheitin.CreativeTool.Domain;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Liekinheitin.CreativeTool.Infrastructure
{
    /// <summary>
    /// Dessine PixelCanvas dans un WriteableBitmap, une case grille = un pixel bitmap
    /// (le scaling visuel se fait via RenderOptions.BitmapScalingMode=NearestNeighbor
    /// sur l'Image XAML, pas ici).
    /// </summary>
    public sealed class PixelGridRenderer
    {
        private readonly WallLayout _layout;
        public WriteableBitmap Bitmap { get; }

        public PixelGridRenderer(WallLayout layout)
        {
            _layout = layout;
            // Row 0 = bas du mur, mais WriteableBitmap a l'origine en haut :
            // on inverse verticalement à l'écriture (voir RowToBitmapY).
            Bitmap = new WriteableBitmap(layout.Columns, layout.Rows, 96, 96, PixelFormats.Bgra32, null);
        }

        private int RowToBitmapY(int row) => _layout.Rows - 1 - row;

        public void DrawPixel(int col, int row, Color color)
        {
            int y = RowToBitmapY(row);
            var rect = new Int32Rect(col, y, 1, 1);
            byte[] pixel = { color.B, color.G, color.R, color.A };
            Bitmap.WritePixels(rect, pixel, 4, 0);
        }

        /// <summary>Redessine tout (ouverture, chargement d'une frame d'animation, etc.)</summary>
        public void DrawAll(PixelCanvas canvas)
        {
            for (int c = 0; c < _layout.Columns; c++)
                for (int r = 0; r < _layout.Rows; r++)
                {
                    var color = _layout.HasLed(c, r) ? canvas.GetPixel(c, r) : Colors.Black;
                    DrawPixel(c, r, color);
                }
        }
    }
}