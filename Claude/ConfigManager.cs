using System.Text.Json;
using System.Text.Json.Serialization;

namespace OsuLazer2TaikoNauts;

public sealed class Config
{
    [JsonPropertyName("realm_path")] public string RealmPath { get; set; } = "";
    [JsonPropertyName("files_path")] public string FilesPath { get; set; } = "";
    [JsonPropertyName("output_path")] public string OutputPath { get; set; } = "";
}

public sealed class ConfigException(string message) : Exception(message);

public static class ConfigManager
{
    const string FileName = "config.json";
    static readonly JsonSerializerOptions Json = new() { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>単一ファイル配布でも exe と同じ場所に置く（AppContext.BaseDirectory は展開先になり得るため使わない）。</summary>
    public static string AppDir => Path.GetDirectoryName(Environment.ProcessPath) ?? Environment.CurrentDirectory;
    public static string ConfigPath => Path.Combine(AppDir, FileName);

    public static Config Default()
    {
        var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var osu = Path.Combine(appdata, "osu");
        return new Config { RealmPath = Path.Combine(osu, "client.realm"), FilesPath = Path.Combine(osu, "files"), OutputPath = "" };
    }

    /// <summary>無ければ作成。引数(overrides)があれば優先して保存し直す。</summary>
    public static Config Load(string? realm = null, string? files = null, string? output = null)
    {
        var cfg = Default();
        bool exists = File.Exists(ConfigPath), changed = !exists;
        if (exists)
        {
            try
            {
                var saved = JsonSerializer.Deserialize<Config>(File.ReadAllText(ConfigPath)) ?? new Config();
                if (!string.IsNullOrWhiteSpace(saved.RealmPath)) cfg.RealmPath = saved.RealmPath;
                if (!string.IsNullOrWhiteSpace(saved.FilesPath)) cfg.FilesPath = saved.FilesPath;
                if (!string.IsNullOrWhiteSpace(saved.OutputPath)) cfg.OutputPath = saved.OutputPath;
            }
            catch (Exception e) when (e is IOException or JsonException)
            {
                throw new ConfigException($"{FileName} を読み込めません: {e.Message}");
            }
        }
        if (!string.IsNullOrWhiteSpace(realm) && cfg.RealmPath != realm) { cfg.RealmPath = realm; changed = true; }
        if (!string.IsNullOrWhiteSpace(files) && cfg.FilesPath != files) { cfg.FilesPath = files; changed = true; }
        if (!string.IsNullOrWhiteSpace(output) && cfg.OutputPath != output) { cfg.OutputPath = output; changed = true; }
        if (changed) Save(cfg);
        return cfg;
    }

    /// <summary>GUI用: 保存済み設定を例外なしで読む（出力先未設定でもOK）。</summary>
    public static Config LoadForEdit()
    {
        try { return Load(); } catch { return Default(); }
    }

    public static void Save(Config cfg) => File.WriteAllText(ConfigPath, JsonSerializer.Serialize(cfg, Json));

    public static void Validate(Config cfg)
    {
        if (string.IsNullOrWhiteSpace(cfg.OutputPath))
            throw new ConfigException("output_path が未設定です。--output で指定してください（初回のみで保存されます）。");
    }
}
