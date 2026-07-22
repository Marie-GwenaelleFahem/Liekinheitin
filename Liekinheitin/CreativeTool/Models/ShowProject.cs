using System.Collections.Generic;

namespace Liekinheitin.CreativeTool.Models
{
    public class ShowProject
    {
        public string Name { get; set; } = "Nouveau projet";

        public double Duration { get; set; } = 30.0;

        public int WallWidth { get; set; } = 128;

        public int WallHeight { get; set; } = 128;

        public string? AudioFilePath { get; set; }

        public double AudioVolume { get; set; } = 1.0;

        public double AudioFadeOutDuration { get; set; }

        public List<Track> Tracks { get; set; } = new();

        public List<MediaOverlayClip> MediaOverlays { get; set; } = new();
    }
}
