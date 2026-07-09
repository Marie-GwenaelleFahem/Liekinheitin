using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Liekinheitin.RoutingHost.Converters;

/// <summary>Affiche l'élément quand la valeur booléenne liée est <c>true</c>.</summary>
public class BoolToVisibleConverter : IValueConverter
{
    public static readonly BoolToVisibleConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Masque l'élément quand la valeur booléenne liée est <c>true</c> (utilisé pour les maillons non courants du fil d'Ariane).</summary>
public class BoolToCollapsedConverter : IValueConverter
{
    public static readonly BoolToCollapsedConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
