using System.Windows;
using Liekinheitin.RoutingHost.ViewModels;

namespace Liekinheitin.RoutingHost
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(PatchVisualizationViewModel patchVisualizationViewModel)
        {
            InitializeComponent();
            PatchVisualizationHost.DataContext = patchVisualizationViewModel;
        }
    }
}