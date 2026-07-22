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
        private byte[] _pixels;
        private WriteableBitmap _bitmap;
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

        public event EventHandler<PixelDragStartedEventArgs>? PixelDragStarted;

        public event EventHandler<PixelDragDeltaEventArgs>? PixelDragDelta;

        public event EventHandler? PixelDragCompleted;

        public int WallWidth { get; private set; }

        public int WallHeight { get; private set; }

        /// <summary>
        /// Adapte la grille d'aperçu à la vraie taille du mur du projet chargé (par exemple
        /// 259 x 64 pour le mur réel en bandes, plutôt que le 128 x 128 par défaut). Ne
        /// réalloue rien si la taille demandée est déjà celle en cours.
        /// </summary>
        public void Resize(int width, int height)
        {
            width = Math.Max(1, width);
            height = Math.Max(1, height);

            if (width == WallWidth && height == WallHeight)
            {
                return;
            }

            WallWidth = width;
            WallHeight = height;
            _pixels = new byte[WallWidth * WallHeight * BytesPerPixel];
            _bitmap = new WriteableBitmap(WallWidth, WallHeight, 96, 96, PixelFormats.Bgra32, null);
            PreviewImage.Source = _bitmap;
            Clear();
        }

        public void ShowSelection(IReadOnlyCollection<int>? entityIds, bool showResizeHandles, bool showRotationHandle)
        {
            if (entityIds is null || entityIds.Count == 0)
            {
                SelectionBounds.Visibility = Visibility.Collapsed;
                SetHandleVisibility(Visibility.Collapsed);
                SetRotationHandleVisibility(Visibility.Collapsed);
                return;
            }

            var minX = WallWidth;
            var maxX = -1;
            var minY = WallHeight;
            var maxY = -1;
            foreach (var entityId in entityIds)
            {
                var x = entityId % WallWidth;
                var y = entityId / WallWidth;
                if (entityId >= 0 && x >= 0 && x < WallWidth && y >= 0 && y < WallHeight)
                {
                    minX = Math.Min(minX, x);
                    maxX = Math.Max(maxX, x);
                    minY = Math.Min(minY, y);
                    maxY = Math.Max(maxY, y);
                }
            }

            if (maxX < minX || maxY < minY)
            {
                SelectionBounds.Visibility = Visibility.Collapsed;
                SetHandleVisibility(Visibility.Collapsed);
                SetRotationHandleVisibility(Visibility.Collapsed);
                return;
            }

            var scaleX = Math.Max(1, PreviewImage.ActualWidth) / WallWidth;
            var scaleY = Math.Max(1, PreviewImage.ActualHeight) / WallHeight;
            var left = minX * scaleX;
            var top = minY * scaleY;
            var right = (maxX + 1) * scaleX;
            var bottom = (maxY + 1) * scaleY;

            Canvas.SetLeft(SelectionBounds, left);
            Canvas.SetTop(SelectionBounds, top);
            SelectionBounds.Width = Math.Max(1, right - left);
            SelectionBounds.Height = Math.Max(1, bottom - top);
            SelectionBounds.Visibility = Visibility.Visible;

            var handleVisibility = showResizeHandles ? Visibility.Visible : Visibility.Collapsed;
            SetHandleVisibility(handleVisibility);
            if (showResizeHandles)
            {
                PositionHandle(TopLeftHandle, left, top);
                PositionHandle(TopRightHandle, right, top);
                PositionHandle(BottomLeftHandle, left, bottom);
                PositionHandle(BottomRightHandle, right, bottom);
            }

            var rotationVisibility = showRotationHandle ? Visibility.Visible : Visibility.Collapsed;
            SetRotationHandleVisibility(rotationVisibility);
            if (showRotationHandle)
            {
                var centerX = (left + right) / 2;
                var handleY = top >= 24 ? top - 20 : top + 20;
                RotationStem.X1 = centerX;
                RotationStem.Y1 = top;
                RotationStem.X2 = centerX;
                RotationStem.Y2 = handleY;
                PositionHandle(RotationHandle, centerX, handleY);
            }
        }

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
                PixelDragStarted?.Invoke(this, new PixelDragStartedEventArgs((int)pixel.X, (int)pixel.Y));
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

        private void SetHandleVisibility(Visibility visibility)
        {
            TopLeftHandle.Visibility = visibility;
            TopRightHandle.Visibility = visibility;
            BottomLeftHandle.Visibility = visibility;
            BottomRightHandle.Visibility = visibility;
        }

        private void SetRotationHandleVisibility(Visibility visibility)
        {
            RotationStem.Visibility = visibility;
            RotationHandle.Visibility = visibility;
        }

        private static void PositionHandle(FrameworkElement handle, double x, double y)
        {
            Canvas.SetLeft(handle, x - (handle.Width / 2));
            Canvas.SetTop(handle, y - (handle.Height / 2));
        }
    }

    public sealed class PixelDragStartedEventArgs : EventArgs
    {
        public PixelDragStartedEventArgs(int pixelX, int pixelY)
        {
            PixelX = pixelX;
            PixelY = pixelY;
        }

        public int PixelX { get; }

        public int PixelY { get; }
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
