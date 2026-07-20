using Liekinheitin.Application.Interfaces;
using Liekinheitin.Domain.Entities;
using MessagePack;
using System.Net.Sockets;

namespace Liekinheitin.Infrastructure.Network;

/// <summary>
/// Implémentation concrète d'<see cref="IStateSource"/>, utilisée côté RoutingHost.
/// </summary>
/// <remarks>
/// <see cref="StartListening"/> ouvre un socket UDP et boucle en tâche de fond, sur un
/// thread séparé de l'interface graphique ; chaque paquet reçu est d'abord recollé par
/// <see cref="UdpChunkReassembler"/> (le <c>State</c> émetteur ayant pu être découpé en
/// plusieurs morceaux par <see cref="UdpChunkSender"/>), puis, une fois complet, désérialisé
/// (format MessagePack, via <see cref="StateMessagePackMapper"/>) en <see cref="State"/> ; à ce
/// moment seulement, l'événement <see cref="StateReceived"/> est déclenché, auquel
/// <c>RoutingEngine</c> est abonné. Cette séparation de thread garantit que la réception réseau
/// ne ralentit jamais l'affichage de RoutingHost.
/// </remarks>
public class UdpStateReceiver : IStateSource, IDisposable
{
    private readonly UdpClient _udpClient;
    private readonly UdpChunkReassembler _reassembler = new();
    private CancellationTokenSource? _cts;

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
                byte[]? complete = _reassembler.Receive(result.Buffer);
                if (complete is null)
                    continue; // il manque encore des morceaux de ce State

                var dto = MessagePackSerializer.Deserialize<StateDto>(complete);
                StateReceived?.Invoke(StateMessagePackMapper.ToDomain(dto));
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