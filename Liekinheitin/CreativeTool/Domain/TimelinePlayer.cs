using System;
using System.Windows.Threading;

namespace Liekinheitin.CreativeTool.Domain
{
    public sealed class TimelinePlayer : IDisposable
    {
        private readonly Timeline _timeline;
        private readonly SceneManager _scene;
        private readonly DispatcherTimer _timer;
        private const int TickIntervalMs = 25; // ~40Hz
        private const int SliderNotifyEveryNTicks = 3; // ~13Hz, slider uniquement

        private int _tickCount;

        public bool IsPlaying { get; private set; }
        public double CurrentTimeSeconds { get; private set; }
        public double DurationSeconds => _timeline.DurationSeconds;

        /// <summary>Déclenché à CHAQUE tick (~40Hz) — pour rafraîchir la grille à l'écran,
        /// au même rythme que le déplacement à la souris.</summary>
        public event Action? Ticked;

        /// <summary>Déclenché à fréquence réduite (~13Hz) et à chaque Seek — pour le slider
        /// et le texte du temps écoulé, qui n'ont pas besoin de plus.</summary>
        public event Action? TimeChanged;

        public TimelinePlayer(Timeline timeline, SceneManager scene)
        {
            _timeline = timeline;
            _scene = scene;
            _timer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(TickIntervalMs)
            };
            _timer.Tick += (_, _) => Tick();
        }

        public void Play()
        {
            if (DurationSeconds <= 0) return;
            IsPlaying = true;
            _timer.Start();
        }

        public void Pause()
        {
            IsPlaying = false;
            _timer.Stop();
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

        private void Tick()
        {
            CurrentTimeSeconds += TickIntervalMs / 1000.0;
            if (CurrentTimeSeconds >= DurationSeconds)
            {
                CurrentTimeSeconds = DurationSeconds;
                Pause();
            }

            ApplyCurrentTime();
            Ticked?.Invoke(); // grille : à chaque tick, pleine fréquence

            _tickCount++;
            if (_tickCount % SliderNotifyEveryNTicks == 0 || !IsPlaying)
                TimeChanged?.Invoke(); // slider : fréquence réduite
        }

        private void ApplyCurrentTime()
        {
            foreach (var track in _timeline.Tracks)
            {
                var kf = track.Evaluate(CurrentTimeSeconds);
                if (kf is null) continue;

                _scene.ApplyShapeState(track.ShapeId, kf.X, kf.Y, kf.BaseWidth, kf.BaseHeight, kf.Scale, kf.Color);
            }
        }

        public void Dispose() => _timer.Stop();
    }
}