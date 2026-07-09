using System.Collections.Generic;

namespace Liekinheitin.CreativeTool.Models
{
    public class ShowProject
    {
        public string Name { get; set; } = "Nouveau projet";

        public double Duration { get; set; } = 30.0;

        public int WallWidth { get; set; } = 128;

        public int WallHeight { get; set; } = 128;

        public List<Track> Tracks { get; set; } = new();
    }
}
