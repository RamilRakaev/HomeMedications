using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using HomeMedications.Models;

namespace HomeMedications.Services;

public sealed class SleepDiaryStore
{
    private readonly string _filePath;
    private readonly object _sync = new();
    private List<SleepEntry> _entries = [];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public SleepDiaryStore(IWebHostEnvironment env)
    {
        var dir = Path.Combine(env.ContentRootPath, "Data");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "sleep-diary.json");
        Load();
    }

    public IReadOnlyList<SleepEntry> GetAll()
    {
        lock (_sync)
        {
            return _entries
                .OrderByDescending(e => e.EntryDate)
                .ThenByDescending(e => e.Id)
                .ToList();
        }
    }

    public IReadOnlyList<SleepEntry> GetByYear(int year)
    {
        lock (_sync)
        {
            return _entries
                .Where(e => e.EntryDate.Year == year)
                .OrderByDescending(e => e.EntryDate)
                .ThenByDescending(e => e.Id)
                .ToList();
        }
    }

    public SleepEntry? GetByEntryDate(DateOnly date)
    {
        lock (_sync)
        {
            return _entries
                .Where(e => e.EntryDate == date)
                .OrderBy(e => e.Id)
                .FirstOrDefault();
        }
    }

    public IReadOnlyList<int> GetSelectableYearsDescending()
    {
        lock (_sync)
        {
            return _entries
                .Select(e => e.EntryDate.Year)
                .Append(DateTime.Today.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();
        }
    }

    public void Add(SleepEntry entry)
    {
        lock (_sync)
        {
            NormalizeAwakeningList(entry);
            _entries.Add(entry);
            SaveUnlocked();
        }
    }

    public void Update(SleepEntry entry)
    {
        lock (_sync)
        {
            var idx = _entries.FindIndex(e => e.Id == entry.Id);
            if (idx < 0)
                return;
            NormalizeAwakeningList(entry);
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

    private static void NormalizeAwakeningList(SleepEntry entry)
    {
        entry.NightAwakeningsCount = Math.Max(0, Math.Min(1000, entry.NightAwakeningsCount));
        while (entry.AwakeningTimes.Count < entry.NightAwakeningsCount)
            entry.AwakeningTimes.Add(null);
        while (entry.AwakeningTimes.Count > entry.NightAwakeningsCount)
            entry.AwakeningTimes.RemoveAt(entry.AwakeningTimes.Count - 1);
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
                json = MigrateLegacySleepJson(json);
                _entries = JsonSerializer.Deserialize<List<SleepEntry>>(json, JsonOptions) ?? [];
                foreach (var e in _entries)
                    NormalizeAwakeningList(e);
            }
            catch
            {
                _entries = [];
            }
        }
    }

    /// <summary>Поддержка старых полей: awakeningsTime, sleepDuration, wakeDuration, lyingAfterWake как строка.</summary>
    private static string MigrateLegacySleepJson(string json)
    {
        try
        {
            var root = JsonNode.Parse(json);
            if (root is not JsonArray arr)
                return json;

            foreach (var node in arr)
            {
                if (node is not JsonObject o)
                    continue;

                o.Remove("sleepDuration");
                o.Remove("wakeDuration");

                if (!o.ContainsKey("awakeningTimes") && o.TryGetPropertyValue("awakeningsTime", out var legacy) && legacy is not null)
                {
                    o["awakeningTimes"] = new JsonArray(legacy.DeepClone());
                    o.Remove("awakeningsTime");
                }

                if (!o.TryGetPropertyValue("lyingAfterWake", out var lyNode) || lyNode is null)
                    continue;

                if (lyNode is JsonValue jv && jv.TryGetValue(out string? s) && s is not null)
                {
                    var t = s.Trim();
                    o["lyingAfterWake"] = t.ToLowerInvariant() switch
                    {
                        "да" or "true" or "1" => true,
                        "нет" or "false" or "0" => false,
                        _ => (JsonNode?)null
                    };
                }
            }

            return arr.ToJsonString(JsonOptions);
        }
        catch
        {
            return json;
        }
    }

    private void SaveUnlocked()
    {
        var json = JsonSerializer.Serialize(_entries, JsonOptions);
        File.WriteAllText(_filePath, json);
    }
}
