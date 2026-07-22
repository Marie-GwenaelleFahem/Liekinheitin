using System;
using System.Collections.Generic;
using System.Linq;
using Liekinheitin.CreativeTool.Models;
using Liekinheitin.Domain.Entities;

namespace Liekinheitin.CreativeTool.Services
{
    public class ShowPlaybackEngine
    {
        // Identifiants réels du patch (patch.json), triés, une seule fois : le pixel de grille
        // interne n (0, 1, 2...) correspond à _realEntityIds[n]. Utilisé uniquement par
        // MapToRealEntityIds() pour traduire un State avant l'envoi réseau — ComputeState()
        // continue de renvoyer des ID de grille bruts, parce que l'aperçu local (PixelGridView)
        // indexe directement son buffer par ces ID et ne connaît rien du vrai patch.
        private readonly IReadOnlyList<int>? _realEntityIds;

        public ShowPlaybackEngine(IReadOnlyList<int>? realEntityIds = null)
        {
            _realEntityIds = realEntityIds;
        }

        public State ComputeState(double currentTime, ShowProject project)
        {
            var colors = new Dictionary<int, RgbwColor>();
            var totalPixels = Math.Max(0, project.WallWidth * project.WallHeight);
            var masterLevel = ResolveMasterLevel(currentTime, project);

            foreach (var track in project.Tracks.Where(track => !track.IsMuted))
            {
                foreach (var clip in track.Clips.Where(clip => !clip.IsAudio && !clip.IsMedia && !clip.IsHidden && IsClipActive(clip, currentTime)))
                {
                    ApplyClip(colors, clip, currentTime, totalPixels, project.WallWidth, project.WallHeight);
                }
            }

            var state = new State();
            foreach (var (entityId, color) in colors.OrderBy(pair => pair.Key))
            {
                var fadedColor = Scale(color, masterLevel);
                state.Entities.Add(new Entity
                {
                    Id = entityId,
                    Channels = fadedColor.W > 0
                        ? new[] { fadedColor.R, fadedColor.G, fadedColor.B, fadedColor.W }
                        : new[] { fadedColor.R, fadedColor.G, fadedColor.B }
                });
            }

            return state;
        }

        /// <summary>
        /// Construit une copie de <paramref name="gridState"/> (tel que renvoyé par
        /// <see cref="ComputeState"/>, en ID de grille) où chaque <c>Entity.Id</c> est traduit
        /// vers le vrai identifiant physique du patch — à appeler uniquement juste avant
        /// l'envoi réseau vers RoutingHost, jamais pour l'aperçu local.
        /// </summary>
        public State MapToRealEntityIds(State gridState)
        {
            if (_realEntityIds is not { Count: > 0 })
            {
                return gridState;
            }

            var mapped = new State();
            foreach (var entity in gridState.Entities)
            {
                mapped.Entities.Add(new Entity
                {
                    Id = ResolveRealEntityId(entity.Id),
                    Channels = entity.Channels,
                });
            }

            return mapped;
        }

        /// <summary>
        /// Traduit un identifiant de grille interne (0, 1, 2...) vers le vrai identifiant
        /// physique du patch, s'il en existe un à cette position. Sans liste réelle chargée (ou
        /// position hors limites), renvoie l'identifiant de grille tel quel.
        /// </summary>
        private int ResolveRealEntityId(int gridIndex)
            => _realEntityIds is { Count: > 0 } && gridIndex >= 0 && gridIndex < _realEntityIds.Count
                ? _realEntityIds[gridIndex]
                : gridIndex;

        private static double ResolveMasterLevel(double currentTime, ShowProject project)
        {
            var fadeDuration = Math.Max(0, project.AudioFadeOutDuration);
            if (fadeDuration <= 0)
            {
                return 1;
            }

            var audioEnd = project.Tracks
                .SelectMany(track => track.Clips)
                .Where(clip => clip.IsAudio)
                .Select(clip => clip.EndTime)
                .DefaultIfEmpty(project.Duration)
                .Max();
            var fadeStart = Math.Max(0, audioEnd - fadeDuration);
            if (currentTime <= fadeStart)
            {
                return 1;
            }

            return Math.Clamp((audioEnd - currentTime) / Math.Max(0.001, audioEnd - fadeStart), 0, 1);
        }

