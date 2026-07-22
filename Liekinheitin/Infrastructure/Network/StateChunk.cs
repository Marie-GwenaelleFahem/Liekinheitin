using Liekinheitin.Domain.Entities;

namespace Liekinheitin.Infrastructure.Network
{
    /// <summary>
    /// Fragment d'un State trop volumineux pour un seul datagramme UDP.
    /// Le récepteur regroupe les chunks par MessageId jusqu'à en avoir TotalChunks.
    /// </summary>
    public sealed class StateChunk
    {
        public Guid MessageId { get; set; }
        public int ChunkIndex { get; set; }
        public int TotalChunks { get; set; }
        public List<Entity> Entities { get; set; } = new();
    }
}