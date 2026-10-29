using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace osuLazer2TaikoNauts
{
    public class Manifest
    {
        [JsonPropertyName("last_synced")]
        public string LastSynced { get; set; } = "";
        
        [JsonPropertyName("beatmap_sets")]
        public Dictionary<string, BeatmapSetData> BeatmapSets { get; set; } = new Dictionary<string, BeatmapSetData>();
    }

    public class ManifestManager
    {
        private readonly string _osuTaikoPath;
        private readonly string _manifestPath;
        private Manifest _manifest;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNameCaseInsensitive = true
        };

        public ManifestManager(string outputPath)
        {
            _osuTaikoPath = Path.Combine(outputPath, "osuTaiko");
            _manifestPath = Path.Combine(_osuTaikoPath, "manifest.json");
            _manifest = new Manifest();
        }

        public void LoadManifest()
        {
            if (!Directory.Exists(_osuTaikoPath))
            {
                Directory.CreateDirectory(_osuTaikoPath);
            }

            if (File.Exists(_manifestPath))
            {
                try
                {
                    string json = File.ReadAllText(_manifestPath);
                    _manifest = JsonSerializer.Deserialize<Manifest>(json, JsonOptions) ?? new Manifest();

                    // folder_name が空の無効データは除外
                    if (_manifest.BeatmapSets != null)
                    {
                        _manifest.BeatmapSets = _manifest.BeatmapSets
                            .Where(kv => kv.Value != null && !string.IsNullOrWhiteSpace(kv.Value.FolderName))
                            .ToDictionary(kv => kv.Key, kv => kv.Value);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"manifest.json の読み込みに失敗しました。新規作成として扱います。 ({ex.Message})");
                    _manifest = new Manifest();
                }
            }
        }

        public (Dictionary<string, BeatmapSetData> CreateList, Dictionary<string, BeatmapSetData> UpdateList, List<string> DeleteList) Compare(Dictionary<string, BeatmapSetData> currentData)
        {
            var oldData = _manifest.BeatmapSets ?? new Dictionary<string, BeatmapSetData>();
            
            var createList = new Dictionary<string, BeatmapSetData>();
            var updateList = new Dictionary<string, BeatmapSetData>();
            var deleteList = new List<string>();

            // 古いデータに存在して新しいデータに存在しないものは削除
            foreach (var kvp in oldData)
            {
                if (!currentData.ContainsKey(kvp.Key))
                {
                    deleteList.Add(kvp.Value.FolderName);
                }
            }

            // 新しいデータを走査して追加・更新を判定
            foreach (var kvp in currentData)
            {
                string bId = kvp.Key;
                var bInfo = kvp.Value;

                if (!oldData.ContainsKey(bId))
                {
                    createList[bId] = bInfo;
                }
                else
                {
                    var oldInfo = oldData[bId];
                    if (bInfo.FolderName != oldInfo.FolderName)
                    {
                        deleteList.Add(oldInfo.FolderName);
                        createList[bId] = bInfo;
                    }
                    else
                    {
                        var updatedFiles = new Dictionary<string, string>();
                        foreach (var fileKvp in bInfo.Files)
                        {
                            string fName = fileKvp.Key;
                            string fHash = fileKvp.Value;
                            
                            if (!oldInfo.Files.ContainsKey(fName) || oldInfo.Files[fName] != fHash)
                            {
                                updatedFiles[fName] = fHash;
                            }
                        }

                        if (updatedFiles.Count > 0)
                        {
                            updateList[bId] = new BeatmapSetData
                            {
                                FolderName = bInfo.FolderName,
                                Files = updatedFiles
                            };
                        }
                    }
                }
            }

            return (createList, updateList, deleteList);
        }

        /// <summary>
        /// folder_name を大文字小文字を区別せずアルファベット順に並べ替え、
        /// 各セット内のファイル一覧もアルファベット順に並べ替えます。
        /// </summary>
        private static Dictionary<string, BeatmapSetData> SortBeatmapSets(Dictionary<string, BeatmapSetData> sets)
        {
            return sets
                .OrderBy(kv => kv.Value.FolderName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(kv => kv.Key, StringComparer.Ordinal)
                .ToDictionary(
                    kv => kv.Key,
                    kv => new BeatmapSetData
                    {
                        FolderName = kv.Value.FolderName,
                        Files = kv.Value.Files
                            .OrderBy(f => f.Key, StringComparer.OrdinalIgnoreCase)
                            .ToDictionary(f => f.Key, f => f.Value)
                    }
                );
        }

        public void SaveManifest(Dictionary<string, BeatmapSetData> currentData)
        {
            _manifest.LastSynced = DateTime.Now.ToString("s");
            _manifest.BeatmapSets = SortBeatmapSets(currentData);
            
            string json = JsonSerializer.Serialize(_manifest, JsonOptions);
            string tempPath = _manifestPath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, _manifestPath, overwrite: true);
        }
    }
}
