using System.IO;
using System.Text.Json;
using Liekinheitin.CreativeTool.Models;

namespace Liekinheitin.CreativeTool.Services
{
    public class ProjectFileService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public void Save(string path, ShowProject project)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(project, JsonOptions);
            File.WriteAllText(path, json);
        }

        public ShowProject Load(string path)
        {
            var json = File.ReadAllText(path);
            var project = JsonSerializer.Deserialize<ShowProject>(json, JsonOptions)
                ?? throw new InvalidDataException("Le fichier projet est vide ou invalide.");

            if (!string.IsNullOrWhiteSpace(project.AudioFilePath) && !Path.IsPathRooted(project.AudioFilePath))
            {
                project.AudioFilePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, project.AudioFilePath));
            }

            return project;
        }
    }
}
