using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Liekinheitin.Application.Interfaces;
using Liekinheitin.Application.Services;
using Liekinheitin.CreativeTool.Models;
using Liekinheitin.CreativeTool.Services;
using Liekinheitin.CreativeTool.Shapes;
using Liekinheitin.Infrastructure.Config;
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
        private readonly ShowPlaybackEngine _playbackEngine;
        private readonly AudioPlaybackService _audioPlaybackService = new();
        private readonly IStatePublisher _statePublisher;
        private readonly DispatcherTimer _playbackTimer;

        private ShowProject _project;
        private TimelineClip? _selectedClip;
        private Track? _selectedTrack;
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
        private string? _pendingMediaPath;
        private string? _loadedMediaPath;
        private readonly List<BitmapFrame> _gifFrames = new();
        private readonly List<double> _gifFrameEnds = new();
        private Point? _mediaDragStart;
        private double _mediaDragOriginX;
        private double _mediaDragOriginY;
        private MediaOverlayClip? _activeMediaOverlay;
        private List<int>? _previewResizeOriginalIds;
        private (int MinX, int MaxX, int MinY, int MaxY)? _previewResizeOriginBounds;
        private int _previewResizeDeltaX;
        private int _previewResizeDeltaY;
        private PreviewResizeEdges _previewResizeEdges;
        private bool _isPreviewRotating;
        private int _previewRotateCurrentX;
        private int _previewRotateCurrentY;
        private double _previewRotateCenterX;
        private double _previewRotateCenterY;
        private double _previewRotateStartAngle;
        private double _previewRotateOriginalDegrees;

        [Flags]
        private enum PreviewResizeEdges
        {
            None = 0,
            Left = 1,
            Right = 2,
            Top = 4,
            Bottom = 8
        }

        public MainWindow()
        {
            InitializeComponent();

            _savedProjectsService = new SavedProjectsService(_projectFileService);

            _playbackEngine = new ShowPlaybackEngine(LoadRealEntityIds());
            _statePublisher = new UdpStatePublisher(RoutingHostIp, RoutingHostStatePort);
            _project = CreateDefaultProject();
            LedPreview.Resize(_project.WallWidth, _project.WallHeight);

            _playbackTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(33)
            };
            _playbackTimer.Tick += OnPlaybackTick;

            PlayButton.Click += OnPlayClick;
            PauseButton.Click += OnPauseClick;
            StopButton.Click += OnStopClick;
            TimeSlider.ValueChanged += OnTimeSliderValueChanged;
            TimelineControl.ClipSelected += OnTimelineClipSelected;
            TimelineControl.ClipChanged += OnTimelineClipChanged;
            TimelineControl.ClipActionRequested += OnTimelineClipActionRequested;
            TimelineControl.TrackChanged += (_, _) =>
            {
                MarkDirty();
                RenderPreview(_playbackController.CurrentTime);
            };
            TimelineControl.TrackSelected += (_, track) =>
            {
                _selectedTrack = track;
                TrackNameTextBox.Text = track.Name;
            };
            ApplyPropertiesButton.Click += OnApplyPropertiesClick;
            AddOrganicRippleButton.Click += OnAddOrganicRippleClick;
            ApplyDurationButton.Click += OnApplyDurationClick;
            ExtendTimelineButton.Click += OnExtendTimelineClick;
            AddShapeLayerButton.Click += OnAddShapeLayerClick;
            AddTrackButton.Click += OnAddTrackClick;
            RenameTrackButton.Click += OnRenameTrackClick;
            DeleteTrackButton.Click += OnDeleteTrackClick;
            DuplicateClipToolbarButton.Click += OnDuplicateClipToolbarClick;
            InsertBeforeButton.Click += OnInsertClipBeforeClick;
            InsertAfterButton.Click += OnInsertClipAfterClick;
            SelectAudioButton.Click += OnSelectAudioClick;
            ConfirmAudioButton.Click += OnConfirmAudioClick;
            AudioVolumeSlider.ValueChanged += OnAudioVolumeChanged;
            AudioFadeOutSlider.ValueChanged += OnAudioFadeOutChanged;
            SelectMediaButton.Click += OnSelectMediaClick;
            AddMediaButton.Click += OnAddMediaClick;
            MediaImageOverlay.MouseLeftButtonDown += OnMediaDragStarted;
            MediaImageOverlay.MouseMove += OnMediaDragMoved;
            MediaImageOverlay.MouseLeftButtonUp += OnMediaDragCompleted;
            LedPreview.PixelDragStarted += OnPreviewPixelDragStarted;
            LedPreview.PixelDragDelta += OnPreviewPixelDragDelta;
            LedPreview.PixelDragCompleted += OnPreviewPixelDragCompleted;
            ShapeEditModeComboBox.SelectionChanged += OnShapeEditModeSelectionChanged;
            MovementEffectComboBox.SelectionChanged += OnMovementEffectSelectionChanged;
            ResetMovementButton.Click += OnResetMovementClick;
            ReplayMotionButton.Click += OnReplayMotionClick;
            RedoMotionButton.Click += OnRedoMotionClick;
            ConfirmMotionButton.Click += OnConfirmMotionClick;
            ResizeSmallerButton.Click += OnResizeSelectionClick;
            ResizeLargerButton.Click += OnResizeSelectionClick;
            ColorPickerControl.ColorChanged += OnColorPickerColorChanged;
            PreviewKeyDown += OnWindowPreviewKeyDown;
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
            if (_playbackController.Status != PlaybackStatus.Playing)
            {
                return;
            }

            var currentTime = _playbackController.CurrentTime;
            ApplyAudioFade(currentTime);
            TimelineControl.SetPlayhead(currentTime);
            RenderPreview(currentTime);
            UpdateMediaOverlay(currentTime);

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
            ApplyAudioFade(e.NewValue);
            TimelineControl.SetPlayhead(_playbackController.CurrentTime);
            RenderPreview(_playbackController.CurrentTime);
            UpdateMediaOverlay(_playbackController.CurrentTime);
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

        private void OnSelectMediaClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Médias (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.mp4;*.wmv;*.avi)|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.mp4;*.wmv;*.avi|Tous les fichiers (*.*)|*.*"
            };
            if (dialog.ShowDialog(this) != true) return;
            _pendingMediaPath = dialog.FileName;
            MediaFileTextBlock.Text = Path.GetFileName(dialog.FileName);
        }

        private void OnAddMediaClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_pendingMediaPath))
            {
                MessageBox.Show(this, "Choisis d'abord une image, un GIF ou une vidéo.", "Médias", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var media = new MediaOverlayClip
            {
                Name = Path.GetFileNameWithoutExtension(_pendingMediaPath),
                FilePath = _pendingMediaPath,
                StartTime = Math.Max(0, ReadDouble(MediaStartTextBox, _playbackController.CurrentTime)),
                Duration = Math.Max(0.05, ReadDouble(MediaDurationTextBox, 3)),
                Opacity = Math.Clamp(MediaOpacitySlider.Value, 0, 1)
            };
            _project.MediaOverlays.Add(media);
            GetOrCreateMediaTrack().Clips.Add(CreateMediaTimelineClip(media));
            EnsureProjectDurationIncludesMedia();
            TimelineControl.SetProject(_project);
            _loadedMediaPath = null;
            UpdateMediaOverlay(_playbackController.CurrentTime);
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
            ApplyAudioFade(_playbackController.CurrentTime);
            UpdateAudioUi();
            MarkDirty();
        }

        private void OnAudioFadeOutChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingAudioUi)
            {
                return;
            }

            _project.AudioFadeOutDuration = Math.Max(0, e.NewValue);
            ApplyAudioFade(_playbackController.CurrentTime);
            RenderPreview(_playbackController.CurrentTime);
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
            _selectedTrack = FindTrackContainingClip(clip);
            TrackNameTextBox.Text = _selectedTrack?.Name ?? string.Empty;
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

            if (_selectedClip.IsMedia)
            {
                SyncMediaFromTimelineClip(_selectedClip);
                EnsureProjectDurationIncludesMedia();
                MarkDirty();
                TimelineControl.Redraw();
                UpdateMediaOverlay(_playbackController.CurrentTime);
                return;
            }

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
            _selectedClip.Speed = Math.Clamp(SpeedSlider.Value, 0, 10);
            _selectedClip.RippleCenterX = Math.Clamp(ReadDouble(RippleCenterXTextBox, _selectedClip.RippleCenterX ?? (_project.WallWidth - 1) / 2.0), 0, _project.WallWidth - 1);
            _selectedClip.RippleCenterY = Math.Clamp(ReadDouble(RippleCenterYTextBox, _selectedClip.RippleCenterY ?? (_project.WallHeight - 1) / 2.0), 0, _project.WallHeight - 1);
            _selectedClip.MovementEffect = ReadComboTag(MovementEffectComboBox, _selectedClip.MovementEffect);
            EnsureProjectDurationIncludesClips();
            EnsurePlaybackInsideClip(_selectedClip);

            MarkDirty();
            TimelineControl.Redraw();
            RenderPreview(_playbackController.CurrentTime);
        }

        private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Space || Keyboard.FocusedElement is System.Windows.Controls.Primitives.TextBoxBase or ComboBox)
            {
                return;
            }

            if (_playbackController.Status == PlaybackStatus.Playing)
            {
                _playbackController.Pause();
            }
            else
            {
                _playbackController.Play();
            }

            e.Handled = true;
        }

        private void OnAddOrganicRippleClick(object sender, RoutedEventArgs e)
        {
            var track = _project.Tracks.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, "Ondulations organiques", StringComparison.OrdinalIgnoreCase));
            if (track is null)
            {
                track = new Track { Name = "Ondulations organiques" };
                _project.Tracks.Add(track);
            }

            var centerX = Math.Clamp(ReadDouble(RippleCenterXTextBox, (_project.WallWidth - 1) / 2.0), 0, _project.WallWidth - 1);
            var centerY = Math.Clamp(ReadDouble(RippleCenterYTextBox, (_project.WallHeight - 1) / 2.0), 0, _project.WallHeight - 1);
            var clip = new TimelineClip
            {
                Name = $"Ondulation organique {track.Clips.Count + 1}",
                StartTime = Math.Min(_playbackController.CurrentTime, Math.Max(0, _project.Duration - 1.1)),
                Duration = 1.1,
                EffectType = EffectType.ClickRipple,
                Target = TargetSelection.FullWall(),
                Color = new RgbwColor(88, 174, 220, 72),
                Intensity = 0.9,
                Speed = 1,
                RippleCenterX = centerX,
                RippleCenterY = centerY
            };
            track.Clips.Add(clip);
            _selectedTrack = track;
            _selectedClip = clip;
            EnsureProjectDurationIncludesClips();
            TimelineControl.SetProject(_project);
            TimelineControl.SelectClip(clip);
            UpdatePropertyPanel(clip);
            MarkDirty();
            RenderPreview(_playbackController.CurrentTime);
        }

        private void OnTimelineClipActionRequested(object? sender, ClipActionEventArgs e)
        {
            var track = FindTrackContainingClip(e.Clip);
            if (track is null) return;

            switch (e.Action)
            {
                case ClipAction.Edit:
                    _selectedClip = e.Clip;
                    TimelineControl.SelectClip(e.Clip);
                    UpdatePropertyPanel(e.Clip);
                    break;

                case ClipAction.Duplicate:
                    var duplicate = DuplicateClip(e.Clip);
                    if (e.Clip.IsMedia && e.Clip.MediaOverlayId is not null)
                    {
                        var sourceMedia = _project.MediaOverlays.FirstOrDefault(media => media.Id == e.Clip.MediaOverlayId);
                        if (sourceMedia is not null)
                        {
                            var mediaCopy = new MediaOverlayClip
                            {
                                Name = $"{sourceMedia.Name} (copie)", FilePath = sourceMedia.FilePath,
                                StartTime = duplicate.StartTime, Duration = duplicate.Duration, Opacity = sourceMedia.Opacity,
                                OffsetX = sourceMedia.OffsetX, OffsetY = sourceMedia.OffsetY
                            };
                            _project.MediaOverlays.Add(mediaCopy);
                            duplicate.MediaOverlayId = mediaCopy.Id;
                            duplicate.Name = mediaCopy.Name;
                        }
                    }
                    track.Clips.Add(duplicate);
                    _selectedClip = duplicate;
                    EnsureProjectDurationIncludesClips();
                    TimelineControl.SelectClip(duplicate);
                    UpdatePropertyPanel(duplicate);
                    MarkDirty();
                    break;

                case ClipAction.InsertBefore:
                    _selectedClip = e.Clip;
                    InsertClipRelative(before: true);
                    break;

                case ClipAction.InsertAfter:
                    _selectedClip = e.Clip;
                    InsertClipRelative(before: false);
                    break;

                case ClipAction.ToggleVisibility:
                    e.Clip.IsHidden = !e.Clip.IsHidden;
                    SyncMediaFromTimelineClip(e.Clip);
                    TimelineControl.Redraw();
                    RenderPreview(_playbackController.CurrentTime);
                    MarkDirty();
                    break;

                case ClipAction.Delete:
                    var answer = MessageBox.Show(this, $"Supprimer le clip « {e.Clip.Name} » ?", "Supprimer le clip", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (answer != MessageBoxResult.Yes) return;
                    track.Clips.Remove(e.Clip);
                    if (e.Clip.IsMedia && e.Clip.MediaOverlayId is not null)
                        _project.MediaOverlays.RemoveAll(media => media.Id == e.Clip.MediaOverlayId);
                    _selectedClip = FindFirstClip(_project);
                    TimelineControl.SetProject(_project);
                    TimelineControl.SelectClip(_selectedClip);
                    UpdatePropertyPanel(_selectedClip);
                    RenderPreview(_playbackController.CurrentTime);
                    MarkDirty();
                    break;
            }
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

        private void OnCreateCustomShapeClick(object sender, RoutedEventArgs e)
        {
            var family = ReadComboTagText(CustomShapeTypeComboBox, "Ripple");
            var density = (int)ReadComboTagNumber(CustomShapeDensityComboBox, 4);
            var thickness = ReadComboTagNumber(CustomShapeThicknessComboBox, 2.4);
            var irregularity = ReadComboTagNumber(CustomShapeIrregularityComboBox, 1.2);
            var variation = ReadComboTagNumber(CustomShapeVariationComboBox, 0.35);
            var isHeartbeat = string.Equals(family, "Heartbeat", StringComparison.Ordinal);
            var entityIds = isHeartbeat
                ? CreateHeartbeatShape(density, thickness, irregularity, variation)
                : CreateRippleShape(density, thickness, irregularity, variation);
            var shapeName = isHeartbeat
                ? $"Tracé cardiaque personnel C{density}"
                : $"Ondulations personnelles C{density}";

            var clip = GetReusableEmptyShapeClip() ?? CreateShapeClip(shapeName);
            clip.Name = shapeName;
            clip.EffectType = isHeartbeat ? EffectType.Wave : EffectType.Ripple;
            clip.Target.Type = TargetType.Selection;
            clip.Target.EntityIds = entityIds;
            clip.Target.TrackName = null;
            clip.Color = new RgbwColor(
                ReadByte(RedTextBox, clip.Color.R),
                ReadByte(GreenTextBox, clip.Color.G),
                ReadByte(BlueTextBox, clip.Color.B),
                ReadByte(WhiteTextBox, clip.Color.W));
            clip.Intensity = Math.Clamp(IntensitySlider.Value, 0, 1);
            clip.Speed = Math.Clamp(SpeedSlider.Value, 0, 10);
            clip.MovementOffsetX = 0;
            clip.MovementOffsetY = 0;
            clip.MovementEffect = ReadComboTag(MovementEffectComboBox, MovementEffectType.None);
            _selectedClip = clip;

            TimelineControl.SetProject(_project);
            TimelineControl.SelectClip(clip);
            SelectComboByTag(ShapeEditModeComboBox, "Movement");
            UpdatePropertyPanel(clip);
            SelectComboByTag(TargetTypeComboBox, TargetType.Selection.ToString());
            UpdateShapeMotionUi(clip);
            EnsurePlaybackInsideClip(clip);
            MarkDirty();
            RenderPreview(_playbackController.CurrentTime);
            ShowActionFeedback("Forme personnelle créée à partir de tes choix");
        }

        private List<int> CreateRippleShape(int density, double thickness, double irregularity, double variation)
        {
            var ids = new HashSet<int>();
            var centerX = _project.WallWidth / 2.0;
            var centerY = (_project.WallHeight / 2.0) + (Math.Sin(variation) * 3);
            var maximumRadius = Math.Max(12, Math.Min(_project.WallWidth, _project.WallHeight) * 0.41);

            for (var y = 0; y < _project.WallHeight; y++)
            {
                for (var x = 0; x < _project.WallWidth; x++)
                {
                    var dx = x - centerX;
                    var dy = y - centerY;
                    var distance = Math.Sqrt((dx * dx) + (dy * dy));
                    var angle = Math.Atan2(dy, dx);
                    for (var ring = 1; ring <= density; ring++)
                    {
                        var radius = 7 + ((maximumRadius - 7) * ring / density);
                        var wobble = irregularity
                                     * ((Math.Sin((angle * 3) + variation + (ring * 0.41)) * 0.78)
                                        + (Math.Sin((angle * 7) - variation - (ring * 0.27)) * 0.36));
                        if (Math.Abs(distance - (radius + wobble)) <= thickness)
                        {
                            ids.Add((y * _project.WallWidth) + x);
                            break;
                        }
                    }
                }
            }

            return ids.OrderBy(id => id).ToList();
        }

        private List<int> CreateHeartbeatShape(int density, double thickness, double irregularity, double variation)
        {
            var ids = new HashSet<int>();
            var points = new List<(double X, double Y)> { (4, _project.WallHeight / 2.0) };
            var baseline = _project.WallHeight / 2.0;
            var usableWidth = Math.Max(20, _project.WallWidth - 8);
            var spacing = usableWidth / (double)density;

            for (var beat = 0; beat < density; beat++)
            {
                var center = 4 + (spacing * (beat + 0.5));
                var character = Math.Sin(variation + (beat * 1.37));
                var amplitude = 11 + (irregularity * 5) + (character * irregularity * 2.2);
                points.Add((center - (spacing * 0.34), baseline));
                points.Add((center - (spacing * 0.24), baseline - (3 + irregularity)));
                points.Add((center - (spacing * 0.15), baseline + (4 + irregularity)));
                points.Add((center - (spacing * 0.07), baseline));
                points.Add((center - (spacing * 0.025), baseline + (amplitude * 0.35)));
                points.Add((center, baseline - amplitude));
                points.Add((center + (spacing * 0.06), baseline + (amplitude * 0.32)));
                points.Add((center + (spacing * 0.14), baseline));
            }

            points.Add((_project.WallWidth - 5, baseline));
            for (var index = 0; index < points.Count - 1; index++)
            {
                AddRasterLine(ids, points[index], points[index + 1], thickness);
            }

            return ids.OrderBy(id => id).ToList();
        }

        private void AddRasterLine(HashSet<int> ids, (double X, double Y) start, (double X, double Y) end, double thickness)
        {
            var steps = Math.Max(Math.Abs(end.X - start.X), Math.Abs(end.Y - start.Y)) * 2;
            for (var step = 0; step <= steps; step++)
            {
                var progress = steps <= 0 ? 0 : step / steps;
                var centerX = start.X + ((end.X - start.X) * progress);
                var centerY = start.Y + ((end.Y - start.Y) * progress);
                var radius = Math.Max(0, (int)Math.Floor(thickness / 2));
                for (var offsetY = -radius; offsetY <= radius; offsetY++)
                {
                    for (var offsetX = -radius; offsetX <= radius; offsetX++)
                    {
                        var x = (int)Math.Round(centerX + offsetX);
                        var y = (int)Math.Round(centerY + offsetY);
                        if (x >= 0 && x < _project.WallWidth && y >= 0 && y < _project.WallHeight)
                        {
                            ids.Add((y * _project.WallWidth) + x);
                        }
                    }
                }
            }
        }

        private static string ReadComboTagText(ComboBox comboBox, string fallback)
            => (comboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? fallback;

        private static double ReadComboTagNumber(ComboBox comboBox, double fallback)
            => double.TryParse(ReadComboTagText(comboBox, string.Empty), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? value
                : fallback;

        private void OnResizeSelectionClick(object sender, RoutedEventArgs e)
        {
            if (_selectedClip?.Target.Type != TargetType.Selection || _selectedClip.Target.EntityIds.Count == 0)
            {
                MessageBox.Show(this, "Sélectionne d'abord une forme dans la timeline.", "Taille de la forme", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (sender is not FrameworkElement { Tag: string factorText }
                || !double.TryParse(factorText, NumberStyles.Float, CultureInfo.InvariantCulture, out var factor))
            {
                return;
            }

            _selectedClip.Target.EntityIds = SelectionTransformService.Scale(
                _selectedClip.Target.EntityIds,
                factor,
                _project.WallWidth,
                _project.WallHeight);

            ClampMovementTarget(_selectedClip);
            foreach (var keyframe in _selectedClip.MovementKeyframes)
            {
                var bounded = ClampMovementOffset(_selectedClip, keyframe.OffsetX, keyframe.OffsetY);
                keyframe.OffsetX = bounded.OffsetX;
                keyframe.OffsetY = bounded.OffsetY;
            }

            MarkDirty();
            UpdateShapeMotionUi(_selectedClip);
            RenderPreview(_playbackController.CurrentTime);
        }

        private void OnPreviewPixelDragCompleted(object? sender, EventArgs e)
        {
            if (_isPreviewRotating)
            {
                EndPreviewRotation();
                return;
            }

            if (_previewResizeOriginalIds is not null)
            {
                EndPreviewResize();
                return;
            }

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

        private void OnPreviewPixelDragStarted(object? sender, PixelDragStartedEventArgs e)
        {
            SelectVisualClipAtPreviewPixel(e.PixelX, e.PixelY);

            if (_selectedClip?.Target.Type != TargetType.Selection || _selectedClip.Target.EntityIds.Count == 0)
            {
                return;
            }

            if (IsEditingResize())
            {
                BeginPreviewResize(e.PixelX, e.PixelY);
                return;
            }

            if (IsEditingRotate())
            {
                BeginPreviewRotation(e.PixelX, e.PixelY);
                return;
            }

            if (!IsEditingMovementTarget())
            {
                return;
            }

            BeginMotionCapture(_selectedClip);
        }

        private void SelectVisualClipAtPreviewPixel(int pixelX, int pixelY)
        {
            var selectedBounds = _selectedClip?.Target.Type == TargetType.Selection
                ? GetSelectionBounds(_selectedClip.Target.EntityIds)
                : null;
            if (selectedBounds is { } bounds
                && pixelX >= bounds.MinX - 3 && pixelX <= bounds.MaxX + 3
                && pixelY >= bounds.MinY - 3 && pixelY <= bounds.MaxY + 3)
            {
                return;
            }

            var entityId = (pixelY * _project.WallWidth) + pixelX;
            var currentTime = _playbackController.CurrentTime;
            var clip = _project.Tracks
                .AsEnumerable()
                .Reverse()
                .SelectMany(track => track.Clips.AsEnumerable().Reverse())
                .FirstOrDefault(candidate =>
                    !candidate.IsAudio
                    && !candidate.IsMedia
                    && !candidate.IsHidden
                    && candidate.Target.Type == TargetType.Selection
                    && currentTime >= candidate.StartTime
                    && currentTime < candidate.EndTime
                    && candidate.Target.EntityIds.Contains(entityId));

            if (clip is null || ReferenceEquals(clip, _selectedClip))
            {
                return;
            }

            _selectedClip = clip;
            TimelineControl.SelectClip(clip);
            UpdatePropertyPanel(clip);
            UpdatePreviewSelectionOverlay();
        }

        private void OnApplyDurationClick(object sender, RoutedEventArgs e)
        {
            SetProjectDuration(ReadDouble(ProjectDurationTextBox, _project.Duration), markDirty: true);
        }

        private void OnTimelineClipChanged(object? sender, TimelineClip clip)
        {
            _selectedClip = clip;
            SyncMediaFromTimelineClip(clip);
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

            if (_isPreviewRotating)
            {
                RotatePreviewSelection(e.DeltaX, e.DeltaY);
                return;
            }

            if (_previewResizeOriginalIds is not null)
            {
                ResizePreviewSelection(e.DeltaX, e.DeltaY);
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
            LedPreview.Resize(project.WallWidth, project.WallHeight);
            _selectedClip = FindFirstClip(project);
            _selectedTrack = _selectedClip is null ? project.Tracks.FirstOrDefault() : FindTrackContainingClip(_selectedClip);
            EnsureMediaTimelineClips();
            _loadedMediaPath = null;
            MediaImageOverlay.Visibility = Visibility.Collapsed;
            MediaVideoOverlay.Visibility = Visibility.Collapsed;
            MediaVideoOverlay.Stop();

            _playbackController.Duration = Math.Max(1, project.Duration);
            LoadAudioFromProject();
            TimeSlider.Maximum = _playbackController.Duration;
            ProjectDurationTextBox.Text = _playbackController.Duration.ToString("0.###", CultureInfo.InvariantCulture);
            TimelineControl.SetProject(project);
            TimelineControl.SelectClip(_selectedClip);
            TimelineControl.SelectTrack(_selectedTrack);
            TrackNameTextBox.Text = _selectedTrack?.Name ?? "Nouvelle piste";
            UpdatePropertyPanel(_selectedClip);
            RenderPreview(_playbackController.CurrentTime);
            UpdatePlaybackUi();
            UpdateProjectStatus();
        }

        private void LoadAudioFromProject()
        {
            _audioPlaybackService.Stop();
            _project.AudioVolume = Math.Clamp(_project.AudioVolume, 0, 1);
            _project.AudioFadeOutDuration = Math.Max(0, _project.AudioFadeOutDuration);
            ApplyAudioFade(_playbackController.CurrentTime);

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
               ?? _project.Tracks.FirstOrDefault(track => track.Name.StartsWith("Musique", StringComparison.OrdinalIgnoreCase));

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
            RippleCenterXTextBox.IsEnabled = enabled && !isAudioClip;
            RippleCenterYTextBox.IsEnabled = enabled && !isAudioClip;
            AddOrganicRippleButton.IsEnabled = enabled && !isAudioClip;
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
            RippleCenterXTextBox.Text = (clip.RippleCenterX ?? ((_project.WallWidth - 1) / 2.0)).ToString("0.##", CultureInfo.InvariantCulture);
            RippleCenterYTextBox.Text = (clip.RippleCenterY ?? ((_project.WallHeight - 1) / 2.0)).ToString("0.##", CultureInfo.InvariantCulture);
            SelectComboByTag(MovementEffectComboBox, clip.MovementEffect.ToString());
            UpdateShapeMotionUi(clip);
            _isUpdatingPropertyUi = false;
        }

        private void UpdateShapesTab(TimelineClip? clip)
        {
            _availableShapes = ShapeFactory.CreateForClip(clip, _project.WallWidth, _project.WallHeight);
            ApplyShapeFilter();
            ShapesTab.IsEnabled = true;
            ShapesClipTextBlock.Text = clip is null
                ? "Clique une forme pour créer une barre"
                : $"Formes pour le clip : {clip.Name}";
            UpdateShapeMotionUi(clip);
        }

        private void OnShapeFilterChanged(object sender, EventArgs e) => ApplyShapeFilter();

        private void ApplyShapeFilter()
        {
            if (ShapesItemsControl is null || ShapeSearchTextBox is null || ShapeCategoryComboBox is null)
            {
                return;
            }

            var query = ShapeSearchTextBox.Text.Trim();
            var category = ShapeCategoryComboBox.SelectedItem is ComboBoxItem { Tag: string tag } ? tag : "All";
            ShapesItemsControl.ItemsSource = _availableShapes
                .Where(shape => (category == "All" || string.Equals(shape.Category, category, StringComparison.OrdinalIgnoreCase))
                                && (string.IsNullOrWhiteSpace(query)
                                    || shape.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
                                    || shape.Category.Contains(query, StringComparison.OrdinalIgnoreCase)))
                .ToList();
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
            AudioFadeOutSlider.Value = Math.Clamp(_project.AudioFadeOutDuration, 0, AudioFadeOutSlider.Maximum);
            AudioFadeOutTextBlock.Text = _project.AudioFadeOutDuration <= 0
                ? "Aucun"
                : $"{_project.AudioFadeOutDuration:0.#} s";
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
                    ApplyAudioFade(_playbackController.CurrentTime);
                    _audioPlaybackService.Play(_playbackController.CurrentTime);
                    break;
                case PlaybackStatus.Paused:
                    _audioPlaybackService.Pause();
                    break;
                default:
                    _audioPlaybackService.Stop();
                    _audioPlaybackService.Volume = _project.AudioVolume;
                    break;
            }
        }

        private void ApplyAudioFade(double currentTime)
        {
            _audioPlaybackService.Volume = _project.AudioVolume * GetAudioFadeLevel(currentTime);
        }

        private double GetAudioFadeLevel(double currentTime)
        {
            var fadeDuration = Math.Max(0, _project.AudioFadeOutDuration);
            if (fadeDuration <= 0)
            {
                return 1;
            }

            var audioEnd = _project.Tracks
                .SelectMany(track => track.Clips)
                .Where(clip => clip.IsAudio)
                .Select(clip => clip.EndTime)
                .DefaultIfEmpty(_project.Duration)
                .Max();
            var fadeStart = Math.Max(0, audioEnd - fadeDuration);
            if (currentTime <= fadeStart)
            {
                return 1;
            }

            return Math.Clamp((audioEnd - currentTime) / Math.Max(0.001, audioEnd - fadeStart), 0, 1);
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

        private void EnsureProjectDurationIncludesMedia()
        {
            var end = _project.MediaOverlays.Select(media => media.StartTime + media.Duration).DefaultIfEmpty(1).Max();
            if (end > _project.Duration) SetProjectDuration(end, markDirty: false);
        }

        private Track GetOrCreateMediaTrack()
        {
            var track = _project.Tracks.FirstOrDefault(item => string.Equals(item.Name, "Médias", StringComparison.OrdinalIgnoreCase));
            if (track is not null) return track;
            track = new Track { Name = "Médias" };
            _project.Tracks.Add(track);
            return track;
        }

        private static TimelineClip CreateMediaTimelineClip(MediaOverlayClip media) => new()
        {
            Name = media.Name,
            StartTime = media.StartTime,
            Duration = media.Duration,
            IsMedia = true,
            IsHidden = media.IsHidden,
            MediaOverlayId = media.Id,
            Color = new RgbwColor(245, 155, 55, 0)
        };

        private void EnsureMediaTimelineClips()
        {
            if (_project.MediaOverlays.Count == 0) return;
            var track = GetOrCreateMediaTrack();
            foreach (var media in _project.MediaOverlays)
            {
                if (string.IsNullOrWhiteSpace(media.Id)) media.Id = Guid.NewGuid().ToString("N");
                if (!track.Clips.Any(clip => clip.MediaOverlayId == media.Id)) track.Clips.Add(CreateMediaTimelineClip(media));
            }
        }

        private void SyncMediaFromTimelineClip(TimelineClip clip)
        {
            if (!clip.IsMedia || clip.MediaOverlayId is null) return;
            var media = _project.MediaOverlays.FirstOrDefault(item => item.Id == clip.MediaOverlayId);
            if (media is null) return;
            media.Name = clip.Name;
            media.StartTime = clip.StartTime;
            media.Duration = clip.Duration;
            media.IsHidden = clip.IsHidden;
        }

        private void OnMediaDragStarted(object sender, MouseButtonEventArgs e)
        {
            if (_activeMediaOverlay is null) return;
            _mediaDragStart = e.GetPosition(LedPreview);
            _mediaDragOriginX = _activeMediaOverlay.OffsetX;
            _mediaDragOriginY = _activeMediaOverlay.OffsetY;
            MediaImageOverlay.CaptureMouse();
            e.Handled = true;
        }

        private void OnMediaDragMoved(object sender, MouseEventArgs e)
        {
            if (_mediaDragStart is null || _activeMediaOverlay is null || e.LeftButton != MouseButtonState.Pressed) return;
            var current = e.GetPosition(LedPreview);
            _activeMediaOverlay.OffsetX = _mediaDragOriginX + current.X - _mediaDragStart.Value.X;
            _activeMediaOverlay.OffsetY = _mediaDragOriginY + current.Y - _mediaDragStart.Value.Y;
            ApplyMediaTranslation(_activeMediaOverlay);
            MarkDirty();
            e.Handled = true;
        }

        private void OnMediaDragCompleted(object sender, MouseButtonEventArgs e)
        {
            _mediaDragStart = null;
            MediaImageOverlay.ReleaseMouseCapture();
            e.Handled = true;
        }

        private void ApplyMediaTranslation(MediaOverlayClip media)
        {
            var transform = new TranslateTransform(media.OffsetX, media.OffsetY);
            MediaImageOverlay.RenderTransform = transform;
            MediaVideoOverlay.RenderTransform = transform;
        }

        private void UpdateMediaOverlay(double currentTime)
        {
            var active = _project.MediaOverlays.LastOrDefault(media => !media.IsHidden &&
                currentTime >= media.StartTime && currentTime <= media.StartTime + media.Duration && File.Exists(media.FilePath));
            if (active is null)
            {
                _activeMediaOverlay = null;
                MediaImageOverlay.Visibility = Visibility.Collapsed;
                MediaVideoOverlay.Visibility = Visibility.Collapsed;
                MediaVideoOverlay.Pause();
                return;
            }

            _activeMediaOverlay = active;
            ApplyMediaTranslation(active);

            if (!string.Equals(_loadedMediaPath, active.FilePath, StringComparison.OrdinalIgnoreCase)) LoadMediaOverlay(active.FilePath);
            var localTime = Math.Max(0, currentTime - active.StartTime);
            MediaImageOverlay.Opacity = active.Opacity;
            MediaVideoOverlay.Opacity = active.Opacity;

            if (IsVideoFile(active.FilePath))
            {
                MediaImageOverlay.Visibility = Visibility.Collapsed;
                MediaVideoOverlay.Visibility = Visibility.Visible;
                var wanted = TimeSpan.FromSeconds(localTime);
                if (Math.Abs((MediaVideoOverlay.Position - wanted).TotalMilliseconds) > 180) MediaVideoOverlay.Position = wanted;
                if (_playbackController.Status == PlaybackStatus.Playing) MediaVideoOverlay.Play(); else MediaVideoOverlay.Pause();
                return;
            }

            MediaVideoOverlay.Visibility = Visibility.Collapsed;
            MediaImageOverlay.Visibility = Visibility.Visible;
            if (_gifFrames.Count > 1)
            {
                var total = _gifFrameEnds[^1];
                var gifTime = total <= 0 ? 0 : localTime % total;
                var index = _gifFrameEnds.FindIndex(end => gifTime < end);
                MediaImageOverlay.Source = _gifFrames[Math.Max(0, index)];
            }
        }

        private void LoadMediaOverlay(string path)
        {
            _loadedMediaPath = path;
            _gifFrames.Clear();
            _gifFrameEnds.Clear();
            MediaVideoOverlay.Stop();
            if (IsVideoFile(path))
            {
                MediaVideoOverlay.Source = new Uri(path, UriKind.Absolute);
                return;
            }

            var decoder = BitmapDecoder.Create(new Uri(path, UriKind.Absolute), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            var elapsed = 0.0;
            foreach (var frame in decoder.Frames)
            {
                _gifFrames.Add(frame);
                var delay = 0.1;
                if (frame.Metadata is BitmapMetadata metadata && metadata.GetQuery("/grctlext/Delay") is ushort centiseconds)
                    delay = Math.Max(0.02, centiseconds / 100.0);
                elapsed += delay;
                _gifFrameEnds.Add(elapsed);
            }
            MediaImageOverlay.Source = _gifFrames.FirstOrDefault();
        }

        private static bool IsVideoFile(string path)
            => new[] { ".mp4", ".wmv", ".avi" }.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

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

        private void ShowActionFeedback(string message)
        {
            ActionFeedbackItem.Content = $"✓ {message}";
        }

        private void RenderPreview(double currentTime)
        {
            var state = _playbackEngine.ComputeState(currentTime, _project);
            if (state.Entities.Count == 0)
            {
                LedPreview.Clear();
                UpdatePreviewSelectionOverlay();
                return;
            }

            LedPreview.RenderState(state);
            UpdatePreviewSelectionOverlay();
        }

        private void UpdatePreviewSelectionOverlay()
        {
            var entityIds = _selectedClip is { IsAudio: false, IsMedia: false, Target.Type: TargetType.Selection }
                ? _selectedClip.Target.EntityIds
                : null;
            LedPreview.ShowSelection(entityIds, IsEditingResize(), IsEditingRotate());
        }

        // Les identifiants 1 à 99 sont réservés aux appareils hors mur (projecteur statique,
        // lyres motorisées) : le mur de LED lui-même commence à 100, par convention posée dans
        // la documentation d'architecture du projet.
        private const int WallEntityIdStart = 100;

        // Chaque bande physique fait 259 LED au total, mais seules 256 sont visibles : la bande
        // est pliée en U et forme 2 colonnes de 128 LED visibles côte à côte (64 bandes x 2 =
        // 128 colonnes = la largeur réelle du mur visible). Dans l'ordre des identifiants DMX
        // d'une bande (0 à 258, relatif au début de la bande) :
        //   - position 0            : invisible (fixation au cadre, en bas, colonne A)
        //   - positions 1..128       : visibles, colonne A, montantes (bas -> haut)
        //   - position 129           : invisible (pli de la bande, en haut)
        //   - positions 130..257     : visibles, colonne B, descendantes (haut -> bas)
        //   - position 258           : invisible (fixation au cadre, en bas, colonne B)
        private const int BandLength = 259;
        private const int VisibleSegmentLength = 128;

        /// <summary>
        /// Charge patch.json et reconstruit, pour le mur, la grille d'identifiants réels 128x128
        /// (largeur x hauteur) attendue par <see cref="ShowPlaybackEngine"/> : chaque bande
        /// physique de 259 LED est "dépliée" en ses 2 colonnes visibles de 128 LED, dans le bon
        /// sens (montante puis descendante), en ignorant les 3 LED invisibles de fixation/pli de
        /// chaque bande. Sans ce fichier (poste de développement sans copie du patch, par
        /// exemple), l'appli continue de fonctionner en local avec les ID de grille bruts.
        /// </summary>
        private static IReadOnlyList<int>? LoadRealEntityIds()
        {
            try
            {
                string patchPath = Path.GetFullPath(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "patch.json"));

                var patchService = new PatchService(new JsonPatchLoader());
                patchService.LoadPatch(patchPath);

                var ids = new List<int>();
                foreach (var controller in patchService.Controllers)
                {
                    foreach (int universe in patchService.GetUniverses(controller.Id))
                    {
                        ids.AddRange(patchService.GetEntityIds(controller.Id, universe)
                            .Where(id => id >= WallEntityIdStart));
                    }
                }

                ids.Sort();

                if (ids.Count == 0 || ids.Count % BandLength != 0)
                {
                    return ids; // structure inattendue : pas de dépliage possible, on renvoie tel quel
                }

                int bandCount = ids.Count / BandLength;
                int wallWidth = bandCount * 2; // colonne A + colonne B par bande
                var grid = new int[wallWidth * VisibleSegmentLength];

                for (int band = 0; band < bandCount; band++)
                {
                    int bandStart = band * BandLength;
                    int columnA = band * 2;
                    int columnB = (band * 2) + 1;

                    // Colonne A, montante : position relative 1..128 (0 = invisible, ignorée).
                    for (int j = 0; j < VisibleSegmentLength; j++)
                    {
                        int row = (VisibleSegmentLength - 1) - j; // j=0 (juste après le bas) -> row du bas
                        grid[(row * wallWidth) + columnA] = ids[bandStart + 1 + j];
                    }

                    // Colonne B, descendante : position relative 130..257 (129 = pli, invisible ;
                    // 258 = fixation basse, invisible, jamais lue ici).
                    for (int k = 0; k < VisibleSegmentLength; k++)
                    {
                        int row = k; // k=0 (juste après le pli) -> row du haut
                        grid[(row * wallWidth) + columnB] = ids[bandStart + 130 + k];
                    }
                }

                return grid;
            }
            catch
            {
                return null;
            }
        }

        private void PublishCurrentState(double currentTime)
        {
            try
            {
                var gridState = _playbackEngine.ComputeState(currentTime, _project);
                var networkState = _playbackEngine.MapToRealEntityIds(gridState);
                _statePublisher.Publish(networkState);
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
                    new Track { Name = "Musique · Master" },
                    new Track { Name = "Piste 1 · Violon" },
                    new Track { Name = "Piste 2 · Chant" },
                    new Track { Name = "Piste 3 · Batterie" },
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

        private static TimelineClip DuplicateClip(TimelineClip source)
        {
            return new TimelineClip
            {
                Name = $"{source.Name} (copie)",
                StartTime = source.EndTime + 0.1,
                Duration = source.Duration,
                EffectType = source.EffectType,
                IsAudio = source.IsAudio,
                IsHidden = false,
                IsMedia = source.IsMedia,
                MediaOverlayId = source.MediaOverlayId,
                Target = new TargetSelection
                {
                    Type = source.Target.Type,
                    TrackName = source.Target.TrackName,
                    EntityIds = source.Target.EntityIds.ToList()
                },
                Color = source.Color,
                Intensity = source.Intensity,
                Speed = source.Speed,
                RippleCenterX = source.RippleCenterX,
                RippleCenterY = source.RippleCenterY,
                MovementEffect = source.MovementEffect,
                MovementOffsetX = source.MovementOffsetX,
                MovementOffsetY = source.MovementOffsetY,
                RotationDegrees = source.RotationDegrees,
                IsMotionDraft = source.IsMotionDraft,
                MovementKeyframes = source.MovementKeyframes
                    .Select(keyframe => new MovementKeyframe
                    {
                        Time = keyframe.Time,
                        OffsetX = keyframe.OffsetX,
                        OffsetY = keyframe.OffsetY
                    })
                    .ToList()
            };
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
            if (_selectedTrack is not null && !_selectedTrack.Clips.Any(clip => clip.IsAudio || clip.IsMedia))
            {
                return _selectedTrack;
            }

            var selectedTrack = _selectedClip is null ? null : FindTrackContainingClip(_selectedClip);
            if (selectedTrack is not null && !selectedTrack.Clips.Any(clip => clip.IsAudio || clip.IsMedia))
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
            if (entityIds.Count == 0)
            {
                return null;
            }

            var minX = _project.WallWidth;
            var maxX = -1;
            var minY = _project.WallHeight;
            var maxY = -1;
            foreach (var id in entityIds)
            {
                var x = id % _project.WallWidth;
                var y = id / _project.WallWidth;
                if (id < 0 || y < 0 || y >= _project.WallHeight) continue;
                minX = Math.Min(minX, x);
                maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y);
                maxY = Math.Max(maxY, y);
            }
            if (maxX < minX || maxY < minY) return null;

            var boundedDeltaX = Math.Clamp(deltaX, -minX, _project.WallWidth - 1 - maxX);
            var boundedDeltaY = Math.Clamp(deltaY, -minY, _project.WallHeight - 1 - maxY);

            if (boundedDeltaX == 0 && boundedDeltaY == 0)
            {
                return null;
            }

            var moved = new List<int>(entityIds.Count);
            foreach (var id in entityIds)
            {
                var x = id % _project.WallWidth;
                var y = id / _project.WallWidth;
                if (id >= 0 && y >= 0 && y < _project.WallHeight)
                {
                    moved.Add(((y + boundedDeltaY) * _project.WallWidth) + x + boundedDeltaX);
                }
            }
            return moved;
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

        private bool IsEditingResize()
            => ShapeEditModeComboBox.SelectedItem is ComboBoxItem { Tag: string tag }
               && string.Equals(tag, "Resize", StringComparison.Ordinal);

        private bool IsEditingRotate()
            => ShapeEditModeComboBox.SelectedItem is ComboBoxItem { Tag: string tag }
               && string.Equals(tag, "Rotate", StringComparison.Ordinal);

        private void UpdateShapeMotionUi(TimelineClip? clip)
        {
            if (clip is null)
            {
                ShapeWorkflowTextBlock.Text = "Clique une forme pour créer une barre dans la timeline";
                MovementDurationTextBlock.Text = "durée 0.000s";
                MovementOffsetTextBlock.Text = "arrivée dx 0 / dy 0";
                RotationAngleTextBlock.Text = "0°";
                return;
            }

            var hasShape = clip.Target.Type == TargetType.Selection && clip.Target.EntityIds.Count > 0;
            var mode = IsEditingRotate() ? "Rotation" : IsEditingResize() ? "Taille" : IsEditingMovementTarget() ? "Arrivée" : "Départ";
            var captureState = clip.IsMotionDraft ? "prise en cours" : "confirmé";
            ShapeWorkflowTextBlock.Text = hasShape
                ? $"{mode} éditable - {captureState}"
                : "Choisis une forme pour créer sa barre";
            MovementDurationTextBlock.Text = $"durée {clip.Duration:0.000}s";
            MovementOffsetTextBlock.Text = clip.MovementKeyframes.Count > 0
                ? $"{clip.MovementKeyframes.Count} points enregistrés"
                : $"arrivée dx {clip.MovementOffsetX} / dy {clip.MovementOffsetY}";
            RotationAngleTextBlock.Text = $"{clip.RotationDegrees:0.#}°";
        }

        private (int MinX, int MaxX, int MinY, int MaxY)? GetSelectionBounds(IReadOnlyCollection<int> entityIds)
        {
            if (entityIds.Count == 0)
            {
                return null;
            }

            var minX = _project.WallWidth;
            var maxX = -1;
            var minY = _project.WallHeight;
            var maxY = -1;
            foreach (var id in entityIds)
            {
                var x = id % _project.WallWidth;
                var y = id / _project.WallWidth;
                if (id < 0 || y < 0 || y >= _project.WallHeight) continue;
                minX = Math.Min(minX, x);
                maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y);
                maxY = Math.Max(maxY, y);
            }

            return maxX < minX || maxY < minY ? null : (minX, maxX, minY, maxY);
        }

        private void BeginPreviewResize(int pixelX, int pixelY)
        {
            if (_selectedClip?.Target.Type != TargetType.Selection)
            {
                return;
            }

            var bounds = GetSelectionBounds(_selectedClip.Target.EntityIds);
            if (bounds is null)
            {
                return;
            }

            _previewResizeOriginalIds = _selectedClip.Target.EntityIds.ToList();
            _previewResizeOriginBounds = bounds;
            _previewResizeDeltaX = 0;
            _previewResizeDeltaY = 0;

            const int handleTolerance = 3;
            var edges = PreviewResizeEdges.None;
            if (Math.Abs(pixelX - bounds.Value.MinX) <= handleTolerance) edges |= PreviewResizeEdges.Left;
            else if (Math.Abs(pixelX - bounds.Value.MaxX) <= handleTolerance) edges |= PreviewResizeEdges.Right;
            if (Math.Abs(pixelY - bounds.Value.MinY) <= handleTolerance) edges |= PreviewResizeEdges.Top;
            else if (Math.Abs(pixelY - bounds.Value.MaxY) <= handleTolerance) edges |= PreviewResizeEdges.Bottom;

            _previewResizeEdges = edges == PreviewResizeEdges.None
                ? PreviewResizeEdges.Right | PreviewResizeEdges.Bottom
                : edges;
        }

        private void ResizePreviewSelection(int deltaX, int deltaY)
        {
            if (_selectedClip is null || _previewResizeOriginalIds is null || _previewResizeOriginBounds is null)
            {
                return;
            }

            _previewResizeDeltaX += deltaX;
            _previewResizeDeltaY += deltaY;
            var origin = _previewResizeOriginBounds.Value;
            var minX = origin.MinX;
            var maxX = origin.MaxX;
            var minY = origin.MinY;
            var maxY = origin.MaxY;

            if (_previewResizeEdges.HasFlag(PreviewResizeEdges.Left))
                minX = Math.Clamp(origin.MinX + _previewResizeDeltaX, 0, maxX);
            if (_previewResizeEdges.HasFlag(PreviewResizeEdges.Right))
                maxX = Math.Clamp(origin.MaxX + _previewResizeDeltaX, minX, _project.WallWidth - 1);
            if (_previewResizeEdges.HasFlag(PreviewResizeEdges.Top))
                minY = Math.Clamp(origin.MinY + _previewResizeDeltaY, 0, maxY);
            if (_previewResizeEdges.HasFlag(PreviewResizeEdges.Bottom))
                maxY = Math.Clamp(origin.MaxY + _previewResizeDeltaY, minY, _project.WallHeight - 1);

            _selectedClip.Target.EntityIds = SelectionTransformService.ResizeToBounds(
                _previewResizeOriginalIds,
                minX,
                maxX,
                minY,
                maxY,
                _project.WallWidth,
                _project.WallHeight);

            ClampMovementTarget(_selectedClip);
            MarkDirty();
            UpdateShapeMotionUi(_selectedClip);
            RenderPreview(_playbackController.CurrentTime);
            UpdateMediaOverlay(_playbackController.CurrentTime);
        }

        private void OnPreviewToolClick(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: string mode })
            {
                SelectComboByTag(ShapeEditModeComboBox, mode);
                UpdateShapeMotionUi(_selectedClip);
                RenderPreview(_playbackController.CurrentTime);
            }
        }

        private void OnAddTrackClick(object sender, RoutedEventArgs e)
        {
            var requestedName = TrackNameTextBox.Text.Trim();
            var baseName = string.IsNullOrWhiteSpace(requestedName) ? "Nouvelle piste" : requestedName;
            var name = baseName;
            var suffix = 2;
            while (_project.Tracks.Any(track => string.Equals(track.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                name = $"{baseName} {suffix++}";
            }

            var track = new Track { Name = name };
            _project.Tracks.Add(track);
            _selectedTrack = track;
            TrackNameTextBox.Text = name;
            TimelineControl.SetProject(_project);
            TimelineControl.SelectTrack(track);
            MarkDirty();
            ShowActionFeedback($"Piste « {name} » ajoutée");
        }

        private void OnRenameTrackClick(object sender, RoutedEventArgs e)
        {
            var track = GetSelectedTrack();
            var name = TrackNameTextBox.Text.Trim();
            if (track is null || string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(this, "Sélectionne un clip de la piste à renommer et saisis un nom.", "Renommer une piste", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            track.Name = name;
            TimelineControl.Redraw();
            MarkDirty();
            ShowActionFeedback($"Piste renommée « {name} »");
        }

        private void OnDeleteTrackClick(object sender, RoutedEventArgs e)
        {
            var track = GetSelectedTrack();
            if (track is null)
            {
                MessageBox.Show(this, "Sélectionne d'abord un clip de la piste à supprimer.", "Supprimer une piste", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var answer = MessageBox.Show(this, $"Supprimer la piste « {track.Name} » et ses {track.Clips.Count} clip(s) ?", "Supprimer une piste", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes) return;

            _project.Tracks.Remove(track);
            _selectedClip = FindFirstClip(_project);
            _selectedTrack = _selectedClip is null ? _project.Tracks.FirstOrDefault() : FindTrackContainingClip(_selectedClip);
            TimelineControl.SetProject(_project);
            TimelineControl.SelectClip(_selectedClip);
            UpdatePropertyPanel(_selectedClip);
            RenderPreview(_playbackController.CurrentTime);
            MarkDirty();
            ShowActionFeedback("Piste supprimée");
        }

        private void OnDuplicateClipToolbarClick(object sender, RoutedEventArgs e)
        {
            if (_selectedClip is not null)
            {
                OnTimelineClipActionRequested(this, new ClipActionEventArgs(_selectedClip, ClipAction.Duplicate));
            }
        }

        private void OnInsertClipBeforeClick(object sender, RoutedEventArgs e) => InsertClipRelative(before: true);

        private void OnInsertClipAfterClick(object sender, RoutedEventArgs e) => InsertClipRelative(before: false);

        private void InsertClipRelative(bool before)
        {
            if (_selectedClip is null || FindTrackContainingClip(_selectedClip) is not { } track)
            {
                MessageBox.Show(this, "Sélectionne d'abord un clip dans la timeline.", "Insérer un clip", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var source = _selectedClip;
            var inserted = DuplicateClip(source);
            inserted.Name = $"{source.Name} ({(before ? "avant" : "après")})";
            inserted.StartTime = before
                ? Math.Max(0, source.StartTime - source.Duration - 0.1)
                : source.EndTime + 0.1;
            track.Clips.Add(inserted);
            _selectedClip = inserted;
            EnsureProjectDurationIncludesClips();
            TimelineControl.SetProject(_project);
            TimelineControl.SelectClip(inserted);
            UpdatePropertyPanel(inserted);
            MarkDirty();
            ShowActionFeedback($"Clip inséré {(before ? "avant" : "après")}");
        }

        private Track? GetSelectedTrack()
            => _selectedTrack ?? (_selectedClip is null ? _project.Tracks.LastOrDefault() : FindTrackContainingClip(_selectedClip));

        private void EndPreviewResize()
        {
            _previewResizeOriginalIds = null;
            _previewResizeOriginBounds = null;
            _previewResizeDeltaX = 0;
            _previewResizeDeltaY = 0;
            _previewResizeEdges = PreviewResizeEdges.None;
            TimelineControl.Redraw();
            UpdateShapeMotionUi(_selectedClip);
            RenderPreview(_playbackController.CurrentTime);
        }

        private void BeginPreviewRotation(int pixelX, int pixelY)
        {
            if (_selectedClip?.Target.Type != TargetType.Selection)
            {
                return;
            }

            var bounds = GetSelectionBounds(_selectedClip.Target.EntityIds);
            if (bounds is null)
            {
                return;
            }

            _isPreviewRotating = true;
            _previewRotateCurrentX = pixelX;
            _previewRotateCurrentY = pixelY;
            _previewRotateCenterX = (bounds.Value.MinX + bounds.Value.MaxX) / 2.0;
            _previewRotateCenterY = (bounds.Value.MinY + bounds.Value.MaxY) / 2.0;
            _previewRotateStartAngle = Math.Atan2(pixelY - _previewRotateCenterY, pixelX - _previewRotateCenterX);
            _previewRotateOriginalDegrees = _selectedClip.RotationDegrees;
        }

        private void RotatePreviewSelection(int deltaX, int deltaY)
        {
            if (!_isPreviewRotating || _selectedClip is null)
            {
                return;
            }

            _previewRotateCurrentX += deltaX;
            _previewRotateCurrentY += deltaY;
            var currentAngle = Math.Atan2(
                _previewRotateCurrentY - _previewRotateCenterY,
                _previewRotateCurrentX - _previewRotateCenterX);
            var angleDelta = (currentAngle - _previewRotateStartAngle) * 180.0 / Math.PI;
            var rotation = _previewRotateOriginalDegrees + angleDelta;
            _selectedClip.RotationDegrees = ((rotation + 180) % 360 + 360) % 360 - 180;

            MarkDirty();
            UpdateShapeMotionUi(_selectedClip);
            RenderPreview(_playbackController.CurrentTime);
        }

        private void EndPreviewRotation()
        {
            _isPreviewRotating = false;
            TimelineControl.Redraw();
            UpdateShapeMotionUi(_selectedClip);
            RenderPreview(_playbackController.CurrentTime);
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


