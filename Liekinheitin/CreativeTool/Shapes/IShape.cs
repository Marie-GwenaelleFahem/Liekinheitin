using System.Collections.Generic;
using Liekinheitin.CreativeTool.Models;

namespace Liekinheitin.CreativeTool.Shapes;

public interface IShape
{
    TimelineClip Clip { get; set; }

    string DisplayName { get; }

    IEnumerable<int> GetEntityIds();
}
