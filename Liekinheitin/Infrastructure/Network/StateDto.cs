using MessagePack;

namespace Liekinheitin.Infrastructure.Network;

/// <summary>
/// Équivalent MessagePack de <c>State</c> (Domain), utilisé uniquement pour la sérialisation
/// binaire sur le réseau. Voir <see cref="EntityDto"/> pour la raison de son existence.
/// </summary>
[MessagePackObject]
public class StateDto
{
    [Key(0)]
    public List<EntityDto> Entities { get; set; } = new();
}
