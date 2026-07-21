using System.Windows.Media;

namespace Liekinheitin.CreativeTool.Domain
{
    public sealed class BrushTool
    {
        private readonly SceneManager _scene;

        public BrushTool(SceneManager scene)
        {
            _scene = scene;
        }

        public Color CurrentColor { get; set; } = Colors.White;

        public bool Paint(int col, int row) => _scene.PaintFreehand(col, row, CurrentColor);
    }
}