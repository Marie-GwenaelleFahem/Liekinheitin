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
            RecomposeAll(); // opération rare (bouton test), le coût est acceptable ici
        }

        public PlacedShape AddShape(ShapeType type, int x, int y, int width, int height, Color color)
        {
            var shape = new PlacedShape { Type = type, BaseWidth = width, BaseHeight = height, Color = color };
            ClampToGrid(shape, x, y);
            _shapes.Add(shape);
            RecomposeCells(ShapeCells(shape));
            return shape;
        }

        public void RemoveShape(Guid id)
        {
            var shape = Find(id);
            if (shape is null) return;
            var cells = ShapeCells(shape).ToList();
            _shapes.Remove(shape);
            RecomposeCells(cells);
        }

        public void MoveShape(Guid id, int newX, int newY)
        {
            var shape = Find(id);
            if (shape is null) return;

            var oldCells = ShapeCells(shape).ToList();
            ClampToGrid(shape, newX, newY);
            var newCells = ShapeCells(shape);

            RecomposeCells(oldCells.Union(newCells));
        }

        public void ResizeShape(Guid id, int newBaseWidth, int newBaseHeight)
        {
            var shape = Find(id);
            if (shape is null) return;

            var oldCells = ShapeCells(shape).ToList();
            shape.BaseWidth = Math.Max(1, newBaseWidth);
            shape.BaseHeight = Math.Max(1, newBaseHeight);
            ClampToGrid(shape, shape.X, shape.Y);
            var newCells = ShapeCells(shape);

            RecomposeCells(oldCells.Union(newCells));
        }

        public void SetScale(Guid id, double scale)
        {
            var shape = Find(id);
            if (shape is null) return;

            var oldCells = ShapeCells(shape).ToList();
            shape.Scale = Math.Max(0.05, scale);
            ClampToGrid(shape, shape.X, shape.Y);
            var newCells = ShapeCells(shape);

            RecomposeCells(oldCells.Union(newCells));
        }

        public void SetColor(Guid id, Color color)
        {
            var shape = Find(id);
            if (shape is null) return;
            shape.Color = color;
            RecomposeCells(ShapeCells(shape));
        }

        public PlacedShape? HitTest(int col, int row)
        {
            for (int i = _shapes.Count - 1; i >= 0; i--)
            {
                if (ShapeCells(_shapes[i]).Any(c => c.Col == col && c.Row == row))
                    return _shapes[i];
            }
            return null;
        }

        private PlacedShape? Find(Guid id) => _shapes.FirstOrDefault(s => s.Id == id);

        private static IEnumerable<(int Col, int Row)> ShapeCells(PlacedShape s) =>
            ShapeRasterizer.Rasterize(s.Type, s.X, s.Y, s.ActualWidth, s.ActualHeight);

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

        /// <summary>Recalcule uniquement les cases indiquées (union des zones avant/après un
        /// changement), en tenant compte de toutes les formes — pas seulement celle qui a
        /// bougé, au cas où une autre forme est révélée en dessous.</summary>
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
                        ShapeCells(shape).Any(c => c.Col == col && c.Row == row))
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