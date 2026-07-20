namespace Liekinheitin.CreativeTool.Domain
{
    /// <summary>
    /// Place les EntityId sur la grille selon la forme physique réelle du mur :
    /// 64 bandes de 259 LED, chacune couvrant 2 colonnes de 128 LED visibles.
    /// Dans chaque bande : 1 LED invisible (fixation bas), 128 visibles montantes
    /// (colonne A, bas->haut), 1 LED invisible (fixation haut), 128 visibles
    /// descendantes (colonne B, haut->bas), 1 LED invisible (fixation bas).
    /// </summary>
    public static class WallLayoutCalculator
    {
        private const int StripLength = 259;
        private const int VisiblePerSide = 128;

        public static IReadOnlyList<(int Id, int Col, int Row)> Compute(
            IEnumerable<int> entityIds, int columns, int rows)
        {
            var ids = entityIds.OrderBy(id => id).ToList();
            int stripCount = columns / 2; // 2 colonnes visibles par bande

            var result = new List<(int Id, int Col, int Row)>();

            for (int s = 0; s < stripCount; s++)
            {
                int offset = s * StripLength;
                if (offset + StripLength > ids.Count) break; // pas assez d'ids générés, on s'arrête proprement

                int colA = s * 2;
                int colB = s * 2 + 1;

                // index 0 de la bande = LED invisible (fixation base) : ignorée.

                // indices 1..128 : colonne A, montante, bas (row 0) -> haut (row 127)
                for (int i = 0; i < VisiblePerSide; i++)
                {
                    int id = ids[offset + 1 + i];
                    result.Add((id, colA, i));
                }

                // index 129 de la bande = LED invisible (fixation haut) : ignorée.

                // indices 130..257 : colonne B, descendante, haut (row 127) -> bas (row 0)
                for (int i = 0; i < VisiblePerSide; i++)
                {
                    int id = ids[offset + 130 + i];
                    result.Add((id, colB, VisiblePerSide - 1 - i));
                }

                // index 258 de la bande = LED invisible (fixation base, extrémité) : ignorée.
            }

            return result;
        }
    }
}