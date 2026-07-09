using Liekinheitin.Application.Interfaces;
using Liekinheitin.Domain.Entities;

namespace Liekinheitin.Application.Services
{

    /// <summary>
    /// Charge, garde en mémoire, et expose le contenu du fichier de patch (contrôleurs et
    /// plages d'adressage).
    /// </summary>
    /// <remarks>
    /// <see cref="LoadPatch"/> le lit une première fois via <see cref="IPatchLoader"/> et remplit
    /// <see cref="Controllers"/> et <see cref="Ranges"/> ; <see cref="ReloadPatch"/> refait la
    /// même chose en cours d'exécution (par exemple après une réparation matérielle sur le
    /// terrain) et déclenche <see cref="PatchReloaded"/>, auquel RoutingHost est abonné pour
    /// renvoyer un State initial à jour à CreativeTool. <see cref="FindAddress"/> est appelée par
    /// <c>RoutingEngine</c> pour chaque entité reçue. <see cref="BuildInitialState"/> construit
    /// l'état initial (toutes les entités connues, canaux à zéro) envoyé à CreativeTool au
    /// démarrage.
    /// </remarks>
    public class PatchService
    {
        private readonly IPatchLoader _patchLoader;
        private string _loadedPath = string.Empty;

        /// <summary>Contrôleurs connus, une fois le patch chargé.</summary>
        public List<Controller> Controllers { get; private set; } = new();

        /// <summary>Plages d'adressage connues, une fois le patch chargé.</summary>
        public List<PatchRange> Ranges { get; private set; } = new();

        /// <summary>Déclenché après chaque rechargement réussi du patch.</summary>
        public event Action? PatchReloaded;

        public PatchService(IPatchLoader patchLoader)
        {
            _patchLoader = patchLoader;
        }

        /// <summary>Charge le patch depuis le chemin indiqué et le conserve en mémoire.</summary>
        /// <param name="path">Chemin du fichier patch.json à charger.</param>
        public void LoadPatch(string path)
        {
            _loadedPath = path;
            PatchData data = _patchLoader.Load(path);
            Controllers = data.Controllers;
            Ranges = data.Ranges;
        }

        /// <summary>
        /// Recharge le patch depuis le dernier chemin utilisé (par exemple après une réparation
        /// matérielle sur le terrain), puis déclenche <see cref="PatchReloaded"/>.
        /// </summary>
        public void ReloadPatch()
        {
            if (string.IsNullOrEmpty(_loadedPath))
                throw new InvalidOperationException("Aucun patch n'a encore été chargé via LoadPatch().");

            LoadPatch(_loadedPath);
            PatchReloaded?.Invoke();
        }

        /// <summary>
        /// Retrouve la ligne de patch (contrôleur, univers, canal) correspondant à une entité.
        /// </summary>
        /// <param name="entityId">Identifiant de l'entité recherchée.</param>
        /// <returns>La <see cref="PatchRange"/> couvrant cet identifiant, ou <c>null</c> si aucune.</returns>
        public PatchRange? FindAddress(int entityId) => Ranges.FirstOrDefault(r => r.Contains(entityId));

        /// <summary>
        /// Construit un State contenant une Entity par identifiant connu dans les plages de
        /// patch, avec des canaux initialisés à zéro.
        /// </summary>
        /// <returns>Le State initial à envoyer à CreativeTool au démarrage.</returns>
        public State BuildInitialState()
        {
            var state = new State();

            foreach (var range in Ranges)
            {
                for (int id = range.EntityIdStart; id <= range.EntityIdEnd; id++)
                {
                    state.Entities.Add(new Entity
                    {
                        Id = id,
                        Channels = new byte[range.ChannelsPerEntity], // initialisé à zéro par défaut en C#
                    });
                }
            }

            return state;
        }
        /// <summary>
        /// Renvoie les numéros d'univers desservis par un contrôleur donné, sans doublons, triés.
        /// </summary>
        /// <param name="controllerId">Identifiant du contrôleur (Controller.Id).</param>
        /// <returns>La liste des univers utilisés par ce contrôleur.</returns>
        public List<int> GetUniverses(string controllerId)
        {
            return Ranges
                .Where(r => r.ControllerId == controllerId)
                .Select(r => r.Universe)
                .Distinct()
                .OrderBy(u => u)
                .ToList();
        }

        /// <summary>
        /// Renvoie tous les identifiants d'entité couverts par un contrôleur et un univers donnés,
        /// dépliés à partir des plages correspondantes.
        /// </summary>
        /// <param name="controllerId">Identifiant du contrôleur.</param>
        /// <param name="universe">Univers concerné.</param>
        /// <returns>La liste des identifiants d'entité de cette plage.</returns>
        public List<int> GetEntityIds(string controllerId, int universe)
        {
            var ids = new List<int>();

            foreach (var range in Ranges.Where(r => r.ControllerId == controllerId && r.Universe == universe))
            {
                for (int id = range.EntityIdStart; id <= range.EntityIdEnd; id++)
                {
                    ids.Add(id);
                }
            }

            return ids;
        }

    }
}
