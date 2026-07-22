using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Liekinheitin.CreativeTool.ViewModels;

namespace Liekinheitin.CreativeTool.Views
{
    public partial class FixtureControlView : UserControl
    {
        public FixtureControlView()
        {
            InitializeComponent();
        }

        private void OnProjectorClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is FixtureControlViewModel vm) vm.ToggleProjector();
        }

        private void OnHeadClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement el && el.DataContext is MovingHeadViewModel head &&
                DataContext is FixtureControlViewModel vm)
                vm.ToggleHead(head);
        }
    }
}