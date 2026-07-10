using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Liekinheitin.Application.Interfaces;
using Liekinheitin.CreativeTool.Models;
using Liekinheitin.CreativeTool.Services;
using Liekinheitin.CreativeTool.Shapes;
using Liekinheitin.Infrastructure.Network;
using Microsoft.Win32;

namespace Liekinheitin.CreativeTool.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string ProjectFilter = "Liekinheitin show (*.lshow)|*.lshow|JSON (*.json)|*.json|Tous les fichiers (*.*)|*.*";
        private const string RoutingHostIp = "127.0.0.1";
        private const int RoutingHostStatePort = 5000;

        private readonly PlaybackController _playbackController = new();
        private readonly ProjectFileService _projectFileService = new();
        private readonly ShowPlaybackEngine _playbackEngine = new();
        private readonly IStatePublisher _statePublisher;
        private readonly DispatcherTimer _playbackTimer;

        private ShowProject _project;
        private TimelineClip? _selectedClip;
        private IReadOnlyList<IShape> _availableShapes = Array.Empty<IShape>();
        private string? _projectPath;
        private bool _isDirty;
        private bool _isUpdatingPlaybackUi;
        private bool _isUpdatingPropertyUi;

        public MainWindow()
        {
            InitializeComponent();

            _statePublisher = new UdpStatePublisher(RoutingHostIp, RoutingHostStatePort);
            _project = CreateDefaultProject();

            _playbackTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(25)
            };
            _playbackTimer.Tick += OnPlaybackTick;

            PlayButton.Click += OnPlayClick;
            PauseButton.Click += OnPauseClick;
            StopButton.Click += OnStopClick;
            TimeSlider.ValueChanged += OnTimeSliderValueChanged;
            TimelineControl.ClipSelected += OnTimelineClipSelected;
            ApplyPropertiesButton.Click += OnApplyPropertiesClick;
            AddShapeLayerButton.Click += OnAddShapeLayerClick;
            LedPreview.PixelDragDelta += OnPreviewPixelDragDelta;
            MovementEffectComboBox.SelectionChanged += OnMovementEffectSelectionChanged;
            ColorPickerControl.ColorChanged += OnColorPickerColorChanged;
            _playbackController.StateChanged += (_, _) => UpdatePlaybackUi();

            LoadProjectIntoUi(_project, null, markDirty: false);
            _playbackTimer.Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_statePublisher is IDisposable disposable)
            {
                disposable.Dispose();
            }

            base.OnClosed(e);
        }

        private void OnPlaybackTick(object? sender, EventArgs e)
        {
            var currentTime = _playbackController.CurrentTime;
            TimelineControl.SetPlayhead(currentTime);
            RenderPreview(currentTime);

            if (_playbackController.Status == PlaybackStatus.Playing)
            {
                PublishCurrentState(currentTime);
            }

            UpdatePlaybackUi();
        }

        private void OnTimeSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingPlaybackUi)
            {
                return;
            }

            _playbackController.Seek(e.NewValue);
            TimelineControl.SetPlayhead(_playbackController.CurrentTime);
            RenderPreview(_playbackController.CurrentTime);
        }

        private void OnPlayClick(object sender, RoutedEventArgs e) => _playbackController.Play();

        private void OnPauseClick(object sender, RoutedEventArgs e) => _playbackController.Pause();

        private void OnStopClick(object sender, RoutedEventArgs e) => _playbackController.Stop();

        private void OnTimelineClipSelected(object? sender, TimelineClip clip)
        {
            _selectedClip = clip;
            UpdatePropertyPanel(clip);
        }

        private void OnColorPickerColorChanged(object? sender, RgbwColor color)
        {
            if (_isUpdatingPropertyUi)
            {
                return;
            }

            RedTextBox.Text = color.R.ToString(CultureInfo.InvariantCulture);
            GreenTextBox.Text = color.G.ToString(CultureInfo.InvariantCulture);
            BlueTextBox.Text = color.B.ToString(CultureInfo.InvariantCulture);
            WhiteTextBox.Text = color.W.ToString(CultureInfo.InvariantCulture);
        }

        private void OnApplyPropertiesClick(object sender, RoutedEventArgs e)
        {
            if (_selectedClip is null || _isUpdatingPropertyUi)
            {
                return;
            }

            _selectedClip.Name = ClipNameTextBox.Text.Trim();
            _selectedClip.StartTime = ReadDouble(ClipStartTextBox, _selectedClip.StartTime);
            _selectedClip.Duration = Math.Max(0.001, ReadDouble(ClipDurationTextBox, _selectedClip.Duration));
            _selectedClip.EffectType = ReadComboTag(EffectTypeComboBox, _selectedClip.EffectType);
            _selectedClip.Target.Type = ReadComboTag(TargetTypeComboBox, _selectedClip.Target.Type);
            _selectedClip.Color = new RgbwColor(
                ReadByte(RedTextBox, _selectedClip.Color.R),
                ReadByte(GreenTextBox, _selectedClip.Color.G),
                ReadByte(BlueTextBox, _selectedClip.Color.B),
                ReadByte(WhiteTextBox, _selectedClip.Color.W));
            _selectedClip.Intensity = Math.Clamp(IntensitySlider.Value, 0, 1);
            _selectedClip.Speed = Math.Clamp(SpeedSlider.Value, 0, 4);
            _selectedClip.MovementEffect = ReadComboTag(MovementEffectComboBox, _selectedClip.MovementEffect);

            MarkDirty();
            TimelineControl.Redraw();
            RenderPreview(_playbackController.CurrentTime);
        }

        private void OnShapeButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: IShape shape } || _selectedClip is null)
            {
                return;
            }

            var entityIds = shape.GetEntityIds()
                .Where(id => id >= 0 && id < _project.WallWidth * _project.WallHeight)
                .Distinct()
                .ToList();

            _selectedClip.Target.Type = TargetType.Selection;
            _selectedClip.Target.EntityIds = entityIds;
            _selectedClip.MovementOffsetX = 0;
            _selectedClip.MovementOffsetY = 0;
            _selectedClip.MovementEffect = ReadComboTag(MovementEffectComboBox, _selectedClip.MovementEffect);
            SelectComboByTag(TargetTypeComboBox, TargetType.Selection.ToString());
            UpdateMovementOffsetText(_selectedClip);

            if (_playbackController.CurrentTime < _selectedClip.StartTime || _playbackController.CurrentTime > _selectedClip.EndTime)
            {
                _playbackController.Seek(_selectedClip.StartTime);
                TimelineControl.SetPlayhead(_playbackController.CurrentTime);
            }

            MarkDirty();
            RenderPreview(_playbackController.CurrentTime);
        }

        private void OnAddShapeLayerClick(object sender, RoutedEventArgs e)
        {
            var shapeLayerCount = _project.Tracks.Count(track => track.Name.StartsWith("Forme", StringComparison.OrdinalIgnoreCase));
            var trackName = shapeLayerCount == 0 ? "Forme" : $"Forme {shapeLayerCount + 1}";
            var clip = new TimelineClip
            {
                Name = trackName,
                StartTime = Math.Min(_playbackController.CurrentTime, Math.Max(0, _project.Duration - 1)),
                Duration = 1,
                EffectType = EffectType.SolidColor,
                Target = new TargetSelection { Type = TargetType.Selection },
                MovementEffect = ReadComboTag(MovementEffectComboBox, MovementEffectType.None),
                Color = RgbwColor.White,
                Intensity = 1
            };
            var track = new Track
            {
                Name = trackName,
                Clips = { clip }
            };

            _project.Tracks.Add(track);
            _selectedClip = clip;

            TimelineControl.SetProject(_project);
            TimelineControl.SelectClip(clip);
            UpdatePropertyPanel(clip);
            MarkDirty();
            RenderPreview(_playbackController.CurrentTime);
        }

        private void OnPreviewPixelDragDelta(object? sender, PixelDragDeltaEventArgs e)
        {
            if (_selectedClip?.Target.Type != TargetType.Selection || _selectedClip.Target.EntityIds.Count == 0)
            {
                return;
            }

            if (IsEditingMovementTarget())
            {
                MoveMovementTarget(_selectedClip, e.DeltaX, e.DeltaY);
                MarkDirty();
                SeekToMovementPreviewTime(_selectedClip);
                UpdateMovementOffsetText(_selectedClip);
                RenderPreview(_playbackController.CurrentTime);
                return;
            }

            var movedEntityIds = MoveSelection(_selectedClip.Target.EntityIds, e.DeltaX, e.DeltaY);
            if (movedEntityIds is null)
            {
                return;
            }

            _selectedClip.Target.EntityIds = movedEntityIds;
            ClampMovementTarget(_selectedClip);
            UpdateMovementOffsetText(_selectedClip);
            MarkDirty();
            RenderPreview(_playbackController.CurrentTime);
        }

        private void OnMovementEffectSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingPropertyUi || _selectedClip is null)
            {
                return;
            }

            _selectedClip.MovementEffect = ReadComboTag(MovementEffectComboBox, _selectedClip.MovementEffect);
            MarkDirty();
            RenderPreview(_playbackController.CurrentTime);
        }

        private void OnNewProjectClick(object sender, RoutedEventArgs e)
        {
            _playbackController.Stop();
            LoadProjectIntoUi(CreateDefaultProject(), null, markDirty: false);
        }

        private void OnOpenProjectClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = ProjectFilter,
                DefaultExt = ".lshow"
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            try
            {
                var project = _projectFileService.Load(dialog.FileName);
                _playbackController.Stop();
                LoadProjectIntoUi(project, dialog.FileName, markDirty: false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Ouverture du projet", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnSaveProjectClick(object sender, RoutedEventArgs e)
        {
            if (_projectPath is null)
            {
                SaveProjectAs();
                return;
            }

            SaveProject(_projectPath);
        }

        private void OnSaveProjectAsClick(object sender, RoutedEventArgs e) => SaveProjectAs();

        private void OnExitClick(object sender, RoutedEventArgs e) => Close();

        private void SaveProjectAs()
        {
            var dialog = new SaveFileDialog
            {
                Filter = ProjectFilter,
                DefaultExt = ".lshow",
                FileName = string.IsNullOrWhiteSpace(_project.Name) ? "show.lshow" : $"{_project.Name}.lshow"
            };

            if (dialog.ShowDialog(this) == true)
            {
                SaveProject(dialog.FileName);
            }
        }

        private void SaveProject(string path)
        {
            try
            {
                _projectFileService.Save(path, _project);
                _projectPath = path;
                _isDirty = false;
                UpdateProjectStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Sauvegarde du projet", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProjectIntoUi(ShowProject project, string? projectPath, bool markDirty)
        {
            _project = project;
            _projectPath = projectPath;
            _isDirty = markDirty;
            _selectedClip = FindFirstClip(project);

            _playbackController.Duration = Math.Max(1, project.Duration);
            TimeSlider.Maximum = _playbackController.Duration;
            TimelineControl.SetProject(project);
            TimelineControl.SelectClip(_selectedClip);
            UpdatePropertyPanel(_selectedClip);
            RenderPreview(_playbackController.CurrentTime);
            UpdatePlaybackUi();
            UpdateProjectStatus();
        }

        private void UpdatePropertyPanel(TimelineClip? clip)
        {
            _isUpdatingPropertyUi = true;
            var enabled = clip is not null;

            UpdateShapesTab(clip);

            ClipNameTextBox.IsEnabled = enabled;
            ClipStartTextBox.IsEnabled = enabled;
            ClipDurationTextBox.IsEnabled = enabled;
            EffectTypeComboBox.IsEnabled = enabled;
            TargetTypeComboBox.IsEnabled = enabled;
            RedTextBox.IsEnabled = enabled;
            GreenTextBox.IsEnabled = enabled;
            BlueTextBox.IsEnabled = enabled;
            WhiteTextBox.IsEnabled = enabled;
            IntensitySlider.IsEnabled = enabled;
            SpeedSlider.IsEnabled = enabled;
            ApplyPropertiesButton.IsEnabled = enabled;
            ColorPickerControl.IsEnabled = enabled;
            ShapeEditModeComboBox.IsEnabled = enabled;
            MovementEffectComboBox.IsEnabled = enabled;

            if (clip is null)
            {
                ClipNameTextBox.Text = string.Empty;
                ColorPickerControl.SetColor(RgbwColor.White);
                MovementOffsetTextBlock.Text = "dx 0 / dy 0";
                _isUpdatingPropertyUi = false;
                return;
            }

            ClipNameTextBox.Text = clip.Name;
            ClipStartTextBox.Text = clip.StartTime.ToString("0.###", CultureInfo.InvariantCulture);
            ClipDurationTextBox.Text = clip.Duration.ToString("0.###", CultureInfo.InvariantCulture);
            SelectComboByTag(EffectTypeComboBox, clip.EffectType.ToString());
            SelectComboByTag(TargetTypeComboBox, clip.Target.Type.ToString());
            RedTextBox.Text = clip.Color.R.ToString(CultureInfo.InvariantCulture);
            GreenTextBox.Text = clip.Color.G.ToString(CultureInfo.InvariantCulture);
            BlueTextBox.Text = clip.Color.B.ToString(CultureInfo.InvariantCulture);
            WhiteTextBox.Text = clip.Color.W.ToString(CultureInfo.InvariantCulture);
            ColorPickerControl.SetColor(clip.Color);
            IntensitySlider.Value = clip.Intensity;
            SpeedSlider.Value = clip.Speed;
            SelectComboByTag(MovementEffectComboBox, clip.MovementEffect.ToString());
            UpdateMovementOffsetText(clip);
            _isUpdatingPropertyUi = false;
        }

        private void UpdateShapesTab(TimelineClip? clip)
        {
            _availableShapes = ShapeFactory.CreateForClip(clip, _project.WallWidth, _project.WallHeight);
            ShapesItemsControl.ItemsSource = _availableShapes;
            ShapesTab.IsEnabled = clip is not null;
            ShapesClipTextBlock.Text = clip is null
                ? "Aucun clip selectionne"
                : $"Formes pour le clip : {clip.Name}";
            UpdateMovementOffsetText(clip);
        }

        private void UpdatePlaybackUi()
        {
            _isUpdatingPlaybackUi = true;
            TimeSlider.Maximum = _playbackController.Duration;
            TimeSlider.Value = _playbackController.CurrentTime;
            TimeDisplayText.Text = $"{FormatTime(_playbackController.CurrentTime)} / {FormatTime(_playbackController.Duration)}";
            PlaybackStatusItem.Content = GetStatusText(_playbackController.Status);
            _isUpdatingPlaybackUi = false;
        }

        private void UpdateProjectStatus()
        {
            var suffix = _isDirty ? " *" : string.Empty;
            ProjectStatusItem.Content = _projectPath is null ? $"Projet non sauvegarde{suffix}" : $"{System.IO.Path.GetFileName(_projectPath)}{suffix}";
            WallStatusItem.Content = $"{_project.WallWidth} x {_project.WallHeight} - UDP {RoutingHostIp}:{RoutingHostStatePort}";
        }

        private void RenderPreview(double currentTime)
        {
            var state = _playbackEngine.ComputeState(currentTime, _project);
            if (state.Entities.Count == 0)
            {
                LedPreview.Clear();
                return;
            }

            LedPreview.RenderState(state);
        }

        private void PublishCurrentState(double currentTime)
        {
            try
            {
                var state = _playbackEngine.ComputeState(currentTime, _project);
                _statePublisher.Publish(state);
            }
            catch
            {
                PlaybackStatusItem.Content = "Erreur envoi UDP";
            }
        }

        private void MarkDirty()
        {
            _isDirty = true;
            UpdateProjectStatus();
        }

        private TimelineClip? FindActiveClip(double currentTime)
        {
            foreach (var track in _project.Tracks)
            {
                var clip = track.Clips.FindLast(item => currentTime >= item.StartTime && currentTime <= item.EndTime);
                if (clip is not null)
                {
                    return clip;
                }
            }

            return null;
        }

        private static ShowProject CreateDefaultProject()
        {
            return new ShowProject
            {
                Name = "Nouveau projet",
                Duration = 30,
                Tracks =
                {
                    new Track
                    {
                        Name = "Fond",
                        Clips =
                        {
                            new TimelineClip
                            {
                                Name = "Ambiance rouge",
                                StartTime = 0,
                                Duration = 8,
                                EffectType = EffectType.SolidColor,
                                Color = new RgbwColor(220, 40, 40, 0),
                                Intensity = 0.9
                            },
                            new TimelineClip
                            {
                                Name = "Vague bleue",
                                StartTime = 8,
                                Duration = 12,
                                EffectType = EffectType.Wave,
                                Color = new RgbwColor(40, 120, 255, 0),
                                Intensity = 1,
                                Speed = 1.2
                            }
                        }
                    },
                    new Track
                    {
                        Name = "Forme",
                        Clips =
                        {
                            new TimelineClip
                            {
                                Name = "Forme 1",
                                StartTime = 20,
                                Duration = 6,
                                EffectType = EffectType.Fade,
                                Color = new RgbwColor(180, 180, 180, 40),
                                Intensity = 1,
                                Target = new TargetSelection { Type = TargetType.Selection }
                            }
                        }
                    }
                }
            };
        }

        private static TimelineClip? FindFirstClip(ShowProject project)
        {
            foreach (var track in project.Tracks)
            {
                if (track.Clips.Count > 0)
                {
                    return track.Clips[0];
                }
            }

            return null;
        }

        private List<int>? MoveSelection(IReadOnlyCollection<int> entityIds, int deltaX, int deltaY)
        {
            var points = entityIds
                .Select(id => new { X = id % _project.WallWidth, Y = id / _project.WallWidth })
                .Where(point => point.Y >= 0 && point.Y < _project.WallHeight)
                .ToList();

            if (points.Count == 0)
            {
                return null;
            }

            var minX = points.Min(point => point.X);
            var maxX = points.Max(point => point.X);
            var minY = points.Min(point => point.Y);
            var maxY = points.Max(point => point.Y);
            var boundedDeltaX = Math.Clamp(deltaX, -minX, _project.WallWidth - 1 - maxX);
            var boundedDeltaY = Math.Clamp(deltaY, -minY, _project.WallHeight - 1 - maxY);

            if (boundedDeltaX == 0 && boundedDeltaY == 0)
            {
                return null;
            }

            return points
                .Select(point => ((point.Y + boundedDeltaY) * _project.WallWidth) + point.X + boundedDeltaX)
                .Distinct()
                .ToList();
        }

        private void MoveMovementTarget(TimelineClip clip, int deltaX, int deltaY)
        {
            var bounds = GetSelectionBounds(clip.Target.EntityIds);
            if (bounds is null)
            {
                return;
            }

            var (minX, maxX, minY, maxY) = bounds.Value;
            clip.MovementOffsetX = Math.Clamp(
                clip.MovementOffsetX + deltaX,
                -minX,
                _project.WallWidth - 1 - maxX);
            clip.MovementOffsetY = Math.Clamp(
                clip.MovementOffsetY + deltaY,
                -minY,
                _project.WallHeight - 1 - maxY);

            if (clip.MovementEffect == MovementEffectType.None)
            {
                clip.MovementEffect = MovementEffectType.Slow;
                SelectComboByTag(MovementEffectComboBox, clip.MovementEffect.ToString());
            }
        }

        private void ClampMovementTarget(TimelineClip clip)
        {
            var bounds = GetSelectionBounds(clip.Target.EntityIds);
            if (bounds is null)
            {
                clip.MovementOffsetX = 0;
                clip.MovementOffsetY = 0;
                return;
            }

            var (minX, maxX, minY, maxY) = bounds.Value;
            clip.MovementOffsetX = Math.Clamp(clip.MovementOffsetX, -minX, _project.WallWidth - 1 - maxX);
            clip.MovementOffsetY = Math.Clamp(clip.MovementOffsetY, -minY, _project.WallHeight - 1 - maxY);
        }

        private void SeekToMovementPreviewTime(TimelineClip clip)
        {
            var previewTime = Math.Max(clip.StartTime, clip.EndTime - 0.001);
            if (Math.Abs(_playbackController.CurrentTime - previewTime) > 0.001)
            {
                _playbackController.Seek(previewTime);
                TimelineControl.SetPlayhead(_playbackController.CurrentTime);
            }
        }

        private bool IsEditingMovementTarget()
            => ShapeEditModeComboBox.SelectedItem is ComboBoxItem { Tag: string tag }
               && string.Equals(tag, "Movement", StringComparison.Ordinal);

        private void UpdateMovementOffsetText(TimelineClip? clip)
        {
            MovementOffsetTextBlock.Text = clip is null
                ? "dx 0 / dy 0"
                : $"dx {clip.MovementOffsetX} / dy {clip.MovementOffsetY}";
        }

        private (int MinX, int MaxX, int MinY, int MaxY)? GetSelectionBounds(IReadOnlyCollection<int> entityIds)
        {
            var points = entityIds
                .Select(id => new { X = id % _project.WallWidth, Y = id / _project.WallWidth })
                .Where(point => point.Y >= 0 && point.Y < _project.WallHeight)
                .ToList();

            if (points.Count == 0)
            {
                return null;
            }

            return (
                points.Min(point => point.X),
                points.Max(point => point.X),
                points.Min(point => point.Y),
                points.Max(point => point.Y));
        }

        private static double ReadDouble(TextBox textBox, double fallback)
            => double.TryParse(textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? value
                : fallback;

        private static byte ReadByte(TextBox textBox, byte fallback)
            => byte.TryParse(textBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : fallback;

        private static T ReadComboTag<T>(ComboBox comboBox, T fallback) where T : struct
        {
            if (comboBox.SelectedItem is ComboBoxItem { Tag: string tag } && Enum.TryParse<T>(tag, out var value))
            {
                return value;
            }

            return fallback;
        }

        private static void SelectComboByTag(ComboBox comboBox, string tag)
        {
            foreach (var item in comboBox.Items)
            {
                if (item is ComboBoxItem comboBoxItem && string.Equals(comboBoxItem.Tag?.ToString(), tag, StringComparison.Ordinal))
                {
                    comboBox.SelectedItem = comboBoxItem;
                    return;
                }
            }
        }

        private static string FormatTime(double seconds)
        {
            var time = TimeSpan.FromSeconds(Math.Max(0, seconds));
            return $"{(int)time.TotalMinutes:00}:{time.Seconds:00}.{time.Milliseconds:000}";
        }

        private static string GetStatusText(PlaybackStatus status) => status switch
        {
            PlaybackStatus.Playing => "Lecture",
            PlaybackStatus.Paused => "Pause",
            _ => "Pret"
        };

        private static double FadeLevel(double localTime, double duration)
        {
            if (duration <= 0)
            {
                return 1;
            }

            var progress = Math.Clamp(localTime / duration, 0, 1);
            return progress <= 0.5 ? progress * 2 : (1 - progress) * 2;
        }

        private static Color ScaleColor(Color color, double factor)
        {
            var level = Math.Clamp(factor, 0, 1);
            return Color.FromRgb(
                ScaleChannel(color.R, level),
                ScaleChannel(color.G, level),
                ScaleChannel(color.B, level));
        }

        private static byte ScaleChannel(byte value, double factor) => (byte)Math.Clamp((int)Math.Round(value * factor), 0, 255);
    }
}


