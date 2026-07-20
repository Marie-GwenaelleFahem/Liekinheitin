using Liekinheitin.Application.Interfaces;
using Liekinheitin.Domain.Entities;
using MessagePack;
using System.Net.Sockets;

namespace Liekinheitin.Infrastructure.Network;

/// <summary>
/// Implémentation concrète d'<see cref="IEntityListPublisher"/>, utilisée côté RoutingHost.
/// </summary>
/// <remarks>
/// <see cref="PublishEntityList"/> envoie par UDP le State initial construit par
/// <c>PatchService.BuildInitialState()</c>. Appelée dès que <see cref="HeartbeatService"/>
/// signale que CreativeTool vient d'être détecté, et à chaque rechargement du patch
/// (<c>PatchService.PatchReloaded</c>). Utilise volontairement un port différent de
/// <see cref="UdpStatePublisher"/> pour ne pas mélanger les deux canaux de communication.
/// </remarks>
public class UdpEntityListPublisher : IEntityListPublisher, IDisposable
{
    private readonly UdpClient _udpClient = new();
    private readonly string _targetIp;
    private readonly int _targetPort;
    private ushort _nextMessageId;

    /// <param name="targetIp">Adresse IP de CreativeTool.</param>
    /// <param name="targetPort">Port UDP sur lequel CreativeTool écoute la liste des entités.</param>
    public UdpEntityListPublisher(string targetIp, int targetPort)
    {
        _targetIp = targetIp;
        _targetPort = targetPort;
    }

    /// <inheritdoc />
    public void PublishEntityList(State state)
    {
        byte[] payload = MessagePackSerializer.Serialize(StateMessagePackMapper.ToDto(state));

        foreach (byte[] chunk in UdpChunkSender.Split(payload, _nextMessageId++))
        {
            _udpClient.Send(chunk, chunk.Length, _targetIp, _targetPort);
        }
    }

    public void Dispose() => _udpClient.Dispose();
}