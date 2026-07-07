using System;
using System.Collections.Generic;
using System.Text;

namespace Liekinheitin.Domain.Entities
{
    /// <summary>
    /// Regroupe la totalité des entités et de leurs valeurs à un instant précis. C'est 
    /// l'objet que CreativeTool construit et envoie à RoutingHost 40 fois par seconde 
    /// pendant la lecture d'une animation ; c'est aussi ce que RoutingHost construit une 
    /// seule fois au démarrage, via PatchService.BuildInitialState(), pour indiquer à 
    /// CreativeTool quelles entités existent, avec leurs canaux à zéro. FindEntity(id) 
    /// parcourt Entities et renvoie l'entité correspondant à l'identifiant demandé 
    /// (ou null si elle n'existe pas) ; PixelGridViewModel s'en sert par exemple pour 
    /// retrouver la couleur actuelle d'un pixel donné.
    /// </summary>
    public class State
    {
        /// <summary>
        /// Liste complète des entités et de leurs valeurs à cet instant.
        /// </summary>
        public List<Entity> Entities { get; set; } = new();

        /// <summary>
        /// Recherche l'entité correspondant à l'identifiant demandé.
        /// </summary>
        /// <param name="id">Identifiant de l'entité recherchée.</param>
        /// <returns>
        /// L'<see cref="Entity"/> correspondante, ou <c>null</c> si aucune entité de ce State
        /// ne porte cet identifiant.
        /// </returns>
        /// <remarks>
        /// Utilisée par exemple par PixelGridViewModel (CreativeTool) pour retrouver la couleur
        /// actuelle d'un pixel donné.
        /// </remarks>
        public Entity? FindEntity(int id) => Entities.FirstOrDefault(e => e.Id == id);

    }
}
