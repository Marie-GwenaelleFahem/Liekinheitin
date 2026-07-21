using System.Windows.Media;

namespace Liekinheitin.CreativeTool.Domain
{
    public sealed class PixelCanvas
    {
        private readonly Color[,] _pixels;
        private readonly Dictionary<(int Col, int Row), int> _dirtyResendCounts = new();
        private readonly object _lock = new();
        private const int ResendCount = 3; // survit à ~2 pertes UDP consécutives sur ce pixel

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
                _dirtyResendCounts[(col, row)] = ResendCount;
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
                        _dirtyResendCounts[(c, r)] = ResendCount;
                    }
            }
        }

        /// <summary>
        /// Récupère les pixels encore "à renvoyer" (modifiés récemment, pas encore
        /// confirmés assez de fois) et décrémente leur compteur. Un pixel modifié une
        /// seule fois est donc envoyé ResendCount fois de suite avant d'être considéré
        /// stable — tolère la perte d'un ou deux paquets UDP sans laisser de trace.
        /// </summary>
        public List<(int Col, int Row)> ConsumeDirty()
        {
            lock (_lock)
            {
                var list = _dirtyResendCounts.Keys.ToList();

                foreach (var key in list)
                {
                    int remaining = _dirtyResendCounts[key] - 1;
                    if (remaining <= 0) _dirtyResendCounts.Remove(key);
                    else _dirtyResendCounts[key] = remaining;
                }

                return list;
            }
        }
    }
}