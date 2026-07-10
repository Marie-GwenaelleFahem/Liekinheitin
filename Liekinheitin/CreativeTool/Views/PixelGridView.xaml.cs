using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Liekinheitin.Domain.Entities;

namespace Liekinheitin.CreativeTool.Views
{
    /// <summary>
    /// High-throughput preview for the logical LED wall.
    /// </summary>
    public partial class PixelGridView : UserControl
    {
        public const int DefaultWidth = 128;
        public const int DefaultHeight = 128;

        private const int BytesPerPixel = 4;
        private readonly byte[] _pixels;
        private readonly WriteableBitmap _bitmap;
        private Point? _lastDragPixel;

        public PixelGridView()
        {
            InitializeComponent();

            WallWidth = DefaultWidth;
            WallHeight = DefaultHeight;
            _pixels = new byte[WallWidth * WallHeight * BytesPerPixel];
            _bitmap = new WriteableBitmap(WallWidth, WallHeight, 96, 96, PixelFormats.Bgra32, null);
            PreviewImage.Source = _bitmap;
            PreviewImage.MouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            PreviewImage.MouseMove += OnPreviewMouseMove;
            PreviewImage.MouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
            PreviewImage.MouseLeave += OnPreviewMouseLeave;

            Clear();
        }

        public event EventHandler? PixelDragStarted;

        public event EventHandler<PixelDragDeltaEventArgs>? PixelDragDelta;

        public event EventHandler? PixelDragCompleted;

        public int WallWidth { get; }

        public int WallHeight { get; }

        public void Clear()
        {
            Array.Clear(_pixels, 0, _pixels.Length);
            CommitPixels();
        }

        public void Fill(Color color)
        {
            for (var index = 0; index < _pixels.Length; index += BytesPerPixel)
            {
                WritePixel(index, color);
            }

            CommitPixels();
        }

        public void Fill(Color color, IReadOnlyCollection<int>? entityIds)
        {
            if (entityIds is null)
            {
                Fill(color);
                return;
            }

            Array.Clear(_pixels, 0, _pixels.Length);
            foreach (var entityId in entityIds)
            {
                if (TryGetPixelIndex(entityId, out var index))
                {
                    WritePixel(index, color);
                }
            }

            CommitPixels();
        }

        public void RenderWave(double time, IReadOnlyCollection<int>? entityIds = null)
        {
            if (entityIds is null)
            {
                for (var y = 0; y < WallHeight; y++)
                {
                    for (var x = 0; x < WallWidth; x++)
                    {
                        var index = ((y * WallWidth) + x) * BytesPerPixel;
                        WriteWavePixel(index, x, y, time);
                    }
                }
            }
            else
            {
                Array.Clear(_pixels, 0, _pixels.Length);
                foreach (var entityId in entityIds)
                {
                    if (TryGetPixelCoordinates(entityId, out var x, out var y, out var index))
                    {
                        WriteWavePixel(index, x, y, time);
                    }
                }
            }

            CommitPixels();
        }

        public void RenderState(State state)
        {
            Array.Clear(_pixels, 0, _pixels.Length);

            foreach (var entity in state.Entities)
            {
                if (!TryGetPixelIndex(entity.Id, out var index))
                {
                    continue;
                }

                var red = entity.Channels.Length > 0 ? entity.Channels[0] : (byte)0;
                var green = entity.Channels.Length > 1 ? entity.Channels[1] : (byte)0;
                var blue = entity.Channels.Length > 2 ? entity.Channels[2] : (byte)0;
                WritePixel(index, Color.FromRgb(red, green, blue));
            }

            CommitPixels();
        }

        private void WriteWavePixel(int index, int x, int y, double time)
        {
            var phase = (x * 0.09) + (y * 0.045) + (time * 4.0);
            var wave = (Math.Sin(phase) + 1.0) * 0.5;
            var red = (byte)(24 + (wave * 180));
            var green = (byte)(30 + ((1.0 - wave) * 90));
            var blue = (byte)(80 + (wave * 175));

            WritePixel(index, Color.FromRgb(red, green, blue));
        }

        private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (TryGetPixelPoint(e.GetPosition(PreviewImage), out var pixel))
            {
                _lastDragPixel = pixel;
                PreviewImage.CaptureMouse();
                PixelDragStarted?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
        }

        private void OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_lastDragPixel is null || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            if (!TryGetPixelPoint(e.GetPosition(PreviewImage), out var pixel))
            {
                return;
            }

            var deltaX = (int)pixel.X - (int)_lastDragPixel.Value.X;
            var deltaY = (int)pixel.Y - (int)_lastDragPixel.Value.Y;
            if (deltaX == 0 && deltaY == 0)
            {
                return;
            }

            _lastDragPixel = pixel;
            PixelDragDelta?.Invoke(this, new PixelDragDeltaEventArgs(deltaX, deltaY));
            e.Handled = true;
        }

        private void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            EndDrag(raiseCompleted: true);
            e.Handled = true;
        }

        private void OnPreviewMouseLeave(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                EndDrag(raiseCompleted: false);
            }
        }

        private void EndDrag(bool raiseCompleted)
        {
            var hadDrag = _lastDragPixel is not null;
            _lastDragPixel = null;
            if (PreviewImage.IsMouseCaptured)
            {
                PreviewImage.ReleaseMouseCapture();
            }

            if (hadDrag && raiseCompleted)
            {
                PixelDragCompleted?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool TryGetPixelPoint(Point position, out Point pixel)
        {
            pixel = default;
            var width = Math.Max(1, PreviewImage.ActualWidth);
            var height = Math.Max(1, PreviewImage.ActualHeight);

            if (position.X < 0 || position.Y < 0 || position.X >= width || position.Y >= height)
            {
                return false;
            }

            pixel = new Point(
                Math.Clamp((int)(position.X / width * WallWidth), 0, WallWidth - 1),
                Math.Clamp((int)(position.Y / height * WallHeight), 0, WallHeight - 1));
            return true;
        }

        private bool TryGetPixelIndex(int entityId, out int index)
        {
            index = entityId * BytesPerPixel;
            return entityId >= 0 && index + BytesPerPixel <= _pixels.Length;
        }

        private bool TryGetPixelCoordinates(int entityId, out int x, out int y, out int index)
        {
            x = 0;
            y = 0;

            if (!TryGetPixelIndex(entityId, out index))
            {
                return false;
            }

            x = entityId % WallWidth;
            y = entityId / WallWidth;
            return y < WallHeight;
        }

        private void WritePixel(int index, Color color)
        {
            _pixels[index] = color.B;
            _pixels[index + 1] = color.G;
            _pixels[index + 2] = color.R;
            _pixels[index + 3] = color.A;
        }

        private void CommitPixels()
        {
            _bitmap.WritePixels(
                new System.Windows.Int32Rect(0, 0, WallWidth, WallHeight),
                _pixels,
                WallWidth * BytesPerPixel,
                0);
        }
    }

    public sealed class PixelDragDeltaEventArgs : EventArgs
    {
        public PixelDragDeltaEventArgs(int deltaX, int deltaY)
        {
            DeltaX = deltaX;
            DeltaY = deltaY;
        }

        public int DeltaX { get; }

        public int DeltaY { get; }
    }
}
