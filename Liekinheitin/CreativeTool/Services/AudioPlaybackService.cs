using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Liekinheitin.CreativeTool.Models;

namespace Liekinheitin.CreativeTool.Services
{
    public sealed class AudioPlaybackService : IDisposable
    {
        private readonly MediaPlayer _player = new();
        private readonly Dictionary<TimelineClip, MediaPlayer> _clipPlayers = new();
        private readonly HashSet<TimelineClip> _playingClips = new();
        private string? _filePath;
        private double _volume = 1.0;

        public AudioPlaybackService()
        {
            _player.MediaOpened += (_, _) => MediaOpened?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? MediaOpened;
        public event Action<TimelineClip, double>? ClipDurationResolved;

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
                foreach (var player in _clipPlayers.Values)
                {
                    player.Volume = _volume;
                }
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
            ClearClipPlayers();
        }

        public void Configure(IEnumerable<TimelineClip> clips)
        {
            ClearClipPlayers();
            foreach (var clip in clips.Where(item => item.IsAudio && !string.IsNullOrWhiteSpace(item.AudioFilePath)))
            {
                var player = new MediaPlayer { Volume = _volume };
                player.MediaOpened += (_, _) =>
                {
                    if (player.NaturalDuration.HasTimeSpan)
                    {
                        ClipDurationResolved?.Invoke(clip, player.NaturalDuration.TimeSpan.TotalSeconds);
                    }
                };
                player.Open(new Uri(clip.AudioFilePath!, UriKind.Absolute));
                _clipPlayers[clip] = player;
            }
        }

        public void Play(double positionSeconds)
        {
            Seek(positionSeconds);
            Update(positionSeconds);
        }

        public void Update(double positionSeconds)
        {
            foreach (var (clip, player) in _clipPlayers)
            {
                player.Volume = _volume * GetClipFadeLevel(clip, positionSeconds);
                if (positionSeconds >= clip.StartTime && positionSeconds < clip.EndTime && !clip.IsHidden)
                {
                    if (_playingClips.Add(clip))
                    {
                        player.Position = TimeSpan.FromSeconds(Math.Max(0, clip.AudioOffsetSeconds + positionSeconds - clip.StartTime));
                        player.Play();
                    }
                }
                else if (_playingClips.Remove(clip))
                    player.Pause();
            }
        }

        public void Pause()
        {
            foreach (var player in _clipPlayers.Values) player.Pause();
            _playingClips.Clear();
        }

        public void Stop()
        {
            _player.Stop();
            _player.Position = TimeSpan.Zero;
            foreach (var player in _clipPlayers.Values)
            {
                player.Stop();
                player.Position = TimeSpan.Zero;
            }
            _playingClips.Clear();
        }

        public void Seek(double positionSeconds)
        {
            foreach (var (clip, player) in _clipPlayers)
            {
                player.Position = TimeSpan.FromSeconds(Math.Max(0, clip.AudioOffsetSeconds + positionSeconds - clip.StartTime));
            }
        }

        public void Dispose()
        {
            _player.Close();
            ClearClipPlayers();
        }

        private void ClearClipPlayers()
        {
            foreach (var player in _clipPlayers.Values)
            {
                player.Stop();
                player.Close();
            }
            _clipPlayers.Clear();
            _playingClips.Clear();
        }

        private static double GetClipFadeLevel(TimelineClip clip, double positionSeconds)
        {
            var localTime = positionSeconds - clip.StartTime;
            if (localTime < 0 || localTime >= clip.Duration) return 0;

            var level = 1.0;
            if (clip.AudioFadeInDuration > 0)
            {
                var progress = Math.Clamp(localTime / clip.AudioFadeInDuration, 0, 1);
                level *= Math.Sin(progress * Math.PI / 2);
            }
            if (clip.AudioFadeOutDuration > 0)
            {
                var remaining = clip.Duration - localTime;
                var progress = Math.Clamp(remaining / clip.AudioFadeOutDuration, 0, 1);
                level *= Math.Sin(progress * Math.PI / 2);
            }
            return level;
        }
    }
}