        private static void ApplyClip(
            IDictionary<int, RgbwColor> colors,
            TimelineClip clip,
            double currentTime,
            int totalPixels,
            int wallWidth,
            int wallHeight)
        {
            var localTime = currentTime - clip.StartTime;
            var movementOffset = ResolveMovementOffset(clip, localTime);
            var movementIntensity = MovementIntensity(clip, localTime);
            var targets = ResolveTargets(clip, totalPixels).ToList();
            var rotationCenter = GetRotationCenter(clip, targets, wallWidth);

            foreach (var entityId in targets)
            {
                var targetEntityId = ApplyTransform(entityId, clip, rotationCenter, movementOffset.OffsetX, movementOffset.OffsetY, wallWidth, wallHeight);
                if (targetEntityId is null)
                {
                    continue;
                }

                colors[targetEntityId.Value] = Scale(ComputeClipColor(clip, localTime, targetEntityId.Value, wallWidth, wallHeight), movementIntensity);
            }
        }

        private static bool IsClipActive(TimelineClip clip, double currentTime)
            => currentTime >= clip.StartTime && currentTime <= clip.EndTime;

        private static IEnumerable<int> ResolveTargets(TimelineClip clip, int totalPixels)
        {
            if (clip.Target.Type == TargetType.Selection)
            {
                return clip.Target.EntityIds;
            }

            return Enumerable.Range(0, totalPixels);
        }

        private static RgbwColor ComputeClipColor(TimelineClip clip, double localTime, int entityId, int wallWidth, int wallHeight)
        {
            var intensity = Math.Clamp(clip.Intensity, 0, 1);
            var color = clip.EffectType switch
            {
                EffectType.Fade => Scale(clip.Color, FadeLevel(localTime, clip.Duration) * intensity),
                EffectType.Wave => Scale(clip.Color, WaveLevel(localTime, entityId, wallWidth, clip.Speed) * intensity),
                EffectType.Pulse => Scale(clip.Color, PulseLevel(localTime, clip.Speed) * intensity),
                EffectType.Strobe => Scale(clip.Color, StrobeLevel(localTime, clip.Speed) * intensity),
                EffectType.Chase => Scale(clip.Color, ChaseLevel(localTime, entityId, wallWidth, clip.Speed) * intensity),
                EffectType.Breath => Scale(clip.Color, BreathLevel(localTime, clip.Speed) * intensity),
                EffectType.Sparkle => Scale(clip.Color, SparkleLevel(localTime, entityId, clip.Speed) * intensity),
                EffectType.Equalizer => Scale(clip.Color, EqualizerLevel(localTime, entityId, wallWidth, wallHeight, clip.Speed) * intensity),
                EffectType.Ripple => Scale(clip.Color, RippleLevel(localTime, entityId, wallWidth, wallHeight, clip.Speed) * intensity),
                _ => Scale(clip.Color, intensity)
            };

            return color;
        }

        private static (double X, double Y)? GetRotationCenter(TimelineClip clip, IReadOnlyCollection<int> targets, int wallWidth)
        {
            if (clip.Target.Type != TargetType.Selection || targets.Count == 0 || Math.Abs(clip.RotationDegrees) < 0.001)
            {
                return null;
            }

            var minX = targets.Min(id => id % wallWidth);
            var maxX = targets.Max(id => id % wallWidth);
            var minY = targets.Min(id => id / wallWidth);
            var maxY = targets.Max(id => id / wallWidth);
            return ((minX + maxX) / 2.0, (minY + maxY) / 2.0);
        }

