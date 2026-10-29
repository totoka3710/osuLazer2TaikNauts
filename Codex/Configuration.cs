using System.Text.Json;
using System.Text.Json.Serialization;

namespace osuLazer2TaikoNauts;

internal sealed class AppConfiguration
{
    [JsonPropertyName("realm_path")]
    public string RealmPath { get; set; } = "";

    [JsonPropertyName("files_path")]
    public string FilesPath { get; set; } = "";

    [JsonPropertyName("output_path")]
    public string OutputPath { get; set; } = "";

    [JsonPropertyName("realm_schema_version")]
    public ulong RealmSchemaVersion { get; set; } = 41;
}

internal static class Configuration
{
    private static readonly JsonSerializerOptions jsonOptions = new() { WriteIndented = true };

    public static string ApplicationDirectory { get; } = AppContext.BaseDirectory;
    public static string ConfigPath => Path.Combine(ApplicationDirectory, "config.json");

    public static AppConfiguration LoadSaved()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var config = new AppConfiguration
        {
            RealmPath = Path.Combine(appData, "osu", "client.realm"),
            FilesPath = Path.Combine(appData, "osu", "files")
        };

        if (!File.Exists(ConfigPath))
            return config;

        try
        {
            config = JsonSerializer.Deserialize<AppConfiguration>(File.ReadAllText(ConfigPath)) ?? config;
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"設定ファイルを読み込めません: {ConfigPath}", ex);
        }

        return config;
    }

    public static AppConfiguration Load(string[] args)
    {
        var config = LoadSaved();
        for (var i = 0; i < args.Length; i++)
        {
            var option = args[i];
            if (option is "--help" or "-h")
            {
                PrintHelp();
                Environment.Exit(0);
            }

            if (i + 1 >= args.Length)
                throw new ArgumentException($"{option} の値がありません。--help で使い方を確認してください。");

            var value = args[++i];
            switch (option)
            {
                case "--realm-path": config.RealmPath = value; break;
                case "--files-path": config.FilesPath = value; break;
                case "--output-path": config.OutputPath = value; break;
                case "--realm-schema-version":
                    if (!ulong.TryParse(value, out var schemaVersion))
                        throw new ArgumentException("--realm-schema-version は整数で指定してください。");
                    config.RealmSchemaVersion = schemaVersion;
                    break;
                default: throw new ArgumentException($"不明なオプションです: {option}。--help で使い方を確認してください。");
            }
        }

        Save(config);
        return config;
    }

    public static void Save(AppConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.OutputPath))
            throw new ArgumentException("出力先が未設定です。TaikoNauts の Songs フォルダーを指定してください。");
        if (!File.Exists(config.RealmPath))
            throw new FileNotFoundException($"osu!lazer の client.realm が見つかりません: {config.RealmPath}");
        if (!Directory.Exists(config.FilesPath))
            throw new DirectoryNotFoundException($"osu!lazer の files フォルダーが見つかりません: {config.FilesPath}");

        WriteConfig(config);
    }

    private static void WriteConfig(AppConfiguration config)
    {
        Directory.CreateDirectory(ApplicationDirectory);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config, jsonOptions));
    }

    private static void PrintHelp()
    {
        Console.WriteLine("osu!lazer の taiko 譜面を TaikoNauts 用 Songs フォルダーへ同期します。\n");
        Console.WriteLine("使い方: dotnet run --project .\\Codex\\osuLazer2TaikoNauts.csproj -- [options]\n");
        Console.WriteLine("  --realm-path PATH              client.realm の場所");
        Console.WriteLine("  --files-path PATH              osu!lazer の files フォルダー");
        Console.WriteLine("  --output-path PATH             TaikoNauts の Songs フォルダー");
        Console.WriteLine("  --realm-schema-version NUMBER  Realm schema version (通常は自動検出)");
    }
}
