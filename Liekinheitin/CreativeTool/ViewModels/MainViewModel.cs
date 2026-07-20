using Liekinheitin.Application.Interfaces;
using Liekinheitin.CreativeTool.Application;
using Liekinheitin.CreativeTool.Domain;
using Liekinheitin.Domain.Entities;
using System;
using System.Threading;
using System.Windows.Media;

namespace Liekinheitin.CreativeTool.ViewModels
{
    public sealed class MainViewModel : IDisposable
    {
        private readonly PixelCanvas _canvas;
        private readonly WallLayout _layout;
        private readonly IStatePublisher _publisher;
        private readonly Timer _publishTimer;
        private int _tickCount;
        private const int FullResyncEveryNTicks = 80; // ~2s à 40Hz

        public BrushTool Brush { get; }
        public PixelCanvas Canvas => _canvas;
        public WallLayout Layout => _layout;
        public ColorPickerViewModel ColorPicker { get; }
        public ColumnListViewModel ColumnList { get; }
        public ColumnFillTool ColumnFill { get; }
        public ShapePlacementController ShapeController { get; }
        public ShapeListViewModel ShapeList { get; } = new();

        public MainViewModel(PixelCanvas canvas, WallLayout layout, BrushTool brush, IStatePublisher publisher, ShapePlacementController shapeController)
        {
            _canvas = canvas;
            _layout = layout;
            _publisher = publisher;
            Brush = brush;

            ColorPicker = new ColorPickerViewModel(brush);
            ColumnFill = new ColumnFillTool(canvas, layout);
            ColumnList = new ColumnListViewModel(layout.Columns);

            // Timer sur un thread du pool : la sérialisation/envoi ne bloque plus le
            // dessin, qui reste sur le thread UI.
            _publishTimer = new Timer(_ => PublishTick(), null, 0, 25);
            ShapeController = shapeController;
        }

        public void FillColumn(int col) => ColumnFill.FillColumn(col, ColorPicker.CurrentColor);

        private void PublishTick()
        {
            _tickCount++;
            bool fullResync = _tickCount % FullResyncEveryNTicks == 0;

            var entities = fullResync
                ? CanvasStateBuilder.Build(_canvas, _layout)
                : CanvasStateBuilder.BuildDelta(_canvas, _layout);

            if (entities.Count == 0) return; // rien à envoyer

            _publisher.Publish(new State { Entities = entities });
        }

        public void DrawOrientationTest()
        {
            _canvas.Clear(Colors.Black);
            _canvas.SetPixel(0, 0, Colors.Red);                                   // bas-gauche
            _canvas.SetPixel(_layout.Columns - 1, 0, Colors.Lime);                // bas-droite
            _canvas.SetPixel(0, _layout.Rows - 1, Colors.Blue);                   // haut-gauche
            _canvas.SetPixel(_layout.Columns - 1, _layout.Rows - 1, Colors.Yellow); // haut-droite
        }

        public void Dispose() => _publishTimer.Dispose();
    }
}