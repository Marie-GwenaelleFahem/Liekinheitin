using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Liekinheitin.CreativeTool.Converters
{
    /// <summary>
    /// Convertit un System.Windows.Media.Color en SolidColorBrush pour le binding XAML.
    /// </summary>
    public sealed class ColorToBrushConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Color color)
                return new SolidColorBrush(color);
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
                return brush.Color;
            return null;
        }
    }
}