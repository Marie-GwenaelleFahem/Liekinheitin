using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace Liekinheitin.CreativeTool.Domain
{
    public enum ShapeType
    {
        Square,
        Circle,
        LineHorizontal,
        LineVertical,
        Cross,
        Triangle,
    }

    /// <summary>
    /// Transforme une forme (type + rectangle englobant en coordonnées grille) en la liste
    /// des cases (col, row) qu'elle occupe. Ne connaît rien du canvas ni de l'UI.
    /// </summary>
    public static class ShapeRasterizer
    {
        /// <param name="type">Type de forme.</param>
        /// <param name="left">Colonne gauche du rectangle englobant.</param>
        /// <param name="top">Ligne basse du rectangle englobant (convention : row 0 = bas).</param>
        /// <param name="width">Largeur en cases (>= 1).</param>
        /// <param name="height">Hauteur en cases (>= 1).</param>
        public static IEnumerable<(int Col, int Row)> Rasterize(ShapeType type, int left, int top, int width, int height)
        {
            width = Math.Max(1, width);
            height = Math.Max(1, height);

            return type switch
            {
                ShapeType.Square => RasterizeSquare(left, top, width, height),
                ShapeType.Circle => RasterizeCircle(left, top, width, height),
                ShapeType.LineHorizontal => RasterizeLineHorizontal(left, top, width),
                ShapeType.LineVertical => RasterizeLineVertical(left, top, height),
                ShapeType.Cross => RasterizeCross(left, top, width, height),
                ShapeType.Triangle => RasterizeTriangle(left, top, width, height),
                _ => Enumerable.Empty<(int, int)>(),
            };
        }

        private static IEnumerable<(int, int)> RasterizeSquare(int left, int top, int width, int height)
        {
            for (int c = 0; c < width; c++)
                for (int r = 0; r < height; r++)
                    yield return (left + c, top + r);
        }

        private static IEnumerable<(int, int)> RasterizeCircle(int left, int top, int width, int height)
        {
            double cx = left + width / 2.0 - 0.5;
            double cy = top + height / 2.0 - 0.5;
            double rx = width / 2.0;
            double ry = height / 2.0;

            for (int c = 0; c < width; c++)
            {
                for (int r = 0; r < height; r++)
                {
                    double dx = (left + c - cx) / rx;
                    double dy = (top + r - cy) / ry;
                    if (dx * dx + dy * dy <= 1.0)
                        yield return (left + c, top + r);
                }
            }
        }

        private static IEnumerable<(int, int)> RasterizeLineHorizontal(int left, int top, int width)
        {
            for (int c = 0; c < width; c++)
                yield return (left + c, top);
        }

        private static IEnumerable<(int, int)> RasterizeLineVertical(int left, int top, int height)
        {
            for (int r = 0; r < height; r++)
                yield return (left, top + r);
        }

        private static IEnumerable<(int, int)> RasterizeCross(int left, int top, int width, int height)
        {
            int midCol = left + width / 2;
            int midRow = top + height / 2;

            for (int c = 0; c < width; c++)
                yield return (left + c, midRow);
            for (int r = 0; r < height; r++)
                yield return (midCol, top + r);
        }

        private static IEnumerable<(int, int)> RasterizeTriangle(int left, int top, int width, int height)
        {
            // Pointe en haut, base en bas — isocèle, centré sur la largeur.
            for (int r = 0; r < height; r++)
            {
                double t = (double)r / (height - 1 == 0 ? 1 : height - 1); // 0 en bas, 1 en haut
                int rowWidth = Math.Max(1, (int)Math.Round(width * (1.0 - t)));
                int rowLeft = left + (width - rowWidth) / 2;

                for (int c = 0; c < rowWidth; c++)
                    yield return (rowLeft + c, top + r);
            }
        }
    }
}