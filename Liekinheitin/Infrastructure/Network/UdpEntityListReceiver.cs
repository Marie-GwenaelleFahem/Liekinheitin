using Liekinheitin.Application.Interfaces;
using Liekinheitin.Domain.Entities;
using MessagePack;
using System.Net.Sockets;

namespace Liekinheitin.Infrastructure.Network;

/// <summary>
/// Implémentation concrète d'<see cref="IEntityListSource"/>, utilisée côté CreativeTool.
/// </summary>
/// <remarks>
/// Écoute le réseau et déclenche <see cref="EntityListReceived"/> dès qu'un State initial
/// est reçu ; <c>MainViewModel</c> utilise ce signal pour appeler
/// <c>PixelGridViewModel.BuildFromInitialState()</c> et construire la grille de pixels.
/// </remarks>
public class UdpEntityListReceiver : IEntityListSource, IDisposable
{
    private readonly UdpClient _udpClient;
    private CancellationTokenSource? _cts;

    /// <inheritdoc />
    public event Action<State>? EntityListReceived;

    /// <param name="listenPort">Port UDP local sur lequel écouter la liste des entités.</param>
    public UdpEntityListReceiver(int listenPort)
    {
        _udpClient = new UdpClient(listenPort);
    }

    /// <summary>Démarre l'écoute en tâche de fond (ne bloque pas l'appelant).</summary>
    public void StartListening()
    {
        _cts = new CancellationTokenSource();
        _ = ListenLoopAsync(_cts.Token);
    }

    /// <summary>Arrête l'écoute et libère le socket.</summary>
    public void StopListening() => _cts?.Cancel();

    private async Task ListenLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var result = await _udpClient.ReceiveAsync(token);
                var dto = MessagePackSerializer.Deserialize<StateDto>(result.Buffer);
                EntityListReceived?.Invoke(StateMessagePackMapper.ToDomain(dto));
            }
            catch (OperationCanceledException)
            {
                // Arrêt normal demandé via StopListening().
            }
            catch (MessagePackSerializationException)
            {
                // Message reçu mal formé : ignoré plutôt que de faire planter la boucle d'écoute.
            }
        }
    }

    public void Dispose()
    {
        StopListening();
        _udpClient.Dispose();
    }
}