        private static int? ApplyTransform(
            int entityId,
            TimelineClip clip,
            (double X, double Y)? rotationCenter,
            int offsetX,
            int offsetY,
            int wallWidth,
            int wallHeight)
        {
            if (clip.Target.Type != TargetType.Selection)
            {
                return entityId;
            }

            var x = entityId % wallWidth;
            var y = entityId / wallWidth;
            if (rotationCenter is { } center)
            {
                var radians = clip.RotationDegrees * Math.PI / 180.0;
                var cosine = Math.Cos(radians);
                var sine = Math.Sin(radians);
                var relativeX = x - center.X;
                var relativeY = y - center.Y;
                x = (int)Math.Round(center.X + (relativeX * cosine) - (relativeY * sine));
                y = (int)Math.Round(center.Y + (relativeX * sine) + (relativeY * cosine));
            }
            var movedX = x + offsetX;
            var movedY = y + offsetY;

            if (movedX < 0 || movedX >= wallWidth || movedY < 0 || movedY >= wallHeight)
            {
                return null;
            }

            return (movedY * wallWidth) + movedX;
        }

        private static (int OffsetX, int OffsetY) ResolveMovementOffset(TimelineClip clip, double localTime)
        {
            if (clip.Target.Type != TargetType.Selection)
            {
                return (0, 0);
            }

            if (clip.MovementKeyframes.Count > 0)
            {
                return InterpolateKeyframes(clip, localTime);
            }

            if (clip.MovementEffect == MovementEffectType.None)
            {
                return (0, 0);
            }

            var progress = MovementProgress(clip, localTime);
            return (
                (int)Math.Round(clip.MovementOffsetX * progress),
                (int)Math.Round(clip.MovementOffsetY * progress));
        }

        private static (int OffsetX, int OffsetY) InterpolateKeyframes(TimelineClip clip, double localTime)
        {
            var keyframes = clip.MovementKeyframes.OrderBy(keyframe => keyframe.Time).ToList();
            if (localTime <= keyframes[0].Time)
            {
                return (keyframes[0].OffsetX, keyframes[0].OffsetY);
            }

            for (var index = 1; index < keyframes.Count; index++)
            {
                var previous = keyframes[index - 1];
                var next = keyframes[index];
                if (localTime > next.Time)
                {
                    continue;
                }

                var span = Math.Max(0.001, next.Time - previous.Time);
                var progress = Math.Clamp((localTime - previous.Time) / span, 0, 1);
                return (
                    (int)Math.Round(previous.OffsetX + ((next.OffsetX - previous.OffsetX) * progress)),
                    (int)Math.Round(previous.OffsetY + ((next.OffsetY - previous.OffsetY) * progress)));
            }

            var last = keyframes[^1];
            return (last.OffsetX, last.OffsetY);
        }

        private static double MovementProgress(TimelineClip clip, double localTime)
        {
            if (clip.MovementEffect == MovementEffectType.None || clip.Duration <= 0)
            {
                return 0;
            }

            var progress = Math.Clamp(localTime / clip.Duration, 0, 1);
            return clip.MovementEffect switch
            {
                MovementEffectType.Snap => progress <= 0 ? 0 : 1,
                MovementEffectType.Punch => progress < 0.18 ? progress / 0.18 : 1,
                MovementEffectType.VeryFast => 1 - Math.Pow(1 - progress, 5),
                MovementEffectType.Slow => progress * progress,
                MovementEffectType.Fast => 1 - Math.Pow(1 - progress, 3),
                _ => progress
            };
        }

        private static double MovementIntensity(TimelineClip clip, double localTime)
        {
            if (clip.MovementEffect != MovementEffectType.Fade)
            {
                return 1;
            }

            return FadeLevel(localTime, clip.Duration);
        }

        private static double FadeLevel(double localTime, double duration)
        {
            if (duration <= 0)
            {
                return 1;
            }

            var progress = Math.Clamp(localTime / duration, 0, 1);
            return progress <= 0.5 ? progress * 2 : (1 - progress) * 2;
        }

