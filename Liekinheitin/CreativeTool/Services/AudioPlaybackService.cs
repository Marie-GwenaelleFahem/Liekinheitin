using System;
using System.Windows.Media;

namespace Liekinheitin.CreativeTool.Services
{
    /// <summary>Lecture audio simple (charger/jouer/pause/stop), indépendante de la
    /// timeline d'animation — aucune synchronisation pour l'instant.</summary>
    public sealed class AudioPlaybackService : IDisposable
    {
        private readonly MediaPlayer _player = new();
        private bool _isLoaded;

        public bool IsPlaying { get; private set; }
        public string? LoadedFileName { get; private set; }

        public void Load(string path)
        {
            _player.Open(new Uri(path));
            LoadedFileName = System.IO.Path.GetFileName(path);
            _isLoaded = true;
            IsPlaying = false;
        }

        public void Play()
        {
            if (!_isLoaded) return;
            _player.Play();
            IsPlaying = true;
        }

        public void Pause()
        {
            if (!_isLoaded) return;
            _player.Pause();
            IsPlaying = false;
        }

        public void Stop()
        {
            if (!_isLoaded) return;
            _player.Stop();
            IsPlaying = false;
        }

        public void Dispose() => _player.Close();
    }
}