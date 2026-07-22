using System;
using System.Diagnostics;
using Liekinheitin.CreativeTool.Models;

namespace Liekinheitin.CreativeTool.Services
{
    public class PlaybackController
    {
        private readonly Stopwatch _stopwatch = new();
        private double _positionAtStart;
        private double _currentTime;
        private double _duration = 30.0;

        public event EventHandler? StateChanged;

        public double CurrentTime
        {
            get
            {
                if (Status != PlaybackStatus.Playing)
                {
                    return _currentTime;
                }

                var current = _positionAtStart + _stopwatch.Elapsed.TotalSeconds;
                if (current >= Duration)
                {
                    Stop();
                    return _currentTime;
                }

                return current;
            }
        }

        public double Duration
        {
            get => _duration;
            set
            {
                _duration = Math.Max(0, value);
                Seek(CurrentTime);
                OnStateChanged();
            }
        }

        public PlaybackStatus Status { get; private set; } = PlaybackStatus.Stopped;

        public void Play()
        {
            if (Duration <= 0)
            {
                return;
            }

            if (Status == PlaybackStatus.Playing)
            {
                return;
            }

            _currentTime = Math.Min(_currentTime, Duration);
            if (_currentTime >= Duration)
            {
                _currentTime = 0;
            }

            _positionAtStart = _currentTime;
            _stopwatch.Restart();
            Status = PlaybackStatus.Playing;
            OnStateChanged();
        }

        public void Pause()
        {
            if (Status != PlaybackStatus.Playing)
            {
                return;
            }

            _currentTime = CurrentTime;
            _stopwatch.Reset();
            Status = PlaybackStatus.Paused;
            OnStateChanged();
        }

        public void Stop()
        {
            _stopwatch.Reset();
            _positionAtStart = 0;
            _currentTime = 0;
            Status = PlaybackStatus.Stopped;
            OnStateChanged();
        }

        public void Seek(double time)
        {
            _currentTime = Math.Clamp(time, 0, Duration);

            if (Status == PlaybackStatus.Playing)
            {
                _positionAtStart = _currentTime;
                _stopwatch.Restart();
            }

            OnStateChanged();
        }

        private void OnStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
