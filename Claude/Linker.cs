namespace OsuLazer2TaikoNauts;

/// <summary>
/// シンボリックリンクの生成・削除。
/// 安全方針: osu!側のデータには一切書き込まない。削除は output/osuTaiko 配下の
/// 「シンボリックリンク」と「空フォルダ」に限定する（実ファイルや中身のあるフォルダは消さない）。
/// </summary>
public static class Linker
{
    public static (Dictionary<string, SetEntry> NewSets, SyncResult Stats) Apply(
        DiffResult diff, string root, string filesPath, Dictionary<string, SetEntry> old, bool dryRun)
    {
        var sets = old.ToDictionary(kv => kv.Key, kv => kv.Value.Clone());
        var stats = new SyncResult();

        if (dryRun)
        {
            stats.Created = diff.Create.Count; stats.Updated = diff.Update.Count; stats.Deleted = diff.Delete.Count;
            return (sets, stats);
        }

        Directory.CreateDirectory(root);

        foreach (var item in diff.Delete)
        {
            bool ok = true;
            foreach (var rel in item.Entry.Files.Keys)
                ok &= RemoveLink(root, LinkPath(root, item.Entry.FolderName, rel), stats.Errors);

            if (ok)
            {
                var sub = Path.Combine(root, item.Entry.FolderName);
                if (Directory.Exists(sub))
                {
                    foreach (var d in Directory.EnumerateDirectories(sub, "*", SearchOption.AllDirectories).OrderByDescending(d => d.Length))
                        TryRemoveEmptyDir(d);
                    TryRemoveEmptyDir(sub);
                }
                sets.Remove(item.Key);
                stats.Deleted++;
            }
            else if (sets.TryGetValue(item.Key, out var remain))
            {
                remain.Files = item.Entry.Files.Where(kv => Exists(LinkPath(root, item.Entry.FolderName, kv.Key)))
                                               .ToDictionary(kv => kv.Key, kv => kv.Value);
            }
        }

        foreach (var item in diff.Update)
        {
            var entry = sets[item.Key];
            foreach (var rel in item.Unlink)
            {
                var path = LinkPath(root, item.FolderName, rel);
                if (RemoveLink(root, path, stats.Errors))
                {
                    entry.Files.Remove(rel);
                    PruneEmptyParents(root, Path.GetDirectoryName(path)!);
                }
            }
            foreach (var (rel, hash) in item.Link)
                if (MakeLink(root, filesPath, item.FolderName, rel, hash, stats.Errors))
                    entry.Files[rel] = hash;
            stats.Updated++;
        }

        foreach (var item in diff.Create)
        {
            var entry = new SetEntry { FolderName = item.Entry.FolderName };
            Directory.CreateDirectory(Path.Combine(root, entry.FolderName));
            foreach (var (rel, hash) in item.Entry.Files)
                if (MakeLink(root, filesPath, entry.FolderName, rel, hash, stats.Errors))
                    entry.Files[rel] = hash;
            sets[item.Key] = entry;     // 失敗分はFilesに入らない → 次回実行で再試行される
            stats.Created++;
        }

        return (sets, stats);
    }

    // ------------------------------------------------------------------ helpers
    static string HashPath(string filesPath, string h) => Path.Combine(filesPath, h[..1], h[..2], h);   // files/<1文字>/<2文字>/<hash>

    static string LinkPath(string root, string folder, string rel) =>
        Path.Combine(root, folder, rel.Replace('/', Path.DirectorySeparatorChar));

    static bool Inside(string root, string path)
    {
        var r = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var p = Path.GetFullPath(path);
        return p.Length > r.Length && p.StartsWith(r, StringComparison.OrdinalIgnoreCase);
    }

    static bool IsLink(string path)
    {
        try { return File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint); }
        catch { return false; }
    }

    static bool Exists(string path) => IsLink(path) || File.Exists(path);

    static bool RemoveLink(string root, string path, List<string> errors)
    {
        if (!Inside(root, path)) { errors.Add($"管理外パスのため削除しません: {path}"); return false; }
        if (IsLink(path))
        {
            try { File.Delete(path); return true; }     // リンク自体のみ削除（原本は消えない）
            catch (Exception e) { errors.Add($"リンク削除失敗: {path} ({e.Message})"); return false; }
        }
        if (File.Exists(path)) { errors.Add($"シンボリックリンクではないため削除しません: {path}"); return false; }
        return true;    // 既に無い
    }

    static bool MakeLink(string root, string filesPath, string folder, string rel, string hash, List<string> errors)
    {
        var dst = LinkPath(root, folder, rel);
        if (!Inside(root, dst)) { errors.Add($"不正なパス: {rel}"); return false; }
        if (hash.Length < 2) { errors.Add($"不正なハッシュ: {folder}/{rel}"); return false; }
        var src = HashPath(filesPath, hash);
        if (!File.Exists(src)) { errors.Add($"原本が見つかりません: {src} ({folder}/{rel})"); return false; }
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
            if (IsLink(dst)) File.Delete(dst);
            else if (File.Exists(dst)) { errors.Add($"同名の実ファイルが存在するためスキップ: {dst}"); return false; }
            File.CreateSymbolicLink(dst, src);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            errors.Add($"リンク作成失敗: {dst} ({e.Message}) ※管理者権限/開発者モードを確認");
            return false;
        }
    }

    static void TryRemoveEmptyDir(string dir)
    {
        try { Directory.Delete(dir, false); } catch { }    // 空でなければ失敗する = 安全
    }

    static void PruneEmptyParents(string root, string dir)
    {
        // セットのフォルダ自体は残し、その下の空サブフォルダ(sb/ 等)だけを消す
        var rootFull = Path.GetFullPath(root);
        var d = Path.GetFullPath(dir);
        while (Inside(root, d) && !string.Equals(Path.GetDirectoryName(d), rootFull, StringComparison.OrdinalIgnoreCase))
        {
            try { Directory.Delete(d, false); } catch { break; }
            d = Path.GetDirectoryName(d)!;
        }
    }
}
