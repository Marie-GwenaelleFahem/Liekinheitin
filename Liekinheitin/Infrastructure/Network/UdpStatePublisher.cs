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
/// binaire compact, via <see cref="StateMessagePackMapper"/>) et l'envoie via UDP vers
/// l'adresse IP et le port de RoutingHost. <c>MainViewModel</c> détient une référence à cette
/// classe uniquement à travers le type <see cref="IStatePublisher"/>, et l'appelle à chaque
/// frame pendant la lecture de la timeline — environ 40 fois par seconde ; le format binaire
/// évite le surcoût du texte JSON à cette fréquence.
/// </remarks>
public class UdpStatePublisher : IStatePublisher, IDisposable
{
    private readonly UdpClient _udpClient = new();
    private readonly string _targetIp;
    private readonly int _targetPort;

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
        _udpClient.Send(payload, payload.Length, _targetIp, _targetPort);
    }

    public void Dispose() => _udpClient.Dispose();
}