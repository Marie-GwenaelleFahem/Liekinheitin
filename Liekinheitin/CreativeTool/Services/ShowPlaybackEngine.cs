using System;
using System.Collections.Generic;
using System.Linq;
using Liekinheitin.CreativeTool.Models;
using Liekinheitin.Domain.Entities;

namespace Liekinheitin.CreativeTool.Services
{
    public class ShowPlaybackEngine
    {
        public State ComputeState(double currentTime, ShowProject project)
        {
            var colors = new Dictionary<int, RgbwColor>();
            var totalPixels = Math.Max(0, project.WallWidth * project.WallHeight);

            foreach (var track in project.Tracks.Where(track => !track.IsMuted))
            {
                foreach (var clip in track.Clips.Where(clip => !clip.IsAudio && IsClipActive(clip, currentTime)))
                {
                    ApplyClip(colors, clip, currentTime, totalPixels, project.WallWidth, project.WallHeight);
                }
            }

            var state = new State();
            foreach (var (entityId, color) in colors.OrderBy(pair => pair.Key))
            {
                state.Entities.Add(new Entity
                {
                    Id = entityId,
                    Channels = color.W > 0
                        ? new[] { color.R, color.G, color.B, color.W }
                        : new[] { color.R, color.G, color.B }
                });
            }

            return state;
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

            foreach (var entityId in ResolveTargets(clip, totalPixels))
            {
                var targetEntityId = ApplyMovement(entityId, clip, movementOffset.OffsetX, movementOffset.OffsetY, wallWidth, wallHeight);
                if (targetEntityId is null)
                {
                    continue;
                }

                colors[targetEntityId.Value] = Scale(ComputeClipColor(clip, localTime, targetEntityId.Value, wallWidth), movementIntensity);
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

        private static RgbwColor ComputeClipColor(TimelineClip clip, double localTime, int entityId, int wallWidth)
        {
            var intensity = Math.Clamp(clip.Intensity, 0, 1);
            var color = clip.EffectType switch
            {
                EffectType.Fade => Scale(clip.Color, FadeLevel(localTime, clip.Duration) * intensity),
                EffectType.Wave => Scale(clip.Color, WaveLevel(localTime, entityId, wallWidth, clip.Speed) * intensity),
                _ => Scale(clip.Color, intensity)
            };

            return color;
        }

        private static int? ApplyMovement(int entityId, TimelineClip clip, int offsetX, int offsetY, int wallWidth, int wallHeight)
        {
            if (clip.Target.Type != TargetType.Selection)
            {
                return entityId;
            }

            var x = entityId % wallWidth;
            var y = entityId / wallWidth;
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
