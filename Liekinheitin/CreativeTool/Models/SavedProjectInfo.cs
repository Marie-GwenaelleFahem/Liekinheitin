using System;

namespace Liekinheitin.CreativeTool.Models
{
    public sealed class SavedProjectInfo
    {
        public required string Name { get; init; }
        public required string FilePath { get; init; }
        public required DateTime ModifiedAt { get; init; }

        public string ModifiedText => ModifiedAt.ToString("dd/MM/yyyy HH:mm");
    }
}
