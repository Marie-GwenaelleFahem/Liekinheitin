using System;
using System.Collections.Generic;
using Liekinheitin.CreativeTool.Models;

namespace Liekinheitin.CreativeTool.Shapes;

public sealed class KnightShape : ChessShapeBase
{
    public KnightShape(TimelineClip clip) : base(clip, "Cavalier d’échecs") { }

    public override IEnumerable<int> GetEntityIds()
    {
        var ids = new HashSet<int>();
        AddRoundedBase(ids, CenterX, CenterY + 22, 38, 8);
        AddRoundedBase(ids, CenterX, CenterY + 16, 30, 7);

        for (var y = CenterY - 5; y <= CenterY + 16; y++)
        {
            var progress = (y - (CenterY - 5)) / 21.0;
            var left = CenterX - 10 + (int)Math.Round(progress * 5);
            var right = CenterX + 10 + (int)Math.Round((1 - progress) * 3);
            for (var x = left; x <= right; x++) ids.Add((y * GridWidth) + x);
        }

        AddEllipse(ids, CenterX + 1, CenterY - 10, 11, 10);
        AddEllipse(ids, CenterX + 10, CenterY - 6, 10, 6);
        AddEllipse(ids, CenterX - 5, CenterY - 20, 4, 10);
        AddEllipse(ids, CenterX + 3, CenterY - 19, 3, 8);

        // Œil et courbe sous le museau : espaces négatifs pour une silhouette plus lisible.
        RemoveEllipse(ids, CenterX + 4, CenterY - 12, 2, 2);
        RemoveEllipse(ids, CenterX + 11, CenterY - 2, 5, 3);
        return ids;
    }

    private void RemoveEllipse(ISet<int> ids, double cx, double cy, double radiusX, double radiusY)
    {
        for (var y = (int)(cy - radiusY); y <= cy + radiusY; y++)
        for (var x = (int)(cx - radiusX); x <= cx + radiusX; x++)
        {
            var dx = (x - cx) / radiusX;
            var dy = (y - cy) / radiusY;
            if ((dx * dx) + (dy * dy) <= 1) ids.Remove((y * GridWidth) + x);
        }
    }
}
