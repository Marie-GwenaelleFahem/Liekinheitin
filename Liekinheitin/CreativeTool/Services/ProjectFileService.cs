using System;
using System.Collections.Generic;
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
            ArgumentNullException.ThrowIfNull(project);
            Normalize(project);

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

            Normalize(project);
            ResolveMediaPaths(project, path);
            return project;
        }

        private static void Normalize(ShowProject project)
        {
            project.Name = string.IsNullOrWhiteSpace(project.Name) ? "Projet sans titre" : project.Name.Trim();
            project.Duration = Math.Max(1, project.Duration);
            project.WallWidth = Math.Clamp(project.WallWidth, 1, 512);
            project.WallHeight = Math.Clamp(project.WallHeight, 1, 512);
            project.AudioVolume = Math.Clamp(project.AudioVolume, 0, 1);
            if (project.AudioPlaybackDuration is { } audioPlaybackDuration)
            {
                project.AudioPlaybackDuration = Math.Clamp(audioPlaybackDuration, 0.05, project.Duration);
            }
            project.AudioFadeOutDuration = Math.Max(0, project.AudioFadeOutDuration);
            project.Tracks ??= new List<Track>();
            project.MediaOverlays ??= new List<MediaOverlayClip>();

            foreach (var track in project.Tracks)
            {
                track.Name = string.IsNullOrWhiteSpace(track.Name) ? "Piste" : track.Name.Trim();
                track.Clips ??= new List<TimelineClip>();
                foreach (var clip in track.Clips)
                {
                    clip.Name = string.IsNullOrWhiteSpace(clip.Name) ? "Clip" : clip.Name.Trim();
                    clip.StartTime = Math.Max(0, clip.StartTime);
                    clip.Duration = Math.Max(0.05, clip.Duration);
                    clip.Intensity = Math.Clamp(clip.Intensity, 0, 1);
                    clip.Speed = Math.Max(0.01, clip.Speed);
                    clip.Target ??= TargetSelection.FullWall();
                    if (clip.Target.Type == TargetType.Track)
                    {
                        clip.Target.Type = TargetType.FullWall;
                        clip.Target.TrackName = null;
                    }
                    clip.Target.EntityIds ??= new List<int>();
                    clip.MovementKeyframes ??= new List<MovementKeyframe>();
                    project.Duration = Math.Max(project.Duration, clip.EndTime);
                }
            }

            foreach (var media in project.MediaOverlays)
            {
                media.Id = string.IsNullOrWhiteSpace(media.Id) ? Guid.NewGuid().ToString("N") : media.Id;
                media.Name = string.IsNullOrWhiteSpace(media.Name) ? "Média" : media.Name.Trim();
                media.StartTime = Math.Max(0, media.StartTime);
                media.Duration = Math.Max(0.05, media.Duration);
                media.Opacity = Math.Clamp(media.Opacity, 0, 1);
                project.Duration = Math.Max(project.Duration, media.StartTime + media.Duration);
            }
        }

        private static void ResolveMediaPaths(ShowProject project, string projectPath)
        {
            var projectFolder = Path.GetDirectoryName(projectPath) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(project.AudioFilePath))
            {
                project.AudioFilePath = ResolveExistingPath(project.AudioFilePath, projectFolder);
            }

            foreach (var media in project.MediaOverlays)
            {
                if (!string.IsNullOrWhiteSpace(media.FilePath))
                {
                    media.FilePath = ResolveExistingPath(media.FilePath, projectFolder);
                }
            }
        }

        private static string ResolveExistingPath(string configuredPath, string projectFolder)
        {
            var resolvedPath = Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.GetFullPath(Path.Combine(projectFolder, configuredPath));
            if (File.Exists(resolvedPath))
            {
                return resolvedPath;
            }

            var fileName = Path.GetFileName(configuredPath);
            var besideProject = Path.Combine(projectFolder, fileName);
            if (File.Exists(besideProject))
            {
                return Path.GetFullPath(besideProject);
            }

            var bundledPath = Path.Combine(AppContext.BaseDirectory, "Animations", fileName);
            return File.Exists(bundledPath) ? Path.GetFullPath(bundledPath) : resolvedPath;
        }
    }
}
