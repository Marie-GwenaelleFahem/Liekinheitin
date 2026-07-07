using Liekinheitin.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Liekinheitin.Application.Interfaces
{
    // <summary>
    /// Contrat : « savoir transmettre un State initial (toutes les entités connues, canaux à
    /// zéro) », utilisé une seule fois au démarrage ou après un rechargement du patch.
    /// </summary>
    /// <remarks>
    /// Côté RoutingHost, ce contrat est rempli une fois que <c>PatchService.BuildInitialState()</c>
    /// a construit le <see cref="Domain.Entities.State"/> initial, et déclenché dès que
    /// <c>HeartbeatService</c> signale que CreativeTool vient d'être détecté (ou dès que
    /// <c>PatchService.PatchReloaded</c> se déclenche). Implémentée par
    /// <c>UdpEntityListPublisher</c> (Infrastructure).
    /// </remarks>
    public interface IEntityListPublisher
    {
        /// <summary>Transmet le State initial décrivant les entités existantes.</summary>
        /// <param name="state">L'état initial (toutes entités, canaux à zéro).</param>
        void PublishEntityList(State state);
    }


}
