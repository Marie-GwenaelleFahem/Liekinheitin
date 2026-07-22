using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Media;
using Liekinheitin.Application.Interfaces;
using Liekinheitin.CreativeTool.Application;
using Liekinheitin.CreativeTool.Domain;
using Liekinheitin.Domain.Entities;

namespace Liekinheitin.CreativeTool.ViewModels
{
    public sealed class MainViewModel : IDisposable
    {
        private readonly SceneManager _scene;
        private readonly WallLayout _layout;
        private readonly IStatePublisher _publisher;
        private readonly FixtureManager _fixtures;
        private readonly Timer _publishTimer;
        private int _tickCount;
        private const int FullResyncEveryNTicks = 80;

        public BrushTool Brush { get; }
        public SceneManager Scene => _scene;
        public WallLayout Layout => _layout;
        public ColorPickerViewModel ColorPicker { get; }
        public ColumnListViewModel ColumnList { get; }
        public ShapeListViewModel ShapeList { get; } = new();
        public ColumnFillTool ColumnFill { get; }
        public ShapeInspectorViewModel ShapeInspector { get; }
        public FixtureManager Fixtures => _fixtures;
        public FixtureControlViewModel FixtureControl { get; }
        public Timeline Timeline { get; } = new();
        public TimelinePlayer TimelinePlayer { get; }
        public TimelineViewModel TimelineViewModel { get; }

        public MainViewModel(SceneManager scene, WallLayout layout, BrushTool brush, IStatePublisher publisher, FixtureManager fixtures)
        {
            _scene = scene;
            _layout = layout;
            _publisher = publisher;
            _fixtures = fixtures;
            Brush = brush;

            ColorPicker = new ColorPickerViewModel(brush);
            ColumnFill = new ColumnFillTool(scene);
            ColumnList = new ColumnListViewModel(layout.Columns);
            ShapeInspector = new ShapeInspectorViewModel(scene);
            FixtureControl = new FixtureControlViewModel(fixtures);

            TimelinePlayer = new TimelinePlayer(Timeline, scene, fixtures);
            TimelineViewModel = new TimelineViewModel(Timeline, TimelinePlayer, scene);

            _publishTimer = new Timer(_ => PublishTick(), null, 0, 25);
        }

        public void FillColumn(int col) => ColumnFill.FillColumn(col, ColorPicker.CurrentColor);

        public void DrawOrientationTest()
        {
            _scene.ClearFreehand(Colors.Black);
            _scene.PaintFreehand(0, 0, Colors.Red);
            _scene.PaintFreehand(_layout.Columns - 1, 0, Colors.Lime);
            _scene.PaintFreehand(0, _layout.Rows - 1, Colors.Blue);
            _scene.PaintFreehand(_layout.Columns - 1, _layout.Rows - 1, Colors.Yellow);
        }

        private void PublishTick()
        {
            _tickCount++;
            bool fullResync = _tickCount % FullResyncEveryNTicks == 0;

            var gridEntities = fullResync
                ? CanvasStateBuilder.Build(_scene.Display, _layout)
                : CanvasStateBuilder.BuildDelta(_scene.Display, _layout);

            var entities = new List<Entity>(gridEntities.Count + 5);
            entities.AddRange(gridEntities);
            entities.AddRange(_fixtures.BuildEntities());

            if (entities.Count == 0) return;
            _publisher.Publish(new State { Entities = entities });
        }

        public void Dispose()
        {
            _publishTimer.Dispose();
            TimelinePlayer.Dispose();
        }
    }
}