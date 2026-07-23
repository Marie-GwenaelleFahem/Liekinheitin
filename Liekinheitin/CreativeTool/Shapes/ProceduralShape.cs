using System;
using System.Collections.Generic;
using Liekinheitin.CreativeTool.Models;

namespace Liekinheitin.CreativeTool.Shapes;

public sealed class ProceduralShape : ShapeBase
{
    private readonly Func<double, double, bool> _contains;

    public ProceduralShape(TimelineClip clip, string displayName, string category, Func<double, double, bool> contains)
        : base(clip, displayName, category)
    {
        _contains = contains;
    }

    public int CenterX { get; init; } = 64;

    public int CenterY { get; init; } = 64;

    public int GridWidth { get; init; } = 128;

    public int GridHeight { get; init; } = 128;

    public override IEnumerable<int> GetEntityIds()
    {
        for (var y = 0; y < GridHeight; y++)
        {
            for (var x = 0; x < GridWidth; x++)
            {
                if (_contains(x - CenterX, y - CenterY))
                {
                    yield return (y * GridWidth) + x;
                }
            }
        }
    }
}
