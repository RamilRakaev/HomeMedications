using System.Globalization;

namespace HomeMedications.Formatting;

/// <summary>Разбор значений полей времени из HTML input и текстовых полей.</summary>
public static class TimeFieldParsing
{
    public static TimeOnly? ParseTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return TimeOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var t)
            ? t
            : null;
    }

    public static string FormatTimeForInput(TimeOnly? value) =>
        value is null ? string.Empty : value.Value.ToString("HH:mm", CultureInfo.InvariantCulture);

    public static TimeSpan? ParseDuration(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var direct))
            return direct;

        var parts = value.Trim().Split(':');
        if (parts.Length is 2 or 3 &&
            int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var h) &&
            int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var m))
        {
            var sec = parts.Length == 3 &&
                      int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var s)
                ? s
                : 0;
            try
            {
                return new TimeSpan(h, m, sec);
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        return null;
    }

    public static string FormatDurationForInput(TimeSpan? value)
    {
        if (value is null)
            return string.Empty;
        var ts = value.Value;
        var h = (int)ts.TotalHours;
        return string.Create(CultureInfo.InvariantCulture, $"{h}:{ts.Minutes:D2}:{ts.Seconds:D2}");
    }
}
