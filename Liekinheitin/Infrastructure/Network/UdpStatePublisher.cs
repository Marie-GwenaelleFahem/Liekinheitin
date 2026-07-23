using Liekinheitin.Application.Interfaces;
using Liekinheitin.Domain.Entities;
using MessagePack;
using System.Net.Sockets;

namespace Liekinheitin.Infrastructure.Network;

/// <summary>
/// Implémentation concrète d'<see cref="IStatePublisher"/>, utilisée côté CreativeTool.
/// </summary>
/// <remarks>
/// <see cref="Publish"/> sérialise l'objet <see cref="State"/> reçu en MessagePack (format
/// binaire compact, via <see cref="StateMessagePackMapper"/>), découpe le résultat en morceaux
/// via <see cref="UdpChunkSender"/> (un <c>State</c> complet du mur de LED peut largement
/// dépasser la taille qu'UDP peut envoyer sans risquer de fragmentation), et envoie chaque
/// morceau vers l'adresse IP et le port de RoutingHost. <c>MainViewModel</c> détient une
/// référence à cette classe uniquement à travers le type <see cref="IStatePublisher"/>, et
/// l'appelle à chaque frame pendant la lecture de la timeline — environ 40 fois par seconde.
/// </remarks>
public class UdpStatePublisher : IStatePublisher, IDisposable
{
    private readonly UdpClient _udpClient = new();
    private readonly string _targetIp;
    private readonly int _targetPort;
    private ushort _nextMessageId;

    /// <param name="targetIp">Adresse IP de RoutingHost.</param>
    /// <param name="targetPort">Port UDP sur lequel RoutingHost écoute les États.</param>
    public UdpStatePublisher(string targetIp, int targetPort)
    {
        _targetIp = targetIp;
        _targetPort = targetPort;
    }

    /// <inheritdoc />
    public void Publish(State state)
    {
        byte[] payload = MessagePackSerializer.Serialize(StateMessagePackMapper.ToDto(state));

        foreach (byte[] chunk in UdpChunkSender.Split(payload, _nextMessageId++))
        {
            _udpClient.Send(chunk, chunk.Length, _targetIp, _targetPort);
        }
    }

    public void Dispose() => _udpClient.Dispose();
}