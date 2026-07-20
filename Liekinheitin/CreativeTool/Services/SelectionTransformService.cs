using System;
using System.Collections.Generic;
using System.Linq;

namespace Liekinheitin.CreativeTool.Services;

public static class SelectionTransformService
{
    public static List<int> Scale(IReadOnlyCollection<int> entityIds, double factor, int width, int height)
    {
        if (entityIds.Count == 0 || factor <= 0) return entityIds.ToList();
        var points = entityIds.Select(id => (X: id % width, Y: id / width)).ToList();
        var centerX = (points.Min(p => p.X) + points.Max(p => p.X)) / 2.0;
        var centerY = (points.Min(p => p.Y) + points.Max(p => p.Y)) / 2.0;

        return points.Select(point =>
            {
                var x = Math.Clamp((int)Math.Round(centerX + ((point.X - centerX) * factor)), 0, width - 1);
                var y = Math.Clamp((int)Math.Round(centerY + ((point.Y - centerY) * factor)), 0, height - 1);
                return (y * width) + x;
            })
            .Distinct()
            .ToList();
    }
}
