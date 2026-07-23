using MessagePack;

namespace Liekinheitin.Infrastructure.Network;

/// <summary>
/// Équivalent MessagePack d'<c>Entity</c> (Domain), utilisé uniquement pour la sérialisation
/// binaire sur le réseau.
/// </summary>
/// <remarks>
/// Domain ne doit dépendre d'aucun package externe (voir le commentaire dans
/// Liekinheitin.Domain.csproj) : les attributs <c>[MessagePackObject]</c>/<c>[Key]</c>, qui
/// viennent du package MessagePack, ne peuvent donc pas être posés directement sur <c>Entity</c>.
/// Ce DTO existe pour porter ces attributs à sa place, sur le même principe que les DTO privés
/// de <c>JsonPatchLoader</c> pour patch.json. La conversion vers/depuis <c>Entity</c> se fait via
/// <see cref="StateMessagePackMapper"/>.
/// </remarks>
[MessagePackObject]
public class EntityDto
{
    [Key(0)]
    public int Id { get; set; }

    [Key(1)]
    public byte[] Channels { get; set; } = Array.Empty<byte>();
}
