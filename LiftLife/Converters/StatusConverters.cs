using System.Globalization;
using LiftLife.Models;

namespace LiftLife.Converters;

/// <summary>
/// Converts a station's status into the badge background colour
/// (green = free, red = busy, grey = out of order).
/// </summary>
public class StatusToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            StationStatus.Free => Color.FromArgb("#3BD16F"),
            StationStatus.Busy => Color.FromArgb("#FF5A5F"),
            _ => Color.FromArgb("#5A5A66")
        };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Converts a station's status into a readable text colour for its badge.
/// </summary>
public class StatusToTextColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            StationStatus.Free => Color.FromArgb("#0B0B0F"),
            _ => Colors.White
        };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
