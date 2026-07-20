using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Liekinheitin.CreativeTool.Domain;
using Liekinheitin.CreativeTool.ViewModels;

namespace Liekinheitin.CreativeTool.Views
{
    public partial class ShapeListView : UserControl
    {
        public const string ShapeDragFormat = "LiekinheitinShapeType";

        private Point _dragStart;
        private bool _isPotentialDrag;

        public ShapeListView()
        {
            InitializeComponent();
        }

        private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _dragStart = e.GetPosition(null);
            _isPotentialDrag = true;
        }

        private void OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isPotentialDrag || e.LeftButton != MouseButtonState.Pressed) return;

            var current = e.GetPosition(null);
            if (Math.Abs(current.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(current.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
                return;

            if (ShapeListBox.SelectedItem is not ShapeListItem item) return;

            _isPotentialDrag = false;

            var data = new DataObject(ShapeDragFormat, item.Type);
            DragDrop.DoDragDrop(ShapeListBox, data, DragDropEffects.Copy);
        }
    }
}