using System.Text.Json;
using System.Text.Json.Serialization;
using HomeMedications.Models;

namespace HomeMedications.Services;

public sealed class MedicationStore
{
    private readonly string _filePath;
    private readonly object _sync = new();
    private List<MedicationEntry> _entries = [];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public MedicationStore(IWebHostEnvironment env)
    {
        var dir = Path.Combine(env.ContentRootPath, "Data");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "medications.json");
        Load();
    }

    public IReadOnlyList<MedicationEntry> GetAll()
    {
        lock (_sync)
        {
            return _entries
                .OrderByDescending(e => e.StartDate)
                .ThenByDescending(e => e.Id)
                .ToList();
        }
    }

    /// <summary>Последняя запись по названию (без учёта регистра), не считая указанный id.</summary>
    public MedicationEntry? GetLatestByName(string medicineName, Guid? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(medicineName))
            return null;

        var key = medicineName.Trim();
        lock (_sync)
        {
            return _entries
                .Where(e => excludeId is null || e.Id != excludeId.Value)
                .Where(e => string.Equals(e.MedicineName.Trim(), key, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(e => e.StartDate)
                .ThenByDescending(e => e.Id)
                .FirstOrDefault();
        }
    }

    public void Add(MedicationEntry entry)
    {
        lock (_sync)
        {
            _entries.Add(entry);
            SaveUnlocked();
        }
    }

    public void Update(MedicationEntry entry)
    {
        lock (_sync)
        {
            var idx = _entries.FindIndex(e => e.Id == entry.Id);
            if (idx < 0)
                return;
            _entries[idx] = entry;
            SaveUnlocked();
        }
    }

    public void Delete(Guid id)
    {
        lock (_sync)
        {
            _entries.RemoveAll(e => e.Id == id);
            SaveUnlocked();
        }
    }

    private void Load()
    {
        lock (_sync)
        {
            if (!File.Exists(_filePath))
            {
                _entries = [];
                return;
            }

            try
            {
                var json = File.ReadAllText(_filePath);
                var list = JsonSerializer.Deserialize<List<MedicationEntry>>(json, JsonOptions);
                _entries = list ?? [];
            }
            catch
            {
                _entries = [];
            }
        }
    }

    private void SaveUnlocked()
    {
        var json = JsonSerializer.Serialize(_entries, JsonOptions);
        File.WriteAllText(_filePath, json);
    }
}
