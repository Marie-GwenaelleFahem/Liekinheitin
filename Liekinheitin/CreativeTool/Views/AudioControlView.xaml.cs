using System.Windows;
using System.Windows.Controls;
using Liekinheitin.CreativeTool.ViewModels;

namespace Liekinheitin.CreativeTool.Views
{
    public partial class AudioControlView : UserControl
    {
        public AudioControlView()
        {
            InitializeComponent();
        }

        private void OnLoadClick(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Fichiers audio (*.mp3;*.wav)|*.mp3;*.wav"
            };

            if (dialog.ShowDialog() == true && DataContext is AudioViewModel vm)
                vm.Load(dialog.FileName);
        }
    }
}