        private static double WaveLevel(double localTime, int entityId, int wallWidth, double speed)
        {
            var width = Math.Max(1, wallWidth);
            var x = entityId % width;
            var y = entityId / width;
            var phase = (x * 0.09) + (y * 0.045) + (localTime * Math.Max(0.1, speed) * 4.0);
            return (Math.Sin(phase) + 1.0) * 0.5;
        }

        private static double PulseLevel(double localTime, double speed)
            => 0.15 + (0.85 * Math.Abs(Math.Sin(localTime * Math.Max(0.1, speed) * Math.PI)));

        private static double StrobeLevel(double localTime, double speed)
            => (int)(localTime * Math.Max(0.1, speed) * 10) % 2 == 0 ? 1 : 0.04;

        private static double ChaseLevel(double localTime, int entityId, int wallWidth, double speed)
        {
            var x = entityId % Math.Max(1, wallWidth);
            var phase = (x * 0.32) - (localTime * Math.Max(0.1, speed) * 7);
            return Math.Pow((Math.Sin(phase) + 1) * 0.5, 3);
        }

        private static double BreathLevel(double localTime, double speed)
            => 0.18 + (0.82 * ((Math.Sin((localTime * Math.Max(0.1, speed) * 2.2) - (Math.PI / 2)) + 1) * 0.5));

        private static double SparkleLevel(double localTime, int entityId, double speed)
        {
            var noise = Math.Sin((entityId * 12.9898) + (Math.Floor(localTime * Math.Max(0.1, speed) * 12) * 78.233)) * 43758.5453;
            return noise - Math.Floor(noise) > 0.86 ? 1 : 0.08;
        }

        private static double EqualizerLevel(double localTime, int entityId, int wallWidth, int wallHeight, double speed)
        {
            var x = entityId % Math.Max(1, wallWidth);
            var y = entityId / Math.Max(1, wallWidth);
            var centerDistance = Math.Abs(x - ((wallWidth - 1) / 2.0));
            var centerProgress = 1 - (centerDistance / Math.Max(1, wallWidth / 2.0));
            var fixedProfile = 0.34 + (0.66 * Math.Sin(Math.Clamp(centerProgress, 0, 1) * Math.PI / 2));
            var commonPulse = 0.76 + (0.24 * ((Math.Sin(localTime * Math.Max(0.1, speed) * 3.2) + 1) * 0.5));
            var amplitude = 0.12 + (0.82 * fixedProfile * commonPulse);
            var normalizedHeight = 1 - (y / (double)Math.Max(1, wallHeight - 1));
            var edgeDistance = amplitude - normalizedHeight;
            if (edgeDistance >= 0.035) return 1;
            if (edgeDistance >= 0) return 0.45 + (edgeDistance / 0.035 * 0.55);
            return 0.025;
        }

        private static double RippleLevel(double localTime, int entityId, int wallWidth, int wallHeight, double speed)
        {
            var x = entityId % Math.Max(1, wallWidth);
            var y = entityId / Math.Max(1, wallWidth);
            var dx = x - ((wallWidth - 1) / 2.0);
            var dy = y - ((wallHeight - 1) / 2.0);
            var distance = Math.Sqrt((dx * dx) + (dy * dy));
            var phase = (distance * 0.42) - (localTime * Math.Max(0.1, speed) * 6);
            return Math.Pow((Math.Sin(phase) + 1) * 0.5, 4);
        }

        private static RgbwColor Scale(RgbwColor color, double factor)
        {
            var level = Math.Clamp(factor, 0, 1);
            return new RgbwColor(
                ScaleChannel(color.R, level),
                ScaleChannel(color.G, level),
                ScaleChannel(color.B, level),
                ScaleChannel(color.W, level));
        }

        private static byte ScaleChannel(byte value, double factor) => (byte)Math.Clamp((int)Math.Round(value * factor), 0, 255);
    }
}
