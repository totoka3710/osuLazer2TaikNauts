using Realms;

namespace OsuLazer2TaikoNauts;

/// <summary>
/// client.realm の解析（読み取り専用）。
/// Realm .NET の動的API（IsDynamic）を使うので、osu!lazer側のクラス定義を持つ必要がない。
/// 原本は一切開かず、一時フォルダへ複製したものだけを読む（osu!起動中のロック対策）。
/// </summary>
public static class RealmReader
{
    public static Dictionary<string, SetEntry> Read(string realmPath, Action<string>? log = null)
    {
        if (!File.Exists(realmPath))
            throw new FileNotFoundException($"client.realm が見つかりません: {realmPath}");

        var tmp = Path.Combine(Path.GetTempPath(), "osu2taiko_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        try
        {
            var copy = Path.Combine(tmp, "client.realm");
            log?.Invoke("client.realm を一時コピー中...");
            using (var src = new FileStream(realmPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var dst = new FileStream(copy, FileMode.CreateNew, FileAccess.Write))
                src.CopyTo(dst);

            var cfg = new RealmConfiguration(copy) { IsDynamic = true, IsReadOnly = true };
            using var realm = Realm.GetInstance(cfg);

            log?.Invoke("BeatmapSet を解析中...");
            return BuildSnapshot(EnumerateRecords(realm));
        }
        finally
        {
            try { Directory.Delete(tmp, true); } catch { }   // 一時コピーの削除
        }
    }

    // ------------------------------------------------------------ Realm access
    static IEnumerable<SetRecord> EnumerateRecords(Realm realm)
    {
        foreach (var set in realm.DynamicApi.All("BeatmapSet"))
        {
            if (Bool(set, "DeletePending")) continue;   // osu!側で削除予定のセットは対象外

            var files = new List<NamedFile>();
            foreach (var v in set.DynamicApi.GetList<RealmValue>("Files"))
            {
                var usage = v.AsIRealmObject();
                var file = Obj(usage, "File");
                if (file is null) continue;
                files.Add(new NamedFile(Str(usage, "Filename"), Str(file, "Hash")));
            }

            var beatmaps = new List<BeatmapInfo>();
            foreach (var v in set.DynamicApi.GetList<RealmValue>("Beatmaps"))
            {
                var bm = v.AsIRealmObject();
                var rs = Obj(bm, "Ruleset");
                var meta = Obj(bm, "Metadata");
                beatmaps.Add(new BeatmapInfo(
                    rs is null ? "" : Str(rs, "ShortName"),
                    Str(bm, "Hash"),
                    meta is null ? "" : Str(meta, "Title"),
                    meta is null ? "" : Str(meta, "Artist")));
            }

            yield return new SetRecord(Str(set, "ID"), files, beatmaps);
        }
    }

    static string Str(IRealmObjectBase o, string prop)
    {
        var v = o.DynamicApi.Get<RealmValue>(prop);
        return v.Type switch
        {
            RealmValueType.Null => "",
            RealmValueType.String => v.AsString(),
            RealmValueType.Guid => v.AsGuid().ToString(),
            _ => v.ToString() ?? "",
        };
    }

    static bool Bool(IRealmObjectBase o, string prop)
    {
        var v = o.DynamicApi.Get<RealmValue>(prop);
        return v.Type == RealmValueType.Bool && v.AsBool();
    }

    static IRealmObjectBase? Obj(IRealmObjectBase o, string prop)
    {
        var v = o.DynamicApi.Get<RealmValue>(prop);
        return v.Type == RealmValueType.Object ? v.AsIRealmObject() : null;
    }

    // --------------------------------------------------------------- snapshot
    /// <summary>
    /// taiko譜面を含むセットだけを {UUID: {folder_name, files}} にまとめる。
    /// フォルダ名 = [タイトル]_[アーティスト名]_[UUID]。禁止文字以外は加工しない。
    /// </summary>
    public static Dictionary<string, SetEntry> BuildSnapshot(IEnumerable<SetRecord> records)
    {
        var snapshot = new Dictionary<string, SetEntry>();
        foreach (var rec in records)
        {
            var taiko = rec.Beatmaps.Where(b => b.Ruleset.Equals("taiko", StringComparison.OrdinalIgnoreCase)).ToList();
            if (taiko.Count == 0) continue;

            // taiko以外の難易度の .osu は除外（TaikøNautsに他モードの譜面を出さない）
            var otherHashes = rec.Beatmaps.Select(b => b.Hash).ToHashSet();
            otherHashes.ExceptWith(taiko.Select(b => b.Hash));

            var files = new Dictionary<string, string>();
            foreach (var f in rec.Files)
            {
                if (string.IsNullOrEmpty(f.Name) || string.IsNullOrEmpty(f.Hash)) continue;
                if (f.Name.EndsWith(".osu", StringComparison.OrdinalIgnoreCase) && otherHashes.Contains(f.Hash)) continue;
                var rel = Utils.SanitizeRelPath(f.Name);
                if (rel.Length > 0) files[rel] = f.Hash;
            }

            var meta = taiko[0];
            var folder = string.Join('_',
                Utils.SanitizeFilename(string.IsNullOrEmpty(meta.Title) ? "Unknown" : meta.Title),
                Utils.SanitizeFilename(string.IsNullOrEmpty(meta.Artist) ? "Unknown" : meta.Artist),
                rec.Id);
            snapshot[rec.Id] = new SetEntry { FolderName = folder, Files = files };
        }
        return snapshot;
    }
}
