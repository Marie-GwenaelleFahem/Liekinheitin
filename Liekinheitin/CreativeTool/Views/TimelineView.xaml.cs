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
        private const double LeftGutter = 110;
        private const double TopRulerHeight = 28;
        private const double TrackHeight = 44;
        private const double TrackGap = 8;
        private const double PixelsPerSecond = 28;

        private ShowProject? _project;
        private TimelineClip? _selectedClip;
        private double _playheadTime;
        private Line? _playheadLine;

        public TimelineView()
        {
            InitializeComponent();
            SizeChanged += (_, _) => Redraw();
        }

        public event EventHandler<TimelineClip>? ClipSelected;

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
            var height = Math.Max(ActualHeight - 24, TopRulerHeight + (tracks.Count * (TrackHeight + TrackGap)) + 20);

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
            for (var trackIndex = 0; trackIndex < tracks.Count; trackIndex++)
            {
                var track = tracks[trackIndex];
                var y = TopRulerHeight + (trackIndex * (TrackHeight + TrackGap));

                var label = new TextBlock
                {
                    Foreground = Brushes.LightGray,
                    FontWeight = FontWeights.SemiBold,
                    Text = track.Name
                };
                Canvas.SetLeft(label, 10);
                Canvas.SetTop(label, y + 13);
                TimelineCanvas.Children.Add(label);

                var lane = new Rectangle
                {
                    Width = Math.Max(0, width - LeftGutter - 16),
                    Height = TrackHeight,
                    Fill = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                    Stroke = new SolidColorBrush(Color.FromRgb(54, 54, 54)),
                    StrokeThickness = 1
                };
                Canvas.SetLeft(lane, LeftGutter);
                Canvas.SetTop(lane, y);
                TimelineCanvas.Children.Add(lane);

                foreach (var clip in track.Clips.OrderBy(c => c.StartTime))
                {
                    DrawClip(clip, y);
                }
            }
        }

        private void DrawClip(TimelineClip clip, double trackY)
        {
            var x = TimeToX(clip.StartTime);
            var width = Math.Max(12, clip.Duration * PixelsPerSecond);
            var isSelected = ReferenceEquals(clip, _selectedClip);

            var border = new Border
            {
                Width = width,
                Height = TrackHeight - 10,
                Background = new SolidColorBrush(GetClipColor(clip.EffectType)),
                BorderBrush = isSelected ? Brushes.White : new SolidColorBrush(Color.FromRgb(90, 90, 90)),
                BorderThickness = isSelected ? new Thickness(2) : new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Tag = clip,
                ToolTip = $"{clip.Name} - {clip.EffectType}"
            };
            border.MouseLeftButtonDown += OnClipMouseLeftButtonDown;

            var text = new TextBlock
            {
                Foreground = Brushes.White,
                FontSize = 12,
                Margin = new Thickness(8, 5, 8, 0),
                Text = clip.Name,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            border.Child = text;

            Canvas.SetLeft(border, x);
            Canvas.SetTop(border, trackY + 5);
            TimelineCanvas.Children.Add(border);
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
                ClipSelected?.Invoke(this, clip);
                Redraw();
                e.Handled = true;
            }
        }

        private void OnTimelineMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SelectClip(null);
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

        private static Color GetClipColor(EffectType effectType) => effectType switch
        {
            EffectType.Fade => Color.FromRgb(120, 82, 170),
            EffectType.Wave => Color.FromRgb(38, 124, 170),
            _ => Color.FromRgb(180, 72, 72)
        };
    }
}
