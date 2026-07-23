using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Media;
using Liekinheitin.CreativeTool.Models;

namespace Liekinheitin.CreativeTool.Views
{
    /// <summary>
    /// Simple RGBW editor used by the clip property panel.
    /// </summary>
    public partial class ColorPickerView : UserControl
    {
        private bool _isUpdating;

        public ColorPickerView()
        {
            InitializeComponent();
            SetColor(RgbwColor.White);
        }

        public event EventHandler<RgbwColor>? ColorChanged;

        public RgbwColor Color => new(
            ToByte(RedSlider.Value),
            ToByte(GreenSlider.Value),
            ToByte(BlueSlider.Value),
            ToByte(WhiteSlider.Value));

        public void SetColor(RgbwColor color)
        {
            _isUpdating = true;
            RedSlider.Value = color.R;
            GreenSlider.Value = color.G;
            BlueSlider.Value = color.B;
            WhiteSlider.Value = color.W;
            RedTextBox.Text = color.R.ToString(CultureInfo.InvariantCulture);
            GreenTextBox.Text = color.G.ToString(CultureInfo.InvariantCulture);
            BlueTextBox.Text = color.B.ToString(CultureInfo.InvariantCulture);
            WhiteTextBox.Text = color.W.ToString(CultureInfo.InvariantCulture);
            UpdatePreview();
            _isUpdating = false;
        }

        private void OnSliderValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdating)
            {
                return;
            }

            _isUpdating = true;
            RedTextBox.Text = ToByte(RedSlider.Value).ToString(CultureInfo.InvariantCulture);
            GreenTextBox.Text = ToByte(GreenSlider.Value).ToString(CultureInfo.InvariantCulture);
            BlueTextBox.Text = ToByte(BlueSlider.Value).ToString(CultureInfo.InvariantCulture);
            WhiteTextBox.Text = ToByte(WhiteSlider.Value).ToString(CultureInfo.InvariantCulture);
            UpdatePreview();
            _isUpdating = false;
            ColorChanged?.Invoke(this, Color);
        }

        private void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdating || !IsLoaded)
            {
                return;
            }

            _isUpdating = true;
            RedSlider.Value = ReadByte(RedTextBox.Text, RedSlider.Value);
            GreenSlider.Value = ReadByte(GreenTextBox.Text, GreenSlider.Value);
            BlueSlider.Value = ReadByte(BlueTextBox.Text, BlueSlider.Value);
            WhiteSlider.Value = ReadByte(WhiteTextBox.Text, WhiteSlider.Value);
            UpdatePreview();
            _isUpdating = false;
            ColorChanged?.Invoke(this, Color);
        }

        private void UpdatePreview()
        {
            PreviewBorder.Background = new SolidColorBrush(Color.ToColor());
        }

        private static double ReadByte(string text, double fallback)
        {
            return byte.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : fallback;
        }

        private static byte ToByte(double value) => (byte)Math.Clamp((int)Math.Round(value), 0, 255);
    }
}
