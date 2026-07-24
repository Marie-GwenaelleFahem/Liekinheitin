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

            // Une trame est un état complet du mur, pas une liste de pixels à modifier.
            // En repartant de noir à chaque image, les pixels allumés par l'animation
            // précédente sont réellement éteints dès qu'ils ne font plus partie du rendu.
            for (var entityId = 0; entityId < totalPixels; entityId++)
            {
                colors[entityId] = new RgbwColor(0, 0, 0, 0);
            }

            var masterLevel = ResolveMasterLevel(currentTime, project);

            var activeTracks = project.Tracks.Where(track => !track.IsMuted).ToList();
            var hasActiveFullscreenRipple = activeTracks
                .SelectMany(track => track.Clips)
                .Any(clip => !clip.IsAudio
                    && !clip.IsMedia
                    && !clip.IsHidden
                    && clip.EffectType == EffectType.ClickRipple
                    && IsClipActive(clip, currentTime));

            foreach (var track in activeTracks
                .Where(track => !hasActiveFullscreenRipple || !IsMaleVoiceTrack(track))
                .OrderBy(track => VisualLayerPriority(track, hasActiveFullscreenRipple)))
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

        private static bool IsMaleVoiceTrack(Track track)
            => track.Name.Contains("Voix", StringComparison.OrdinalIgnoreCase)
                && track.Name.Contains("homme", StringComparison.OrdinalIgnoreCase);

        private static int VisualLayerPriority(Track track, bool hasActiveFullscreenRipple)
        {
            if (!hasActiveFullscreenRipple)
            {
                return 0;
            }

            if (track.Name.Contains("Violon", StringComparison.OrdinalIgnoreCase))
            {
                return 100;
            }

            return track.Name.Contains("Le Juge", StringComparison.OrdinalIgnoreCase) ? 50 : 0;
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
            if (project.DisableVisualFadeOut)
            {
                return 1.0;
            }

            const double visualFadeDuration = 2.0;
            var visualEnd = project.Tracks
                .SelectMany(track => track.Clips)
                .Where(clip => !clip.IsAudio && !clip.IsMedia && !clip.IsHidden)
                .Select(clip => clip.EndTime)
                .DefaultIfEmpty(project.Duration)
                .Max();
            var visualFadeStart = Math.Max(0, visualEnd - visualFadeDuration);
            var visualLevel = 1.0;
            if (currentTime > visualFadeStart)
            {
                var progress = Math.Clamp((currentTime - visualFadeStart) / Math.Max(0.001, visualEnd - visualFadeStart), 0, 1);
                var smoothProgress = progress * progress * (3 - (2 * progress));
                visualLevel = 1 - smoothProgress;
            }

            return visualLevel;
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
            if (clip.EffectType == EffectType.Sparkle)
            {
                ApplySparkles(colors, clip, localTime, totalPixels, wallWidth, wallHeight);
                return;
            }
            if (clip.EffectType == EffectType.FallingEmbers)
            {
                var rendered = new Dictionary<int, RgbwColor>();
                ApplyFallingEmbers(rendered, clip, localTime, wallWidth, wallHeight);
                CompositeMaskedEffect(colors, rendered, clip, localTime, totalPixels, wallWidth, wallHeight);
                return;
            }
            if (clip.EffectType == EffectType.WhiteFallingLines)
            {
                var rendered = new Dictionary<int, RgbwColor>();
                ApplyWhiteFallingLines(rendered, clip, localTime, wallWidth, wallHeight);
                CompositeMaskedEffect(colors, rendered, clip, localTime, totalPixels, wallWidth, wallHeight);
                return;
            }

            var cinematic = new Dictionary<int, RgbwColor>();
            if (CinematicEffectsRenderer.TryApply(cinematic, clip, localTime, wallWidth, wallHeight))
            {
                CompositeMaskedEffect(colors, cinematic, clip, localTime, totalPixels, wallWidth, wallHeight);
                return;
            }

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

        private static void CompositeMaskedEffect(
            IDictionary<int, RgbwColor> colors,
            IReadOnlyDictionary<int, RgbwColor> rendered,
            TimelineClip clip,
            double localTime,
            int totalPixels,
            int wallWidth,
            int wallHeight)
        {
            var sourceTargets = ResolveTargets(clip, totalPixels).ToList();
            var rotationCenter = GetRotationCenter(clip, sourceTargets, wallWidth);
            var movementOffset = ResolveMovementOffset(clip, localTime);
            var allowed = sourceTargets
                .Select(entityId => ApplyTransform(entityId, clip, rotationCenter, movementOffset.OffsetX, movementOffset.OffsetY, wallWidth, wallHeight))
                .Where(entityId => entityId.HasValue)
                .Select(entityId => entityId!.Value)
                .ToHashSet();
            var movementIntensity = MovementIntensity(clip, localTime);
            foreach (var (entityId, color) in rendered)
            {
                if (allowed.Contains(entityId)) colors[entityId] = Scale(color, movementIntensity);
            }
        }

        private static bool IsClipActive(TimelineClip clip, double currentTime)
            => currentTime >= clip.StartTime && currentTime <= clip.EndTime;

        private static void ApplyFallingEmbers(
            IDictionary<int, RgbwColor> colors,
            TimelineClip clip,
            double localTime,
            int wallWidth,
            int wallHeight)
        {
            var progress = Math.Clamp(localTime / Math.Max(0.001, clip.Duration), 0, 1);
            var seed = (int)Math.Round(clip.StartTime * 1000) ^ 0x51ED270B;
            const int particleCount = 18;

            for (var index = 0; index < particleCount; index++)
            {
                var originX = Random01(seed, index, 1) * (wallWidth - 1);
                var verticalPhase = Random01(seed, index, 2) * 1.28;
                var fallSpeed = 0.16 + (Random01(seed, index, 3) * 0.12);
                var verticalPosition = (verticalPhase + (progress * fallSpeed)) % 1.28;
                var anchorY = (int)Math.Round((verticalPosition * (wallHeight + 30)) - 15);
                var oscillation = Math.Sin((progress * Math.PI * 1.15) + (index * 1.37));
                var anchorX = (int)Math.Round(originX + (oscillation * (6 + (Random01(seed, index, 4) * 8))));
                var featherLength = 7 + (int)Math.Round(Random01(seed, index, 5) * 9);
                var lean = (Random01(seed, index, 6) - 0.5) * 0.72;
                var flicker = 0.72 + (0.28 * Random01(seed + (int)(localTime * 25), index, 7));
                var screenFade = Math.Clamp(Math.Min(verticalPosition / 0.09, (1.28 - verticalPosition) / 0.12), 0, 1);
                var level = clip.Intensity * flicker * screenFade;

                for (var segment = 0; segment < featherLength; segment++)
                {
                    var ratio = segment / (double)Math.Max(1, featherLength - 1);
                    var curve = Math.Sin((ratio * Math.PI * 1.35) + (index * 0.41)) * 2.1;
                    var x = (int)Math.Round(anchorX + (lean * segment) + curve);
                    var y = anchorY - segment;
                    if (x < 0 || x >= wallWidth || y < 0 || y >= wallHeight)
                    {
                        continue;
                    }

                    var bodyFade = Math.Pow(1 - (ratio * 0.72), 0.8) * level;
                    var color = ratio switch
                    {
                        < 0.20 => new RgbwColor(255, 214, 72, 28),
                        < 0.52 => new RgbwColor(255, 126, 15, 2),
                        < 0.78 => new RgbwColor(238, 58, 7, 0),
                        _ => new RgbwColor(148, 22, 4, 0)
                    };
                    colors[(y * wallWidth) + x] = Scale(color, bodyFade);

                    var width = ratio < 0.62 ? 1 : 0;
                    if (width > 0 && x + 1 < wallWidth)
                    {
                        colors[(y * wallWidth) + x + 1] = Scale(color, bodyFade * 0.72);
                    }

                    if (segment > 2 && segment % 3 == 0)
                    {
                        var barbX = x + (index % 2 == 0 ? -2 : 2);
                        var barbY = y + 1;
                        if (barbX >= 0 && barbX < wallWidth && barbY >= 0 && barbY < wallHeight)
                        {
                            colors[(barbY * wallWidth) + barbX] = Scale(new RgbwColor(255, 82, 8, 0), bodyFade * 0.58);
                        }
                    }
                }
            }
        }

        private static void ApplySparkles(
            IDictionary<int, RgbwColor> colors,
            TimelineClip clip,
            double localTime,
            int totalPixels,
            int wallWidth,
            int wallHeight)
        {
            var targets = SelectParticleTargets(clip, totalPixels, wallWidth, wallHeight);
            var movementOffset = ResolveMovementOffset(clip, localTime);
            var movementIntensity = MovementIntensity(clip, localTime);
            var rotationCenter = GetRotationCenter(clip, targets, wallWidth);
            var seed = (int)Math.Round(clip.StartTime * 1000) ^ 0x41C64E6D;
            var speed = Math.Max(0.1, clip.Speed);

            for (var index = 0; index < targets.Count; index++)
            {
                var targetEntityId = ApplyTransform(
                    targets[index], clip, rotationCenter,
                    movementOffset.OffsetX, movementOffset.OffsetY,
                    wallWidth, wallHeight);
                if (targetEntityId is null) continue;

                var phase = Random01(seed, index, 1) * Math.PI * 2;
                var rate = 0.62 + (Random01(seed, index, 2) * 0.92);
                var wave = (Math.Sin((localTime * speed * rate * Math.PI * 2) + phase) + 1) * 0.5;
                var smoothWave = wave * wave * (3 - (2 * wave));
                var level = 0.62 + (0.38 * smoothWave);
                colors[targetEntityId.Value] = Scale(clip.Color, level * clip.Intensity * movementIntensity);
            }
        }

        private static List<int> SelectParticleTargets(TimelineClip clip, int totalPixels, int wallWidth, int wallHeight)
        {
            List<int> source;
            if (clip.ParticlePlacement == ParticlePlacement.Selection)
            {
                source = clip.Target.EntityIds
                    .Where(id => id >= 0 && id < totalPixels)
                    .Distinct()
                    .ToList();
            }
            else
            {
                var centerX = clip.RippleCenterX ?? ((wallWidth - 1) / 2.0);
                var centerY = clip.RippleCenterY ?? ((wallHeight - 1) / 2.0);
                var radiusX = Math.Max(2, wallWidth * 0.25 * Math.Clamp(clip.VisualScale, 0.35, 4));
                var radiusY = Math.Max(2, wallHeight * 0.25 * Math.Clamp(clip.VisualScale, 0.35, 4));
                var edgeSize = Math.Max(2, Math.Min(wallWidth, wallHeight) / 8);
                source = new List<int>(totalPixels);
                for (var entityId = 0; entityId < totalPixels; entityId++)
                {
                    var x = entityId % wallWidth;
                    var y = entityId / wallWidth;
                    var included = clip.ParticlePlacement switch
                    {
                        ParticlePlacement.AroundCenter =>
                            Math.Pow((x - centerX) / radiusX, 2) + Math.Pow((y - centerY) / radiusY, 2) <= 1,
                        ParticlePlacement.Left => x < wallWidth / 3,
                        ParticlePlacement.Right => x >= (wallWidth * 2) / 3,
                        ParticlePlacement.Top => y < wallHeight / 3,
                        ParticlePlacement.Bottom => y >= (wallHeight * 2) / 3,
                        ParticlePlacement.Edges => x < edgeSize || x >= wallWidth - edgeSize || y < edgeSize || y >= wallHeight - edgeSize,
                        _ => true
                    };
                    if (included) source.Add(entityId);
                }
            }
            if (source.Count == 0) return new List<int>();

            var count = Math.Clamp(clip.ParticleCount, 1, source.Count);
            var seed = (int)Math.Round(clip.StartTime * 1000) ^ 0x2C9277B5;
            var random = new Random(seed);
            var result = new List<int>(count);
            for (var index = 0; index < count; index++)
            {
                var randomIndex = random.Next(index, source.Count);
                (source[index], source[randomIndex]) = (source[randomIndex], source[index]);
                result.Add(source[index]);
            }
            return result;
        }

        private static double Random01(int seed, int particle, int channel)
        {
            unchecked
            {
                uint value = (uint)(seed + (particle * 374761393) + (channel * 668265263));
                value = (value ^ (value >> 13)) * 1274126177u;
                value ^= value >> 16;
                return value / (double)uint.MaxValue;
            }
        }

        private static void ApplyWhiteFallingLines(
            IDictionary<int, RgbwColor> colors,
            TimelineClip clip,
            double localTime,
            int wallWidth,
            int wallHeight)
        {
            var progress = Math.Clamp(localTime / Math.Max(0.001, clip.Duration), 0, 1);
            var seed = (int)Math.Round(clip.StartTime * 1000) ^ 0x2374A91;
            const int lineCount = 27;

            for (var index = 0; index < lineCount; index++)
            {
                var x = (int)Math.Round(Random01(seed, index, 1) * (wallWidth - 1));
                var phase = Random01(seed, index, 2);
                var speed = 0.82 + (Random01(seed, index, 3) * 0.58);
                var position = (phase + (progress * speed)) % 1.18;
                var headY = (int)Math.Round((position * (wallHeight + 26)) - 8);
                var length = 5 + (int)Math.Round(Random01(seed, index, 4) * 5);
                var brightness = (0.58 + (Random01(seed, index, 5) * 0.42)) * clip.Intensity;
                var edgeFade = Math.Clamp(Math.Min(position / 0.07, (1.18 - position) / 0.08), 0, 1);

                for (var segment = 0; segment < length; segment++)
                {
                    var y = headY - segment;
                    if (y < 0 || y >= wallHeight)
                    {
                        continue;
                    }

                    var level = brightness * edgeFade;
                    var barColor = Scale(clip.Color, level);
                    colors[(y * wallWidth) + x] = barColor;

                    var haloColor = Scale(clip.Color, level * 0.36);
                    if (x - 1 >= 0)
                    {
                        colors[(y * wallWidth) + x - 1] = haloColor;
                    }
                    if (x + 1 < wallWidth)
                    {
                        colors[(y * wallWidth) + x + 1] = haloColor;
                    }

                    var outerGlow = Scale(clip.Color, level * 0.12);
                    if (x - 2 >= 0)
                    {
                        colors[(y * wallWidth) + x - 2] = outerGlow;
                    }
                    if (x + 2 < wallWidth)
                    {
                        colors[(y * wallWidth) + x + 2] = outerGlow;
                    }
                }

                var capGlow = Scale(clip.Color, brightness * edgeFade * 0.22);
                var topGlowY = headY - length;
                if (topGlowY >= 0 && topGlowY < wallHeight)
                {
                    colors[(topGlowY * wallWidth) + x] = capGlow;
                }
                var bottomGlowY = headY + 1;
                if (bottomGlowY >= 0 && bottomGlowY < wallHeight)
                {
                    colors[(bottomGlowY * wallWidth) + x] = capGlow;
                }
            }
        }

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
                EffectType.ClickRipple => Scale(clip.Color, ClickRippleLevel(localTime, clip.Duration, entityId, wallWidth, wallHeight, clip.RippleCenterX, clip.RippleCenterY) * intensity),
                EffectType.HeartbeatTrace => Scale(clip.Color, HeartbeatTraceLevel(localTime, entityId, wallWidth, clip.Speed) * intensity),
                EffectType.ContractExplodeRipple => Scale(clip.Color, ContractExplodeRippleLevel(localTime, clip.Duration, entityId, wallWidth, wallHeight) * intensity),
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

    private static double ClickRippleLevel(
            double localTime,
            double duration,
            int entityId,
            int wallWidth,
            int wallHeight,
            double? requestedCenterX,
            double? requestedCenterY)
        {
            if (duration <= 0)
            {
                return 0;
            }

            var progress = Math.Clamp(localTime / duration, 0, 1);
            var centerX = Math.Clamp(requestedCenterX ?? ((wallWidth - 1) / 2.0), 0, Math.Max(0, wallWidth - 1));
            var centerY = Math.Clamp(requestedCenterY ?? ((wallHeight - 1) / 2.0), 0, Math.Max(0, wallHeight - 1));
            var x = entityId % Math.Max(1, wallWidth);
            var y = entityId / Math.Max(1, wallWidth);
            var distance = Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2));

            var farthestX = Math.Max(centerX, (wallWidth - 1) - centerX);
            var farthestY = Math.Max(centerY, (wallHeight - 1) - centerY);
            var maximumRadius = Math.Sqrt((farthestX * farthestX) + (farthestY * farthestY));
            var expansion = progress;
            var radius = 3 + ((maximumRadius + 4) * expansion);
            var thickness = 7.2 - (5.4 * progress);
            var edgeDistance = Math.Abs(distance - radius);
            var ring = 1 - Math.Clamp(edgeDistance / Math.Max(0.8, thickness), 0, 1);
            ring = ring * ring * (3 - (2 * ring));

            var fadeIn = 0.58 + (0.42 * Math.Clamp(progress / 0.055, 0, 1));
            var fade = fadeIn * Math.Pow(1 - progress, 0.72);
            return ring * fade;
        }

        private static double HeartbeatTraceLevel(double localTime, int entityId, int wallWidth, double speed)
        {
            var cycleDuration = 1.18 / Math.Max(0.1, speed);
            var progress = (localTime % cycleDuration) / cycleDuration;
            var normalizedX = (entityId % Math.Max(1, wallWidth)) / (double)Math.Max(1, wallWidth - 1);

            var writeHead = Math.Clamp(progress / 0.56, 0, 1);
            var eraseHead = progress < 0.48 ? 0 : Math.Clamp((progress - 0.48) / 0.52, 0, 1);
            const double feather = 0.025;
            var written = Math.Clamp((writeHead - normalizedX) / feather, 0, 1);
            var notErased = Math.Clamp((normalizedX - eraseHead) / feather, 0, 1);
            var body = written * notErased;

            var leadingGlow = Math.Max(0, 1 - (Math.Abs(normalizedX - writeHead) / 0.045));
            return Math.Clamp((body * 0.82) + (leadingGlow * 0.28), 0, 1);
        }

        private static double ContractExplodeRippleLevel(
            double localTime,
            double duration,
            int entityId,
            int wallWidth,
            int wallHeight)
        {
            var progress = Math.Clamp(localTime / Math.Max(0.001, duration), 0, 1);
            var x = entityId % Math.Max(1, wallWidth);
            var y = entityId / Math.Max(1, wallWidth);
            var centerX = (wallWidth - 1) / 2.0;
            var centerY = (wallHeight - 1) / 2.0;
            var distance = Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2));
            var maximumRadius = Math.Sqrt((centerX * centerX) + (centerY * centerY));

            double radius;
            double thickness;
            double intensity;
            if (progress < 0.83)
            {
                var contraction = progress / 0.83;
                var eased = contraction * contraction * (3 - (2 * contraction));
                radius = maximumRadius * (0.78 - (0.70 * eased));
                thickness = 4.8 + (2.2 * contraction);
                intensity = 0.58 + (0.34 * contraction);
            }
            else
            {
                var explosion = (progress - 0.83) / 0.17;
                var expansion = 1 - Math.Pow(1 - explosion, 2.6);
                radius = 4 + ((maximumRadius + 8) * expansion);
                thickness = 10.5 - (6.5 * explosion);
                intensity = Math.Pow(1 - explosion, 0.38);
            }

            var edgeDistance = Math.Abs(distance - radius);
            var ring = 1 - Math.Clamp(edgeDistance / Math.Max(1, thickness), 0, 1);
            ring = ring * ring * (3 - (2 * ring));
            var innerGlow = Math.Max(0, 1 - (distance / Math.Max(1, radius))) * (progress < 0.83 ? 0.18 : 0.42);
            return Math.Clamp((ring * intensity) + innerGlow, 0, 1);
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
