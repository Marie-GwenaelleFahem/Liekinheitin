namespace Liekinheitin.CreativeTool.Domain
{
    public sealed class GridPosition
    {
        public int Col { get; init; }
        public int Row { get; init; }
    }

    /// <summary>
    /// Grille du mur LED (128 colonnes x 129 pixels), calculée entièrement
    /// en mémoire via EntityIdSequenceGenerator + WallLayoutCalculator.
    /// Aucune dépendance fichier ni réseau.
    /// </summary>
    public sealed class WallLayout
    {
        public int Columns { get; }
        public int Rows { get; }

        private readonly Dictionary<int, GridPosition> _positionByEntityId;
        private readonly int[,] _entityIdGrid; // -1 = case vide (pas de LED physique)

        public WallLayout(int columns = 128, int rows = 128)
        {
            Columns = columns;
            Rows = rows;

            var ids = EntityIdSequenceGenerator.Generate();
            var placed = WallLayoutCalculator.Compute(ids, columns, rows);

            _positionByEntityId = new Dictionary<int, GridPosition>();
            _entityIdGrid = new int[columns, rows];
            for (int c = 0; c < columns; c++)
                for (int r = 0; r < rows; r++)
                    _entityIdGrid[c, r] = -1;

            foreach (var e in placed)
            {
                _positionByEntityId[e.Id] = new GridPosition { Col = e.Col, Row = e.Row };
                _entityIdGrid[e.Col, e.Row] = e.Id;
            }
        }

        public GridPosition? GetPosition(int entityId) =>
            _positionByEntityId.TryGetValue(entityId, out var pos) ? pos : null;

        public int GetEntityId(int col, int row) => _entityIdGrid[col, row];
        public bool HasLed(int col, int row) => _entityIdGrid[col, row] != -1;
    }
}