using System.Collections.Generic;
using Liekinheitin.CreativeTool.Models;

namespace Liekinheitin.CreativeTool.Shapes;

public static class ShapeFactory
{
    public static IReadOnlyList<IShape> CreateForClip(TimelineClip? clip, int gridWidth = 128, int gridHeight = 128)
    {
        clip ??= new TimelineClip { Target = new TargetSelection { Type = TargetType.Selection } };

        return
        [
            new RectangleShape(clip) { X = 24, Y = 24, Width = 32, Height = 24, GridWidth = gridWidth, GridHeight = gridHeight },
            new CircleShape(clip) { CenterX = gridWidth / 2, CenterY = gridHeight / 2, Radius = 18, GridWidth = gridWidth, GridHeight = gridHeight },
            new LineShape(clip) { X1 = 16, Y1 = 16, X2 = gridWidth - 17, Y2 = gridHeight - 17, GridWidth = gridWidth, GridHeight = gridHeight },
            new KnightShape(clip) { CenterX = gridWidth / 2, CenterY = gridHeight / 2, GridWidth = gridWidth, GridHeight = gridHeight },
            new CrownShape(clip) { CenterX = gridWidth / 2, CenterY = gridHeight / 2, GridWidth = gridWidth, GridHeight = gridHeight },
            new TriangleShape(clip) { GridWidth = gridWidth, GridHeight = gridHeight },
            new PolygonShape(clip) { GridWidth = gridWidth, GridHeight = gridHeight }
        ];
    }
}
