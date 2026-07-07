namespace Liekinheitin.Domain.Entities
{

    /// <summary>
    /// Contenu complet d'un fichier de patch une fois chargé : la liste des contrôleurs connus
    /// et la liste des plages d'adressage.
    /// </summary>
    /// <remarks>
    /// Renvoyée par <c>IPatchLoader.Load()</c> (Application/Infrastructure) et utilisée par
    /// <c>PatchService</c> pour remplir ses propres listes <c>Controllers</c> et <c>Ranges</c>.
    /// </remarks>
    public class PatchData
    {
        /// <summary>Contrôleurs déclarés dans le fichier de patch.</summary>
        public List<Controller> Controllers { get; set; } = new();

        /// <summary>Plages d'adressage déclarées dans le fichier de patch.</summary>
        public List<PatchRange> Ranges { get; set; } = new();
    }

}
