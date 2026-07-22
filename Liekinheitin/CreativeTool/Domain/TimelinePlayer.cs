using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media;

namespace Liekinheitin.CreativeTool.Domain
{
    public sealed class TimelinePlayer : IDisposable
    {
        private readonly Timeline _timeline;
        private readonly SceneManager _scene;
        private readonly FixtureManager _fixtures;
        private readonly Stopwatch _stopwatch = new();
        private readonly List<(Guid, int, int, int, int, double, Color)> _shapeUpdatesBuffer = new();
        private double _lastElapsedSeconds;
        private double _sliderAccumulator;
        private double _updateAccumulator;
        private const double SliderUpdateIntervalSeconds = 1.0 / 13.0;
        private const double TargetUpdateIntervalSeconds = 1.0 / 25.0; // 25Hz — plafond du calcul réel

        public bool IsPlaying { get; private set; }
        public double CurrentTimeSeconds { get; private set; }
        public double DurationSeconds => _timeline.DurationSeconds;

        public event Action? Ticked;
        public event Action? TimeChanged;

        public TimelinePlayer(Timeline timeline, SceneManager scene, FixtureManager fixtures)
        {
            _timeline = timeline;
            _scene = scene;
            _fixtures = fixtures;
        }

        public void Play()
        {
            if (DurationSeconds <= 0 || IsPlaying) return;
            IsPlaying = true;
            _lastElapsedSeconds = 0;
            _updateAccumulator = 0;
            _stopwatch.Restart();
            CompositionTarget.Rendering += OnRendering;
        }

        public void Pause()
        {
            if (!IsPlaying) return;
            IsPlaying = false;
            CompositionTarget.Rendering -= OnRendering;
            _stopwatch.Stop();
        }

        public void Stop()
        {
            Pause();
            SeekTo(0);
        }

        public void SeekTo(double timeSeconds)
        {
            CurrentTimeSeconds = Math.Max(0, timeSeconds);
            ApplyCurrentTime();
            Ticked?.Invoke();
            TimeChanged?.Invoke();
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            double nowElapsed = _stopwatch.Elapsed.TotalSeconds;
            double delta = nowElapsed - _lastElapsedSeconds;
            _lastElapsedSeconds = nowElapsed;

            _updateAccumulator += delta;
            if (_updateAccumulator < TargetUpdateIntervalSeconds)
                return; // l'écran rafraîchit plus vite que nécessaire ; on ignore ce passage

            double stepDelta = _updateAccumulator;
            _updateAccumulator = 0;

            CurrentTimeSeconds += stepDelta;
            bool finished = CurrentTimeSeconds >= DurationSeconds;
            if (finished) CurrentTimeSeconds = DurationSeconds;

            ApplyCurrentTime();
            Ticked?.Invoke();

            _sliderAccumulator += stepDelta;
            if (_sliderAccumulator >= SliderUpdateIntervalSeconds || finished)
            {
                _sliderAccumulator = 0;
                TimeChanged?.Invoke();
            }

            if (finished) Pause();
        }

        private void ApplyCurrentTime()
        {
            _shapeUpdatesBuffer.Clear();

            foreach (var track in _timeline.Tracks)
            {
                var kf = track.Evaluate(CurrentTimeSeconds);
                if (kf is null) continue;
                _shapeUpdatesBuffer.Add((track.ShapeId, kf.X, kf.Y, kf.BaseWidth, kf.BaseHeight, kf.Scale, kf.Color));
            }

            if (_shapeUpdatesBuffer.Count > 0)
                _scene.ApplyShapeStatesBatch(_shapeUpdatesBuffer);

            foreach (var track in _timeline.FixtureTracks)
            {
                var kf = track.Evaluate(CurrentTimeSeconds);
                if (kf is null) continue;
                _fixtures.ApplyKeyframe(track.EntityId, kf);
            }
        }

        public void Dispose()
        {
            CompositionTarget.Rendering -= OnRendering;
        }
    }
}