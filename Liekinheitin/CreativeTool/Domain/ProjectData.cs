using System;
using System.Collections.Generic;

namespace Liekinheitin.CreativeTool.Domain
{
    /// <summary>Format sérialisable d'un projet complet — formes posées, pistes d'animation,
    /// durée totale. Utilise des composants byte séparés pour les couleurs plutôt que Color
    /// (System.Text.Json ne sait pas sérialiser Color nativement).</summary>
    public sealed class ProjectData
    {
        public double TotalDuration { get; set; }
        public List<ShapeData> Shapes { get; set; } = new();
        public List<ShapeTrackData> ShapeTracks { get; set; } = new();
        public List<FixtureTrackData> FixtureTracks { get; set; } = new();
    }

    public sealed class ShapeData
    {
        public Guid Id { get; set; }
        public ShapeType Type { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int BaseWidth { get; set; }
        public int BaseHeight { get; set; }
        public double Scale { get; set; }
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
    }

    public sealed class ShapeKeyframeData
    {
        public double TimeSeconds { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int BaseWidth { get; set; }
        public int BaseHeight { get; set; }
        public double Scale { get; set; }
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
    }

    public sealed class ShapeTrackData
    {
        public Guid ShapeId { get; set; }
        public ShapeType Type { get; set; }
        public List<ShapeKeyframeData> Keyframes { get; set; } = new();
    }

    public sealed class FixtureKeyframeData
    {
        public double TimeSeconds { get; set; }
        public byte Pan { get; set; }
        public byte Tilt { get; set; }
        public byte Speed { get; set; }
        public byte Dimming { get; set; }
        public byte Strobe { get; set; }
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte W { get; set; }
    }

    public sealed class FixtureTrackData
    {
        public int EntityId { get; set; }
        public List<FixtureKeyframeData> Keyframes { get; set; } = new();
    }
}