namespace HomeMedications.Formatting;

/// <summary>Трёхсостояние да/нет для HTML select и JSON-совместимого UI.</summary>
public static class TriStateBool
{
    public const string Unset = "";
    public const string No = "n";
    public const string Yes = "y";

    public static string Serialize(bool? value) =>
        value switch { true => Yes, false => No, null => Unset };

    public static bool? Deserialize(string? raw) =>
        raw switch { Yes => true, No => false, _ => null };
}
