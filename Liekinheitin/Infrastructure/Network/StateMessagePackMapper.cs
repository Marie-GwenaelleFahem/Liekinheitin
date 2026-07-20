using Liekinheitin.Domain.Entities;

namespace Liekinheitin.Infrastructure.Network;

/// <summary>
/// Convertit entre <c>State</c>/<c>Entity</c> (Domain, purs) et leurs équivalents
/// <see cref="StateDto"/>/<see cref="EntityDto"/> (portant les attributs MessagePack).
/// </summary>
internal static class StateMessagePackMapper
{
    public static StateDto ToDto(State state) => new()
    {
        Entities = state.Entities
            .Select(e => new EntityDto { Id = e.Id, Channels = e.Channels })
            .ToList(),
    };

    public static State ToDomain(StateDto dto) => new()
    {
        Entities = dto.Entities
            .Select(e => new Entity { Id = e.Id, Channels = e.Channels })
            .ToList(),
    };
}
