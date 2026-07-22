using System.Collections.ObjectModel;
using System.ComponentModel;
using Liekinheitin.CreativeTool.Domain;

namespace Liekinheitin.CreativeTool.ViewModels
{
    public sealed class MovingHeadViewModel : INotifyPropertyChanged
    {
        private readonly MovingHead _head;
        public event PropertyChangedEventHandler? PropertyChanged;

        public MovingHeadViewModel(MovingHead head) => _head = head;

        public int Index => _head.Index;
        public int EntityId => _head.EntityId;

        public byte Pan { get => _head.Pan; set { _head.Pan = value; Raise(nameof(Pan)); } }
        public byte Tilt { get => _head.Tilt; set { _head.Tilt = value; Raise(nameof(Tilt)); } }
        public byte Speed { get => _head.Speed; set { _head.Speed = value; Raise(nameof(Speed)); } }
        public byte Dimming { get => _head.Dimming; set { _head.Dimming = value; Raise(nameof(Dimming)); } }
        public byte Strobe { get => _head.Strobe; set { _head.Strobe = value; Raise(nameof(Strobe)); } }
        public byte R { get => _head.R; set { _head.R = value; Raise(nameof(R)); } }
        public byte G { get => _head.G; set { _head.G = value; Raise(nameof(G)); } }
        public byte B { get => _head.B; set { _head.B = value; Raise(nameof(B)); } }
        public byte W { get => _head.W; set { _head.W = value; Raise(nameof(W)); } }

        private void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public sealed class ProjectorViewModel : INotifyPropertyChanged
    {
        private readonly StaticProjector _projector;
        public event PropertyChangedEventHandler? PropertyChanged;

        public ProjectorViewModel(StaticProjector projector) => _projector = projector;

        public byte R { get => _projector.R; set { _projector.R = value; Raise(nameof(R)); } }
        public byte G { get => _projector.G; set { _projector.G = value; Raise(nameof(G)); } }
        public byte B { get => _projector.B; set { _projector.B = value; Raise(nameof(B)); } }
        public byte W { get => _projector.W; set { _projector.W = value; Raise(nameof(W)); } }

        private void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public sealed class FixtureControlViewModel
    {
        public ProjectorViewModel Projector { get; }
        public ObservableCollection<MovingHeadViewModel> MovingHeads { get; } = new();
        public FixtureInspectorViewModel Inspector { get; } = new();

        public FixtureControlViewModel(FixtureManager fixtures)
        {
            Projector = new ProjectorViewModel(fixtures.Projector);
            foreach (var head in fixtures.MovingHeads)
                MovingHeads.Add(new MovingHeadViewModel(head));
        }

        public void ToggleProjector() => Inspector.ToggleProjector(Projector);
        public void ToggleHead(MovingHeadViewModel head) => Inspector.ToggleHead(head);
    }
}