using System.IO;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
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

            var root = JsonSerializer.SerializeToNode(project, JsonOptions)!.AsObject();
            if (root["AudioFilePath"] is JsonValue legacy && legacy.TryGetValue<string>(out var legacyPath) && !string.IsNullOrWhiteSpace(legacyPath))
                root["AudioFilePath"] = MakePortableAudioPath(legacyPath);
            if (root["Tracks"] is JsonArray tracks)
                foreach (var clip in tracks.OfType<JsonObject>().SelectMany(track => track["Clips"]?.AsArray().OfType<JsonObject>() ?? []))
                    if (clip["AudioFilePath"] is JsonValue value && value.TryGetValue<string>(out var audioPath) && !string.IsNullOrWhiteSpace(audioPath))
                        clip["AudioFilePath"] = MakePortableAudioPath(audioPath);
            File.WriteAllText(path, root.ToJsonString(JsonOptions));
        }

        public ShowProject Load(string path)
        {
            var json = File.ReadAllText(path);
            var project = JsonSerializer.Deserialize<ShowProject>(json, JsonOptions)
                ?? throw new InvalidDataException("Le fichier projet est vide ou invalide.");

            project.AudioFilePath = ResolveMediaPath(project.AudioFilePath, path);

            var audioClips = project.Tracks.SelectMany(track => track.Clips).Where(clip => clip.IsAudio).ToList();
            foreach (var clip in audioClips)
                clip.AudioFilePath = ResolveMediaPath(clip.AudioFilePath, path);

            // Compatibilite avec les anciens projets qui ne possedaient qu'un chemin global.
            if (audioClips.Count > 0 && audioClips.All(clip => string.IsNullOrWhiteSpace(clip.AudioFilePath)))
                audioClips[0].AudioFilePath = project.AudioFilePath;

            foreach (var media in project.MediaOverlays)
            {
                if (!string.IsNullOrWhiteSpace(media.FilePath) && !Path.IsPathRooted(media.FilePath))
                {
                    media.FilePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, media.FilePath));
                }
            }

            return project;
        }

        public static string MakePortableAudioPath(string path)
        {
            var fullPath = Path.GetFullPath(path);
            var marker = $"{Path.DirectorySeparatorChar}Animations{Path.DirectorySeparatorChar}";
            var markerIndex = fullPath.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
            return markerIndex >= 0 ? fullPath[(markerIndex + 1)..] : fullPath;
        }

        private static string? ResolveMediaPath(string? storedPath, string projectPath)
        {
            if (string.IsNullOrWhiteSpace(storedPath)) return null;
            var normalized = storedPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).Trim();
            var candidates = new System.Collections.Generic.List<string>();
            if (Path.IsPathRooted(normalized)) candidates.Add(normalized);

            var relative = normalized.TrimStart(Path.DirectorySeparatorChar);
            var creativeMarker = $"CreativeTool{Path.DirectorySeparatorChar}";
            var creativeIndex = relative.IndexOf(creativeMarker, StringComparison.OrdinalIgnoreCase);
            if (creativeIndex >= 0) relative = relative[(creativeIndex + creativeMarker.Length)..];

            candidates.Add(Path.Combine(Path.GetDirectoryName(projectPath) ?? string.Empty, relative));
            candidates.Add(Path.Combine(AppContext.BaseDirectory, relative));
            candidates.Add(Path.Combine(AppContext.BaseDirectory, "Animations", "Musique", Path.GetFileName(relative)));

            return candidates.Select(Path.GetFullPath).FirstOrDefault(File.Exists) ?? storedPath;
        }
    }
}
