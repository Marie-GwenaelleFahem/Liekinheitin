using Liekinheitin.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Liekinheitin.Application.Interfaces
{

    /// <summary>
    /// Contrat : « savoir envoyer physiquement une trame DMX vers un contrôleur », sans préciser
    /// le protocole réseau utilisé.
    /// </summary>
    /// <remarks>
    /// <c>RoutingEngine</c> appelle <see cref="Send"/> pour chaque <see cref="DmxFrame"/> qu'il a
    /// construit, sans savoir qu'il s'agit concrètement d'un envoi ArtNet en UDP — ce détail est
    /// entièrement délégué à <c>ArtNetSender</c> (Infrastructure), qui implémente cette interface
    /// et transforme la trame en véritable paquet ArtNet avant de l'envoyer.
    /// </remarks>
    public interface IPacketSender
    {
        /// <summary>Envoie une trame DMX vers son contrôleur cible.</summary>
        /// <param name="frame">La trame à envoyer (IP, univers, 512 octets).</param>
        void Send(DmxFrame frame);
    }

}
