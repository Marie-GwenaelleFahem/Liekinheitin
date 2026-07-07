using System;
using System.Collections.Generic;
using System.Text;

namespace Liekinheitin.Domain.Entities
{
    /// <summary>
    /// Représente une seule source lumineuse adressable : une LED du mur (3 ou 4 canaux) 
    /// ou un projecteur/lyre (jusqu'à 13 canaux). Id est l'identifiant logique choisi pour 
    /// cette source (par exemple 173), et Channels contient ses valeurs actuelles, dans l'ordre 
    /// attendu par le matériel. La longueur du tableau Channels varie donc d'une Entity à l'autre : 
    /// c'est ce qui permet à une seule et même classe de représenter aussi bien une LED qu'une lyre.
    /// </summary>
    public class Entity
    {
        /// <summary>
        /// Identifiant logique unique de l'entité. Les identifiants ne sont pas forcément
        /// consécutifs (des plages sont réservées par zone de l'installation).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Valeurs actuelles des canaux de cette entité (0 à 255 chacune), dans l'ordre attendu
        /// par le matériel. Longueur variable selon le type d'appareil représenté.
        /// </summary>
        public byte[] Channels { get; set; } = Array.Empty<byte>();

    }
}
