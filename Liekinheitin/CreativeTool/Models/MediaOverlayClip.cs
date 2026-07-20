namespace Liekinheitin.CreativeTool.Models
{
    public sealed class MediaOverlayClip
    {
        public string Name { get; set; } = "Média";
        public string FilePath { get; set; } = string.Empty;
        public double StartTime { get; set; }
        public double Duration { get; set; } = 3;
        public double Opacity { get; set; } = 1;
        public string Id { get; set; } = System.Guid.NewGuid().ToString("N");
        public double OffsetX { get; set; }
        public double OffsetY { get; set; }
        public bool IsHidden { get; set; }
    }
}
