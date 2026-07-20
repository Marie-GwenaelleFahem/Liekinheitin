namespace Liekinheitin.Infrastructure.Network;

/// <summary>
/// Découpe un message binaire (par exemple un <c>State</c> sérialisé en MessagePack) en
/// plusieurs paquets UDP ("chunks") d'au plus <see cref="MaxChunkPayloadSize"/> octets de
/// données utiles chacun.
/// </summary>
/// <remarks>
/// Un <c>State</c> complet (mur de ~16 500 LED) peut largement dépasser la taille où UDP
/// risquerait de fragmenter le paquet au niveau réseau (perte accrue si un seul fragment se
/// perd) ; découper en petits morceaux, chacun envoyé comme un paquet UDP indépendant, évite ce
/// risque. Chaque morceau porte un petit en-tête de 6 octets : l'identifiant du message
/// (<c>messageId</c>, pour que <see cref="UdpChunkReassembler"/> ne mélange jamais deux
/// messages différents), la position du morceau dans le message (<c>chunkIndex</c>), et le
/// nombre total de morceaux (<c>chunkCount</c>).
/// </remarks>
internal static class UdpChunkSender
{
    /// <summary>Taille maximale, en octets, des données utiles d'un morceau (hors en-tête).</summary>
    public const int MaxChunkPayloadSize = 500;

    private const int HeaderSize = 6;

    /// <summary>Découpe <paramref name="payload"/> en morceaux prêts à être envoyés tels quels sur un socket UDP.</summary>
    /// <param name="payload">Le message binaire complet à découper.</param>
    /// <param name="messageId">
    /// Identifiant unique de ce message, pour que le récepteur puisse distinguer les morceaux
    /// de deux envois différents (par exemple un compteur incrémenté à chaque appel).
    /// </param>
    public static IEnumerable<byte[]> Split(byte[] payload, ushort messageId)
    {
        int chunkCount = Math.Max(1, (int)Math.Ceiling(payload.Length / (double)MaxChunkPayloadSize));

        for (int i = 0; i < chunkCount; i++)
        {
            int offset = i * MaxChunkPayloadSize;
            int length = Math.Min(MaxChunkPayloadSize, payload.Length - offset);

            var packet = new byte[HeaderSize + length];
            WriteUInt16(packet, 0, messageId);
            WriteUInt16(packet, 2, (ushort)i);
            WriteUInt16(packet, 4, (ushort)chunkCount);
            Array.Copy(payload, offset, packet, HeaderSize, length);

            yield return packet;
        }
    }

    private static void WriteUInt16(byte[] buffer, int offset, ushort value)
    {
        buffer[offset] = (byte)(value & 0xFF);
        buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
    }
}
