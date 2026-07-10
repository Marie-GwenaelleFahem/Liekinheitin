using System.Collections.Generic;
using Liekinheitin.CreativeTool.Models;

namespace Liekinheitin.CreativeTool.Shapes;

public abstract class ShapeBase : IShape
{
    protected ShapeBase(TimelineClip clip, string displayName)
    {
        Clip = clip;
        DisplayName = displayName;
    }

    public TimelineClip Clip { get; set; }

    public string DisplayName { get; }

    public abstract IEnumerable<int> GetEntityIds();
}
