using System.Collections.Generic;
using Liekinheitin.CreativeTool.Models;

namespace Liekinheitin.CreativeTool.Shapes;

public class PolygonShape : ShapeBase
{
    public PolygonShape(TimelineClip clip)
        : base(clip, "Polygone")
    {
    }

    public int GridWidth { get; set; } = 128;

    public int GridHeight { get; set; } = 128;

    public override IEnumerable<int> GetEntityIds()
    {
        var points = new[]
        {
            (X: GridWidth / 2, Y: 18),
            (X: GridWidth - 24, Y: GridHeight / 2),
            (X: (GridWidth * 2) / 3, Y: GridHeight - 20),
            (X: GridWidth / 3, Y: GridHeight - 20),
            (X: 24, Y: GridHeight / 2)
        };
        var ids = new HashSet<int>();

        for (var index = 0; index < points.Length; index++)
        {
            var current = points[index];
            var next = points[(index + 1) % points.Length];
            var line = new LineShape(Clip)
            {
                X1 = current.X,
                Y1 = current.Y,
                X2 = next.X,
                Y2 = next.Y,
                GridWidth = GridWidth,
                GridHeight = GridHeight
            };

            ids.UnionWith(line.GetEntityIds());
        }

        return ids;
    }
}
