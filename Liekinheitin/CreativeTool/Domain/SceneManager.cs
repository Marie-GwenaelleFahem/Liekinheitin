using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace Liekinheitin.CreativeTool.Domain
{
    public sealed class SceneManager
    {
        private readonly WallLayout _layout;
        private readonly PixelCanvas _freehand;
        private readonly PixelCanvas _display;
        private readonly List<PlacedShape> _shapes = new();

        // Cache de la géométrie rasterisée par forme, invalidé uniquement quand LA forme
        // concernée change (position/taille/scale) — pas à chaque recomposition globale.
        private readonly Dictionary<Guid, HashSet<(int Col, int Row)>> _shapeCellCache = new();

        public PixelCanvas Display => _display;
        public IReadOnlyList<PlacedShape> Shapes => _shapes;

        public SceneManager(WallLayout layout)
        {
            _layout = layout;
            _freehand = new PixelCanvas(layout.Columns, layout.Rows, Colors.Black);
            _display = new PixelCanvas(layout.Columns, layout.Rows, Colors.Black);
        }

        public bool PaintFreehand(int col, int row, Color color)
        {
            if (!_layout.HasLed(col, row)) return false;
            _freehand.SetPixel(col, row, color);
            RecomposeCells(new[] { (col, row) });
            return true;
        }

        public void FillColumnFreehand(int col, Color color)
        {
            if (col < 0 || col >= _layout.Columns) return;
            var cells = new List<(int, int)>();
            for (int row = 0; row < _layout.Rows; row++)
            {
                if (!_layout.HasLed(col, row)) continue;
                _freehand.SetPixel(col, row, color);
                cells.Add((col, row));
            }
            RecomposeCells(cells);
        }

        public void ClearFreehand(Color color)
        {
            _freehand.Clear(color);
            RecomposeAll();
        }

        public PlacedShape AddShape(ShapeType type, int x, int y, int width, int height, Color color)
        {
            var shape = new PlacedShape { Type = type, BaseWidth = width, BaseHeight = height, Color = color };
            ClampToGrid(shape, x, y);
            _shapes.Add(shape);
            InvalidateCache(shape.Id);
            RecomposeCells(GetShapeCells(shape));
            return shape;
        }

        public void RemoveShape(Guid id)
        {
            var shape = Find(id);
            if (shape is null) return;
            var cells = GetShapeCells(shape).ToList();
            _shapes.Remove(shape);
            InvalidateCache(id);
            RecomposeCells(cells);
        }

        public void MoveShape(Guid id, int newX, int newY)
        {
            var shape = Find(id);
            if (shape is null) return;

            var oldCells = GetShapeCells(shape).ToList();
            ClampToGrid(shape, newX, newY);
            InvalidateCache(id); // géométrie changée : le cache de CETTE forme est obsolète
            var newCells = GetShapeCells(shape);

            RecomposeCells(oldCells.Union(newCells));
        }

        public void ResizeShape(Guid id, int newBaseWidth, int newBaseHeight)
        {
            var shape = Find(id);
            if (shape is null) return;

            var oldCells = GetShapeCells(shape).ToList();
            shape.BaseWidth = Math.Max(1, newBaseWidth);
            shape.BaseHeight = Math.Max(1, newBaseHeight);
            ClampToGrid(shape, shape.X, shape.Y);
            InvalidateCache(id);
            var newCells = GetShapeCells(shape);

            RecomposeCells(oldCells.Union(newCells));
        }

        public void SetScale(Guid id, double scale)
        {
            var shape = Find(id);
            if (shape is null) return;

            var oldCells = GetShapeCells(shape).ToList();
            shape.Scale = Math.Max(0.05, scale);
            ClampToGrid(shape, shape.X, shape.Y);
            InvalidateCache(id);
            var newCells = GetShapeCells(shape);

            RecomposeCells(oldCells.Union(newCells));
        }

        public void SetColor(Guid id, Color color)
        {
            var shape = Find(id);
            if (shape is null) return;
            shape.Color = color;
            // Géométrie inchangée : pas besoin d'invalider le cache, juste recomposer.
            RecomposeCells(GetShapeCells(shape));
        }

        public void ApplyShapeState(Guid id, int x, int y, int baseWidth, int baseHeight, double scale, Color color)
        {
            var shape = Find(id);
            if (shape is null) return;

            var oldCells = GetShapeCells(shape).ToList();

            shape.BaseWidth = Math.Max(1, baseWidth);
            shape.BaseHeight = Math.Max(1, baseHeight);
            shape.Scale = Math.Max(0.05, scale);
            shape.Color = color;
            ClampToGrid(shape, x, y);
            InvalidateCache(id);

            var newCells = GetShapeCells(shape);
            RecomposeCells(oldCells.Union(newCells));
        }

        public PlacedShape? HitTest(int col, int row)
        {
            for (int i = _shapes.Count - 1; i >= 0; i--)
            {
                if (GetShapeCells(_shapes[i]).Contains((col, row)))
                    return _shapes[i];
            }
            return null;
        }

        private PlacedShape? Find(Guid id) => _shapes.FirstOrDefault(s => s.Id == id);

        /// <summary>Renvoie la géométrie rasterisée de la forme, depuis le cache si disponible,
        /// sinon la calcule et la met en cache pour les appels suivants.</summary>
        private HashSet<(int Col, int Row)> GetShapeCells(PlacedShape shape)
        {
            if (_shapeCellCache.TryGetValue(shape.Id, out var cached))
                return cached;

            var cells = new HashSet<(int Col, int Row)>(
                ShapeRasterizer.Rasterize(shape.Type, shape.X, shape.Y, shape.ActualWidth, shape.ActualHeight));
            _shapeCellCache[shape.Id] = cells;
            return cells;
        }

        private void InvalidateCache(Guid id) => _shapeCellCache.Remove(id);

        private void ClampToGrid(PlacedShape shape, int desiredX, int desiredY)
        {
            if (shape.ActualWidth > _layout.Columns)
                shape.Scale = _layout.Columns / (double)shape.BaseWidth;
            if (shape.ActualHeight > _layout.Rows)
                shape.Scale = Math.Min(shape.Scale, _layout.Rows / (double)shape.BaseHeight);

            int w = shape.ActualWidth;
            int h = shape.ActualHeight;

            shape.X = Math.Clamp(desiredX, 0, _layout.Columns - w);
            shape.Y = Math.Clamp(desiredY, 0, _layout.Rows - h);
        }

        /// <summary>Recalcule uniquement les cases indiquées, en utilisant la géométrie en
        /// cache de chaque forme plutôt que de la re-rasteriser à chaque appel.</summary>
        private void RecomposeCells(IEnumerable<(int Col, int Row)> cells)
        {
            foreach (var (col, row) in cells.Distinct())
            {
                if (!_layout.HasLed(col, row)) continue;

                Color final = _freehand.GetPixel(col, row);

                foreach (var shape in _shapes)
                {
                    if (col >= shape.X && col < shape.X + shape.ActualWidth &&
                        row >= shape.Y && row < shape.Y + shape.ActualHeight &&
                        GetShapeCells(shape).Contains((col, row)))
                    {
                        final = shape.Color;
                    }
                }

                if (_display.GetPixel(col, row) != final)
                    _display.SetPixel(col, row, final);
            }
        }

        private void RecomposeAll()
        {
            var all = new List<(int, int)>();
            for (int c = 0; c < _layout.Columns; c++)
                for (int r = 0; r < _layout.Rows; r++)
                    if (_layout.HasLed(c, r)) all.Add((c, r));
            RecomposeCells(all);
        }
    }
}