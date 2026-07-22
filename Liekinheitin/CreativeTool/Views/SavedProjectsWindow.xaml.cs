using System;
using System.Windows;
using System.Windows.Input;
using Liekinheitin.CreativeTool.Models;
using Liekinheitin.CreativeTool.Services;

namespace Liekinheitin.CreativeTool.Views
{
    public partial class SavedProjectsWindow : Window
    {
        private readonly SavedProjectsService _service;

        public SavedProjectsWindow(SavedProjectsService service)
        {
            InitializeComponent();
            _service = service;
            ProjectsList.MouseDoubleClick += (_, _) => ResumeSelected();
            Refresh();
        }

        public string? SelectedProjectPath { get; private set; }

        private void Refresh() => ProjectsList.ItemsSource = _service.GetAll();

        private void OnResumeClick(object sender, RoutedEventArgs e) => ResumeSelected();

        private void ResumeSelected()
        {
            if (ProjectsList.SelectedItem is not SavedProjectInfo project)
            {
                MessageBox.Show(this, "Sélectionne une animation à reprendre.", "Sauvegardes", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SelectedProjectPath = project.FilePath;
            DialogResult = true;
        }

        private void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            if (ProjectsList.SelectedItem is not SavedProjectInfo project)
            {
                return;
            }

            var answer = MessageBox.Show(this, $"Supprimer définitivement « {project.Name} » ?", "Supprimer la sauvegarde", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes) return;
            _service.Delete(project.FilePath);
            Refresh();
        }

        private void OnOpenFolderClick(object sender, RoutedEventArgs e) => _service.OpenFolder();
    }
}
