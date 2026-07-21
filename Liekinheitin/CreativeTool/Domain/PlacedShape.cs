using System;
using System.Windows.Media;

namespace Liekinheitin.CreativeTool.Domain
{
    /// <summary>
    /// Une forme placée sur la grille, avec identité et propriétés indépendantes.
    /// Scale multiplie BaseWidth/BaseHeight pour donner la taille réellement affichée
    /// (même principe que Width/Height + Scale sur un RectTransform Unity).
    /// </summary>
    public sealed class PlacedShape
    {
        public Guid Id { get; } = Guid.NewGuid();
        public ShapeType Type { get; set; }

        /// <summary>Colonne gauche du rectangle englobant (0 = bord gauche de la grille).</summary>
        public int X { get; set; }

        /// <summary>Ligne basse du rectangle englobant (0 = bas de la grille).</summary>
        public int Y { get; set; }

        public int BaseWidth { get; set; }
        public int BaseHeight { get; set; }
        public double Scale { get; set; } = 1.0;
        public Color Color { get; set; }

        public int ActualWidth => Math.Max(1, (int)Math.Round(BaseWidth * Scale));
        public int ActualHeight => Math.Max(1, (int)Math.Round(BaseHeight * Scale));
    }
}