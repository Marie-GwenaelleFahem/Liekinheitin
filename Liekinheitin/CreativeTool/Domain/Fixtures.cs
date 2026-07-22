namespace Liekinheitin.CreativeTool.Domain
{
    public sealed class StaticProjector
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte W { get; set; }
    }

    public sealed class MovingHead
    {
        public int EntityId { get; init; }
        public int Index { get; init; }

        public byte Pan { get; set; }
        public byte Tilt { get; set; }
        public byte Speed { get; set; }
        public byte Dimming { get; set; } = 255;
        public byte Strobe { get; set; }
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte W { get; set; }
    }
}