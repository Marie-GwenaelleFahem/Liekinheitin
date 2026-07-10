namespace Liekinheitin.CreativeTool.Models
{
    public class TimelineClip
    {
        public string Name { get; set; } = "Clip";

        public double StartTime { get; set; }

        public double Duration { get; set; } = 1.0;

        public EffectType EffectType { get; set; } = EffectType.SolidColor;

        public TargetSelection Target { get; set; } = TargetSelection.FullWall();

        public RgbwColor Color { get; set; } = RgbwColor.White;

        public double Intensity { get; set; } = 1.0;

        public double Speed { get; set; } = 1.0;

        public MovementEffectType MovementEffect { get; set; } = MovementEffectType.None;

        public int MovementOffsetX { get; set; }

        public int MovementOffsetY { get; set; }

        public double EndTime => StartTime + Duration;
    }
}
