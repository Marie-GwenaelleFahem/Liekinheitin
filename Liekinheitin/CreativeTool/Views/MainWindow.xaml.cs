using System.Windows;
using System.Windows.Controls;

namespace Liekinheitin.CreativeTool
{
    public partial class MainWindow : Window
    {
        private Window? _timelineFullscreenWindow;
        private int _timelineOriginalRow;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.MainViewModel vm)
            {
                GridView.Initialize(vm.Layout, vm.Scene, vm.Brush, () => vm.ColorPicker.CurrentColor);

                GridView.SelectionChanged += vm.ShapeInspector.Load;
                GridView.SelectionChanged += TimelineViewControl.SetSelectedShape;
                vm.ShapeInspector.ShapeModified += () => GridView.RefreshDirtyFromScene();
                vm.TimelinePlayer.Ticked += () => GridView.RefreshDirtyFromScene();
                TimelineViewControl.SaveRequested += () => OnSaveProject(vm);
                TimelineViewControl.LoadRequested += () => OnLoadProject(vm);

                FixtureInspector.TimelineViewModel = vm.TimelineViewModel;
            }

            ColumnList.ColumnSelected += OnColumnSelected;

            _timelineOriginalRow = Grid.GetRow(TimelineViewControl);
            TimelineViewControl.FullscreenToggleRequested += OnTimelineFullscreenToggle;
        }
        private void OnSaveProject(ViewModels.MainViewModel vm)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Projet Liekinheitin (*.json)|*.json",
                DefaultExt = "json",
                FileName = "animation.json"
            };

            if (dialog.ShowDialog() == true)
                vm.SaveProject(dialog.FileName);
        }

        private void OnLoadProject(ViewModels.MainViewModel vm)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Projet Liekinheitin (*.json)|*.json",
            };

            if (dialog.ShowDialog() == true)
            {
                vm.LoadProject(dialog.FileName);
                GridView.RefreshFromScene();
                TimelineViewControl.RefreshFromViewModel(); // voir note ci-dessous
            }
        }

        private void OnTimelineFullscreenToggle()
        {
            if (_timelineFullscreenWindow is not null)
            {
                _timelineFullscreenWindow.Close();
                return;
            }

            var rootGrid = (Grid)TimelineViewControl.Parent;
            rootGrid.Children.Remove(TimelineViewControl);

            _timelineFullscreenWindow = new Window
            {
                Title = "Timeline — plein écran",
                Content = TimelineViewControl,
                WindowState = WindowState.Maximized,
                Owner = this,
            };

            _timelineFullscreenWindow.Closed += (_, __) =>
            {
                _timelineFullscreenWindow!.Content = null;
                rootGrid.Children.Add(TimelineViewControl);
                Grid.SetRow(TimelineViewControl, _timelineOriginalRow);
                _timelineFullscreenWindow = null;
            };

            _timelineFullscreenWindow.Show();
        }

        private void OnColumnSelected(int col)
        {
            if (DataContext is ViewModels.MainViewModel vm)
            {
                vm.FillColumn(col);
                GridView.RefreshDirtyFromScene();
            }
        }
    }
}