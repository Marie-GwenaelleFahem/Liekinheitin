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
            var masterLevel = ResolveMasterLevel(currentTime, project);

            foreach (var track in project.Tracks.Where(track => !track.IsMuted))
            {
                foreach (var clip in track.Clips.Where(clip => !clip.IsAudio && !clip.IsMedia && !clip.IsHidden && IsClipActive(clip, currentTime)))
                {
                    ApplyClip(colors, clip, currentTime, totalPixels, project.WallWidth, project.WallHeight);
                }
            }

            var state = new State();
            for (var entityId = 0; entityId < totalPixels; entityId++)
            {
                var color = colors.GetValueOrDefault(entityId, new RgbwColor(0, 0, 0, 0));
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

        public State ComputeBlackoutState(ShowProject project)
        {
            var state = new State();
            var totalPixels = Math.Max(0, project.WallWidth * project.WallHeight);
            for (var entityId = 0; entityId < totalPixels; entityId++)
            {
                state.Entities.Add(new Entity
                {
                    Id = entityId,
                    Channels = new byte[] { 0, 0, 0, 0 }
                });
            }

            return state;
        }
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
                EffectType.Snowfall => Scale(SnowfallColor(localTime, clip.Duration, entityId, wallWidth, wallHeight, clip.Speed), intensity),
                EffectType.Frost => Scale(FrostColor(localTime, clip.Duration, entityId, wallWidth, wallHeight), intensity),
                EffectType.Fire => Scale(FireColor(localTime, entityId, wallWidth, wallHeight, clip.Speed), intensity),
                EffectType.ToxicHeart => Scale(ToxicHeartColor(localTime, entityId, wallWidth, wallHeight, clip.Speed), intensity),
                EffectType.FireIce => Scale(FireIceColor(localTime, entityId, wallWidth, wallHeight, clip.Speed), intensity),
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

        private static RgbwColor SnowfallColor(double localTime, double duration, int entityId, int width, int height, double speed)
        {
            var x = entityId % Math.Max(1, width);
            var y = entityId / Math.Max(1, width);
            var progress = Math.Clamp(localTime / Math.Max(0.1, duration), 0, 1);
            var snow = 0.0;
            for (var flake = 0; flake < 34; flake++)
            {
                var seed = Hash01(flake * 91.17);
                var flakeX = (seed * width + Math.Sin(localTime * (0.7 + seed) + flake) * 5 + width) % width;
                var fallSpeed = (8 + (seed * 18)) * Math.Max(0.2, speed);
                var flakeY = ((Hash01(flake * 47.31) * height) + (localTime * fallSpeed)) % height;
                var dx = Math.Abs(x - flakeX);
                dx = Math.Min(dx, width - dx);
                var dy = Math.Abs(y - flakeY);
                if ((dx * dx) + (dy * dy) < 2.8) snow = Math.Max(snow, 0.72 + (seed * 0.28));
            }

            var bankHeight = progress * height * 0.24;
            var unevenBank = bankHeight + (Math.Sin(x * 0.19) * 2.2) + (Hash01(x * 13.7) * 3);
            if (y >= height - unevenBank) snow = Math.Max(snow, 0.58 + (0.42 * (y / (double)height)));

            if (progress > 0.52)
            {
                var cx = width / 2.0;
                var cy = height * 0.78;
                var dx = x - cx;
                var dy = y - cy;
                var radius = Math.Sqrt((dx * dx) + (dy * dy));
                var spiral = Math.Sin(Math.Atan2(dy, dx) * 3 + radius * 0.23 - localTime * 4.2);
                if (radius < width * 0.42 && spiral > 0.84) snow = Math.Max(snow, (spiral - 0.84) / 0.16);
            }

            return snow <= 0.02 ? new RgbwColor(0, 0, 0, 0) : Scale(new RgbwColor(185, 230, 255, 110), snow);
        }

        private static RgbwColor FrostColor(double localTime, double duration, int entityId, int width, int height)
        {
            var x = entityId % Math.Max(1, width);
            var y = entityId / Math.Max(1, width);
            var edgeDistance = Math.Min(Math.Min(x, width - 1 - x), Math.Min(y, height - 1 - y));
            var reach = Math.Clamp(localTime / Math.Max(0.1, duration), 0, 1) * Math.Min(width, height) * 0.52;
            if (edgeDistance > reach) return new RgbwColor(0, 0, 0, 0);
            var crystal = Math.Max(Math.Abs(Math.Sin((x + y) * 0.24)), Math.Abs(Math.Sin((x - y) * 0.27)));
            var branches = Math.Abs(Math.Sin((x * 0.11) + Math.Sin(y * 0.16) * 2.4));
            var level = crystal > 0.91 || branches > 0.965 ? 1.0 : 0.08 + (0.24 * (1 - edgeDistance / Math.Max(1, reach)));
            return Scale(new RgbwColor(92, 190, 255, 95), level);
        }

        private static RgbwColor FireColor(double localTime, int entityId, int width, int height, double speed)
        {
            var x = entityId % Math.Max(1, width);
            var y = entityId / Math.Max(1, width);
            var fromBottom = height - 1 - y;
            var flicker = (Math.Sin((x * 0.31) + localTime * 7 * Math.Max(0.2, speed)) + Math.Sin((x * 0.77) - localTime * 11)) * 0.5;
            var noise = Hash01((x * 17.13) + (Math.Floor(localTime * 14) * 37.7));
            var flameHeight = height * (0.22 + (noise * 0.22)) + (flicker * 8);
            if (fromBottom > flameHeight) return new RgbwColor(0, 0, 0, 0);
            var heat = Math.Clamp(1 - (fromBottom / Math.Max(1, flameHeight)), 0, 1);
            return heat switch
            {
                > 0.72 => new RgbwColor(255, 205, 55, 35),
                > 0.38 => new RgbwColor(255, 82, 18, 0),
                _ => new RgbwColor(150, 12, 28, 0)
            };
        }

        private static RgbwColor ToxicHeartColor(double localTime, int entityId, int width, int height, double speed)
        {
            var x = entityId % Math.Max(1, width);
            var y = entityId / Math.Max(1, width);
            var pulse = 0.88 + (0.12 * Math.Sin(localTime * Math.Max(0.2, speed) * Math.PI * 2));
            var nx = (x - (width / 2.0)) / (width * 0.27 * pulse);
            var ny = ((height / 2.0) - y) / (height * 0.27 * pulse);
            var equation = Math.Pow((nx * nx) + (ny * ny) - 1, 3) - (nx * nx * ny * ny * ny);
            var outline = Math.Abs(equation) < 0.075;
            var glitch = Math.Abs(y - ((int)(localTime * 9) * 17 % Math.Max(1, height))) <= 1 && Math.Abs(nx) < 1.25;
            if (!outline && !glitch) return new RgbwColor(0, 0, 0, 0);
            var toxic = ((x + (int)(localTime * 12)) / 7) % 3 == 0;
            return toxic ? new RgbwColor(115, 255, 42, 0) : new RgbwColor(255, 28, 132, 0);
        }

        private static RgbwColor FireIceColor(double localTime, int entityId, int width, int height, double speed)
        {
            var x = entityId % Math.Max(1, width);
            if (x < width / 2)
            {
                var frost = FrostColor(localTime + 2, 4, entityId, width, height);
                var snow = SnowfallColor(localTime, 5, entityId, width, height, speed);
                return Add(frost, snow);
            }

            var fire = FireColor(localTime, entityId, width, height, speed);
            var centerDistance = Math.Abs(x - (width / 2.0));
            if (centerDistance < 4 && Hash01(entityId + Math.Floor(localTime * 18) * 31) > 0.55)
                return new RgbwColor(255, 255, 255, 120);
            return fire;
        }

        private static double Hash01(double value)
        {
            var noise = Math.Sin(value * 12.9898) * 43758.5453;
            return noise - Math.Floor(noise);
        }

        private static RgbwColor Add(RgbwColor left, RgbwColor right)
            => new(
                (byte)Math.Min(255, left.R + right.R),
                (byte)Math.Min(255, left.G + right.G),
                (byte)Math.Min(255, left.B + right.B),
                (byte)Math.Min(255, left.W + right.W));
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
