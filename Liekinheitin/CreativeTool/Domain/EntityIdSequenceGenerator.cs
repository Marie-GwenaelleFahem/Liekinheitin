namespace Liekinheitin.CreativeTool.Domain
{
    /// <summary>
    /// Génère la séquence des EntityId valides du mur, par formule pure —
    /// aucune dépendance fichier. Reproduit le motif matériel :
    /// par contrôleur, 16 paires de segments (170 + 89 ids) espacées de 300,
    /// avec un trou de 41 ids non utilisés entre chaque paire.
    /// </summary>
    public static class EntityIdSequenceGenerator
    {
        public static IEnumerable<int> Generate(
            int controllerCount = 4,
            int pairsPerController = 16,
            int firstSegmentLength = 170,
            int secondSegmentLength = 89,
            int pairStride = 300,
            int controllerStride = 5000,
            int baseStart = 100)
        {
            for (int k = 0; k < controllerCount; k++)
            {
                int controllerBase = baseStart + k * controllerStride;
                for (int p = 0; p < pairsPerController; p++)
                {
                    int pairStart = controllerBase + p * pairStride;

                    for (int i = 0; i < firstSegmentLength; i++)
                        yield return pairStart + i;

                    for (int i = 0; i < secondSegmentLength; i++)
                        yield return pairStart + firstSegmentLength + i;
                }
            }
        }
    }
}