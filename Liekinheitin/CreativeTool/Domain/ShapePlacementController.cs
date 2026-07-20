using System.Windows.Media;

namespace Liekinheitin.CreativeTool.Domain
{
    /// <summary>
    /// Gère une forme en cours de placement (glissée/redimensionnée), peinte directement
    /// et en continu dans le canvas — chaque changement suit le pipeline delta existant
    /// pour un retour temps réel sur le matériel. Se souvient de ce qu'il y avait sous
    /// la forme avant de peindre, pour le restaurer exactement quand elle se déplace.
    /// </summary>
    public sealed class ShapePlacementController
    {
        private readonly PixelCanvas _canvas;
        private readonly WallLayout _layout;

        private Dictionary<(int Col, int Row), Color> _underlying = new();
        private ShapeType _type;
        private int _left, _top, _width, _height;
        private Color _color;
        private bool _isActive;

        public ShapePlacementController(PixelCanvas canvas, WallLayout layout)
        {
            _canvas = canvas;
            _layout = layout;
        }

        public bool IsActive => _isActive;
        public (int Left, int Top, int Width, int Height) Bounds => (_left, _top, _width, _height);

        /// <summary>Démarre le placement d'une nouvelle forme (au moment du drop).</summary>
        public void Begin(ShapeType type, int left, int top, int width, int height, Color color)
        {
            _type = type;
            _color = color;
            _isActive = true;
            _underlying = new Dictionary<(int, int), Color>();
            ApplyAt(left, top, width, height);
        }

        /// <summary>Déplace la forme active (taille inchangée).</summary>
        public void MoveTo(int newLeft, int newTop) => ApplyAt(newLeft, newTop, _width, _height);

        /// <summary>Redimensionne/repositionne la forme active (utilisé par les poignées).</summary>
        public void Resize(int newLeft, int newTop, int newWidth, int newHeight) =>
            ApplyAt(newLeft, newTop, newWidth, newHeight);

        /// <summary>Change la couleur de la forme active sans changer sa géométrie.</summary>
        public void SetColor(Color color)
        {
            _color = color;
            ApplyAt(_left, _top, _width, _height);
        }

        /// <summary>
        /// Valide la forme : elle devient définitive (déjà peinte au fil des changements,
        /// rien à écrire de plus). On arrête juste de suivre les cases sous-jacentes.
        /// </summary>
        public void Commit()
        {
            _isActive = false;
            _underlying.Clear();
        }

        /// <summary>Annule le placement : restaure exactement ce qu'il y avait avant.</summary>
        public void Cancel()
        {
            foreach (var ((col, row), color) in _underlying)
                _canvas.SetPixel(col, row, color);

            _isActive = false;
            _underlying.Clear();
        }

        private void ApplyAt(int left, int top, int width, int height)
        {
            var newCells = new HashSet<(int Col, int Row)>(
                ShapeRasterizer.Rasterize(_type, left, top, width, height)
                    .Where(cell => _layout.HasLed(cell.Col, cell.Row)));

            var oldCells = new HashSet<(int Col, int Row)>(_underlying.Keys);

            // Cases quittées : restaurer leur couleur d'origine.
            foreach (var cell in oldCells.Except(newCells))
            {
                _canvas.SetPixel(cell.Col, cell.Row, _underlying[cell]);
                _underlying.Remove(cell);
            }

            // Cases nouvellement couvertes : capturer AVANT de peindre par-dessus.
            foreach (var cell in newCells.Except(oldCells))
                _underlying[cell] = _canvas.GetPixel(cell.Col, cell.Row);

            // Peindre toutes les cases actuellement couvertes.
            foreach (var cell in newCells)
                _canvas.SetPixel(cell.Col, cell.Row, _color);

            _left = left; _top = top; _width = width; _height = height;
        }
    }
}