using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Liekinheitin.CreativeTool.Domain;
using Liekinheitin.CreativeTool.Infrastructure;

namespace Liekinheitin.CreativeTool.Views
{
    public partial class PixelGridCanvasView : UserControl
    {
        private WallLayout _layout;
        private BrushTool _brush;
        private PixelGridRenderer _renderer;
        private PixelCanvas _canvas;
        private ShapePlacementController _shapeController;
        private Func<Color> _getCurrentColor;

        private bool _isPainting;

        // Suivi du drag de déplacement de la forme active (pas encore le redimensionnement).
        private bool _isDraggingShape;
        private Point _dragStartScreen;
        private int _dragStartLeft, _dragStartTop;

        private const int DefaultShapeSize = 6;

        public PixelGridCanvasView()
        {
            InitializeComponent();
        }

        public void Initialize(
            WallLayout layout, PixelCanvas canvas, BrushTool brush,
            ShapePlacementController shapeController, Func<Color> getCurrentColor)
        {
            _layout = layout;
            _canvas = canvas;
            _brush = brush;
            _shapeController = shapeController;
            _getCurrentColor = getCurrentColor;

            _renderer = new PixelGridRenderer(layout);
            _renderer.DrawAll(canvas);
            GridImage.Source = _renderer.Bitmap;
        }

        public void RefreshFromCanvas(PixelCanvas canvas) => _renderer.DrawAll(canvas);

        // ----- Pinceau (inchangé) -----

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isPainting = true;
            PaintAtScreenPoint(e.GetPosition(GridImage));
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isPainting && e.LeftButton == MouseButtonState.Pressed)
                PaintAtScreenPoint(e.GetPosition(GridImage));
        }

        private void PaintAtScreenPoint(Point p)
        {
            var (col, row) = ScreenToGrid(p);
            if (col < 0 || col >= _layout.Columns || row < 0 || row >= _layout.Rows) return;

            if (_brush.Paint(col, row))
                _renderer.DrawPixel(col, row, _brush.CurrentColor);
        }

        // ----- Conversion écran -> grille (partagée) -----

        private (int Col, int Row) ScreenToGrid(Point p)
        {
            int col = (int)(p.X / GridImage.ActualWidth * _layout.Columns);
            int rowFromTop = (int)(p.Y / GridImage.ActualHeight * _layout.Rows);
            int row = _layout.Rows - 1 - rowFromTop;
            return (col, row);
        }

        private double CellWidth => GridImage.ActualWidth / _layout.Columns;
        private double CellHeight => GridImage.ActualHeight / _layout.Rows;

        // ----- Drop d'une forme depuis ShapeListView -----

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(ShapeListView.ShapeDragFormat)) return;

            var shapeType = (ShapeType)e.Data.GetData(ShapeListView.ShapeDragFormat);
            var (dropCol, dropRow) = ScreenToGrid(e.GetPosition(GridImage));

            // Le point de drop devient le centre de la forme par défaut.
            int left = dropCol - DefaultShapeSize / 2;
            int top = dropRow - DefaultShapeSize / 2;

            _shapeController.Begin(shapeType, left, top, DefaultShapeSize, DefaultShapeSize, _getCurrentColor());
            _renderer.DrawAll(_canvas);
            DrawOverlayRectangle();

            RootGrid.Focus(); // pour capter les touches Entrée/Échap ensuite
        }

        // ----- Overlay : rectangle englobant, déplaçable -----

        private void DrawOverlayRectangle()
        {
            ShapeOverlay.Children.Clear();
            if (!_shapeController.IsActive) return;

            var bounds = _shapeController.Bounds;

            var rect = new System.Windows.Shapes.Rectangle
            {
                Width = bounds.Width * CellWidth,
                Height = bounds.Height * CellHeight,
                Stroke = Brushes.White,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                Fill = Brushes.Transparent,
                Cursor = Cursors.SizeAll,
            };
            rect.MouseLeftButtonDown += OnShapeBodyMouseDown;
            rect.MouseMove += OnShapeBodyMouseMove;
            rect.MouseLeftButtonUp += OnShapeBodyMouseUp;

            PositionOverlayRectangle(rect, bounds.Left, bounds.Top, bounds.Width, bounds.Height);
            ShapeOverlay.Children.Add(rect);
        }

        private void PositionOverlayRectangle(FrameworkElement rect, int left, int top, int width, int height)
        {
            // top de la forme (row la plus haute) -> coordonnée écran Y la plus petite.
            int topRowFromTop = _layout.Rows - 1 - (top + height - 1);
            Canvas.SetLeft(rect, left * CellWidth);
            Canvas.SetTop(rect, topRowFromTop * CellHeight);
        }

        private void OnShapeBodyMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingShape = true;
            _dragStartScreen = e.GetPosition(ShapeOverlay);
            var bounds = _shapeController.Bounds;
            _dragStartLeft = bounds.Left;
            _dragStartTop = bounds.Top;
            ((UIElement)sender).CaptureMouse();
        }

        private void OnShapeBodyMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDraggingShape) return;

            var current = e.GetPosition(ShapeOverlay);
            int deltaCol = (int)Math.Round((current.X - _dragStartScreen.X) / CellWidth);
            int deltaRow = -(int)Math.Round((current.Y - _dragStartScreen.Y) / CellHeight); // écran Y inversé

            _shapeController.MoveTo(_dragStartLeft + deltaCol, _dragStartTop + deltaRow);
            _renderer.DrawAll(_canvas);
            DrawOverlayRectangle();
        }

        private void OnShapeBodyMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingShape = false;
            ((UIElement)sender).ReleaseMouseCapture();
        }

        // ----- Validation / annulation -----

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!_shapeController.IsActive) return;

            if (e.Key == Key.Enter)
            {
                _shapeController.Commit();
                ShapeOverlay.Children.Clear();
            }
            else if (e.Key == Key.Escape)
            {
                _shapeController.Cancel();
                _renderer.DrawAll(_canvas);
                ShapeOverlay.Children.Clear();
            }
        }
    }
}