namespace OsuLazer2TaikoNauts;

/// <summary>CLI/GUI共通の同期処理: Realm読み取り → manifestと差分比較 → リンク反映 → manifest保存。</summary>
public static class SyncService
{
    public static SyncResult Run(Config cfg, bool dryRun, Action<string> log)
    {
        // 1) 最新(A): taiko譜面とハッシュ一覧
        var latest = RealmReader.Read(cfg.RealmPath, log);
        log($"taiko譜面を含むセット: {latest.Count} 件");

        // 2) 前回(B)と照合
        var old = ManifestManager.Load(cfg.OutputPath);
        if (latest.Count == 0 && old.Count > 0)
            throw new InvalidOperationException("taiko譜面が0件でした。読み取り異常の可能性があるため、既存リンクの全削除は行いません。");

        var diff = ManifestManager.ComputeDiff(latest, old);

        // 3) ディスク同期
        var root = ManifestManager.RootDir(cfg.OutputPath);
        var (newSets, stats) = Linker.Apply(diff, root, cfg.FilesPath, old, dryRun);

        // 4) キャッシュ保存
        if (!dryRun && (!diff.IsEmpty || !File.Exists(ManifestManager.ManifestPath(cfg.OutputPath))))
            ManifestManager.Save(cfg.OutputPath, newSets);

        return stats;
    }

    /// <summary>エラー時のみログファイルへ出力（不要なディスク書き込みを抑止）。</summary>
    public static void WriteErrorLog(string output, IEnumerable<string> lines)
    {
        try
        {
            Directory.CreateDirectory(ManifestManager.RootDir(output));
            var path = Path.Combine(ManifestManager.RootDir(output), "error.log");
            File.AppendAllText(path, $"--- {DateTime.Now:s} ---\n" + string.Join('\n', lines) + "\n");
        }
        catch { }
    }
}
