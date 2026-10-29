using System.Text.Json.Serialization;

namespace OsuLazer2TaikoNauts;

/// <summary>manifest.json の1セット分: フォルダ名と「リンク名 → 原本ハッシュ」。</summary>
public sealed class SetEntry
{
    [JsonPropertyName("folder_name")] public string FolderName { get; set; } = "";
    [JsonPropertyName("files")] public Dictionary<string, string> Files { get; set; } = new();

    public SetEntry Clone() => new() { FolderName = FolderName, Files = new(Files) };
}

/// <summary>realmから読んだ生レコード（taikoで絞る前）。</summary>
public sealed record NamedFile(string Name, string Hash);
public sealed record BeatmapInfo(string Ruleset, string Hash, string Title, string Artist);
public sealed record SetRecord(string Id, List<NamedFile> Files, List<BeatmapInfo> Beatmaps);

public sealed class DiffResult
{
    public sealed record CreateItem(string Key, SetEntry Entry);
    public sealed record UpdateItem(string Key, string FolderName, List<string> Unlink, Dictionary<string, string> Link);
    public sealed record DeleteItem(string Key, SetEntry Entry);

    public List<CreateItem> Create { get; } = new();
    public List<UpdateItem> Update { get; } = new();
    public List<DeleteItem> Delete { get; } = new();
    public bool IsEmpty => Create.Count == 0 && Update.Count == 0 && Delete.Count == 0;
}

public sealed class SyncResult
{
    public int Created, Updated, Deleted;
    public List<string> Errors { get; } = new();
}
