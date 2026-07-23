namespace Liekinheitin.Infrastructure.Network;

/// <summary>
/// Recolle, côté réception, les morceaux UDP découpés par <see cref="UdpChunkSender"/>.
/// </summary>
/// <remarks>
/// Ne garde en mémoire qu'un seul message en cours d'assemblage à la fois : à ~40 envois par
/// seconde, seul le dernier état complet compte réellement pour l'affichage/le routage — un
/// assemblage plus ancien et resté incomplet (morceau perdu en route) est simplement abandonné
/// dès qu'un morceau d'un message plus récent arrive, plutôt que d'accumuler indéfiniment des
/// messages jamais terminés en mémoire. Une instance de cette classe traite les morceaux reçus
/// dans n'importe quel ordre (UDP ne garantit pas l'ordre d'arrivée).
/// </remarks>
internal class UdpChunkReassembler
{
    private const int HeaderSize = 6;

    private ushort? _currentMessageId;
    private byte[]?[] _chunks = Array.Empty<byte[]?>();
    private int _receivedCount;

    /// <summary>
    /// Traite un paquet UDP reçu.
    /// </summary>
    /// <param name="packet">Le paquet brut reçu sur le socket.</param>
    /// <returns>
    /// Le message complet reconstitué dès que tous ses morceaux sont arrivés, ou <c>null</c>
    /// s'il en manque encore (ou si le paquet reçu est invalide/hors limites).
    /// </returns>
    public byte[]? Receive(byte[] packet)
    {
        if (packet.Length < HeaderSize)
            return null;

        ushort messageId = ReadUInt16(packet, 0);
        ushort chunkIndex = ReadUInt16(packet, 2);
        ushort chunkCount = ReadUInt16(packet, 4);

        if (_currentMessageId != messageId)
        {
            // Nouveau message : l'assemblage précédent, terminé ou non, est abandonné.
            _currentMessageId = messageId;
            _chunks = new byte[chunkCount][];
            _receivedCount = 0;
        }

        if (chunkIndex >= _chunks.Length || _chunks[chunkIndex] is not null)
            return null; // morceau hors limites, ou déjà reçu (doublon réseau) : ignoré

        int dataLength = packet.Length - HeaderSize;
        var data = new byte[dataLength];
        Array.Copy(packet, HeaderSize, data, 0, dataLength);
        _chunks[chunkIndex] = data;
        _receivedCount++;

        if (_receivedCount < _chunks.Length)
            return null;

        var result = new byte[_chunks.Sum(c => c!.Length)];
        int writeOffset = 0;
        foreach (var chunk in _chunks)
        {
            Array.Copy(chunk!, 0, result, writeOffset, chunk!.Length);
            writeOffset += chunk!.Length;
        }

        _currentMessageId = null; // prêt pour le prochain message
        return result;
    }

    private static ushort ReadUInt16(byte[] buffer, int offset)
        => (ushort)(buffer[offset] | (buffer[offset + 1] << 8));
}
