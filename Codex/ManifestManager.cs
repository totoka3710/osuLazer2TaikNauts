using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;
using System.Globalization;

namespace osuLazer2TaikoNauts;

internal sealed class ManifestManager
{
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public string Root { get; }
    public string ManifestPath => Path.Combine(Root, "manifest.json");
    public ManifestData Data { get; private set; } = new();

    public ManifestManager(string songsPath)
    {
        Root = Path.Combine(Path.GetFullPath(songsPath), "osuTaiko");
    }

    public void Load()
    {
        if (!File.Exists(ManifestPath))
            return;
        try
        {
            Data = JsonSerializer.Deserialize<ManifestData>(File.ReadAllText(ManifestPath), jsonOptions)
                   ?? throw new InvalidDataException("manifest.json が空です。");
            foreach (var set in Data.BeatmapSets.Values)
            {
                var normalizedFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var (name, hash) in set.Files)
                    normalizedFiles[name.Replace('\\', '/')] = hash;
                set.Files = normalizedFiles;
            }
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"manifest.json を読み込めません: {ManifestPath}", ex);
        }
    }

    public ManifestChanges Compare(Dictionary<string, BeatmapSetSnapshot> current)
    {
        var changes = new ManifestChanges();
        foreach (var (key, previous) in Data.BeatmapSets)
        {
            if (!current.ContainsKey(key))
                changes.Delete.Add(previous.FolderName);
        }

        foreach (var (key, latest) in current)
        {
            if (!Data.BeatmapSets.TryGetValue(key, out var previous))
            {
                changes.Create[key] = latest;
                continue;
            }

            if (!string.Equals(previous.FolderName, latest.FolderName, StringComparison.Ordinal))
                changes.Rename.Add((key, previous.FolderName, latest));

            if (!FilesEqual(previous.Files, latest.Files))
                changes.Update[key] = (latest, previous.Files);
        }
        return changes;
    }

    public void Save(Dictionary<string, BeatmapSetSnapshot> current)
    {
        Directory.CreateDirectory(Root);
        var orderedSets = current
            .OrderBy(item => item.Value.FolderName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Value.FolderName, StringComparer.Ordinal)
            .ThenBy(item => item.Key, StringComparer.Ordinal)
            .ToDictionary(item => item.Key, item => new BeatmapSetSnapshot
            {
                FolderName = item.Value.FolderName,
                Files = item.Value.Files
                    .OrderBy(file => file.Key, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(file => file.Key, StringComparer.Ordinal)
                    .ToDictionary(file => file.Key, file => file.Value, StringComparer.OrdinalIgnoreCase)
            }, StringComparer.Ordinal);
        var next = new ManifestData
        {
            LastSynced = DateTime.Now.ToString("s", CultureInfo.InvariantCulture),
            BeatmapSets = orderedSets
        };
        var temporaryPath = ManifestPath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(next, jsonOptions));
        File.Move(temporaryPath, ManifestPath, overwrite: true);
        Data = next;
    }

    private static bool FilesEqual(Dictionary<string, string> left, Dictionary<string, string> right)
    {
        if (left.Count != right.Count)
            return false;
        foreach (var (name, hash) in left)
        {
            if (!right.TryGetValue(name, out var otherHash)
                || !string.Equals(hash, otherHash, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }
}
