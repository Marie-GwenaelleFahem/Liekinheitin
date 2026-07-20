using System.Windows.Media;

namespace Liekinheitin.CreativeTool.Domain
{
    /// <summary>
    /// Peint une colonne entière d'un coup, en ignorant les cases sans LED
    /// (même logique que BrushTool.Paint, appliquée à toute la colonne).
    /// </summary>
    public sealed class ColumnFillTool
    {
        private readonly PixelCanvas _canvas;
        private readonly WallLayout _layout;

        public ColumnFillTool(PixelCanvas canvas, WallLayout layout)
        {
            _canvas = canvas;
            _layout = layout;
        }

        public void FillColumn(int col, Color color)
        {
            if (col < 0 || col >= _layout.Columns) return;

            for (int row = 0; row < _layout.Rows; row++)
            {
                if (_layout.HasLed(col, row))
                    _canvas.SetPixel(col, row, color);
            }
        }
    }
}