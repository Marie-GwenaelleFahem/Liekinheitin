using Liekinheitin.CreativeTool.ViewModels;
using System.Windows;

namespace Liekinheitin.CreativeTool
{
    public partial class MainWindow : Window
    {
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
            }

            ColumnList.ColumnSelected += OnColumnSelected;
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