using System.Collections.Generic;
using Liekinheitin.CreativeTool.Models;

namespace Liekinheitin.CreativeTool.Shapes;

public abstract class ShapeBase : IShape
{
    protected ShapeBase(TimelineClip clip, string displayName, string category = "Classiques")
    {
        Clip = clip;
        DisplayName = displayName;
        Category = category;
    }

    public TimelineClip Clip { get; set; }

    public string DisplayName { get; }

    public string Category { get; }

    public abstract IEnumerable<int> GetEntityIds();
}
