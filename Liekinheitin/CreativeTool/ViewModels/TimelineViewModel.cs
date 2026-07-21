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
        private double _totalDuration = 10.0; // durée totale de travail, extensible par l'utilisateur

        public event PropertyChangedEventHandler? PropertyChanged;

        public TimelineViewModel(Timeline timeline, TimelinePlayer player, SceneManager scene)
        {
            _timeline = timeline;
            _player = player;
            _scene = scene;
            _player.TimeChanged += () =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTime)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPlaying)));
            };
        }

        /// <summary>
        /// Plage du curseur : le plus grand entre la durée totale de travail (éditable) et
        /// la dernière keyframe posée (au cas où l'utilisateur pose une keyframe au-delà).
        /// </summary>
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

        public void TogglePlayPause()
        {
            if (_player.IsPlaying) _player.Pause();
            else _player.Play();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPlaying)));
        }

        public void Stop() => _player.Stop();

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
        }
    }
}