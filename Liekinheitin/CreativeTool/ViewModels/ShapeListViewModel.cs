using Liekinheitin.CreativeTool.Domain;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace Liekinheitin.CreativeTool.ViewModels
{
    public sealed class ShapeListItem
    {
        public ShapeType Type { get; init; }
        public string Label { get; init; } = "";
        public Geometry Icon { get; init; } = Geometry.Empty;
    }

    public sealed class ShapeListViewModel
    {
        private const double Size = 24;

        public ObservableCollection<ShapeListItem> Shapes { get; } = new()
        {
            new ShapeListItem
            {
                Type = ShapeType.Square, Label = "Carré",
                Icon = new RectangleGeometry(new Rect(0, 0, Size, Size)),
            },
            new ShapeListItem
            {
                Type = ShapeType.Circle, Label = "Cercle",
                Icon = new EllipseGeometry(new Point(Size / 2, Size / 2), Size / 2, Size / 2),
            },
            new ShapeListItem
            {
                Type = ShapeType.LineHorizontal, Label = "Ligne horizontale",
                Icon = new RectangleGeometry(new Rect(0, Size / 2 - 2, Size, 4)),
            },
            new ShapeListItem
            {
                Type = ShapeType.LineVertical, Label = "Ligne verticale",
                Icon = new RectangleGeometry(new Rect(Size / 2 - 2, 0, 4, Size)),
            },
            new ShapeListItem
            {
                Type = ShapeType.Cross, Label = "Croix",
                Icon = new CombinedGeometry(
                    GeometryCombineMode.Union,
                    new RectangleGeometry(new Rect(0, Size / 2 - 2, Size, 4)),
                    new RectangleGeometry(new Rect(Size / 2 - 2, 0, 4, Size))),
            },
            new ShapeListItem
            {
                Type = ShapeType.Triangle, Label = "Triangle",
                Icon = new PathGeometry(new[]
                {
                    new PathFigure(
                        new Point(Size / 2, 0),
                        new PathSegment[]
                        {
                            new LineSegment(new Point(Size, Size), true),
                            new LineSegment(new Point(0, Size), true),
                        },
                        closed: true)
                }),
            },
        };
    }
}