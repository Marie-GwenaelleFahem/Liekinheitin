using Liekinheitin.Application.Interfaces;
using Liekinheitin.Domain.Entities;
using System.Net.Sockets;
using System.Text.Json;

namespace Liekinheitin.Infrastructure.Network;

/// <summary>
/// Implémentation concrète d'<see cref="IStateSource"/>, utilisée côté RoutingHost.
/// </summary>
/// <remarks>
/// <see cref="StartListening"/> ouvre un socket UDP et boucle en tâche de fond. Chaque
/// message reçu est un fragment <see cref="StateChunk"/> (un State complet dépasse la
/// taille max d'un datagramme UDP). Les fragments sont regroupés par <c>MessageId</c> ;
/// dès que tous les <c>TotalChunks</c> d'un même message sont arrivés, ils sont fusionnés
/// en un <see cref="State"/> unique et <see cref="StateReceived"/> est déclenché.
/// Si un fragment se perd, le message reste incomplet et est silencieusement abandonné
/// à l'arrivée du message suivant — sans conséquence à 40Hz, la frame suivante arrive 25ms après.
/// </remarks>
public class UdpStateReceiver : IStateSource, IDisposable
{
    private readonly UdpClient _udpClient;
    private CancellationTokenSource? _cts;

    // Réassemblage en cours, par MessageId. Un seul message incomplet est conservé à la
    // fois : dès qu'un chunk d'un nouveau MessageId arrive, l'ancien (forcément incomplet,
    // sinon il aurait déjà été fusionné et retiré) est abandonné pour ne pas fuiter en mémoire.
    private Guid _pendingMessageId;
    private StateChunk?[] _pendingChunks = Array.Empty<StateChunk?>();
    private int _pendingReceivedCount;

    /// <inheritdoc />
    public event Action<State>? StateReceived;

    /// <param name="listenPort">Port UDP local sur lequel écouter les États envoyés par CreativeTool.</param>
    public UdpStateReceiver(int listenPort)
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
                var chunk = JsonSerializer.Deserialize<StateChunk>(result.Buffer);
                if (chunk is not null)
                {
                    TryAssemble(chunk);
                }
            }
            catch (OperationCanceledException)
            {
                // Arrêt normal demandé via StopListening().
            }
            catch (JsonException)
            {
                // Message reçu mal formé : ignoré plutôt que de faire planter la boucle d'écoute.
            }
        }
    }

    private void TryAssemble(StateChunk chunk)
    {
        // Nouveau message : on abandonne l'ancien réassemblage (forcément incomplet à ce stade).
        if (chunk.MessageId != _pendingMessageId)
        {
            _pendingMessageId = chunk.MessageId;
            _pendingChunks = new StateChunk?[chunk.TotalChunks];
            _pendingReceivedCount = 0;
        }

        if (chunk.ChunkIndex < 0 || chunk.ChunkIndex >= _pendingChunks.Length)
            return; // Chunk incohérent (TotalChunks changé en cours de route) : ignoré.

        if (_pendingChunks[chunk.ChunkIndex] is null)
        {
            _pendingChunks[chunk.ChunkIndex] = chunk;
            _pendingReceivedCount++;
        }

        if (_pendingReceivedCount == _pendingChunks.Length)
        {
            var state = new State();
            foreach (var c in _pendingChunks)
                state.Entities.AddRange(c!.Entities);

            StateReceived?.Invoke(state);

            // Message complet et transmis : on réinitialise pour éviter de le refusionner
            // si un chunk dupliqué arrivait en retard.
            _pendingChunks = Array.Empty<StateChunk?>();
            _pendingReceivedCount = 0;
        }
    }

    public void Dispose()
    {
        StopListening();
        _udpClient.Dispose();
    }
}