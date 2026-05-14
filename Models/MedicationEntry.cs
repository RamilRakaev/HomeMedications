using System.ComponentModel.DataAnnotations;

namespace HomeMedications.Models;

public enum IntakePeriodKind
{
    PerDay,
    PerWeek
}

public enum MedicationFrequency
{
    EveryDay,
    SeveralTimesPerWeek
}

public sealed class MedicationEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Название лекарства.</summary>
    [Required(ErrorMessage = "Укажите название лекарства.")]
    public string MedicineName { get; set; } = "";

    public string Dosage { get; set; } = "";

    /// <summary>Количество штук (таблеток и т.п.).</summary>
    [Range(0, 1_000_000, ErrorMessage = "Количество не может быть отрицательным.")]
    public int QuantityUnits { get; set; }

    /// <summary>Приёмов за день или за неделю (см. IntakePeriod).</summary>
    [Range(1, 1000, ErrorMessage = "Укажите число приёмов от 1 до 1000.")]
    public int IntakesPerPeriod { get; set; }

    public IntakePeriodKind IntakePeriod { get; set; } = IntakePeriodKind.PerDay;

    public MedicationFrequency Frequency { get; set; } = MedicationFrequency.EveryDay;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }
}
