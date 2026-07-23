using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Liekinheitin.CreativeTool.Models;

namespace Liekinheitin.CreativeTool.Services
{
    public sealed class ReusableAnimationLibraryService
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public string FolderPath { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Liekinheitin",
            "Bibliotheque");

        public ReusableAnimationLibraryService()
        {
            Directory.CreateDirectory(FolderPath);
        }

        public IReadOnlyList<ReusableAnimationTemplate> GetAll()
        {
            var templates = new List<ReusableAnimationTemplate>();
            foreach (var path in Directory.EnumerateFiles(FolderPath, "*.ltemplate").OrderBy(path => path))
            {
                try
                {
                    var template = JsonSerializer.Deserialize<ReusableAnimationTemplate>(File.ReadAllText(path), JsonOptions);
                    if (template is not null) templates.Add(template);
                }
                catch (JsonException)
                {
                    // Un fichier utilisateur endommagé ne doit pas empêcher l'ouverture de l'atelier.
                }
            }
            return templates.OrderBy(template => template.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
        }

        public void Save(ReusableAnimationTemplate template)
        {
            var fileName = SanitizeFileName(template.Name);
            File.WriteAllText(Path.Combine(FolderPath, fileName + ".ltemplate"), JsonSerializer.Serialize(template, JsonOptions));
        }

        private static string SanitizeFileName(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var safe = new string(value.Select(character => invalid.Contains(character) ? '_' : character).ToArray()).Trim();
            return string.IsNullOrWhiteSpace(safe) ? "Animation reutilisable" : safe;
        }
    }
}
