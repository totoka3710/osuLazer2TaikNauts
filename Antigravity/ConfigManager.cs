using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace osuLazer2TaikoNauts
{
    public class Config
    {
        [JsonPropertyName("realm_path")]
        public string RealmPath { get; set; } = "";
        
        [JsonPropertyName("files_path")]
        public string FilesPath { get; set; } = "";
        
        [JsonPropertyName("output_path")]
        public string OutputPath { get; set; } = "";
    }

    public class ConfigManager
    {
        private readonly string _configPath;
        public Config CurrentConfig { get; private set; }

        public ConfigManager(string configPath = "config.json")
        {
            _configPath = configPath;
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string defaultOsu = Path.Combine(appData, "osu");
            
            CurrentConfig = new Config
            {
                RealmPath = Path.Combine(defaultOsu, "client.realm"),
                FilesPath = Path.Combine(defaultOsu, "files"),
                OutputPath = ""
            };
        }

        public void LoadConfig(string[] args)
        {
            if (File.Exists(_configPath))
            {
                try
                {
                    string json = File.ReadAllText(_configPath);
                    var loaded = JsonSerializer.Deserialize<Config>(json);
                    if (loaded != null)
                    {
                        CurrentConfig.RealmPath = loaded.RealmPath ?? CurrentConfig.RealmPath;
                        CurrentConfig.FilesPath = loaded.FilesPath ?? CurrentConfig.FilesPath;
                        CurrentConfig.OutputPath = loaded.OutputPath ?? CurrentConfig.OutputPath;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"config.json の読み込みに失敗しました。デフォルト値を使用します。 ({ex.Message})");
                }
            }
            else
            {
                SaveConfig();
            }

            bool updated = false;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--realm_path" && i + 1 < args.Length)
                {
                    CurrentConfig.RealmPath = args[++i];
                    updated = true;
                }
                else if (args[i] == "--files_path" && i + 1 < args.Length)
                {
                    CurrentConfig.FilesPath = args[++i];
                    updated = true;
                }
                else if (args[i] == "--output_path" && i + 1 < args.Length)
                {
                    CurrentConfig.OutputPath = args[++i];
                    updated = true;
                }
            }

            if (updated)
            {
                SaveConfig();
            }
        }

        public void SaveConfig()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(CurrentConfig, options);
            File.WriteAllText(_configPath, json);
        }
    }
}
