using Liekinheitin.CreativeTool.Domain;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;

namespace Liekinheitin.CreativeTool.ViewModels
{
    public sealed class ColorPickerViewModel : INotifyPropertyChanged
    {
        private readonly BrushTool _brush;
        private byte _r = 255, _g = 255, _b = 255;

        public ColorPickerViewModel(BrushTool brush)
        {
            _brush = brush;
        }

        public byte R { get => _r; set { _r = value; OnColorChanged(); } }
        public byte G { get => _g; set { _g = value; OnColorChanged(); } }
        public byte B { get => _b; set { _b = value; OnColorChanged(); } }

        public Color CurrentColor => Color.FromRgb(R, G, B);

        public ObservableCollection<Color> Palette { get; } = new(new[]
        {
            Colors.Red, Colors.Orange, Colors.Yellow, Colors.Green,
            Colors.Cyan, Colors.Blue, Colors.Purple, Colors.White, Colors.Black
        });

        public void SelectFromPalette(Color color)
        {
            _r = color.R; _g = color.G; _b = color.B;
            OnColorChanged();
        }

        private void OnColorChanged()
        {
            _brush.CurrentColor = CurrentColor;
            OnPropertyChanged(nameof(R));
            OnPropertyChanged(nameof(G));
            OnPropertyChanged(nameof(B));
            OnPropertyChanged(nameof(CurrentColor));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}