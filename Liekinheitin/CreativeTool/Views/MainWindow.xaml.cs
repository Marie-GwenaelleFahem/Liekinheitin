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
                GridView.Initialize(vm.Layout, vm.Canvas, vm.Brush, vm.ShapeController, () => vm.ColorPicker.CurrentColor);

            ColumnList.ColumnSelected += OnColumnSelected;
        }

        private void OnColumnSelected(int col)
        {
            if (DataContext is ViewModels.MainViewModel vm)
            {
                vm.FillColumn(col);
                GridView.RefreshFromCanvas(vm.Canvas);
            }
        }
    }
}