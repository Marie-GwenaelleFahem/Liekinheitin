using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Liekinheitin.CreativeTool.ViewModels;

namespace Liekinheitin.CreativeTool.Views
{
    public partial class FixtureInspectorView : UserControl
    {
        public TimelineViewModel? TimelineViewModel { get; set; }

        private bool _isDragging;
        private Point _dragStart;

        public FixtureInspectorView()
        {
            InitializeComponent();
        }

        private void OnKeyframeClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is not FixtureInspectorViewModel vm || TimelineViewModel is null) return;

            if (vm.ProjectorRef is not null) TimelineViewModel.AddProjectorKeyframe(vm.ProjectorRef);
            else if (vm.HeadRef is not null) TimelineViewModel.AddMovingHeadKeyframe(vm.HeadRef);
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