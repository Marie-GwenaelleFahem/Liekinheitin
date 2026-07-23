using System;
using System.Collections.Generic;
using Liekinheitin.CreativeTool.Models;

namespace Liekinheitin.CreativeTool.Shapes;

public class RectangleShape : ShapeBase
{
    public RectangleShape(TimelineClip clip)
        : base(clip, "Rectangle")
    {
    }

    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public int GridWidth { get; set; } = 128;

    public int GridHeight { get; set; } = 128;

    public override IEnumerable<int> GetEntityIds()
    {
        var ids = new List<int>();
        var startX = Math.Clamp(X, 0, GridWidth - 1);
        var startY = Math.Clamp(Y, 0, GridHeight - 1);
        var endX = Math.Clamp(X + Width, 0, GridWidth);
        var endY = Math.Clamp(Y + Height, 0, GridHeight);

        for (var y = startY; y < endY; y++)
        {
            for (var x = startX; x < endX; x++)
            {
                ids.Add((y * GridWidth) + x);
            }
        }

        return ids;
    }
}
