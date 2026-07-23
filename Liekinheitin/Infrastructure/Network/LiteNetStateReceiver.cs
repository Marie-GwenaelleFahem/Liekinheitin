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
    /// Implémentation concrète d'<see cref="IStateSource"/> basée sur LiteNetLib — symétrique
    /// de <see cref="LiteNetStatePublisher"/>. Écoute les connexions entrantes, désérialise
    /// chaque message MessagePack reçu en <see cref="State"/> et déclenche <see cref="StateReceived"/>.
    /// </summary>
    public sealed class LiteNetStateReceiver : IStateSource, IDisposable
    {
        private const string ConnectionKey = "liekinheitin";

        private static readonly MessagePackSerializerOptions Options =
            MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);

        private readonly EventBasedNetListener _listener = new();
        private readonly NetManager _server;
        private readonly Timer _pollTimer;

        /// <inheritdoc />
        public event Action<State>? StateReceived;

        public LiteNetStateReceiver(int listenPort)
        {
            _server = new NetManager(_listener);

            _listener.ConnectionRequestEvent += request => request.AcceptIfKey(ConnectionKey);

            _listener.NetworkReceiveEvent += (peer, reader, channel, method) =>
            {
                try
                {
                    byte[] data = reader.GetRemainingBytes();
                    var state = MessagePackSerializer.Deserialize<State>(data, Options);
                    StateReceived?.Invoke(state);
                }
                catch (MessagePackSerializationException)
                {
                    // Message mal formé : ignoré plutôt que de faire planter la réception.
                }
                finally
                {
                    reader.Recycle();
                }
            };

            _server.Start(listenPort);
            _pollTimer = new Timer(_ => _server.PollEvents(), null, 0, 15);
        }

        /// <summary>Conservées pour compatibilité avec l'appelant existant (App.xaml.cs de
        /// RoutingHost) — LiteNetLib démarre l'écoute dès le constructeur, ces méthodes
        /// ne font donc rien de plus, mais évitent d'avoir à modifier l'appelant.</summary>
        public void StartListening() { }
        public void StopListening() => _server.Stop();

        public void Dispose()
        {
            _pollTimer.Dispose();
            _server.Stop();
        }
    }
}