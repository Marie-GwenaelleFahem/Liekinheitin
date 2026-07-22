using System;
using System.ComponentModel;
using Liekinheitin.CreativeTool.Domain;

namespace Liekinheitin.CreativeTool.ViewModels
{
    public sealed class TimelineViewModel : INotifyPropertyChanged
    {
        private readonly Timeline _timeline;
        private readonly TimelinePlayer _player;
        private readonly SceneManager _scene;
        private readonly AudioViewModel _audio;
        private double _totalDuration = 10.0;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action? KeyframeAdded;

        public Timeline Timeline => _timeline;
        public TimelinePlayer Player => _player;

        public TimelineViewModel(Timeline timeline, TimelinePlayer player, SceneManager scene, AudioViewModel audio)
        {
            _timeline = timeline;
            _player = player;
            _scene = scene;
            _audio = audio;
            _player.TimeChanged += () =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTime)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPlaying)));
            };
        }

        public double Duration => Math.Max(_totalDuration, _player.DurationSeconds);

        public double TotalDuration
        {
            get => _totalDuration;
            set
            {
                _totalDuration = Math.Max(0.1, value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalDuration)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Duration)));
            }
        }

        public bool IsPlaying => _player.IsPlaying;

        public double CurrentTime
        {
            get => _player.CurrentTimeSeconds;
            set => _player.SeekTo(Math.Min(value, Duration));
        }

        /// <summary>Un seul bouton : anime ET musique démarrent/pausent ensemble.
        /// Sans synchronisation temporelle — juste un déclenchement simultané.</summary>
        public void TogglePlayPause()
        {
            if (_player.IsPlaying)
            {
                _player.Pause();
                _audio.Pause();
            }
            else
            {
                _player.Play();
                _audio.Play();
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPlaying)));
        }

        public void Stop()
        {
            _player.Stop();
            _audio.Stop();
        }

        public void AddKeyframe(PlacedShape shape)
        {
            var track = _timeline.GetOrCreateTrack(shape.Id, shape.Type);
            track.SetKeyframe(new ShapeKeyframe
            {
                TimeSeconds = CurrentTime,
                X = shape.X,
                Y = shape.Y,
                BaseWidth = shape.BaseWidth,
                BaseHeight = shape.BaseHeight,
                Scale = shape.Scale,
                Color = shape.Color,
            });
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Duration)));
            KeyframeAdded?.Invoke();
        }

        public void AddProjectorKeyframe(ProjectorViewModel projector)
        {
            var track = _timeline.GetOrCreateFixtureTrack(1);
            track.SetKeyframe(new FixtureKeyframe
            {
                TimeSeconds = CurrentTime,
                R = projector.R,
                G = projector.G,
                B = projector.B,
                W = projector.W,
            });
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Duration)));
            KeyframeAdded?.Invoke();
        }

        public void AddMovingHeadKeyframe(MovingHeadViewModel head)
        {
            var track = _timeline.GetOrCreateFixtureTrack(head.EntityId);
            track.SetKeyframe(new FixtureKeyframe
            {
                TimeSeconds = CurrentTime,
                Pan = head.Pan,
                Tilt = head.Tilt,
                Speed = head.Speed,
                Dimming = head.Dimming,
                Strobe = head.Strobe,
                R = head.R,
                G = head.G,
                B = head.B,
                W = head.W,
            });
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Duration)));
            KeyframeAdded?.Invoke();
        }
    }
}