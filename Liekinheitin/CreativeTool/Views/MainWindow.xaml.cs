using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
        private readonly SavedProjectsService _savedProjectsService;
        private readonly ShowPlaybackEngine _playbackEngine = new();
        private readonly AudioPlaybackService _audioPlaybackService = new();
        private readonly IStatePublisher _statePublisher;
        private readonly DispatcherTimer _playbackTimer;

        private ShowProject _project;
        private TimelineClip? _selectedClip;
        private TimelineClip? _recordingClip;
        private IReadOnlyList<IShape> _availableShapes = Array.Empty<IShape>();
        private readonly Stopwatch _motionCaptureClock = new();
        private readonly Stopwatch _motionUiClock = new();
        private int _recordingOffsetX;
        private int _recordingOffsetY;
        private string? _projectPath;
        private bool _isDirty;
        private bool _isUpdatingPlaybackUi;
        private bool _isUpdatingPropertyUi;
        private bool _isUpdatingAudioUi;

        public MainWindow()
        {
            InitializeComponent();

            _savedProjectsService = new SavedProjectsService(_projectFileService);

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
            TimelineControl.ClipChanged += OnTimelineClipChanged;
            ApplyPropertiesButton.Click += OnApplyPropertiesClick;
            ApplyDurationButton.Click += OnApplyDurationClick;
            ExtendTimelineButton.Click += OnExtendTimelineClick;
            AddShapeLayerButton.Click += OnAddShapeLayerClick;
            SelectAudioButton.Click += OnSelectAudioClick;
            ConfirmAudioButton.Click += OnConfirmAudioClick;
            AudioVolumeSlider.ValueChanged += OnAudioVolumeChanged;
            LedPreview.PixelDragStarted += OnPreviewPixelDragStarted;
            LedPreview.PixelDragDelta += OnPreviewPixelDragDelta;
            LedPreview.PixelDragCompleted += OnPreviewPixelDragCompleted;
            ShapeEditModeComboBox.SelectionChanged += OnShapeEditModeSelectionChanged;
            MovementEffectComboBox.SelectionChanged += OnMovementEffectSelectionChanged;
            ResetMovementButton.Click += OnResetMovementClick;
            ReplayMotionButton.Click += OnReplayMotionClick;
            RedoMotionButton.Click += OnRedoMotionClick;
            ConfirmMotionButton.Click += OnConfirmMotionClick;
            ColorPickerControl.ColorChanged += OnColorPickerColorChanged;
            _playbackController.StateChanged += (_, _) =>
            {
                SyncAudioPlayback();
                UpdatePlaybackUi();
            };
            _audioPlaybackService.MediaOpened += OnAudioMediaOpened;

            LoadProjectIntoUi(_project, null, markDirty: false);
            _playbackTimer.Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_statePublisher is IDisposable disposable)
            {
                disposable.Dispose();
            }

            _audioPlaybackService.Dispose();

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
            _audioPlaybackService.Seek(e.NewValue);
            TimelineControl.SetPlayhead(_playbackController.CurrentTime);
            RenderPreview(_playbackController.CurrentTime);
        }

        private void OnPlayClick(object sender, RoutedEventArgs e) => _playbackController.Play();

        private void OnPauseClick(object sender, RoutedEventArgs e) => _playbackController.Pause();

        private void OnStopClick(object sender, RoutedEventArgs e) => _playbackController.Stop();

        private void OnSelectAudioClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "MP3 (*.mp3)|*.mp3|Tous les fichiers audio (*.mp3;*.wav;*.wma)|*.mp3;*.wav;*.wma|Tous les fichiers (*.*)|*.*",
                DefaultExt = ".mp3"
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            _project.AudioFilePath = dialog.FileName;
            _audioPlaybackService.Load(dialog.FileName);
            _audioPlaybackService.Seek(_playbackController.CurrentTime);
            SyncAudioPlayback();
            UpdateAudioUi();
            MarkDirty();
        }

        private void OnAudioVolumeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingAudioUi)
            {
                return;
            }

            var volume = Math.Clamp(e.NewValue, 0, 1);
            _project.AudioVolume = volume;
            _audioPlaybackService.Volume = volume;
            UpdateAudioUi();
            MarkDirty();
        }

        private void OnConfirmAudioClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_project.AudioFilePath))
            {
                MessageBox.Show(this, "Choisis d'abord un fichier MP3.", "Musique", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var clip = AddOrUpdateAudioClip();
            _selectedClip = clip;
            TimelineControl.SetProject(_project);
            TimelineControl.SelectClip(clip);
            UpdatePropertyPanel(clip);
            EnsureProjectDurationIncludesClips();
            MarkDirty();
        }

        private void OnAudioMediaOpened(object? sender, EventArgs e)
        {
            var audioClip = FindAudioClip();
            if (audioClip is null || _audioPlaybackService.DurationSeconds is not { } duration || duration <= 0)
            {
                return;
            }

            audioClip.Duration = duration;
            EnsureProjectDurationIncludesClips();
            TimelineControl.Redraw();
        }

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

            if (_selectedClip.IsAudio)
            {
                EnsureProjectDurationIncludesClips();
                MarkDirty();
                TimelineControl.Redraw();
                return;
            }

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
            EnsureProjectDurationIncludesClips();
            EnsurePlaybackInsideClip(_selectedClip);

            MarkDirty();
            TimelineControl.Redraw();
            RenderPreview(_playbackController.CurrentTime);
        }

        private void OnShapeButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: IShape shape })
            {
                return;
            }

            var entityIds = shape.GetEntityIds()
                .Where(id => id >= 0 && id < _project.WallWidth * _project.WallHeight)
                .Distinct()
                .ToList();

            var clip = GetReusableEmptyShapeClip() ?? CreateShapeClip(shape.DisplayName);
            clip.Name = shape.DisplayName;
            clip.Target.Type = TargetType.Selection;
            clip.Target.EntityIds = entityIds;
            clip.MovementOffsetX = 0;
            clip.MovementOffsetY = 0;
            clip.MovementEffect = ReadComboTag(MovementEffectComboBox, clip.MovementEffect);
            _selectedClip = clip;

            TimelineControl.SetProject(_project);
            TimelineControl.SelectClip(clip);
            SelectComboByTag(ShapeEditModeComboBox, "Movement");
            UpdatePropertyPanel(clip);
            SelectComboByTag(TargetTypeComboBox, TargetType.Selection.ToString());
            UpdateShapeMotionUi(clip);

            if (_playbackController.CurrentTime < clip.StartTime || _playbackController.CurrentTime > clip.EndTime)
            {
                _playbackController.Seek(clip.StartTime);
                TimelineControl.SetPlayhead(_playbackController.CurrentTime);
            }

            MarkDirty();
            RenderPreview(_playbackController.CurrentTime);
        }

        private void OnPreviewPixelDragCompleted(object? sender, EventArgs e)
        {
            if (_recordingClip is not null)
            {
                EndMotionCapture(replay: false);
                return;
            }

            if (_selectedClip is null || !IsEditingMovementTarget())
            {
                return;
            }

            if (_selectedClip.MovementEffect == MovementEffectType.None
                || (_selectedClip.MovementOffsetX == 0 && _selectedClip.MovementOffsetY == 0))
            {
                return;
            }

            _playbackController.Seek(_selectedClip.StartTime);
            TimelineControl.SetPlayhead(_playbackController.CurrentTime);
            RenderPreview(_playbackController.CurrentTime);
            _playbackController.Play();
        }

        private void OnPreviewPixelDragStarted(object? sender, EventArgs e)
        {
            if (_selectedClip?.Target.Type != TargetType.Selection || _selectedClip.Target.EntityIds.Count == 0)
            {
                return;
            }

            if (!IsEditingMovementTarget())
            {
                return;
            }

            BeginMotionCapture(_selectedClip);
        }

        private void OnApplyDurationClick(object sender, RoutedEventArgs e)
        {
            SetProjectDuration(ReadDouble(ProjectDurationTextBox, _project.Duration), markDirty: true);
        }

        private void OnTimelineClipChanged(object? sender, TimelineClip clip)
        {
            _selectedClip = clip;
            EnsureProjectDurationIncludesClips();
            EnsurePlaybackInsideClip(clip);
            UpdatePropertyPanel(clip);
            MarkDirty();
            RenderPreview(_playbackController.CurrentTime);
        }

        private void OnExtendTimelineClick(object sender, RoutedEventArgs e)
        {
            SetProjectDuration(_project.Duration + 10, markDirty: true);
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

            if (_recordingClip is not null)
            {
                RecordMotionDelta(e.DeltaX, e.DeltaY);
                return;
            }

            if (IsEditingMovementTarget())
            {
                MoveMovementTarget(_selectedClip, e.DeltaX, e.DeltaY);
                MarkDirty();
                SeekToMovementPreviewTime(_selectedClip);
                UpdateShapeMotionUi(_selectedClip);
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
            UpdateShapeMotionUi(_selectedClip);
            MarkDirty();
            RenderPreview(_playbackController.CurrentTime);
        }

        private void OnReplayMotionClick(object sender, RoutedEventArgs e)
        {
            if (_selectedClip is null)
            {
                return;
            }

            ReplayClip(_selectedClip);
        }

        private void OnRedoMotionClick(object sender, RoutedEventArgs e)
        {
            if (_selectedClip is null)
            {
                return;
            }

            ClearMotion(_selectedClip);
            SelectComboByTag(ShapeEditModeComboBox, "Movement");
            _playbackController.Seek(_selectedClip.StartTime);
            TimelineControl.SetPlayhead(_playbackController.CurrentTime);
            UpdateShapeMotionUi(_selectedClip);
            MarkDirty();
            RenderPreview(_playbackController.CurrentTime);
        }

        private void OnConfirmMotionClick(object sender, RoutedEventArgs e)
        {
            if (_selectedClip is null)
            {
                return;
            }

            _selectedClip.IsMotionDraft = false;
            _recordingClip = null;
            _motionCaptureClock.Reset();
            UpdateShapeMotionUi(_selectedClip);
            MarkDirty();
        }

        private void OnMovementEffectSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingPropertyUi || _selectedClip is null)
            {
                return;
            }

            _selectedClip.MovementEffect = ReadComboTag(MovementEffectComboBox, _selectedClip.MovementEffect);
            MarkDirty();
            UpdateShapeMotionUi(_selectedClip);
            RenderPreview(_playbackController.CurrentTime);
        }

        private void OnResetMovementClick(object sender, RoutedEventArgs e)
        {
            if (_selectedClip is null)
            {
                return;
            }

            _selectedClip.MovementOffsetX = 0;
            _selectedClip.MovementOffsetY = 0;
            _selectedClip.MovementEffect = MovementEffectType.None;
            SelectComboByTag(MovementEffectComboBox, _selectedClip.MovementEffect.ToString());
            _playbackController.Seek(_selectedClip.StartTime);
            TimelineControl.SetPlayhead(_playbackController.CurrentTime);
            UpdateShapeMotionUi(_selectedClip);
            MarkDirty();
            RenderPreview(_playbackController.CurrentTime);
        }

        private void OnShapeEditModeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingPropertyUi || _selectedClip is null)
            {
                return;
            }

            if (IsEditingMovementTarget())
            {
                SeekToMovementPreviewTime(_selectedClip);
            }
            else
            {
                _playbackController.Seek(_selectedClip.StartTime);
                TimelineControl.SetPlayhead(_playbackController.CurrentTime);
            }

            UpdateShapeMotionUi(_selectedClip);
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

        private void OnSaveToLibraryClick(object sender, RoutedEventArgs e)
        {
            try
            {
                _projectPath = _savedProjectsService.Save(_project);
                _isDirty = false;
                UpdateProjectStatus();
                MessageBox.Show(this, "Animation enregistrée dans Mes sauvegardes.", "Sauvegarde", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Sauvegarde", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnSavedProjectsClick(object sender, RoutedEventArgs e)
        {
            var window = new SavedProjectsWindow(_savedProjectsService) { Owner = this };
            if (window.ShowDialog() != true || window.SelectedProjectPath is not { } path) return;

            try
            {
                _playbackController.Stop();
                LoadProjectIntoUi(_projectFileService.Load(path), path, markDirty: false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Reprise de l'animation", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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
            LoadAudioFromProject();
            TimeSlider.Maximum = _playbackController.Duration;
            ProjectDurationTextBox.Text = _playbackController.Duration.ToString("0.###", CultureInfo.InvariantCulture);
            TimelineControl.SetProject(project);
            TimelineControl.SelectClip(_selectedClip);
            UpdatePropertyPanel(_selectedClip);
            RenderPreview(_playbackController.CurrentTime);
            UpdatePlaybackUi();
            UpdateProjectStatus();
        }

        private void LoadAudioFromProject()
        {
            _audioPlaybackService.Stop();
            _project.AudioVolume = Math.Clamp(_project.AudioVolume, 0, 1);
            _audioPlaybackService.Volume = _project.AudioVolume;

            if (!string.IsNullOrWhiteSpace(_project.AudioFilePath) && File.Exists(_project.AudioFilePath))
            {
                _audioPlaybackService.Load(_project.AudioFilePath);
            }
            else
            {
                _audioPlaybackService.Clear();
            }

            UpdateAudioUi();
        }

        private TimelineClip AddOrUpdateAudioClip()
        {
            var track = FindAudioTrack();
            if (track is null)
            {
                track = new Track { Name = "Musique" };
                _project.Tracks.Insert(0, track);
            }

            var clip = track.Clips.FirstOrDefault(item => item.IsAudio);
            var audioName = Path.GetFileNameWithoutExtension(_project.AudioFilePath) ?? "Musique";
            var duration = GetAudioClipDuration();

            if (clip is null)
            {
                clip = new TimelineClip
                {
                    IsAudio = true,
                    Name = audioName,
                    StartTime = 0,
                    Duration = duration,
                    Target = new TargetSelection { Type = TargetType.Selection },
                    Intensity = 0
                };
                track.Clips.Add(clip);
            }
            else
            {
                clip.IsAudio = true;
                clip.Name = audioName;
                clip.StartTime = 0;
                clip.Duration = duration;
                clip.Target.Type = TargetType.Selection;
                clip.Target.EntityIds.Clear();
                clip.Intensity = 0;
            }

            return clip;
        }

        private double GetAudioClipDuration()
        {
            if (_audioPlaybackService.DurationSeconds is { } duration && duration > 0)
            {
                return duration;
            }

            return Math.Max(1, _project.Duration);
        }

        private Track? FindAudioTrack()
            => _project.Tracks.FirstOrDefault(track => track.Clips.Any(clip => clip.IsAudio))
               ?? _project.Tracks.FirstOrDefault(track => string.Equals(track.Name, "Musique", StringComparison.OrdinalIgnoreCase));

        private TimelineClip? FindAudioClip()
            => _project.Tracks.SelectMany(track => track.Clips).FirstOrDefault(clip => clip.IsAudio);

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
            ResetMovementButton.IsEnabled = enabled;
            ReplayMotionButton.IsEnabled = enabled;
            RedoMotionButton.IsEnabled = enabled;
            ConfirmMotionButton.IsEnabled = enabled;

            if (clip is null)
            {
                ClipNameTextBox.Text = string.Empty;
                ColorPickerControl.SetColor(RgbwColor.White);
                UpdateShapeMotionUi(null);
                _isUpdatingPropertyUi = false;
                return;
            }

            var isAudioClip = clip.IsAudio;
            EffectTypeComboBox.IsEnabled = enabled && !isAudioClip;
            TargetTypeComboBox.IsEnabled = enabled && !isAudioClip;
            RedTextBox.IsEnabled = enabled && !isAudioClip;
            GreenTextBox.IsEnabled = enabled && !isAudioClip;
            BlueTextBox.IsEnabled = enabled && !isAudioClip;
            WhiteTextBox.IsEnabled = enabled && !isAudioClip;
            IntensitySlider.IsEnabled = enabled && !isAudioClip;
            SpeedSlider.IsEnabled = enabled && !isAudioClip;
            ColorPickerControl.IsEnabled = enabled && !isAudioClip;
            ShapeEditModeComboBox.IsEnabled = enabled && !isAudioClip;
            MovementEffectComboBox.IsEnabled = enabled && !isAudioClip;
            ResetMovementButton.IsEnabled = enabled && !isAudioClip;
            ReplayMotionButton.IsEnabled = enabled && !isAudioClip;
            RedoMotionButton.IsEnabled = enabled && !isAudioClip;
            ConfirmMotionButton.IsEnabled = enabled && !isAudioClip;

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
            UpdateShapeMotionUi(clip);
            _isUpdatingPropertyUi = false;
        }

        private void UpdateShapesTab(TimelineClip? clip)
        {
            _availableShapes = ShapeFactory.CreateForClip(clip, _project.WallWidth, _project.WallHeight);
            ShapesItemsControl.ItemsSource = _availableShapes;
            ShapesTab.IsEnabled = true;
            ShapesClipTextBlock.Text = clip is null
                ? "Clique une forme pour créer une barre"
                : $"Formes pour le clip : {clip.Name}";
            UpdateShapeMotionUi(clip);
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

        private void UpdateAudioUi()
        {
            _isUpdatingAudioUi = true;

            AudioVolumeSlider.Value = Math.Clamp(_project.AudioVolume, 0, 1);
            AudioVolumeTextBlock.Text = $"{Math.Round(_project.AudioVolume * 100):0}%";
            AudioFileTextBlock.Text = string.IsNullOrWhiteSpace(_project.AudioFilePath)
                ? "Aucun fichier selectionne"
                : Path.GetFileName(_project.AudioFilePath);
            ConfirmAudioButton.IsEnabled = !string.IsNullOrWhiteSpace(_project.AudioFilePath);

            _isUpdatingAudioUi = false;
        }

        private void SyncAudioPlayback()
        {
            switch (_playbackController.Status)
            {
                case PlaybackStatus.Playing:
                    _audioPlaybackService.Play(_playbackController.CurrentTime);
                    break;
                case PlaybackStatus.Paused:
                    _audioPlaybackService.Pause();
                    break;
                default:
                    _audioPlaybackService.Stop();
                    break;
            }
        }

        private void SetProjectDuration(double duration, bool markDirty)
        {
            var boundedDuration = Math.Max(Math.Max(1, duration), GetMaxClipEnd(_project));
            _project.Duration = boundedDuration;
            _playbackController.Duration = boundedDuration;
            TimeSlider.Maximum = boundedDuration;
            ProjectDurationTextBox.Text = boundedDuration.ToString("0.###", CultureInfo.InvariantCulture);
            TimelineControl.Redraw();
            UpdatePlaybackUi();

            if (markDirty)
            {
                MarkDirty();
            }
        }

        private void EnsureProjectDurationIncludesClips()
        {
            var maxClipEnd = GetMaxClipEnd(_project);
            if (maxClipEnd > _project.Duration)
            {
                SetProjectDuration(maxClipEnd, markDirty: false);
            }
        }

        private void EnsurePlaybackInsideClip(TimelineClip clip)
        {
            if (_playbackController.CurrentTime >= clip.StartTime && _playbackController.CurrentTime <= clip.EndTime)
            {
                return;
            }

            _playbackController.Seek(clip.StartTime);
            TimelineControl.SetPlayhead(_playbackController.CurrentTime);
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

        private static double GetMaxClipEnd(ShowProject project)
            => project.Tracks
                .SelectMany(track => track.Clips)
                .Select(clip => clip.EndTime)
                .DefaultIfEmpty(1)
                .Max();

        private TimelineClip? GetReusableEmptyShapeClip()
        {
            if (_selectedClip is null || _selectedClip.Target.Type != TargetType.Selection || _selectedClip.Target.EntityIds.Count > 0)
            {
                return null;
            }

            return FindTrackContainingClip(_selectedClip)?.Name.StartsWith("Forme", StringComparison.OrdinalIgnoreCase) == true
                ? _selectedClip
                : null;
        }

        private TimelineClip CreateShapeClip(string shapeName)
        {
            var track = FindPreferredShapeTrack();
            var startTime = Math.Min(_playbackController.CurrentTime, Math.Max(0, _project.Duration - 1));
            var clip = new TimelineClip
            {
                Name = shapeName,
                StartTime = startTime,
                Duration = 1,
                EffectType = EffectType.SolidColor,
                Target = new TargetSelection { Type = TargetType.Selection },
                MovementEffect = ReadComboTag(MovementEffectComboBox, MovementEffectType.None),
                Color = RgbwColor.White,
                Intensity = 1,
                IsMotionDraft = true
            };

            track.Clips.Add(clip);
            EnsureProjectDurationIncludesClips();
            return clip;
        }

        private Track FindPreferredShapeTrack()
        {
            var selectedTrack = _selectedClip is null ? null : FindTrackContainingClip(_selectedClip);
            if (selectedTrack?.Name.StartsWith("Forme", StringComparison.OrdinalIgnoreCase) == true)
            {
                return selectedTrack;
            }

            var shapeTrack = _project.Tracks.FirstOrDefault(track => track.Name.StartsWith("Forme", StringComparison.OrdinalIgnoreCase));
            if (shapeTrack is not null)
            {
                return shapeTrack;
            }

            shapeTrack = new Track { Name = "Forme" };
            _project.Tracks.Add(shapeTrack);
            return shapeTrack;
        }

        private Track? FindTrackContainingClip(TimelineClip clip)
            => _project.Tracks.FirstOrDefault(track => track.Clips.Contains(clip));

        private void BeginMotionCapture(TimelineClip clip)
        {
            _playbackController.Pause();
            _recordingClip = clip;
            _recordingOffsetX = 0;
            _recordingOffsetY = 0;
            clip.IsMotionDraft = true;
            clip.MovementOffsetX = 0;
            clip.MovementOffsetY = 0;
            clip.MovementEffect = MovementEffectType.VeryFast;
            clip.MovementKeyframes.Clear();
            clip.MovementKeyframes.Add(new MovementKeyframe { Time = 0, OffsetX = 0, OffsetY = 0 });
            _motionCaptureClock.Restart();
            _motionUiClock.Restart();

            SelectComboByTag(MovementEffectComboBox, clip.MovementEffect.ToString());
            _playbackController.Seek(clip.StartTime);
            TimelineControl.SetPlayhead(_playbackController.CurrentTime);
            TimelineControl.SelectClip(clip);
            UpdateShapeMotionUi(clip);
            RenderPreview(_playbackController.CurrentTime);
        }

        private void RecordMotionDelta(int deltaX, int deltaY)
        {
            if (_recordingClip is null)
            {
                return;
            }

            var offset = ClampMovementOffset(_recordingClip, _recordingOffsetX + deltaX, _recordingOffsetY + deltaY);
            _recordingOffsetX = offset.OffsetX;
            _recordingOffsetY = offset.OffsetY;

            var time = Math.Max(0.001, _motionCaptureClock.Elapsed.TotalSeconds);
            _recordingClip.Duration = Math.Max(0.05, time);
            _recordingClip.MovementOffsetX = _recordingOffsetX;
            _recordingClip.MovementOffsetY = _recordingOffsetY;
            AddOrUpdateMotionKeyframe(_recordingClip, time, _recordingOffsetX, _recordingOffsetY);
            EnsureProjectDurationIncludesClips();

            var previewTime = _recordingClip.StartTime + time;
            _playbackController.Seek(previewTime);
            TimelineControl.SetPlayhead(_playbackController.CurrentTime);
            UpdateMotionCaptureUi(_recordingClip);
            MarkDirty();
            RenderPreview(_playbackController.CurrentTime);
        }

        private void UpdateMotionCaptureUi(TimelineClip clip)
        {
            MovementDurationTextBlock.Text = $"durée {clip.Duration:0.000}s";
            MovementOffsetTextBlock.Text = $"{clip.MovementKeyframes.Count} points enregistrés";

            if (_motionUiClock.ElapsedMilliseconds < 90)
            {
                return;
            }

            ClipDurationTextBox.Text = clip.Duration.ToString("0.###", CultureInfo.InvariantCulture);
            TimelineControl.Redraw();
            _motionUiClock.Restart();
        }

        private void EndMotionCapture(bool replay)
        {
            if (_recordingClip is null)
            {
                return;
            }

            var clip = _recordingClip;
            _recordingClip = null;
            _motionCaptureClock.Stop();
            _motionUiClock.Reset();

            if (clip.MovementKeyframes.Count == 1)
            {
                clip.MovementKeyframes.Clear();
                clip.MovementOffsetX = 0;
                clip.MovementOffsetY = 0;
            }

            UpdatePropertyPanel(clip);
            TimelineControl.Redraw();
            RenderPreview(_playbackController.CurrentTime);

            if (replay)
            {
                ReplayClip(clip);
            }
        }

        private void AddOrUpdateMotionKeyframe(TimelineClip clip, double time, int offsetX, int offsetY)
        {
            var last = clip.MovementKeyframes.LastOrDefault();
            if (last is not null && time - last.Time < 0.03)
            {
                last.Time = time;
                last.OffsetX = offsetX;
                last.OffsetY = offsetY;
                return;
            }

            clip.MovementKeyframes.Add(new MovementKeyframe { Time = time, OffsetX = offsetX, OffsetY = offsetY });
        }

        private void ReplayClip(TimelineClip clip)
        {
            _playbackController.Seek(clip.StartTime);
            TimelineControl.SetPlayhead(_playbackController.CurrentTime);
            RenderPreview(_playbackController.CurrentTime);
            _playbackController.Play();
        }

        private void ClearMotion(TimelineClip clip)
        {
            clip.MovementKeyframes.Clear();
            clip.MovementOffsetX = 0;
            clip.MovementOffsetY = 0;
            clip.MovementEffect = MovementEffectType.None;
            clip.IsMotionDraft = true;
            SelectComboByTag(MovementEffectComboBox, clip.MovementEffect.ToString());
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
            var offset = ClampMovementOffset(clip, clip.MovementOffsetX + deltaX, clip.MovementOffsetY + deltaY);
            clip.MovementOffsetX = offset.OffsetX;
            clip.MovementOffsetY = offset.OffsetY;

            if (clip.MovementEffect == MovementEffectType.None)
            {
                clip.MovementEffect = MovementEffectType.VeryFast;
                SelectComboByTag(MovementEffectComboBox, clip.MovementEffect.ToString());
            }
        }

        private (int OffsetX, int OffsetY) ClampMovementOffset(TimelineClip clip, int offsetX, int offsetY)
        {
            var bounds = GetSelectionBounds(clip.Target.EntityIds);
            if (bounds is null)
            {
                return (0, 0);
            }

            var (minX, maxX, minY, maxY) = bounds.Value;
            return (
                Math.Clamp(offsetX, -minX, _project.WallWidth - 1 - maxX),
                Math.Clamp(offsetY, -minY, _project.WallHeight - 1 - maxY));
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

        private void UpdateShapeMotionUi(TimelineClip? clip)
        {
            if (clip is null)
            {
                ShapeWorkflowTextBlock.Text = "Clique une forme pour créer une barre dans la timeline";
                MovementDurationTextBlock.Text = "durée 0.000s";
                MovementOffsetTextBlock.Text = "arrivée dx 0 / dy 0";
                return;
            }

            var hasShape = clip.Target.Type == TargetType.Selection && clip.Target.EntityIds.Count > 0;
            var mode = IsEditingMovementTarget() ? "Arrivée" : "Départ";
            var captureState = clip.IsMotionDraft ? "prise en cours" : "confirmé";
            ShapeWorkflowTextBlock.Text = hasShape
                ? $"{mode} éditable - {captureState}"
                : "Choisis une forme pour créer sa barre";
            MovementDurationTextBlock.Text = $"durée {clip.Duration:0.000}s";
            MovementOffsetTextBlock.Text = clip.MovementKeyframes.Count > 0
                ? $"{clip.MovementKeyframes.Count} points enregistrés"
                : $"arrivée dx {clip.MovementOffsetX} / dy {clip.MovementOffsetY}";
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


