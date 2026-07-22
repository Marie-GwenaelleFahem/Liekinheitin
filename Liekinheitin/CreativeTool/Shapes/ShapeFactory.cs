using System;
using System.Collections.Generic;
using Liekinheitin.CreativeTool.Models;

namespace Liekinheitin.CreativeTool.Shapes;

public static class ShapeFactory
{
    public static IReadOnlyList<IShape> CreateForClip(TimelineClip? clip, int gridWidth = 128, int gridHeight = 128)
    {
        clip ??= new TimelineClip { Target = new TargetSelection { Type = TargetType.Selection } };
        var shapes = new List<IShape>(36);
        AddBrushCircles(shapes, clip, gridWidth, gridHeight);
        AddBrokenHalos(shapes, clip, gridWidth, gridHeight);
        AddDrops(shapes, clip, gridWidth, gridHeight);
        AddRibbons(shapes, clip, gridWidth, gridHeight);
        AddSpirals(shapes, clip, gridWidth, gridHeight);
        AddOrganicBlobs(shapes, clip, gridWidth, gridHeight);
        return shapes;
    }

    private static ProceduralShape Shape(TimelineClip clip, string name, string category, int width, int height, Func<double, double, bool> contains)
        => new(clip, name, category, contains)
        {
            CenterX = width / 2,
            CenterY = height / 2,
            GridWidth = width,
            GridHeight = height
        };

    private static void AddBrushCircles(ICollection<IShape> shapes, TimelineClip clip, int width, int height)
    {
        for (var index = 0; index < 6; index++)
        {
            var radius = 13 + (index * 4.2);
            var thickness = 2.2 + (index * 0.55);
            var phase = index * 0.63;
            shapes.Add(Shape(clip, $"Cercle au pinceau {index + 1}", "Traits au pinceau", width, height,
                (x, y) =>
                {
                    var angle = Math.Atan2(y, x);
                    var distance = Math.Sqrt((x * x) + (y * y));
                    var imperfectRadius = radius + (Math.Sin((angle * 3) + phase) * 1.4) + (Math.Sin((angle * 7) - phase) * 0.65);
                    return Math.Abs(distance - imperfectRadius) <= thickness;
                }));
        }
    }

    private static void AddBrokenHalos(ICollection<IShape> shapes, TimelineClip clip, int width, int height)
    {
        for (var index = 0; index < 6; index++)
        {
            var radius = 15 + (index * 4.0);
            var phase = index * 0.72;
            shapes.Add(Shape(clip, $"Halo dessiné {index + 1}", "Halos & anneaux", width, height,
                (x, y) =>
                {
                    var angle = Math.Atan2(y, x);
                    var distance = Math.Sqrt((x * x) + (y * y));
                    var edge = radius + (Math.Sin((angle * 4) + phase) * 1.7);
                    var handGap = Math.Sin((angle * 2.5) - phase) > -0.72;
                    return handGap && Math.Abs(distance - edge) <= 2.4 + ((index % 3) * 0.7);
                }));
        }
    }

    private static void AddDrops(ICollection<IShape> shapes, TimelineClip clip, int width, int height)
    {
        for (var index = 0; index < 6; index++)
        {
            var size = 17 + (index * 2.8);
            var hollow = index >= 3;
            shapes.Add(Shape(clip, $"Goutte {(hollow ? "contour" : "encre")} {index + 1}", "Gouttes", width, height,
                (x, y) =>
                {
                    bool Inside(double scale)
                    {
                        var shiftedY = y + (size * 0.15);
                        var normalizedY = shiftedY / (size * scale);
                        if (normalizedY < -1 || normalizedY > 1.35) return false;
                        var taper = normalizedY <= 0.15
                            ? Math.Sqrt(Math.Max(0, 1 - (normalizedY * normalizedY)))
                            : Math.Max(0, 1 - ((normalizedY - 0.15) / 1.2));
                        var wobble = Math.Sin((y * 0.22) + index) * 0.8;
                        return Math.Abs(x + wobble) <= size * 0.72 * scale * taper;
                    }

                    return Inside(1) && (!hollow || !Inside(0.76));
                }));
        }
    }

    private static void AddRibbons(ICollection<IShape> shapes, TimelineClip clip, int width, int height)
    {
        for (var index = 0; index < 6; index++)
        {
            var amplitude = 6 + (index * 2.4);
            var frequency = 6.5 + (index * 1.15);
            var thickness = 1.8 + (index * 0.5);
            shapes.Add(Shape(clip, $"Ruban libre {index + 1}", "Rubans & ondes", width, height,
                (x, y) =>
                {
                    if (Math.Abs(x) > 49) return false;
                    var stroke = (Math.Sin(x / frequency) * amplitude)
                                 + (Math.Sin((x / (frequency * 0.42)) + index) * 1.4);
                    return Math.Abs(y - stroke) <= thickness;
                }));
        }
    }

    private static void AddSpirals(ICollection<IShape> shapes, TimelineClip clip, int width, int height)
    {
        for (var index = 0; index < 6; index++)
        {
            var maximumRadius = 22 + (index * 3.2);
            var density = 0.34 + (index * 0.035);
            var thickness = 0.76 + (index * 0.08);
            shapes.Add(Shape(clip, $"Spirale à main levée {index + 1}", "Spirales", width, height,
                (x, y) =>
                {
                    var radius = Math.Sqrt((x * x) + (y * y));
                    if (radius < 3 || radius > maximumRadius) return false;
                    var angle = Math.Atan2(y, x);
                    var stroke = Math.Abs(Math.Sin((radius * density) - angle + (Math.Sin(angle * 3) * 0.12)));
                    return stroke <= thickness * 0.22;
                }));
        }
    }

    private static void AddOrganicBlobs(ICollection<IShape> shapes, TimelineClip clip, int width, int height)
    {
        for (var index = 0; index < 6; index++)
        {
            var baseRadius = 15 + (index * 3.2);
            var hollow = index % 2 == 1;
            var phase = index * 0.81;
            shapes.Add(Shape(clip, $"Tache organique {index + 1}", "Formes organiques", width, height,
                (x, y) =>
                {
                    var angle = Math.Atan2(y, x);
                    var distance = Math.Sqrt((x * x) + (y * y));
                    var edge = baseRadius
                               + (Math.Sin((angle * 3) + phase) * 3.1)
                               + (Math.Sin((angle * 5) - phase) * 1.8)
                               + (Math.Sin((angle * 8) + (phase * 0.5)) * 0.8);
                    return hollow ? distance <= edge && distance >= edge - 3.2 : distance <= edge;
                }));
        }
    }
}
