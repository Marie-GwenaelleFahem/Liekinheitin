using System.Windows.Media;

namespace Liekinheitin.CreativeTool.Domain
{
    public sealed class PixelCanvas
    {
        private readonly Color[,] _pixels;
        private readonly HashSet<(int Col, int Row)> _dirty = new();
        private readonly object _lock = new();

        public int Columns { get; }
        public int Rows { get; }

        public PixelCanvas(int columns, int rows, Color? fill = null)
        {
            Columns = columns;
            Rows = rows;
            _pixels = new Color[columns, rows];
            Clear(fill ?? Colors.Black);
        }

        public Color GetPixel(int col, int row)
        {
            lock (_lock) return _pixels[col, row];
        }

        public void SetPixel(int col, int row, Color color)
        {
            if (col < 0 || col >= Columns || row < 0 || row >= Rows) return;
            lock (_lock)
            {
                _pixels[col, row] = color;
                _dirty.Add((col, row));
            }
        }

        public void Clear(Color color)
        {
            lock (_lock)
            {
                for (int c = 0; c < Columns; c++)
                    for (int r = 0; r < Rows; r++)
                    {
                        _pixels[c, r] = color;
                        _dirty.Add((c, r));
                    }
            }
        }

        /// <summary>
        /// Récupère et vide la liste des pixels modifiés depuis le dernier appel.
        /// Thread-safe : appelée depuis le thread d'envoi, pendant que l'UI peint.
        /// </summary>
        public List<(int Col, int Row)> ConsumeDirty()
        {
            lock (_lock)
            {
                var list = _dirty.ToList();
                _dirty.Clear();
                return list;
            }
        }
    }
}