using System;
using System.IO;
using System.Text.Json;
using System.Windows.Media;
using Liekinheitin.CreativeTool.Domain;

namespace Liekinheitin.CreativeTool.Services
{
    /// <summary>Sauvegarde/charge un projet complet (formes + timeline) en JSON.</summary>
    public static class ProjectFileService
    {
        public static void Save(string path, SceneManager scene, Timeline timeline, double totalDuration)
        {
            var data = new ProjectData { TotalDuration = totalDuration };

            foreach (var shape in scene.Shapes)
            {
                data.Shapes.Add(new ShapeData
                {
                    Id = shape.Id,
                    Type = shape.Type,
                    X = shape.X,
                    Y = shape.Y,
                    BaseWidth = shape.BaseWidth,
                    BaseHeight = shape.BaseHeight,
                    Scale = shape.Scale,
                    R = shape.Color.R,
                    G = shape.Color.G,
                    B = shape.Color.B,
                });
            }

            foreach (var track in timeline.Tracks)
            {
                var trackData = new ShapeTrackData { ShapeId = track.ShapeId, Type = track.Type };
                foreach (var kf in track.Keyframes)
                {
                    trackData.Keyframes.Add(new ShapeKeyframeData
                    {
                        TimeSeconds = kf.TimeSeconds,
                        X = kf.X,
                        Y = kf.Y,
                        BaseWidth = kf.BaseWidth,
                        BaseHeight = kf.BaseHeight,
                        Scale = kf.Scale,
                        R = kf.Color.R,
                        G = kf.Color.G,
                        B = kf.Color.B,
                    });
                }
                data.ShapeTracks.Add(trackData);
            }

            foreach (var track in timeline.FixtureTracks)
            {
                var trackData = new FixtureTrackData { EntityId = track.EntityId };
                foreach (var kf in track.Keyframes)
                {
                    trackData.Keyframes.Add(new FixtureKeyframeData
                    {
                        TimeSeconds = kf.TimeSeconds,
                        Pan = kf.Pan,
                        Tilt = kf.Tilt,
                        Speed = kf.Speed,
                        Dimming = kf.Dimming,
                        Strobe = kf.Strobe,
                        R = kf.R,
                        G = kf.G,
                        B = kf.B,
                        W = kf.W,
                    });
                }
                data.FixtureTracks.Add(trackData);
            }

            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        /// <summary>Charge le fichier et applique son contenu à scene/timeline (efface d'abord
        /// les formes et pistes existantes). Renvoie la durée totale à réappliquer au ViewModel.</summary>
        public static double Load(string path, SceneManager scene, Timeline timeline)
        {
            string json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<ProjectData>(json)
                ?? throw new InvalidOperationException("Fichier de projet invalide.");

            scene.ClearShapes();
            timeline.Tracks.Clear();
            timeline.FixtureTracks.Clear();

            foreach (var s in data.Shapes)
                scene.AddLoadedShape(s.Id, s.Type, s.X, s.Y, s.BaseWidth, s.BaseHeight, s.Scale, Color.FromRgb(s.R, s.G, s.B));

            foreach (var t in data.ShapeTracks)
            {
                var track = timeline.GetOrCreateTrack(t.ShapeId, t.Type);
                foreach (var kf in t.Keyframes)
                {
                    track.SetKeyframe(new ShapeKeyframe
                    {
                        TimeSeconds = kf.TimeSeconds,
                        X = kf.X,
                        Y = kf.Y,
                        BaseWidth = kf.BaseWidth,
                        BaseHeight = kf.BaseHeight,
                        Scale = kf.Scale,
                        Color = Color.FromRgb(kf.R, kf.G, kf.B),
                    });
                }
            }

            foreach (var t in data.FixtureTracks)
            {
                var track = timeline.GetOrCreateFixtureTrack(t.EntityId);
                foreach (var kf in t.Keyframes)
                {
                    track.SetKeyframe(new FixtureKeyframe
                    {
                        TimeSeconds = kf.TimeSeconds,
                        Pan = kf.Pan,
                        Tilt = kf.Tilt,
                        Speed = kf.Speed,
                        Dimming = kf.Dimming,
                        Strobe = kf.Strobe,
                        R = kf.R,
                        G = kf.G,
                        B = kf.B,
                        W = kf.W,
                    });
                }
            }

            return data.TotalDuration;
        }
    }
}