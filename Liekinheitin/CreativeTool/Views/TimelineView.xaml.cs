using System.Windows.Controls;
using Liekinheitin.CreativeTool.Domain;
using Liekinheitin.CreativeTool.ViewModels;

namespace Liekinheitin.CreativeTool.Views
{
    public partial class TimelineView : UserControl
    {
        private PlacedShape? _selectedShape;

        public TimelineView()
        {
            InitializeComponent();
        }

        /// <summary>Appelée depuis MainWindow à chaque changement de sélection sur la grille.</summary>
        public void SetSelectedShape(PlacedShape? shape) => _selectedShape = shape;

        private void OnPlayPauseClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is TimelineViewModel vm) vm.TogglePlayPause();
        }

        private void OnStopClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is TimelineViewModel vm) vm.Stop();
        }

        private void OnAddKeyframeClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is TimelineViewModel vm && _selectedShape is not null)
                vm.AddKeyframe(_selectedShape);
        }
    }
}