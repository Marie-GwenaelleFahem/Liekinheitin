using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Liekinheitin.CreativeTool.Domain;
using Liekinheitin.CreativeTool.ViewModels;

namespace Liekinheitin.CreativeTool.Views
{
    public partial class TimelineView : UserControl
    {
        private PlacedShape? _selectedShape;
        private TimelineViewModel? _vm;
        private Line? _playhead;
        private double _tracksWidth;

        private const double PixelsPerSecond = 60;
        private const double RowHeight = 28;
        private const double RulerHeight = 20;
        private bool _isScrubbing;

        public TimelineView()
        {
            InitializeComponent();
            DataContextChanged += (_, __) => AttachViewModel();
        }

        private void OnContentGridMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isScrubbing = true;
            ContentGrid.CaptureMouse();
            SeekFromScreenPoint(e.GetPosition(ContentGrid));
        }

        private void OnContentGridMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isScrubbing || e.LeftButton != MouseButtonState.Pressed) return;
            SeekFromScreenPoint(e.GetPosition(ContentGrid));
        }

        private void OnContentGridMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isScrubbing = false;
            ContentGrid.ReleaseMouseCapture();
        }

        private void SeekFromScreenPoint(Point p)
        {
            if (_vm is null) return;
            double x = p.X - 100; // 100 = largeur de la colonne labels
            _vm.CurrentTime = Math.Max(0, Math.Min(x / PixelsPerSecond, _vm.Duration));
        }

        private void AttachViewModel()
        {
            if (_vm is not null)
            {
                _vm.KeyframeAdded -= RefreshTracks;
                _vm.Player.Ticked -= OnTicked;
            }

            _vm = DataContext as TimelineViewModel;
            if (_vm is null) return;

            _vm.KeyframeAdded += RefreshTracks;
            _vm.Player.Ticked += OnTicked;

            RefreshTracks();
        }

        public void SetSelectedShape(PlacedShape? shape) => _selectedShape = shape;

        private void OnPlayPauseClick(object sender, RoutedEventArgs e) => _vm?.TogglePlayPause();
        private void OnStopClick(object sender, RoutedEventArgs e) => _vm?.Stop();

        private void OnAddKeyframeClick(object sender, RoutedEventArgs e)
        {
            if (_vm is not null && _selectedShape is not null)
                _vm.AddKeyframe(_selectedShape);
        }

        public event Action? FullscreenToggleRequested;
        private void OnFullscreenClick(object sender, RoutedEventArgs e) => FullscreenToggleRequested?.Invoke();
        /*
        private void OnContentGridMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_vm is null) return;
            double x = e.GetPosition(ContentGrid).X - 100; // 100 = largeur de la colonne labels
            if (x < 0) return;
            _vm.CurrentTime = Math.Max(0, x / PixelsPerSecond);
        }
        */
        private void OnTicked()
        {
            UpdatePlayhead();
            if (_vm is not null)
                TimeLabel.Text = $"{_vm.CurrentTime:0.00}s";
        }

        /// <summary>Reconstruit entièrement la grille : une ligne = un rang de Grid,
        /// partagé entre le label (colonne 0) et le contenu de la piste (colonne 1).
        /// Alignement garanti par construction, aucune coordonnée Y à synchroniser à la main.</summary>
        private void RefreshTracks()
        {
            if (_vm is null) return;

            ContentGrid.RowDefinitions.Clear();
            ContentGrid.Children.Clear();

            _tracksWidth = Math.Max(400, _vm.Duration * PixelsPerSecond + 40);

            // Ligne 0 : la règle temporelle.
            ContentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(RulerHeight) });
            var ruler = BuildRuler();
            Grid.SetRow(ruler, 0);
            Grid.SetColumn(ruler, 1);
            ContentGrid.Children.Add(ruler);

            int rowIndex = 1;

            foreach (var track in _vm.Timeline.Tracks)
            {
                string label = $"{track.Type} {track.ShapeId.ToString()[..4]}";
                var times = track.Keyframes.ConvertAll(k => k.TimeSeconds);
                AddRow(label, rowIndex, times);
                rowIndex++;
            }

            foreach (var track in _vm.Timeline.FixtureTracks)
            {
                string label = track.EntityId == 1 ? "Projecteur" : $"Lyre {track.EntityId - 1}";
                var times = track.Keyframes.ConvertAll(k => k.TimeSeconds);
                AddRow(label, rowIndex, times);
                rowIndex++;
            }

            // Tête de lecture : une ligne verticale par-dessus tout, sur toute la hauteur des
            // lignes déjà construites (règle + toutes les pistes). Y2 doit être une valeur
            // numérique concrète, calculée ici — WPF n'accepte pas NaN pour une Line.
            double totalHeight = RulerHeight + (rowIndex - 1) * RowHeight;

            _playhead = new Line
            {
                X1 = 0,
                X2 = 0,
                Y1 = 0,
                Y2 = totalHeight,
                Stroke = Brushes.OrangeRed,
                StrokeThickness = 2,
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            Grid.SetRow(_playhead, 0);
            Grid.SetRowSpan(_playhead, rowIndex);
            Grid.SetColumn(_playhead, 1);
            ContentGrid.Children.Add(_playhead);

            UpdatePlayhead();
        }

        private FrameworkElement BuildRuler()
        {
            var canvas = new Canvas { Width = _tracksWidth, Height = RulerHeight, ClipToBounds = true };

            for (int s = 0; s <= (int)Math.Ceiling(_vm!.Duration); s++)
            {
                var tick = new Line
                {
                    X1 = s * PixelsPerSecond,
                    X2 = s * PixelsPerSecond,
                    Y1 = RulerHeight - 6,
                    Y2 = RulerHeight,
                    Stroke = Brushes.Gray,
                    StrokeThickness = 1
                };
                canvas.Children.Add(tick);

                var label = new TextBlock { Text = $"{s}s", Foreground = Brushes.Gray, FontSize = 9 };
                Canvas.SetLeft(label, s * PixelsPerSecond + 2);
                canvas.Children.Add(label);
            }

            return canvas;
        }

        private void AddRow(string label, int rowIndex, List<double> keyframeTimes)
        {
            ContentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(RowHeight) });

            var labelText = new TextBlock
            {
                Text = label,
                Foreground = Brushes.White,
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 0, 0)
            };
            Grid.SetRow(labelText, rowIndex);
            Grid.SetColumn(labelText, 0);
            ContentGrid.Children.Add(labelText);

            var rowCanvas = new Canvas { Width = _tracksWidth, Height = RowHeight - 6, ClipToBounds = true };

            var bar = new Rectangle
            {
                Width = _tracksWidth,
                Height = RowHeight - 6,
                Fill = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255))
            };
            rowCanvas.Children.Add(bar);

            foreach (var t in keyframeTimes)
            {
                var diamond = new Polygon
                {
                    Points = new PointCollection { new(5, 0), new(10, 5), new(5, 10), new(0, 5) },
                    Fill = Brushes.OrangeRed
                };
                Canvas.SetLeft(diamond, t * PixelsPerSecond - 5);
                Canvas.SetTop(diamond, (RowHeight - 6 - 10) / 2);
                rowCanvas.Children.Add(diamond);
            }

            Grid.SetRow(rowCanvas, rowIndex);
            Grid.SetColumn(rowCanvas, 1);
            ContentGrid.Children.Add(rowCanvas);
        }

        private void UpdatePlayhead()
        {
            if (_playhead is null || _vm is null) return;
            double x = _vm.CurrentTime * PixelsPerSecond;
            _playhead.X1 = x;
            _playhead.X2 = x;
        }
    }
}