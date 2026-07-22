using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Liekinheitin.CreativeTool.Models;

namespace Liekinheitin.CreativeTool.Views
{
    /// <summary>
    /// Minimal visual timeline with tracks, clips, ruler and playhead.
    /// </summary>
    public partial class TimelineView : UserControl
    {
        private const double LeftGutter = 140;
        private const double TopRulerHeight = 32;
        private const double TrackHeight = 50;
        private const double TrackGap = 6;
        private const double ClipHeight = 38;
        private const double MinClipWidth = 34;
        private const double ResizeHandleWidth = 8;
        private const double MinClipDuration = 0.05;
        private const double PixelsPerSecond = 36;

        private ShowProject? _project;
        private TimelineClip? _selectedClip;
        private Track? _selectedTrack;
        private double _playheadTime;
        private Line? _playheadLine;
        private TimelineClip? _resizingClip;
        private ResizeEdge _resizeEdge;
        private double _resizeStartX;
        private double _resizeOriginalStart;
        private double _resizeOriginalDuration;
        private TimelineClip? _draggingClip;
        private double _dragStartX;
        private double _dragOriginalStart;
        private readonly HashSet<Track> _expandedTracks = new();

        public TimelineView()
        {
            InitializeComponent();
            SizeChanged += (_, _) => Redraw();
        }

        public event EventHandler<TimelineClip>? ClipSelected;

        public event EventHandler<TimelineClip>? ClipChanged;

        public event EventHandler<ClipActionEventArgs>? ClipActionRequested;

        public event EventHandler<Track>? TrackChanged;

        public event EventHandler<Track>? TrackSelected;

        public void SetProject(ShowProject project)
        {
            _project = project;
            Redraw();
        }

        public void SetPlayhead(double currentTime)
        {
            _playheadTime = Math.Max(0, currentTime);
            UpdatePlayhead();
        }

        public void SelectClip(TimelineClip? clip)
        {
            _selectedClip = clip;
            _selectedTrack = clip is null ? _selectedTrack : _project?.Tracks.FirstOrDefault(track => track.Clips.Contains(clip));
            Redraw();
        }

        public void SelectTrack(Track? track)
        {
            _selectedTrack = track;
            Redraw();
        }

        public void Redraw()
        {
            TimelineCanvas.Children.Clear();
            _playheadLine = null;

            if (_project is null)
            {
                DrawEmptyState();
                return;
            }

            var duration = Math.Max(1, _project.Duration);
            var tracks = _project.Tracks;
            var width = Math.Max(ActualWidth - 24, LeftGutter + (duration * PixelsPerSecond) + 40);
            var tracksHeight = tracks.Sum(track => GetTrackDisplayHeight(track) + TrackGap);
            var height = Math.Max(ActualHeight - 24, TopRulerHeight + tracksHeight + 20);

            TimelineCanvas.MinWidth = width;
            TimelineCanvas.MinHeight = height;

            DrawRuler(duration, width);
            DrawTracks(tracks, duration, width);
            DrawPlayhead(height);
        }

        private void DrawEmptyState()
        {
            TimelineCanvas.MinWidth = Math.Max(ActualWidth, 400);
            TimelineCanvas.MinHeight = Math.Max(ActualHeight, 140);
            var text = new TextBlock
            {
                Foreground = Brushes.Gray,
                Text = "Aucun projet charge"
            };
            Canvas.SetLeft(text, 16);
            Canvas.SetTop(text, 16);
            TimelineCanvas.Children.Add(text);
        }

        private void DrawRuler(double duration, double width)
        {
            AddLine(LeftGutter, TopRulerHeight - 1, width, TopRulerHeight - 1, "#3A3A3A", 1);

            var seconds = (int)Math.Ceiling(duration);
            for (var second = 0; second <= seconds; second++)
            {
                var x = TimeToX(second);
                var isMajor = second % 5 == 0;
                AddLine(x, isMajor ? 4 : 14, x, TopRulerHeight, isMajor ? "#707070" : "#444444", 1);

                if (isMajor)
                {
                    var label = new TextBlock
                    {
                        Foreground = Brushes.LightGray,
                        FontSize = 11,
                        Text = $"{second}s"
                    };
                    Canvas.SetLeft(label, x + 4);
                    Canvas.SetTop(label, 4);
                    TimelineCanvas.Children.Add(label);
                }
            }
        }

