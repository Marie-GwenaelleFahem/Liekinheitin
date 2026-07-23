using System.Collections.Generic;

namespace Liekinheitin.CreativeTool.Models
{
    public class TargetSelection
    {
        public TargetType Type { get; set; } = TargetType.FullWall;

        public List<int> EntityIds { get; set; } = new();

        public string? TrackName { get; set; }

        public static TargetSelection FullWall() => new() { Type = TargetType.FullWall };
    }
}
