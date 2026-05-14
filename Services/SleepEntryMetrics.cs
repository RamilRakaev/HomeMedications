using HomeMedications.Models;

namespace HomeMedications.Services;

/// <summary>Расчёт длительности сна и бодрствования по правилам дневника.</summary>
public static class SleepEntryMetrics
{
    /// <summary>
    /// Длительность сна: от момента засыпания до окончательного пробуждения в дату записи.
    /// Если время засыпания по часам не меньше времени пробуждения — засыпание относится к предыдущему календарному дню.
    /// </summary>
    public static TimeSpan? ComputeSleepDuration(SleepEntry entry)
    {
        if (entry.FallAsleepTimeYesterday is null || entry.FinalWakeTime is null)
            return null;

        var fall = entry.FallAsleepTimeYesterday.Value;
        var wake = entry.FinalWakeTime.Value;
        var wakeAt = entry.EntryDate.ToDateTime(wake);
        var sleepStart = fall >= wake
            ? entry.EntryDate.AddDays(-1).ToDateTime(fall)
            : entry.EntryDate.ToDateTime(fall);

        if (wakeAt <= sleepStart)
            return null;

        return wakeAt - sleepStart;
    }

    /// <summary>
    /// Бодрствование: от окончательного пробуждения в дату записи до засыпания «вчера» по записи со следующей календарной датой.
    /// Если записи на следующий день нет или в ней не указано время засыпания вчера — <c>null</c>.
    /// </summary>
    public static TimeSpan? ComputeWakeDuration(SleepEntry entry, SleepEntry? nextCalendarDayEntry)
    {
        if (entry.FinalWakeTime is null)
            return null;

        if (nextCalendarDayEntry is null ||
            nextCalendarDayEntry.EntryDate != entry.EntryDate.AddDays(1) ||
            nextCalendarDayEntry.FallAsleepTimeYesterday is null)
            return null;

        var wakeAt = entry.EntryDate.ToDateTime(entry.FinalWakeTime.Value);
        var nextBed = nextCalendarDayEntry.EntryDate.AddDays(-1)
            .ToDateTime(nextCalendarDayEntry.FallAsleepTimeYesterday.Value);

        if (nextBed <= wakeAt)
            return null;

        return nextBed - wakeAt;
    }
}
