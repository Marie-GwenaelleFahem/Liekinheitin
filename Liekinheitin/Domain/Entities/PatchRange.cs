using System;
using System.Collections.Generic;
using System.Text;

namespace Liekinheitin.Domain.Entities
{
    /// <summary>
    /// Une instance de cette classe correspond à une seule ligne du fichier patch.json : 
    /// elle affirme que les entités dont l'identifiant est compris entre EntityIdStart 
    /// et EntityIdEnd sont branchées sur le contrôleur désigné par ControllerId, sur 
    /// l'univers DMX Universe, en commençant au canal ChannelStart, chaque entité 
    /// utilisant ChannelsPerEntity canaux consécutifs. La méthode Contains(entityId) 
    /// renvoie vrai si l'identifiant passé en paramètre appartient à cette plage : 
    /// c'est elle que PatchService.FindAddress() appelle, pour chaque entité reçue dans 
    /// un State, afin de retrouver la ligne de patch à utiliser.
    /// </summary>
    public class PatchRange
    {
        /// <summary>Premier identifiant d'entité couvert par cette ligne (inclus).</summary>
        public int EntityIdStart { get; set; }

        /// <summary>Dernier identifiant d'entité couvert par cette ligne (inclus).</summary>
        public int EntityIdEnd { get; set; }

        /// <summary>
        /// Référence vers <see cref="Controller.Id"/> : quel contrôleur dessert cette plage.
        /// </summary>
        public string ControllerId { get; set; } = string.Empty;

        /// <summary>
        /// Univers DMX/ArtNet utilisé par cette plage. Une ligne ne couvre qu'un seul univers :
        /// une bande de LED qui déborde sur deux univers nécessite donc deux PatchRange.
        /// </summary>
        public int Universe { get; set; }

        /// <summary>
        /// Canal de départ (1 à 512) pour la première entité de la plage, dans l'univers indiqué.
        /// </summary>
        public int ChannelStart { get; set; }

        /// <summary>
        /// Nombre de canaux DMX utilisés par chaque entité de cette plage
        /// (3 pour du RGB, 4 pour du RGBW, 13 pour une lyre motorisée, etc.).
        /// </summary>
        public int ChannelsPerEntity { get; set; }

        /// <summary>
        /// Indique si l'identifiant d'entité donné appartient à cette plage
        /// (c'est-à-dire si <c>EntityIdStart &lt;= entityId &lt;= EntityIdEnd</c>).
        /// </summary>
        /// <param name="entityId">Identifiant de l'entité à rechercher.</param>
        /// <returns><c>true</c> si l'entité est couverte par cette ligne de patch.</returns>
        public bool Contains(int entityId) => entityId >= EntityIdStart && entityId <= EntityIdEnd;

    }
}
