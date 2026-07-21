using System.Windows.Media;

namespace Liekinheitin.CreativeTool.Domain
{
    public sealed class ColumnFillTool
    {
        private readonly SceneManager _scene;

        public ColumnFillTool(SceneManager scene)
        {
            _scene = scene;
        }

        public void FillColumn(int col, Color color) => _scene.FillColumnFreehand(col, color);
    }
}