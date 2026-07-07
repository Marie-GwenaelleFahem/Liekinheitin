using Liekinheitin.Domain.Entities;

namespace Liekinheitin.Application.Interfaces
{
    /// <summary>
    /// Contrat : « savoir transmettre un <see cref="State"/> quelque part », sans préciser comment.
    /// </summary>
    /// <remarks>
    /// Utilisée côté CreativeTool par <c>MainViewModel</c>, qui appelle <see cref="Publish"/> à
    /// chaque frame pendant la lecture de la timeline, sans jamais savoir que l'envoi se fait
    /// réellement en UDP. L'implémentation concrète fournie à <c>MainViewModel</c> au démarrage
    /// est <c>UdpStatePublisher</c> (Infrastructure). Si ce mécanisme de transport changeait un
    /// jour, seule une nouvelle implémentation de cette interface serait nécessaire.
    /// </remarks>
    public interface IStatePublisher
    {
        /// <summary>Transmet l'état courant de l'installation.</summary>
        /// <param name="state">L'état à transmettre.</param>
        void Publish(State state);
    }

}
