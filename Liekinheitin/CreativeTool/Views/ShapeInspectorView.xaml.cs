using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Liekinheitin.CreativeTool.Views
{
    public partial class ShapeInspectorView : UserControl
    {
        private bool _isDragging;
        private Point _dragStart;

        public ShapeInspectorView()
        {
            InitializeComponent();
        }

        private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _dragStart = e.GetPosition(this);
            TitleBar.CaptureMouse();
        }

        private void OnTitleBarMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;

            var parentPoint = e.GetPosition((UIElement)Parent);
            Canvas.SetLeft(this, parentPoint.X - _dragStart.X);
            Canvas.SetTop(this, parentPoint.Y - _dragStart.Y);
        }

        private void OnTitleBarMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            TitleBar.ReleaseMouseCapture();
        }
    }
}