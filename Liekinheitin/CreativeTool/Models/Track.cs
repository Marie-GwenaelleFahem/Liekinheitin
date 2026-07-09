using System.Collections.Generic;

namespace Liekinheitin.CreativeTool.Models
{
    public class Track
    {
        public string Name { get; set; } = "Piste";

        public bool IsMuted { get; set; }

        public List<TimelineClip> Clips { get; set; } = new();
    }
}
