using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using Realms;

namespace osuLazer2TaikoNauts
{
    public class BeatmapSetData
    {
        [JsonPropertyName("folder_name")]
        public string FolderName { get; set; } = "";

        [JsonPropertyName("files")]
        public Dictionary<string, string> Files { get; set; } = new Dictionary<string, string>();
    }

    public class RealmReader
    {
        private readonly string _realmPath;

        public RealmReader(string realmPath)
        {
            _realmPath = realmPath;
        }

        public Dictionary<string, BeatmapSetData>? ExtractTaikoBeatmaps()
        {
            var result = new Dictionary<string, BeatmapSetData>();

            if (!File.Exists(_realmPath))
            {
                Console.WriteLine($"Realmファイルが見つかりません: {_realmPath}");
                return null;
            }

            string tempDir = Path.Combine(Path.GetTempPath(), "osuLazer2TaikoNauts_" + Guid.NewGuid().ToString());
            string tempRealm = Path.Combine(tempDir, "client_copy.realm");
            
            try
            {
                Directory.CreateDirectory(tempDir);
                File.Copy(_realmPath, tempRealm, true);

                var config = new RealmConfiguration(tempRealm) { IsReadOnly = true, IsDynamic = true };
                using var realm = Realm.GetInstance(config);

                var beatmapSets = realm.DynamicApi.All("BeatmapSet");
                foreach (dynamic set in beatmapSets)
                {
                    try
                    {
                        var delPending = set.DynamicApi.Get<RealmValue>("DeletePending");
                        if (delPending.Type == RealmValueType.Bool && delPending.AsBool())
                        {
                            continue;
                        }
                    }
                    catch { }

                    string title = "Unknown";
                    string artist = "Unknown";
                    var taikoHashes = new HashSet<string>();
                    var otherHashes = new HashSet<string>();

                    var beatmaps = set.Beatmaps;
                    foreach (dynamic beatmap in beatmaps)
                    {
                        var ruleset = beatmap.Ruleset;
                        string shortName = ruleset != null ? (string)ruleset.ShortName : "";
                        string bHash = (string)beatmap.Hash;

                        if (string.Equals(shortName, "taiko", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!string.IsNullOrEmpty(bHash)) taikoHashes.Add(bHash);

                            var metadata = beatmap.Metadata;
                            if (metadata != null)
                            {
                                if (!string.IsNullOrEmpty((string)metadata.Title)) title = (string)metadata.Title;
                                if (!string.IsNullOrEmpty((string)metadata.Artist)) artist = (string)metadata.Artist;
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(bHash)) otherHashes.Add(bHash);
                        }
                    }

                    if (taikoHashes.Count > 0)
                    {
                        string setId = set.ID.ToString();
                        string folderName = string.Join('_',
                            Utils.SanitizeFilename(title),
                            Utils.SanitizeFilename(artist),
                            setId);

                        var setData = new BeatmapSetData
                        {
                            FolderName = folderName
                        };

                        var files = set.Files;
                        foreach (dynamic fileUsage in files)
                        {
                            string filename = (string)fileUsage.Filename;
                            var fileObj = fileUsage.File;
                            string hash = fileObj != null ? (string)fileObj.Hash : "";

                            if (string.IsNullOrEmpty(filename) || string.IsNullOrEmpty(hash))
                                continue;

                            // taiko以外のモード専用の .osu 難易度ファイルは除外
                            if (filename.EndsWith(".osu", StringComparison.OrdinalIgnoreCase) && otherHashes.Contains(hash))
                                continue;

                            setData.Files[filename] = hash;
                        }

                        result[setId] = setData;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Realmデータベースの読み込み中にエラーが発生しました: {ex.Message}");
                return null;
            }
            finally
            {
                try
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
                catch { }
            }

            return result;
        }
    }
}
