using System;
using System.Collections.Generic;
using Liekinheitin.CreativeTool.Models;

namespace Liekinheitin.CreativeTool.Shapes;

public abstract class ChessShapeBase : ShapeBase
{
    protected ChessShapeBase(TimelineClip clip, string displayName) : base(clip, displayName) { }

    public int CenterX { get; set; } = 64;
    public int CenterY { get; set; } = 64;
    public int GridWidth { get; set; } = 128;
    public int GridHeight { get; set; } = 128;

    protected void AddEllipse(ISet<int> ids, double cx, double cy, double radiusX, double radiusY)
    {
        for (var y = (int)Math.Floor(cy - radiusY); y <= Math.Ceiling(cy + radiusY); y++)
        for (var x = (int)Math.Floor(cx - radiusX); x <= Math.Ceiling(cx + radiusX); x++)
        {
            if (x < 0 || x >= GridWidth || y < 0 || y >= GridHeight) continue;
            var dx = (x - cx) / radiusX;
            var dy = (y - cy) / radiusY;
            if ((dx * dx) + (dy * dy) <= 1) ids.Add((y * GridWidth) + x);
        }
    }

    protected void AddRoundedBase(ISet<int> ids, int centerX, int centerY, int width, int height)
    {
        var radius = height / 2.0;
        AddEllipse(ids, centerX - (width / 2.0) + radius, centerY, radius, radius);
        AddEllipse(ids, centerX + (width / 2.0) - radius, centerY, radius, radius);
        for (var y = centerY - height / 2; y <= centerY + height / 2; y++)
        for (var x = centerX - width / 2 + (int)radius; x <= centerX + width / 2 - (int)radius; x++)
            if (x >= 0 && x < GridWidth && y >= 0 && y < GridHeight) ids.Add((y * GridWidth) + x);
    }
}
