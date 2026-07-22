using Liekinheitin.Application.Interfaces;
using Liekinheitin.Domain.Entities;
using System.Net.Sockets;
using System.Text.Json;

namespace Liekinheitin.Infrastructure.Network
{
    public class UdpStatePublisher : IStatePublisher, IDisposable
    {
        private readonly UdpClient _udpClient = new();
        private readonly string _targetIp;
        private readonly int _targetPort;
        private const int MaxEntitiesPerChunk = 500;

        public UdpStatePublisher(string targetIp, int targetPort)
        {
            _targetIp = targetIp;
            _targetPort = targetPort;
        }

        public void Publish(State state)
        {
            var entities = state.Entities;
            int totalChunks = (int)Math.Ceiling(entities.Count / (double)MaxEntitiesPerChunk);
            if (totalChunks == 0) totalChunks = 1;

            var messageId = Guid.NewGuid();

            for (int i = 0; i < totalChunks; i++)
            {
                var chunkEntities = entities
                    .Skip(i * MaxEntitiesPerChunk)
                    .Take(MaxEntitiesPerChunk)
                    .ToList();

                var chunk = new StateChunk
                {
                    MessageId = messageId,
                    ChunkIndex = i,
                    TotalChunks = totalChunks,
                    Entities = chunkEntities
                };

                byte[] payload = JsonSerializer.SerializeToUtf8Bytes(chunk);
                _udpClient.Send(payload, payload.Length, _targetIp, _targetPort);
            }
        }

        public void Dispose() => _udpClient.Dispose();
    }
}