        private void DrawTracks(IReadOnlyList<Track> tracks, double duration, double width)
        {
            var y = TopRulerHeight;
            for (var trackIndex = 0; trackIndex < tracks.Count; trackIndex++)
            {
                var track = tracks[trackIndex];
                var trackDisplayHeight = GetTrackDisplayHeight(track);

                var label = new TextBlock
                {
                    Foreground = track.IsMuted ? Brushes.Gray : Brushes.LightGray,
                    FontWeight = FontWeights.SemiBold,
                    Text = track.Name,
                    Width = 62,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    ToolTip = track.Name,
                    Tag = track,
                    Cursor = Cursors.Hand
                };
                label.MouseLeftButtonDown += OnTrackSelected;
                Canvas.SetLeft(label, 10);
                Canvas.SetTop(label, y + 13);
                TimelineCanvas.Children.Add(label);

                var muteButton = new Button
                {
                    Width = 24,
                    Height = 22,
                    Padding = new Thickness(0),
                    Content = "M",
                    Foreground = track.IsMuted ? Brushes.White : new SolidColorBrush(Color.FromRgb(98, 230, 255)),
                    Background = new SolidColorBrush(track.IsMuted ? Color.FromRgb(184, 56, 82) : Color.FromRgb(24, 38, 50)),
                    BorderBrush = new SolidColorBrush(track.IsMuted ? Color.FromRgb(255, 102, 132) : Color.FromRgb(49, 91, 108)),
                    Tag = track,
                    ToolTip = track.IsMuted ? "Réactiver la piste" : "Couper la piste"
                };
                muteButton.Click += OnTrackMuteClick;
                Canvas.SetLeft(muteButton, 62);
                Canvas.SetTop(muteButton, y + 10);
                TimelineCanvas.Children.Add(muteButton);

                var zoomButton = new Button
                {
                    Width = 22,
                    Height = 22,
                    Padding = new Thickness(0),
                    Content = _expandedTracks.Contains(track) ? "−" : "+",
                    ToolTip = _expandedTracks.Contains(track) ? "Replier cette piste" : "Agrandir cette piste",
                    Tag = track
                };
                zoomButton.Click += OnTrackZoomClick;
                Canvas.SetLeft(zoomButton, LeftGutter - 28);
                Canvas.SetTop(zoomButton, y + 10);
                TimelineCanvas.Children.Add(zoomButton);

                var lane = new Rectangle
                {
                    Width = Math.Max(0, width - LeftGutter - 16),
                    Height = trackDisplayHeight,
                    Fill = new SolidColorBrush(Color.FromRgb(13, 23, 35)),
                    Stroke = new SolidColorBrush(Color.FromRgb(31, 55, 73)),
                    StrokeThickness = 1
                };
                if (ReferenceEquals(track, _selectedTrack))
                {
                    lane.Stroke = new SolidColorBrush(Color.FromRgb(98, 230, 255));
                    lane.StrokeThickness = 2;
                }
                lane.Tag = track;
                lane.MouseLeftButtonDown += OnTrackSelected;
                Canvas.SetLeft(lane, LeftGutter);
                Canvas.SetTop(lane, y);
                TimelineCanvas.Children.Add(lane);

                var clipLanes = _expandedTracks.Contains(track) ? AssignClipLanes(track) : null;
                foreach (var clip in track.Clips
                             .OrderBy(c => ReferenceEquals(c, _selectedClip) ? 1 : 0)
                             .ThenBy(c => c.StartTime))
                {
                    var laneIndex = clipLanes is not null && clipLanes.TryGetValue(clip, out var value) ? value : 0;
                    DrawClip(clip, y + (laneIndex * (ClipHeight + 4)));
                }

                y += trackDisplayHeight + TrackGap;
            }
        }

