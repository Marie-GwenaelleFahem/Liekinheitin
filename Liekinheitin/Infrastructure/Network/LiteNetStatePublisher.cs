using System;
using System.Threading;
using LiteNetLib;
using MessagePack;
using MessagePack.Resolvers;
using Liekinheitin.Application.Interfaces;
using Liekinheitin.Domain.Entities;

namespace Liekinheitin.Infrastructure.Network
{
    /// <summary>
    /// Implémentation concrète d'<see cref="IStatePublisher"/> basée sur LiteNetLib (UDP
    /// fiabilisé) plutôt qu'UDP brut. Envoie chaque State en canal ReliableOrdered : en cas
    /// de perte de paquet, LiteNetLib retransmet automatiquement — remplace notre ancien
    /// système de renvoi redondant (ResendCount) et le découpage manuel (StateChunk), que
    /// LiteNetLib gère nativement (fragmentation/réassemblage interne des gros messages).
    /// Sérialisation en MessagePack (binaire) plutôt que JSON, pour réduire la taille des
    /// paquets envoyés.
    /// </summary>
    public sealed class LiteNetStatePublisher : IStatePublisher, IDisposable
    {
        private const string ConnectionKey = "liekinheitin";

        // Entity/State sont des classes simples sans attributs MessagePack : le résolveur
        // "contractless" les sérialise par réflexion, comme System.Text.Json, sans qu'il
        // soit nécessaire de les modifier.
        private static readonly MessagePackSerializerOptions Options =
            MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);

        private readonly EventBasedNetListener _listener = new();
        private readonly NetManager _client;
        private readonly string _targetIp;
        private readonly int _targetPort;
        private readonly Timer _pollTimer;
        public bool IsConnected => _peer?.ConnectionState == ConnectionState.Connected;

        private NetPeer? _peer;

        public LiteNetStatePublisher(string targetIp, int targetPort)
        {
            _targetIp = targetIp;
            _targetPort = targetPort;

            _client = new NetManager(_listener);
            _listener.PeerConnectedEvent += peer => _peer = peer;
            _listener.PeerDisconnectedEvent += (peer, info) => _peer = null;

            _client.Start();
            _client.Connect(_targetIp, _targetPort, ConnectionKey);

            // LiteNetLib nécessite un appel régulier à PollEvents() pour traiter les
            // événements réseau (connexion, déconnexion, envois en attente).
            _pollTimer = new Timer(_ => _client.PollEvents(), null, 0, 15);
        }

        /// <inheritdoc />
        public void Publish(State state)
        {
            if (_peer is null || _peer.ConnectionState != ConnectionState.Connected)
                return; // pas encore connecté (ou déconnecté) : cette frame est perdue, la suivante arrivera dans 25ms

            byte[] payload = MessagePackSerializer.Serialize(state, Options);
            _peer.Send(payload, DeliveryMethod.ReliableOrdered);
        }

        public void Dispose()
        {
            _pollTimer.Dispose();
            _client.Stop();
        }
    }
}