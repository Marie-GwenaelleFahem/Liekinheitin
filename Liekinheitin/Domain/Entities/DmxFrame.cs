using System;
using System.Collections.Generic;
using System.Text;

namespace Liekinheitin.Domain.Entities
{

    /// <summary>
    /// Représente, de façon neutre et indépendante de tout protocole réseau, les données à
    /// envoyer à un contrôleur pour un univers donné : son adresse IP, le numéro d'univers, et
    /// les 512 valeurs de canaux.
    /// </summary>
    /// <remarks>
    /// Cette classe existe pour que <c>RoutingEngine</c> (Application) puisse construire le
    /// résultat de son calcul sans dépendre du protocole ArtNet, qui est un détail
    /// d'Infrastructure. C'est <c>ArtNetSender</c> qui transforme un <see cref="DmxFrame"/> en
    /// véritable paquet ArtNet (avec son encodage d'octets propre au protocole) au moment de
    /// l'envoi. Si l'équipe ajoutait un jour un autre moyen de transmission (port USB, sACN...),
    /// seul le convertisseur changerait : ni <see cref="DmxFrame"/>, ni <c>RoutingEngine</c>
    /// n'auraient à être modifiés.
    /// </remarks>
    public class DmxFrame
    {
        /// <summary>Adresse IP du contrôleur destinataire.</summary>
        public string TargetIp { get; set; } = string.Empty;

        /// <summary>Univers DMX concerné par cette trame.</summary>
        public int Universe { get; set; }

        /// <summary>Les 512 valeurs de canaux de cet univers.</summary>
        public byte[] Data { get; set; } = new byte[512];
    }

}
