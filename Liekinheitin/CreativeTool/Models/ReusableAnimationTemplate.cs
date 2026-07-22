using System.Collections.Generic;

namespace Liekinheitin.CreativeTool.Models
{
    public sealed class ReusableAnimationTemplate
    {
        public string Name { get; set; } = "Animation réutilisable";

        public string Category { get; set; } = "Mes animations";

        public int WallWidth { get; set; } = 128;

        public int WallHeight { get; set; } = 128;

        public double Duration { get; set; } = 1;

        public List<Track> Tracks { get; set; } = new();
    }
}
