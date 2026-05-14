using System.Globalization;
using System.Text;
using HomeMedications.Models;

namespace HomeMedications.Formatting;

/// <summary>Текстовое представление полей дневника для UI и экспорта.</summary>
public static class SleepEntryFormatting
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

    public static string YesNo(bool? value) =>
        value switch { true => "Да", false => "Нет", null => string.Empty };

    public static string Fatigue(FatigueBeforeSleepLevel? value) =>
        value switch
        {
            FatigueBeforeSleepLevel.None => "Нет",
            FatigueBeforeSleepLevel.Strong => "Сильная",
            FatigueBeforeSleepLevel.Medium => "Средняя",
            FatigueBeforeSleepLevel.Weak => "Слабая",
            _ => string.Empty
        };

    public static string Date(DateOnly d) => d.ToString("dd.MM.yyyy", Ru);

    public static string Time(TimeOnly? t) =>
        t is null ? string.Empty : t.Value.ToString("H:mm", Ru);

    public static string Duration(TimeSpan? ts) =>
        ts is null ? string.Empty : FormatDuration(ts.Value);

    public static string DurationExcel(TimeSpan? ts) =>
        ts is null ? string.Empty : FormatDuration(ts.Value);

    private static string FormatDuration(TimeSpan ts)
    {
        var h = (int)ts.TotalHours;
        return string.Create(Ru, $"{h}:{ts.Minutes:D2}:{ts.Seconds:D2}");
    }

    public static string? OptionalString(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /// <summary>Несколько времён пробуждений: для UI с переносами строк.</summary>
    public static string AwakeningTimesDisplay(IReadOnlyList<TimeOnly?> times)
    {
        if (times.Count == 0)
            return "—";

        var sb = new StringBuilder();
        for (var i = 0; i < times.Count; i++)
        {
            if (i > 0)
                sb.Append('\n');
            sb.Append(times[i] is null ? "—" : Time(times[i]));
        }

        return sb.ToString();
    }

    /// <summary>Для Excel: те же строки, разделитель LF.</summary>
    public static string AwakeningTimesExcel(IReadOnlyList<TimeOnly?> times)
    {
        if (times.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        for (var i = 0; i < times.Count; i++)
        {
            if (i > 0)
                sb.Append('\n');
            var t = times[i];
            sb.Append(t is null ? string.Empty : Time(t));
        }

        return sb.ToString();
    }
}
