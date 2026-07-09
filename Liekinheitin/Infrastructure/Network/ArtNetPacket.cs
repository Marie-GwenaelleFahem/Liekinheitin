namespace Liekinheitin.Infrastructure.Network;

/// <summary>
/// Représente un paquet ArtNet (message ArtDMX) prêt à être envoyé : l'IP du contrôleur
/// cible, l'univers concerné, et les 512 valeurs de canaux de cet univers.
/// </summary>
/// <remarks>
/// Cette classe vit ici, dans Infrastructure, et non dans Domain, car elle est
/// indissociable du protocole ArtNet : si ce protocole était remplacé, elle serait
/// remplacée avec lui. C'est <see cref="ArtNetSender"/> qui la construit à partir d'un
/// <c>DmxFrame</c> (Domain, indépendant du protocole) juste avant l'envoi.
/// </remarks>
public class ArtNetPacket
{
    private const int DmxDataLength = 512;

    /// <summary>Adresse IP du contrôleur destinataire.</summary>
    public string TargetIp { get; set; } = string.Empty;

    /// <summary>Univers DMX concerné (0 à 32767 selon la spécification ArtNet).</summary>
    public int Universe { get; set; }

    /// <summary>Les 512 valeurs de canaux de cet univers.</summary>
    public byte[] DmxData { get; set; } = new byte[DmxDataLength];

    /// <summary>
    /// Encode ce paquet au format binaire exact attendu par la spécification ArtNet
    /// (message ArtDMX, OpCode 0x5000).
    /// </summary>
    /// <returns>
    /// Un tableau d'octets prêt à être envoyé tel quel sur un socket UDP, port 6454.
    /// </returns>
    public byte[] ToBytes()
    {
        // 8 octets d'en-tête "Art-Net" (avec le zéro terminal) + 10 octets d'en-tête ArtDMX
        // + 512 octets de données = 530 octets au total.
        var data = DmxData.Length == DmxDataLength ? DmxData : ResizeTo512(DmxData);
        var bytes = new byte[8 + 10 + DmxDataLength];
        int offset = 0;

        // "Art-Net" suivi d'un octet nul.
        byte[] header = System.Text.Encoding.ASCII.GetBytes("Art-Net");
        Array.Copy(header, 0, bytes, offset, header.Length);
        offset += header.Length;
        bytes[offset++] = 0x00;

        // OpCode ArtDMX (0x5000), envoyé petit-boutiste (low byte, puis high byte).
        bytes[offset++] = 0x00;
        bytes[offset++] = 0x50;

        // Version du protocole ArtNet (14 = 0x000e), envoyée gros-boutiste.
        bytes[offset++] = 0x00;
        bytes[offset++] = 0x0e;

        // Sequence (0 = suivi de séquence désactivé) et Physical (port physique, 0 par défaut).
        bytes[offset++] = 0x00;
        bytes[offset++] = 0x00;

        // Univers : SubUni (octet bas) puis Net (octet haut, 7 bits utiles).
        bytes[offset++] = (byte)(Universe & 0xFF);
        bytes[offset++] = (byte)((Universe >> 8) & 0x7F);

        // Longueur des données DMX (512), envoyée gros-boutiste.
        bytes[offset++] = (byte)((DmxDataLength >> 8) & 0xFF);
        bytes[offset++] = (byte)(DmxDataLength & 0xFF);

        Array.Copy(data, 0, bytes, offset, DmxDataLength);

        return bytes;
    }

    private static byte[] ResizeTo512(byte[] source)
    {
        var resized = new byte[DmxDataLength];
        Array.Copy(source, resized, Math.Min(source.Length, DmxDataLength));
        return resized;
    }
}