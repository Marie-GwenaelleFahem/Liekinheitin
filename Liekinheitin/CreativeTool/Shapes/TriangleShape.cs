using System.Collections.Generic;
using Liekinheitin.CreativeTool.Models;

namespace Liekinheitin.CreativeTool.Shapes;

public class TriangleShape : ShapeBase
{
    public TriangleShape(TimelineClip clip)
        : base(clip, "Triangle")
    {
    }

    public int GridWidth { get; set; } = 128;

    public int GridHeight { get; set; } = 128;

    public override IEnumerable<int> GetEntityIds()
    {
        var left = new LineShape(Clip) { X1 = GridWidth / 2, Y1 = 24, X2 = 24, Y2 = GridHeight - 25, GridWidth = GridWidth, GridHeight = GridHeight };
        var right = new LineShape(Clip) { X1 = GridWidth / 2, Y1 = 24, X2 = GridWidth - 25, Y2 = GridHeight - 25, GridWidth = GridWidth, GridHeight = GridHeight };
        var bottom = new LineShape(Clip) { X1 = 24, Y1 = GridHeight - 25, X2 = GridWidth - 25, Y2 = GridHeight - 25, GridWidth = GridWidth, GridHeight = GridHeight };
        var ids = new HashSet<int>(left.GetEntityIds());

        ids.UnionWith(right.GetEntityIds());
        ids.UnionWith(bottom.GetEntityIds());

        return ids;
    }
}
