using System.ComponentModel;

namespace Liekinheitin.CreativeTool.ViewModels
{
    public sealed class FixtureInspectorViewModel : INotifyPropertyChanged
    {
        private ProjectorViewModel? _projector;
        private MovingHeadViewModel? _head;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsVisible => _projector is not null || _head is not null;
        public bool IsProjector => _projector is not null;
        public string Title => _projector is not null ? "Projecteur" : _head is not null ? $"Lyre {_head.Index}" : "";
        public ProjectorViewModel? ProjectorRef { get; private set; }
        public MovingHeadViewModel? HeadRef { get; private set; }

        public byte R
        {
            get => _projector?.R ?? _head?.R ?? 0;
            set { if (_projector is not null) _projector.R = value; if (_head is not null) _head.R = value; Raise(nameof(R)); }
        }
        public byte G
        {
            get => _projector?.G ?? _head?.G ?? 0;
            set { if (_projector is not null) _projector.G = value; if (_head is not null) _head.G = value; Raise(nameof(G)); }
        }
        public byte B
        {
            get => _projector?.B ?? _head?.B ?? 0;
            set { if (_projector is not null) _projector.B = value; if (_head is not null) _head.B = value; Raise(nameof(B)); }
        }
        public byte W
        {
            get => _projector?.W ?? _head?.W ?? 0;
            set { if (_projector is not null) _projector.W = value; if (_head is not null) _head.W = value; Raise(nameof(W)); }
        }

        public byte Pan { get => _head?.Pan ?? 0; set { if (_head is not null) { _head.Pan = value; Raise(nameof(Pan)); } } }
        public byte Tilt { get => _head?.Tilt ?? 0; set { if (_head is not null) { _head.Tilt = value; Raise(nameof(Tilt)); } } }
        public byte Speed { get => _head?.Speed ?? 0; set { if (_head is not null) { _head.Speed = value; Raise(nameof(Speed)); } } }
        public byte Dimming { get => _head?.Dimming ?? 0; set { if (_head is not null) { _head.Dimming = value; Raise(nameof(Dimming)); } } }
        public byte Strobe { get => _head?.Strobe ?? 0; set { if (_head is not null) { _head.Strobe = value; Raise(nameof(Strobe)); } } }

        /// <summary>Bascule : si le projecteur est déjà affiché, referme le panneau. Sinon l'affiche.</summary>
        public void ToggleProjector(ProjectorViewModel projector)
        {
            if (_projector is not null) Hide();
            else { _projector = projector; _head = null; ProjectorRef = projector; HeadRef = null; RaiseAll(); }
        }

        public void ToggleHead(MovingHeadViewModel head)
        {
            if (_head is not null && _head == head) Hide();
            else { _head = head; _projector = null; HeadRef = head; ProjectorRef = null; RaiseAll(); }
        }

        public void Hide()
        {
            _projector = null; _head = null;
            ProjectorRef = null; HeadRef = null;
            RaiseAll();
        }

        private void RaiseAll()
        {
            foreach (var name in new[] { nameof(IsVisible), nameof(IsProjector), nameof(Title),
                nameof(R), nameof(G), nameof(B), nameof(W), nameof(Pan), nameof(Tilt), nameof(Speed), nameof(Dimming), nameof(Strobe) })
                Raise(name);
        }

        private void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}