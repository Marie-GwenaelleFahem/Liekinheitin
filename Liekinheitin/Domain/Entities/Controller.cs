using System;
using System.Collections.Generic;
using System.Text;

namespace Liekinheitin.Domain.Entities
{
    /// <summary>
    /// Cette classe ne fait que stocker deux informations : l'identifiant choisi pour désigner 
    /// un contrôleur BC216 (par exemple "ctrl_1") et son adresse IP réelle sur le réseau 
    /// (par exemple "192.168.1.45"). Elle n'a aucune méthode car elle ne représente aucun 
    /// comportement, juste une donnée de configuration. Elle est créée par JsonPatchLoader 
    /// lors de la lecture du fichier patch.json, puis conservée dans PatchService.Controllers 
    /// pendant toute l'exécution de RoutingHost, pour que RoutingEngine sache à quelle adresse 
    /// envoyer chaque paquet ArtNet.
    /// </summary>
    public class Controller
    {
        /// <summary>
        /// Identifiant unique choisi pour désigner ce contrôleur (par exemple "ctrl_1").
        /// Sert de clé de référence depuis <see cref="PatchRange.ControllerId"/>.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Adresse IP réelle du contrôleur sur le réseau (par exemple "192.168.1.45"),
        /// utilisée comme adresse de destination des paquets ArtNet.
        /// </summary>
        public string IpAddress { get; set; } = string.Empty;

    }
}
