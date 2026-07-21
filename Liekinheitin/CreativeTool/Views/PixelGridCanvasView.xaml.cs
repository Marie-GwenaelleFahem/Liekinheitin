using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Liekinheitin.CreativeTool.Domain;
using Liekinheitin.CreativeTool.Infrastructure;

namespace Liekinheitin.CreativeTool.Views
{
    public partial class PixelGridCanvasView : UserControl
    {
        private WallLayout _layout;
        private SceneManager _scene;
        private BrushTool _brush;
        private PixelGridRenderer _renderer;
        private Func<Color> _getCurrentColor;

        private bool _isPainting;
        private bool _isDraggingShape;
        private Guid _selectedShapeId;
        private int _dragOffsetCol, _dragOffsetRow;

        private const int DefaultShapeSize = 20;

        /// <summary>Déclenché à chaque changement de sélection ou de propriété d'une forme
        /// sélectionnée (déplacement en direct compris). null = plus rien sélectionné.</summary>
        public event Action<PlacedShape?>? SelectionChanged;

        public PixelGridCanvasView()
        {
            InitializeComponent();
        }

        public void Initialize(WallLayout layout, SceneManager scene, BrushTool brush, Func<Color> getCurrentColor)
        {
            _layout = layout;
            _scene = scene;
            _brush = brush;
            _getCurrentColor = getCurrentColor;

            _renderer = new PixelGridRenderer(layout);
            _renderer.DrawAll(scene.Display);
            GridImage.Source = _renderer.Bitmap;
        }

        public void RefreshFromScene() => _renderer.DrawAll(_scene.Display);

        // ----- Conversion écran <-> grille -----

        private (int Col, int Row) ScreenToGrid(Point p)
        {
            int col = (int)(p.X / GridImage.ActualWidth * _layout.Columns);
            int rowFromTop = (int)(p.Y / GridImage.ActualHeight * _layout.Rows);
            int row = _layout.Rows - 1 - rowFromTop;
            return (col, row);
        }

        private double CellWidth => GridImage.ActualWidth / _layout.Columns;
        private double CellHeight => GridImage.ActualHeight / _layout.Rows;

        // ----- Clic : sélectionner une forme existante, ou peindre au pinceau -----

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var (col, row) = ScreenToGrid(e.GetPosition(GridImage));
            var hit = _scene.HitTest(col, row);

            if (hit is not null)
            {
                _selectedShapeId = hit.Id;
                _isDraggingShape = true;
                _dragOffsetCol = col - hit.X;
                _dragOffsetRow = row - hit.Y;

                DrawSelectionOverlay(hit);
                SelectionChanged?.Invoke(hit);
                GridImage.CaptureMouse();
            }
            else
            {
                ShapeOverlay.Children.Clear();
                SelectionChanged?.Invoke(null);

                _isPainting = true;
                PaintAtScreenPoint(e.GetPosition(GridImage));
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingShape && e.LeftButton == MouseButtonState.Pressed)
            {
                var (col, row) = ScreenToGrid(e.GetPosition(GridImage));
                int newX = col - _dragOffsetCol;
                int newY = row - _dragOffsetRow;

                _scene.MoveShape(_selectedShapeId, newX, newY); // clampe automatiquement

                RefreshFromScene();

                var shape = _scene.Shapes.FirstOrDefault(s => s.Id == _selectedShapeId);
                if (shape is not null)
                {
                    DrawSelectionOverlay(shape);
                    SelectionChanged?.Invoke(shape); // mise à jour temps réel du panneau
                }
            }
            else if (_isPainting && e.LeftButton == MouseButtonState.Pressed)
            {
                PaintAtScreenPoint(e.GetPosition(GridImage));
            }
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingShape = false;
            _isPainting = false;
            GridImage.ReleaseMouseCapture();
        }

        private void PaintAtScreenPoint(Point p)
        {
            var (col, row) = ScreenToGrid(p);
            if (col < 0 || col >= _layout.Columns || row < 0 || row >= _layout.Rows) return;

            if (_brush.Paint(col, row))
                RefreshFromScene();
        }

        // ----- Dépôt d'une nouvelle forme -----

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(ShapeListView.ShapeDragFormat)) return;

            var shapeType = (ShapeType)e.Data.GetData(ShapeListView.ShapeDragFormat);
            var (dropCol, dropRow) = ScreenToGrid(e.GetPosition(GridImage));

            int x = dropCol - DefaultShapeSize / 2;
            int y = dropRow - DefaultShapeSize / 2;

            var shape = _scene.AddShape(shapeType, x, y, DefaultShapeSize, DefaultShapeSize, _getCurrentColor());
            RefreshFromScene();

            _selectedShapeId = shape.Id;
            DrawSelectionOverlay(shape);
            SelectionChanged?.Invoke(shape);
        }

        // ----- Overlay de sélection -----

        private void DrawSelectionOverlay(PlacedShape shape)
        {
            ShapeOverlay.Children.Clear();

            var rect = new Rectangle
            {
                Width = shape.ActualWidth * CellWidth,
                Height = shape.ActualHeight * CellHeight,
                Stroke = Brushes.White,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                Fill = Brushes.Transparent,
                IsHitTestVisible = false,
            };

            int topRowFromTop = _layout.Rows - 1 - (shape.Y + shape.ActualHeight - 1);
            Canvas.SetLeft(rect, shape.X * CellWidth);
            Canvas.SetTop(rect, topRowFromTop * CellHeight);

            ShapeOverlay.Children.Add(rect);
        }
    }
}