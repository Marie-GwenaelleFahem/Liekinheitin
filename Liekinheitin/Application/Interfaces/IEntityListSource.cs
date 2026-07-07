using Liekinheitin.Domain.Entities;


namespace Liekinheitin.Application.Interfaces
{
    /// <summary>
    /// Contrat : « savoir signaler l'arrivée du State initial », côté CreativeTool.
    /// </summary>
    /// <remarks>
    /// <c>MainViewModel</c> s'abonne à <see cref="EntityListReceived"/> pour déclencher
    /// <c>PixelGridViewModel.BuildFromInitialState()</c> dès que RoutingHost a répondu, sans que
    /// CreativeTool n'ait jamais besoin de lire patch.json lui-même. Implémentée par
    /// <c>UdpEntityListReceiver</c> (Infrastructure).
    /// </remarks>
    public interface IEntityListSource
    {
        /// <summary>Déclenché quand le State initial est reçu.</summary>
        event Action<State>? EntityListReceived;
    }

}
