using Liekinheitin.Domain.Entities;

namespace Liekinheitin.Application.Interfaces
{

    /// <summary>
    /// Contrat : « savoir signaler l'arrivée d'un nouveau <see cref="State"/> », sans préciser
    /// comment il est reçu.
    /// </summary>
    /// <remarks>
    /// Symétrique de <see cref="IStatePublisher"/>, côté réception. <c>RoutingEngine</c>
    /// (Application) s'abonne à <see cref="StateReceived"/> sans savoir comment les données
    /// arrivent réellement sur le réseau. C'est <c>UdpStateReceiver</c> (Infrastructure) qui
    /// remplit ce contrat en écoutant un socket UDP sur un thread séparé et en déclenchant
    /// l'événement à chaque message reçu.
    /// </remarks>
    public interface IStateSource
    {
        /// <summary>Déclenché à chaque nouvel état reçu.</summary>
        event Action<State>? StateReceived;
    }

}
