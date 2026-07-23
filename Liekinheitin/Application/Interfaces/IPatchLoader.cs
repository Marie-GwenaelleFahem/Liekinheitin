using Liekinheitin.Domain.Entities;

namespace Liekinheitin.Application.Interfaces
{

    /// <summary>
    /// Contrat : « savoir lire et écrire un fichier de patch », sans préciser le format de
    /// fichier ni son emplacement.
    /// </summary>
    public interface IPatchLoader
    {
        /// <summary>Charge le contenu d'un fichier de patch.</summary>
        /// <param name="path">Chemin du fichier à lire.</param>
        /// <returns>Les contrôleurs et plages d'adressage décrits par le fichier.</returns>
        PatchData Load(string path);

        /// <summary>Écrit le contenu fourni dans un fichier de patch.</summary>
        /// <param name="path">Chemin du fichier à écrire.</param>
        /// <param name="data">Les contrôleurs et plages d'adressage à enregistrer.</param>
        void Save(string path, PatchData data);
    }

}
