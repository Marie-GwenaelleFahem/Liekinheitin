using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace Liekinheitin.CreativeTool.Domain
{
    /// <summary>
    /// L'état d'une forme à un instant précis. Toutes les propriétés interpolables
    /// (numériques + couleur) ; Type et Id ne changent jamais dans le temps pour une forme
    /// donnée, ils ne sont donc pas répétés ici mais dans PlacedShape.
    /// </summary>
    public sealed class ShapeKeyframe
    {
        public double TimeSeconds { get; init; }
        public int X { get; init; }
        public int Y { get; init; }
        public int BaseWidth { get; init; }
        public int BaseHeight { get; init; }
        public double Scale { get; init; }
        public Color Color { get; init; }
    }

    /// <summary>La trajectoire complète d'une forme dans le temps : ses keyframes triées.</summary>
    public sealed class ShapeTrack
    {
        public Guid ShapeId { get; init; }
        public ShapeType Type { get; init; }
        public List<ShapeKeyframe> Keyframes { get; } = new();

        /// <summary>Ajoute ou remplace la keyframe à cet instant (une seule par instant).</summary>
        public void SetKeyframe(ShapeKeyframe keyframe)
        {
            Keyframes.RemoveAll(k => Math.Abs(k.TimeSeconds - keyframe.TimeSeconds) < 0.001);
            Keyframes.Add(keyframe);
            Keyframes.Sort((a, b) => a.TimeSeconds.CompareTo(b.TimeSeconds));
        }

        /// <summary>
        /// Calcule l'état interpolé de la forme à un instant donné. Avant la première
        /// keyframe : reste figée sur la première. Après la dernière : reste figée sur la
        /// dernière. Entre deux : interpolation linéaire de toutes les propriétés.
        /// </summary>
        public ShapeKeyframe? Evaluate(double timeSeconds)
        {
            if (Keyframes.Count == 0) return null;
            if (timeSeconds <= Keyframes[0].TimeSeconds) return Keyframes[0];
            if (timeSeconds >= Keyframes[^1].TimeSeconds) return Keyframes[^1];

            for (int i = 0; i < Keyframes.Count - 1; i++)
            {
                var a = Keyframes[i];
                var b = Keyframes[i + 1];
                if (timeSeconds < a.TimeSeconds || timeSeconds > b.TimeSeconds) continue;

                double span = b.TimeSeconds - a.TimeSeconds;
                double t = span <= 0 ? 0 : (timeSeconds - a.TimeSeconds) / span;

                return new ShapeKeyframe
                {
                    TimeSeconds = timeSeconds,
                    X = (int)Math.Round(Lerp(a.X, b.X, t)),
                    Y = (int)Math.Round(Lerp(a.Y, b.Y, t)),
                    BaseWidth = (int)Math.Round(Lerp(a.BaseWidth, b.BaseWidth, t)),
                    BaseHeight = (int)Math.Round(Lerp(a.BaseHeight, b.BaseHeight, t)),
                    Scale = Lerp(a.Scale, b.Scale, t),
                    Color = LerpColor(a.Color, b.Color, t),
                };
            }

            return Keyframes[^1]; // filet de sécurité, ne devrait pas être atteint
        }

        private static double Lerp(double a, double b, double t) => a + (b - a) * t;

        private static Color LerpColor(Color a, Color b, double t) => Color.FromRgb(
            (byte)Math.Round(Lerp(a.R, b.R, t)),
            (byte)Math.Round(Lerp(a.G, b.G, t)),
            (byte)Math.Round(Lerp(a.B, b.B, t)));
    }

    /// <summary>
    /// L'animation complète : une trajectoire indépendante par forme. Une forme absente
    /// de la timeline (jamais keyframée) garde sa position/couleur figée, gérée par
    /// SceneManager comme avant, en dehors de toute lecture d'animation.
    /// </summary>
    public sealed class Timeline
    {
        public List<ShapeTrack> Tracks { get; } = new();

        public double DurationSeconds => Tracks.Count == 0
            ? 0
            : Tracks.SelectMany(t => t.Keyframes).Select(k => k.TimeSeconds).DefaultIfEmpty(0).Max();

        public ShapeTrack GetOrCreateTrack(Guid shapeId, ShapeType type)
        {
            var track = Tracks.FirstOrDefault(t => t.ShapeId == shapeId);
            if (track is null)
            {
                track = new ShapeTrack { ShapeId = shapeId, Type = type };
                Tracks.Add(track);
            }
            return track;
        }

        public ShapeTrack? FindTrack(Guid shapeId) => Tracks.FirstOrDefault(t => t.ShapeId == shapeId);
    }
}