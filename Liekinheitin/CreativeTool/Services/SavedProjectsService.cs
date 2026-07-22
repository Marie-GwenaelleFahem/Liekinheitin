using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Liekinheitin.CreativeTool.Models;

namespace Liekinheitin.CreativeTool.Services
{
    public sealed class SavedProjectsService
    {
        private readonly ProjectFileService _projectFileService;

        public SavedProjectsService(ProjectFileService projectFileService)
        {
            _projectFileService = projectFileService;
            Directory.CreateDirectory(FolderPath);
            InstallBundledAnimations();
        }

        public string FolderPath { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Liekinheitin",
            "Sauvegardes");

        public IReadOnlyList<SavedProjectInfo> GetAll()
            => Directory.EnumerateFiles(FolderPath, "*.lshow")
                .Select(path => new FileInfo(path))
                .OrderByDescending(file => file.LastWriteTime)
                .Select(file => new SavedProjectInfo
                {
                    Name = Path.GetFileNameWithoutExtension(file.Name),
                    FilePath = file.FullName,
                    ModifiedAt = file.LastWriteTime
                })
                .ToList();

        public string Save(ShowProject project)
        {
            var fileName = SanitizeFileName(string.IsNullOrWhiteSpace(project.Name) ? "Animation" : project.Name);
            var path = Path.Combine(FolderPath, fileName + ".lshow");
            _projectFileService.Save(path, project);
            return path;
        }

        public void Delete(string path)
        {
            var fullPath = Path.GetFullPath(path);
            var folder = Path.GetFullPath(FolderPath) + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(folder, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Cette sauvegarde n'appartient pas au dossier Liekinheitin.");
            }

            File.Delete(fullPath);
        }

        public void OpenFolder()
            => Process.Start(new ProcessStartInfo(FolderPath) { UseShellExecute = true });

        private static string SanitizeFileName(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var safe = new string(value.Select(character => invalid.Contains(character) ? '_' : character).ToArray()).Trim();
            return string.IsNullOrWhiteSpace(safe) ? "Animation" : safe;
        }

        private void InstallBundledAnimations()
        {
            var bundledFolder = Path.Combine(AppContext.BaseDirectory, "Animations");
            if (!Directory.Exists(bundledFolder)) return;

            foreach (var sourcePath in Directory.EnumerateFiles(bundledFolder))
            {
                var destinationPath = Path.Combine(FolderPath, Path.GetFileName(sourcePath));
                if (!File.Exists(destinationPath)) File.Copy(sourcePath, destinationPath);
            }
        }
    }
}
