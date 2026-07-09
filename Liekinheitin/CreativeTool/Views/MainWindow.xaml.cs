using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Liekinheitin.Application.Interfaces;
using Liekinheitin.CreativeTool.Models;
using Liekinheitin.CreativeTool.Services;
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

            MarkDirty();
            TimelineControl.Redraw();
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

            if (clip is null)
            {
                ClipNameTextBox.Text = string.Empty;
                ColorPickerControl.SetColor(RgbwColor.White);
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
            _isUpdatingPropertyUi = false;
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
            var activeClip = FindActiveClip(currentTime);
            if (activeClip is null)
            {
                LedPreview.Clear();
                return;
            }

            if (activeClip.EffectType == EffectType.Wave)
            {
                LedPreview.RenderWave(currentTime * activeClip.Speed);
                return;
            }

            var level = activeClip.EffectType == EffectType.Fade
                ? FadeLevel(currentTime - activeClip.StartTime, activeClip.Duration)
                : 1;
            var color = ScaleColor(activeClip.Color.ToColor(), level * activeClip.Intensity);
            LedPreview.Fill(color);
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
                        Name = "Accent",
                        Clips =
                        {
                            new TimelineClip
                            {
                                Name = "Fade blanc",
                                StartTime = 20,
                                Duration = 6,
                                EffectType = EffectType.Fade,
                                Color = new RgbwColor(180, 180, 180, 40),
                                Intensity = 1
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


