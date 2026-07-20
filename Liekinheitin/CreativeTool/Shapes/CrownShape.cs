using System;
using System.Collections.Generic;
using Liekinheitin.CreativeTool.Models;

namespace Liekinheitin.CreativeTool.Shapes;

public sealed class CrownShape : ChessShapeBase
{
    public CrownShape(TimelineClip clip) : base(clip, "Couronne royale") { }

    public override IEnumerable<int> GetEntityIds()
    {
        var ids = new HashSet<int>();
        AddRoundedBase(ids, CenterX, CenterY + 18, 42, 8);
        AddRoundedBase(ids, CenterX, CenterY + 12, 34, 7);

        for (var y = CenterY - 8; y <= CenterY + 11; y++)
        {
            var progress = (y - (CenterY - 8)) / 19.0;
            var halfWidth = 10 + (int)Math.Round(progress * 7);
            for (var x = CenterX - halfWidth; x <= CenterX + halfWidth; x++) ids.Add((y * GridWidth) + x);
        }

        AddSpike(ids, CenterX - 13, CenterY - 5, CenterY - 19, 7);
        AddSpike(ids, CenterX, CenterY - 7, CenterY - 25, 8);
        AddSpike(ids, CenterX + 13, CenterY - 5, CenterY - 19, 7);
        AddEllipse(ids, CenterX - 13, CenterY - 20, 3, 3);
        AddEllipse(ids, CenterX, CenterY - 26, 3, 3);
        AddEllipse(ids, CenterX + 13, CenterY - 20, 3, 3);
        return ids;
    }

    private void AddSpike(ISet<int> ids, int centerX, int baseY, int apexY, int halfWidth)
    {
        for (var y = apexY; y <= baseY; y++)
        {
            var progress = (y - apexY) / (double)Math.Max(1, baseY - apexY);
            var width = (int)Math.Round(halfWidth * progress);
            for (var x = centerX - width; x <= centerX + width; x++) ids.Add((y * GridWidth) + x);
        }
    }
}
