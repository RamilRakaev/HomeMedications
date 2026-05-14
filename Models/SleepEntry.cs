using System.ComponentModel.DataAnnotations;

namespace HomeMedications.Models;

/// <summary>Уровень усталости перед сном (цветовая шкала в таблице).</summary>
public enum FatigueBeforeSleepLevel
{
    None,
    Strong,
    Medium,
    Weak
}

/// <summary>Запись дневника сна (колонки как в эталонной таблице Excel).</summary>
public sealed class SleepEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "Укажите дату записи.")]
    public DateOnly EntryDate { get; set; }

    public TimeOnly? FallAsleepTimeYesterday { get; set; }

    public TimeOnly? FinalWakeTime { get; set; }

    [Range(0, 1000, ErrorMessage = "Количество пробуждений от 0 до 1000.")]
    public int NightAwakeningsCount { get; set; }

    /// <summary>Времена пробуждений посреди сна; длина согласована с <see cref="NightAwakeningsCount"/>, элементы могут быть пустыми.</summary>
    public List<TimeOnly?> AwakeningTimes { get; set; } = [];

    [Range(0, 1000, ErrorMessage = "Попыток уснуть от 0 до 1000.")]
    public int FallAsleepAttempts { get; set; }

    /// <summary>Еда перед сном: да / нет / не указано.</summary>
    public bool? FoodBeforeSleep { get; set; }

    public FatigueBeforeSleepLevel? FatigueBeforeSleep { get; set; }

    public bool? LyingAfterWake { get; set; }

    public bool? AnxietyBeforeSleep { get; set; }

    public bool? SleepSatisfaction { get; set; }

    public bool? SleepSatisfactionHalfDay { get; set; }

    public string? Comments { get; set; }
}
