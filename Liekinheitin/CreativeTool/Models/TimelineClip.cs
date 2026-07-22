using System.Collections.Generic;

namespace Liekinheitin.CreativeTool.Models
{
    public class TimelineClip
    {
        public string Name { get; set; } = "Clip";
        public double StartTime { get; set; }
        public double Duration { get; set; } = 1.0;
        public EffectType EffectType { get; set; } = EffectType.SolidColor;
        public bool IsAudio { get; set; }
        public bool IsHidden { get; set; }
        public bool IsMedia { get; set; }
        public string? MediaOverlayId { get; set; }
        public TargetSelection Target { get; set; } = TargetSelection.FullWall();
        public RgbwColor Color { get; set; } = RgbwColor.White;
        public double Intensity { get; set; } = 1.0;
        public double Speed { get; set; } = 1.0;
        public string TextContent { get; set; } = string.Empty;
        public double VisualScale { get; set; } = 1.0;
        public int ParticleCount { get; set; } = 96;
        public ParticlePlacement ParticlePlacement { get; set; } = ParticlePlacement.FullWall;

        // La sélection de pixels garantit le rendu exact ; ce nom conserve la forme
        // organique d'origine afin de pouvoir la retrouver et la remplacer dans FORMES.
        public string SourceShapeName { get; set; } = "Toile organique — mur complet";

        public double? RippleCenterX { get; set; }
        public double? RippleCenterY { get; set; }
        public MovementEffectType MovementEffect { get; set; } = MovementEffectType.None;
        public int MovementOffsetX { get; set; }
        public int MovementOffsetY { get; set; }
        public double RotationDegrees { get; set; }
        public bool IsMotionDraft { get; set; }
        public List<MovementKeyframe> MovementKeyframes { get; set; } = new();
        public double EndTime => StartTime + Duration;
    }
}
