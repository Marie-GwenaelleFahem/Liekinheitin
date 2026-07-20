using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Liekinheitin.CreativeTool.ViewModels;

namespace Liekinheitin.CreativeTool.Views
{
    public partial class ColorPickerView : UserControl
    {
        public ColorPickerView()
        {
            InitializeComponent();
        }

        private void OnPaletteClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement el &&
                el.DataContext is Color color &&
                DataContext is ColorPickerViewModel vm)
            {
                vm.SelectFromPalette(color);
            }
        }
    }
}