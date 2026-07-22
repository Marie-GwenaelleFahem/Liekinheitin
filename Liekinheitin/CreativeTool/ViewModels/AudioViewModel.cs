using System.ComponentModel;
using Liekinheitin.CreativeTool.Services;

namespace Liekinheitin.CreativeTool.ViewModels
{
    public sealed class AudioViewModel : INotifyPropertyChanged
    {
        private readonly AudioPlaybackService _audio;

        public event PropertyChangedEventHandler? PropertyChanged;

        public AudioViewModel(AudioPlaybackService audio)
        {
            _audio = audio;
        }

        public string FileLabel => _audio.LoadedFileName ?? "Aucun fichier chargé";
        public bool IsPlaying => _audio.IsPlaying;

        public void Load(string path)
        {
            _audio.Load(path);
            Raise(nameof(FileLabel));
            Raise(nameof(IsPlaying));
        }

        public void Play()
        {
            _audio.Play();
            Raise(nameof(IsPlaying));
        }

        public void Pause()
        {
            _audio.Pause();
            Raise(nameof(IsPlaying));
        }

        public void Stop()
        {
            _audio.Stop();
            Raise(nameof(IsPlaying));
        }

        private void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}