        private void DrawClip(TimelineClip clip, double trackY)
        {
            var x = TimeToX(clip.StartTime);
            var width = Math.Max(MinClipWidth, clip.Duration * PixelsPerSecond);
            var isSelected = ReferenceEquals(clip, _selectedClip);
            var clipType = clip.IsAudio ? "Audio" : clip.EffectType.ToString();
            var hiddenPrefix = clip.IsHidden ? "[CACHÉ] " : string.Empty;
            var label = $"{hiddenPrefix}{clip.Name}  {clip.StartTime:0.#}-{clip.EndTime:0.#}s";

            var border = new Border
            {
                Width = width,
                Height = ClipHeight,
                Background = new SolidColorBrush(GetClipColor(clip)),
                BorderBrush = isSelected ? Brushes.White : new SolidColorBrush(Color.FromRgb(90, 90, 90)),
                BorderThickness = isSelected ? new Thickness(2) : new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Opacity = clip.IsHidden ? 0.38 : 1,
                Tag = clip,
                ToolTip = $"{label} - {clipType}"
            };
            border.ContextMenu = CreateClipContextMenu(clip);
            border.MouseLeftButtonDown += OnClipMouseLeftButtonDown;
            border.MouseMove += OnClipMouseMove;
            border.MouseLeave += OnClipMouseLeave;

            var text = new TextBlock
            {
                Foreground = Brushes.White,
                FontSize = 12,
                Margin = new Thickness(8, 5, 8, 0),
                Text = label,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            if (clip.IsAudio)
            {
                var audioContent = new Grid();
                audioContent.Children.Add(text);
                var waveform = new Canvas { Height = 12, Margin = new Thickness(7, 19, 7, 2), Opacity = 0.72 };
                var barCount = Math.Max(4, Math.Min(48, (int)(width / 5)));
                for (var index = 0; index < barCount; index++)
                {
                    var level = 3 + (Math.Abs(Math.Sin((index * 1.73) + clip.Duration)) * 9);
                    var bar = new Rectangle
                    {
                        Width = 2,
                        Height = level,
                        RadiusX = 1,
                        RadiusY = 1,
                        Fill = new SolidColorBrush(Color.FromRgb(150, 255, 223))
                    };
                    Canvas.SetLeft(bar, index * 5);
                    Canvas.SetTop(bar, (12 - level) / 2);
                    waveform.Children.Add(bar);
                }
                audioContent.Children.Add(waveform);
                border.Child = audioContent;
            }
            else
            {
                border.Child = text;
            }

            Canvas.SetLeft(border, x);
            Canvas.SetTop(border, trackY + 5);
            TimelineCanvas.Children.Add(border);
        }

        private void OnTrackZoomClick(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: Track track }) return;

            if (!_expandedTracks.Add(track)) _expandedTracks.Remove(track);
            Redraw();
            e.Handled = true;
        }

