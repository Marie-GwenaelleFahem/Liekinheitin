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
                foreach (var clip in track.Clips.Where(clip => IsClipActive(clip, currentTime)))
                {
                    var localTime = currentTime - clip.StartTime;
                    foreach (var entityId in ResolveTargets(clip, totalPixels))
                    {
                        colors[entityId] = ComputeClipColor(clip, localTime, entityId, project.WallWidth);
                    }
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

        private static bool IsClipActive(TimelineClip clip, double currentTime)
            => currentTime >= clip.StartTime && currentTime <= clip.EndTime;

        private static IEnumerable<int> ResolveTargets(TimelineClip clip, int totalPixels)
        {
            if (clip.Target.Type == TargetType.Selection && clip.Target.EntityIds.Count > 0)
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
