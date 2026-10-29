using System.Text.Json;
using System.Text.Json.Serialization;

namespace OsuLazer2TaikoNauts;

/// <summary>キャッシュ(manifest.json)の読み書きと、メモリ上での高速差分比較。</summary>
public static class ManifestManager
{
    public const string DirName = "osuTaiko";
    public const string FileName = "manifest.json";

    sealed class ManifestFile
    {
        [JsonPropertyName("last_synced")] public string LastSynced { get; set; } = "";
        [JsonPropertyName("beatmap_sets")] public Dictionary<string, SetEntry> BeatmapSets { get; set; } = new();
    }

    static readonly JsonSerializerOptions Json = new() { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static string RootDir(string output) => Path.Combine(output, DirName);
    public static string ManifestPath(string output) => Path.Combine(RootDir(output), FileName);

    public static Dictionary<string, SetEntry> Load(string output)
    {
        try
        {
            return JsonSerializer.Deserialize<ManifestFile>(File.ReadAllText(ManifestPath(output)))?.BeatmapSets ?? new();
        }
        catch (Exception e) when (e is IOException or JsonException)
        {
            return new();   // 無い・壊れている場合は空扱い（全セットを新規作成として扱う）
        }
    }

    /// <summary>folder_name のABC順（大文字小文字は区別しない）に並べる。同名はキーで決定的に並べる。</summary>
    static Dictionary<string, SetEntry> Sorted(Dictionary<string, SetEntry> sets) =>
        sets.OrderBy(kv => kv.Value.FolderName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(kv => kv.Key, StringComparer.Ordinal)
            .ToDictionary(kv => kv.Key, kv => kv.Value);   // 新規Dictionaryは挿入順で列挙・書き出しされる

    public static void Save(string output, Dictionary<string, SetEntry> sets)
    {
        Directory.CreateDirectory(RootDir(output));
        var data = new ManifestFile { LastSynced = DateTime.Now.ToString("s"), BeatmapSets = Sorted(sets) };
        var path = ManifestPath(output);
        File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(data, Json));
        File.Move(path + ".tmp", path, overwrite: true);
    }

    /// <summary>最新(A)と前回(B)を比較して 作成/更新/削除 リストを作る。</summary>
    public static DiffResult ComputeDiff(Dictionary<string, SetEntry> latest, Dictionary<string, SetEntry> old)
    {
        var diff = new DiffResult();
        foreach (var (key, nw) in latest)
        {
            if (!old.TryGetValue(key, out var o))
            {
                diff.Create.Add(new(key, nw));
            }
            else if (o.FolderName != nw.FolderName)          // 改名 = 削除して作り直し
            {
                diff.Delete.Add(new(key, o));
                diff.Create.Add(new(key, nw));
            }
            else
            {
                var unlink = o.Files.Keys.Where(n => !nw.Files.ContainsKey(n)).ToList();
                var link = nw.Files.Where(kv => !o.Files.TryGetValue(kv.Key, out var h) || h != kv.Value)
                                   .ToDictionary(kv => kv.Key, kv => kv.Value);
                if (unlink.Count > 0 || link.Count > 0)
                {
                    unlink.AddRange(link.Keys.Where(n => o.Files.ContainsKey(n)));   // ハッシュ変更は旧リンクを消して再作成
                    diff.Update.Add(new(key, nw.FolderName, unlink, link));
                }
            }
        }
        foreach (var (key, o) in old)
            if (!latest.ContainsKey(key)) diff.Delete.Add(new(key, o));
        return diff;
    }
}
