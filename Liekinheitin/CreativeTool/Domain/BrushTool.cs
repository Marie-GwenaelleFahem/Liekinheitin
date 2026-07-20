using System.Windows.Media;

namespace Liekinheitin.CreativeTool.Domain
{
    /// <summary>
    /// Applique la couleur sélectionnée sur le buffer, en ignorant
    /// les cases sans LED physique.
    /// </summary>
    public sealed class BrushTool
    {
        private readonly PixelCanvas _canvas;
        private readonly WallLayout _layout;

        public BrushTool(PixelCanvas canvas, WallLayout layout)
        {
            _canvas = canvas;
            _layout = layout;
        }

        public Color CurrentColor { get; set; } = Colors.White;

        /// <summary>
        /// Peint une case si elle correspond à une LED réelle. Ne fait rien sinon.
        /// </summary>
        public bool Paint(int col, int row)
        {
            if (!_layout.HasLed(col, row))
                return false;

            _canvas.SetPixel(col, row, CurrentColor);
            return true;
        }
    }
}