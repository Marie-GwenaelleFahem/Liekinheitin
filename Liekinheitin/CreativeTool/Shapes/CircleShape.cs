using System;
using System.Collections.Generic;
using Liekinheitin.CreativeTool.Models;

namespace Liekinheitin.CreativeTool.Shapes;

public class CircleShape : ShapeBase
{
    public CircleShape(TimelineClip clip)
        : base(clip, "Cercle")
    {
    }

    public int CenterX { get; set; } = 64;

    public int CenterY { get; set; } = 64;

    public int Radius { get; set; } = 16;

    public int GridWidth { get; set; } = 128;

    public int GridHeight { get; set; } = 128;

    public override IEnumerable<int> GetEntityIds()
    {
        var ids = new List<int>();
        var radiusSquared = Radius * Radius;
        var minX = Math.Clamp(CenterX - Radius, 0, GridWidth - 1);
        var maxX = Math.Clamp(CenterX + Radius, 0, GridWidth - 1);
        var minY = Math.Clamp(CenterY - Radius, 0, GridHeight - 1);
        var maxY = Math.Clamp(CenterY + Radius, 0, GridHeight - 1);

        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                var dx = x - CenterX;
                var dy = y - CenterY;
                if ((dx * dx) + (dy * dy) <= radiusSquared)
                {
                    ids.Add((y * GridWidth) + x);
                }
            }
        }

        return ids;
    }
}
