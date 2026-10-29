using System.Text.Json.Serialization;

namespace osuLazer2TaikoNauts;

internal sealed class BeatmapSetSnapshot
{
    [JsonPropertyName("folder_name")]
    public string FolderName { get; set; } = "";

    [JsonPropertyName("files")]
    public Dictionary<string, string> Files { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

internal sealed class ManifestData
{
    [JsonPropertyName("last_synced")]
    public string? LastSynced { get; set; }

    [JsonPropertyName("beatmap_sets")]
    public Dictionary<string, BeatmapSetSnapshot> BeatmapSets { get; set; } = new(StringComparer.Ordinal);
}

internal sealed class ManifestChanges
{
    public Dictionary<string, BeatmapSetSnapshot> Create { get; } = new(StringComparer.Ordinal);
    public List<(string Key, string OldFolderName, BeatmapSetSnapshot Current)> Rename { get; } = [];
    public Dictionary<string, (BeatmapSetSnapshot Current, Dictionary<string, string> PreviousFiles)> Update { get; } = new(StringComparer.Ordinal);
    public List<string> Delete { get; } = [];
}
