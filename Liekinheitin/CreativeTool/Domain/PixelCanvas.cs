using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace Liekinheitin.CreativeTool.Domain
{
    public sealed class PixelCanvas
    {
        private readonly Color[,] _pixels;
        private readonly Dictionary<(int Col, int Row), int> _networkDirtyResendCounts = new();
        private readonly HashSet<(int Col, int Row)> _uiDirty = new();
        private readonly object _lock = new();
        private const int ResendCount = 1;

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
                _networkDirtyResendCounts[(col, row)] = ResendCount;
                _uiDirty.Add((col, row));
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
                        _networkDirtyResendCounts[(c, r)] = ResendCount;
                        _uiDirty.Add((c, r));
                    }
            }
        }

        /// <summary>Pour l'envoi réseau (delta) : renvoie chaque pixel modifié ResendCount fois
        /// de suite avant de l'estimer stable (tolère la perte d'un paquet UDP).</summary>
        public List<(int Col, int Row)> ConsumeDirty()
        {
            lock (_lock)
            {
                var list = _networkDirtyResendCounts.Keys.ToList();
                foreach (var key in list)
                {
                    int remaining = _networkDirtyResendCounts[key] - 1;
                    if (remaining <= 0) _networkDirtyResendCounts.Remove(key);
                    else _networkDirtyResendCounts[key] = remaining;
                }
                return list;
            }
        }

        /// <summary>Pour l'affichage écran : renvoie chaque pixel modifié une seule fois
        /// (le rendu local est synchrone, pas de perte possible comme sur le réseau).</summary>
        public List<(int Col, int Row)> ConsumeUiDirty()
        {
            lock (_lock)
            {
                var list = _uiDirty.ToList();
                _uiDirty.Clear();
                return list;
            }
        }
    }
}