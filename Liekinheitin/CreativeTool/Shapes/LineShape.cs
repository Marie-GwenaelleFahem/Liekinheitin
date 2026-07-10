using System;
using System.Collections.Generic;
using Liekinheitin.CreativeTool.Models;

namespace Liekinheitin.CreativeTool.Shapes;

public class LineShape : ShapeBase
{
    public LineShape(TimelineClip clip)
        : base(clip, "Ligne")
    {
    }

    public int X1 { get; set; }

    public int Y1 { get; set; }

    public int X2 { get; set; } = 127;

    public int Y2 { get; set; } = 127;

    public int GridWidth { get; set; } = 128;

    public int GridHeight { get; set; } = 128;

    public override IEnumerable<int> GetEntityIds()
    {
        var ids = new List<int>();
        var x = X1;
        var y = Y1;
        var dx = Math.Abs(X2 - X1);
        var dy = -Math.Abs(Y2 - Y1);
        var sx = X1 < X2 ? 1 : -1;
        var sy = Y1 < Y2 ? 1 : -1;
        var error = dx + dy;

        while (true)
        {
            AddIfInside(ids, x, y);

            if (x == X2 && y == Y2)
            {
                break;
            }

            var twiceError = 2 * error;
            if (twiceError >= dy)
            {
                error += dy;
                x += sx;
            }

            if (twiceError <= dx)
            {
                error += dx;
                y += sy;
            }
        }

        return ids;
    }

    protected void AddIfInside(ICollection<int> ids, int x, int y)
    {
        if (x >= 0 && x < GridWidth && y >= 0 && y < GridHeight)
        {
            ids.Add((y * GridWidth) + x);
        }
    }
}
