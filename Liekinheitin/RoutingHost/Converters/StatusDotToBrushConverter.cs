using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Liekinheitin.RoutingHost.ViewModels;

namespace Liekinheitin.RoutingHost.Converters;

/// <summary>Convertit un <see cref="StatusDot"/> en couleur de pastille (vert/rouge/gris).</summary>
public class StatusDotToBrushConverter : IValueConverter
{
    private static readonly Brush Ok = new SolidColorBrush(Color.FromRgb(0x3e, 0xcf, 0x8e));
    private static readonly Brush Err = new SolidColorBrush(Color.FromRgb(0xef, 0x44, 0x44));
    private static readonly Brush Off = new SolidColorBrush(Color.FromRgb(0x5b, 0x64, 0x72));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            StatusDot.Ok => Ok,
            StatusDot.Err => Err,
            _ => Off,
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
