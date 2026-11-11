using System.Globalization;

namespace LiftLife.Services;

/// <summary>
/// Reads numbers typed into Entries. Accepts "80", "80.5" or "80,5" and returns null if
/// the text isn't a valid number, so forms can show a friendly validation message.
/// </summary>
public static class NumberInput
{
    public static double? ToDouble(string? text) =>
        double.TryParse(text?.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;

    public static int? ToInt(string? text) =>
        int.TryParse(text?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;

    /// <summary>Formats a number for an Entry without trailing zeros (e.g. 185, 7.5).</summary>
    public static string Format(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);
}
