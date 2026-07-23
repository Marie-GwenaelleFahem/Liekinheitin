using System;
using System.Windows.Media;

namespace Liekinheitin.CreativeTool.Services
{
    public sealed class AudioPlaybackService : IDisposable
    {
        private readonly MediaPlayer _player = new();
        private string? _filePath;
        private double _volume = 1.0;

        public AudioPlaybackService()
        {
            _player.MediaOpened += (_, _) => MediaOpened?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? MediaOpened;

        public string? FilePath => _filePath;

        public bool HasAudio => !string.IsNullOrWhiteSpace(_filePath);

        public double? DurationSeconds => _player.NaturalDuration.HasTimeSpan
            ? _player.NaturalDuration.TimeSpan.TotalSeconds
            : null;

        public double Volume
        {
            get => _volume;
            set
            {
                _volume = Math.Clamp(value, 0, 1);
                _player.Volume = _volume;
            }
        }

        public void Load(string filePath)
        {
            _filePath = filePath;
            _player.Open(new Uri(filePath, UriKind.Absolute));
            _player.Volume = _volume;
        }

        public void Clear()
        {
            _player.Stop();
            _player.Close();
            _filePath = null;
        }

        public void Play(double positionSeconds)
        {
            if (!HasAudio)
            {
                return;
            }

            Seek(positionSeconds);
            _player.Play();
        }

        public void Pause()
        {
            if (HasAudio)
            {
                _player.Pause();
            }
        }

        public void Stop()
        {
            if (!HasAudio)
            {
                return;
            }

            _player.Stop();
            _player.Position = TimeSpan.Zero;
        }

        public void Seek(double positionSeconds)
        {
            if (HasAudio)
            {
                _player.Position = TimeSpan.FromSeconds(Math.Max(0, positionSeconds));
            }
        }

        public void Dispose()
        {
            _player.Close();
        }
    }
}
