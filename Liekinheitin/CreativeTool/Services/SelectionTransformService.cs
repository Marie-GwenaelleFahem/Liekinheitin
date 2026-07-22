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

    public static List<int> ResizeToBounds(
        IReadOnlyCollection<int> entityIds,
        int newMinX,
        int newMaxX,
        int newMinY,
        int newMaxY,
        int width,
        int height)
    {
        if (entityIds.Count == 0) return entityIds.ToList();

        var source = entityIds
            .Where(id => id >= 0 && id < width * height)
            .ToHashSet();
        if (source.Count == 0) return new List<int>();

        var sourcePoints = source.Select(id => (X: id % width, Y: id / width)).ToList();
        var oldMinX = sourcePoints.Min(point => point.X);
        var oldMaxX = sourcePoints.Max(point => point.X);
        var oldMinY = sourcePoints.Min(point => point.Y);
        var oldMaxY = sourcePoints.Max(point => point.Y);

        newMinX = Math.Clamp(newMinX, 0, width - 1);
        newMaxX = Math.Clamp(newMaxX, newMinX, width - 1);
        newMinY = Math.Clamp(newMinY, 0, height - 1);
        newMaxY = Math.Clamp(newMaxY, newMinY, height - 1);

        var oldSpanX = Math.Max(1, oldMaxX - oldMinX);
        var oldSpanY = Math.Max(1, oldMaxY - oldMinY);
        var newSpanX = Math.Max(1, newMaxX - newMinX);
        var newSpanY = Math.Max(1, newMaxY - newMinY);
        var resized = new List<int>();

        for (var y = newMinY; y <= newMaxY; y++)
        {
            var sourceY = oldMinY + (int)Math.Round((y - newMinY) / (double)newSpanY * oldSpanY);
            for (var x = newMinX; x <= newMaxX; x++)
            {
                var sourceX = oldMinX + (int)Math.Round((x - newMinX) / (double)newSpanX * oldSpanX);
                if (source.Contains((sourceY * width) + sourceX))
                {
                    resized.Add((y * width) + x);
                }
            }
        }

        return resized.Distinct().ToList();
    }
}