        private void OnTrackMuteClick(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: Track track }) return;
            track.IsMuted = !track.IsMuted;
            TrackChanged?.Invoke(this, track);
            Redraw();
            e.Handled = true;
        }

        private void OnTrackSelected(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: Track track }) return;
            _selectedTrack = track;
            TrackSelected?.Invoke(this, track);
            Redraw();
            e.Handled = true;
        }

        private double GetTrackDisplayHeight(Track track)
        {
            if (!_expandedTracks.Contains(track)) return TrackHeight;
            var laneCount = AssignClipLanes(track).Values.DefaultIfEmpty(0).Max() + 1;
            return Math.Max(104, 10 + (laneCount * (ClipHeight + 4)));
        }

        private static Dictionary<TimelineClip, int> AssignClipLanes(Track track)
        {
            var result = new Dictionary<TimelineClip, int>();
            var laneEnds = new List<double>();
            foreach (var clip in track.Clips.OrderBy(item => item.StartTime))
            {
                var laneIndex = laneEnds.FindIndex(end => end <= clip.StartTime);
                var displayedEnd = clip.StartTime + Math.Max(clip.Duration, MinClipWidth / PixelsPerSecond);
                if (laneIndex < 0)
                {
                    laneIndex = laneEnds.Count;
                    laneEnds.Add(displayedEnd);
                }
                else
                {
                    laneEnds[laneIndex] = displayedEnd;
                }

                result[clip] = laneIndex;
            }

            return result;
        }

        private ContextMenu CreateClipContextMenu(TimelineClip clip)
        {
            var menu = new ContextMenu();
            var track = _project?.Tracks.FirstOrDefault(candidate => candidate.Clips.Contains(clip));
            if (track is not null && track.Clips.Count > 1)
            {
                var picker = new MenuItem { Header = $"Choisir dans « {track.Name} »" };
                foreach (var candidate in track.Clips.OrderBy(candidate => candidate.StartTime))
                {
                    var state = candidate.IsHidden ? " [caché]" : string.Empty;
                    var item = new MenuItem
                    {
                        Header = $"{candidate.StartTime:0.##}s — {candidate.Name}{state}",
                        IsChecked = ReferenceEquals(candidate, _selectedClip),
                        Tag = candidate
                    };
                    item.Click += (_, _) => SelectClipForEditing(candidate);
                    picker.Items.Add(item);
                }

                menu.Items.Add(picker);
                menu.Items.Add(new Separator());
            }

            menu.Items.Add(CreateActionMenuItem("Modifier", ClipAction.Edit, clip));
            menu.Items.Add(CreateActionMenuItem("Dupliquer", ClipAction.Duplicate, clip));
            menu.Items.Add(CreateActionMenuItem("Insérer avant", ClipAction.InsertBefore, clip));
            menu.Items.Add(CreateActionMenuItem("Insérer après", ClipAction.InsertAfter, clip));
            menu.Items.Add(CreateActionMenuItem(clip.IsHidden ? "Afficher" : "Masquer", ClipAction.ToggleVisibility, clip));
            menu.Items.Add(new Separator());
            menu.Items.Add(CreateActionMenuItem("Supprimer", ClipAction.Delete, clip));
            return menu;
        }

        private void SelectClipForEditing(TimelineClip clip)
        {
            _selectedClip = clip;
            ClipSelected?.Invoke(this, clip);
            Redraw();
        }

        private MenuItem CreateActionMenuItem(string label, ClipAction action, TimelineClip clip)
        {
            var item = new MenuItem { Header = label };
            item.Click += (_, _) => ClipActionRequested?.Invoke(this, new ClipActionEventArgs(clip, action));
            return item;
        }

        private void DrawPlayhead(double height)
        {
            var x = TimeToX(_playheadTime);
            _playheadLine = new Line
            {
                X1 = x,
                Y1 = 0,
                X2 = x,
                Y2 = height,
                Stroke = new SolidColorBrush(Color.FromRgb(255, 77, 77)),
                StrokeThickness = 2
            };
            TimelineCanvas.Children.Add(_playheadLine);
        }

        private void UpdatePlayhead()
        {
            if (_playheadLine is null)
            {
                Redraw();
                return;
            }

            var x = TimeToX(_playheadTime);
            _playheadLine.X1 = x;
            _playheadLine.X2 = x;
        }

        private void OnClipMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement { Tag: TimelineClip clip })
            {
                _selectedClip = clip;
                _selectedTrack = _project?.Tracks.FirstOrDefault(track => track.Clips.Contains(clip));
                ClipSelected?.Invoke(this, clip);

                var edge = GetResizeEdge(sender, e);
                if (edge != ResizeEdge.None)
                {
                    _resizingClip = clip;
                    _resizeEdge = edge;
                    _resizeStartX = e.GetPosition(TimelineCanvas).X;
                    _resizeOriginalStart = clip.StartTime;
                    _resizeOriginalDuration = clip.Duration;
                }
                else
                {
                    _draggingClip = clip;
                    _dragStartX = e.GetPosition(TimelineCanvas).X;
                    _dragOriginalStart = clip.StartTime;
                }

                Mouse.Capture(TimelineCanvas);
                TimelineCanvas.MouseMove += OnTimelineCanvasMouseMove;
                TimelineCanvas.MouseLeftButtonUp += OnTimelineCanvasMouseLeftButtonUp;
                Redraw();

                e.Handled = true;
            }
        }

        private void OnClipMouseMove(object sender, MouseEventArgs e)
        {
            if (_resizingClip is not null || _draggingClip is not null)
            {
                return;
            }

            if (sender is FrameworkElement)
            {
                var edge = GetResizeEdge(sender, e);
                Cursor = edge == ResizeEdge.None ? Cursors.Arrow : Cursors.SizeWE;
            }
        }

        private void OnClipMouseLeave(object sender, MouseEventArgs e)
        {
            if (_resizingClip is null)
            {
                Cursor = Cursors.Arrow;
            }
        }

        private void OnTimelineCanvasMouseMove(object sender, MouseEventArgs e)
        {
            if (_resizingClip is null && _draggingClip is null)
            {
                return;
            }


            if (_draggingClip is not null)
            {
                var dragDeltaSeconds = (e.GetPosition(TimelineCanvas).X - _dragStartX) / PixelsPerSecond;
                var maximumStart = Math.Max(0, (_project?.Duration ?? _draggingClip.EndTime) - _draggingClip.Duration);
                _draggingClip.StartTime = Math.Clamp(_dragOriginalStart + dragDeltaSeconds, 0, maximumStart);
                ClipChanged?.Invoke(this, _draggingClip);
                Redraw();
                return;
            }

            var resizingClip = _resizingClip;
            if (resizingClip is null) return;
            var deltaSeconds = (e.GetPosition(TimelineCanvas).X - _resizeStartX) / PixelsPerSecond;
            if (_resizeEdge == ResizeEdge.Left)
            {
                var originalEnd = _resizeOriginalStart + _resizeOriginalDuration;
                var newStart = Math.Clamp(_resizeOriginalStart + deltaSeconds, 0, originalEnd - MinClipDuration);
                resizingClip.StartTime = newStart;
                resizingClip.Duration = Math.Max(MinClipDuration, originalEnd - newStart);
            }
            else
            {
                resizingClip.Duration = Math.Max(MinClipDuration, _resizeOriginalDuration + deltaSeconds);
            }

            ClipChanged?.Invoke(this, resizingClip);
            Redraw();
        }

        private void OnTimelineCanvasMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            EndPointerInteraction();
            e.Handled = true;
        }

        private void EndPointerInteraction()
        {
            if (_resizingClip is null && _draggingClip is null)
            {
                return;
            }

            TimelineCanvas.MouseMove -= OnTimelineCanvasMouseMove;
            TimelineCanvas.MouseLeftButtonUp -= OnTimelineCanvasMouseLeftButtonUp;
            Mouse.Capture(null);
            Cursor = Cursors.Arrow;
            _resizingClip = null;
            _draggingClip = null;
            _resizeEdge = ResizeEdge.None;
        }

        private void OnTimelineMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SelectClip(null);
        }

        private static ResizeEdge GetResizeEdge(object sender, MouseEventArgs e)
        {
            if (sender is not FrameworkElement element || element.ActualWidth <= 0)
            {
                return ResizeEdge.None;
            }

            var x = e.GetPosition(element).X;
            if (x <= ResizeHandleWidth)
            {
                return ResizeEdge.Left;
            }

            return x >= element.ActualWidth - ResizeHandleWidth ? ResizeEdge.Right : ResizeEdge.None;
        }

        private double TimeToX(double time) => LeftGutter + (Math.Max(0, time) * PixelsPerSecond);

        private void AddLine(double x1, double y1, double x2, double y2, string color, double thickness)
        {
            TimelineCanvas.Children.Add(new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                StrokeThickness = thickness
            });
        }

        private static Color GetClipColor(TimelineClip clip)
        {
            if (clip.IsMedia)
            {
                return Color.FromRgb(208, 125, 42);
            }

            if (clip.IsAudio)
            {
                return Color.FromRgb(54, 130, 105);
            }

            return clip.EffectType switch
            {
                EffectType.Fade => Color.FromRgb(120, 82, 170),
                EffectType.Wave => Color.FromRgb(38, 124, 170),
                EffectType.Pulse => Color.FromRgb(203, 74, 139),
                EffectType.Strobe => Color.FromRgb(219, 184, 54),
                EffectType.Chase => Color.FromRgb(43, 174, 164),
                EffectType.Breath => Color.FromRgb(86, 112, 190),
                EffectType.Sparkle => Color.FromRgb(173, 121, 212),
                EffectType.Equalizer => Color.FromRgb(34, 151, 104),
                EffectType.Ripple => Color.FromRgb(56, 116, 205),
                EffectType.Snowfall => Color.FromRgb(112, 196, 232),
                EffectType.Frost => Color.FromRgb(58, 134, 214),
                EffectType.Fire => Color.FromRgb(232, 82, 28),
                EffectType.ToxicHeart => Color.FromRgb(222, 35, 126),
                EffectType.FireIce => Color.FromRgb(152, 78, 196),
                _ => Color.FromRgb(180, 72, 72)
            };
        }

        private enum ResizeEdge
        {
            None,
            Left,
            Right
        }
    }

    public enum ClipAction
    {
        Edit,
        Duplicate,
        InsertBefore,
        InsertAfter,
        ToggleVisibility,
        Delete
    }

    public sealed class ClipActionEventArgs : EventArgs
    {
        public ClipActionEventArgs(TimelineClip clip, ClipAction action)
        {
            Clip = clip;
            Action = action;
        }

        public TimelineClip Clip { get; }
        public ClipAction Action { get; }
    }
}
