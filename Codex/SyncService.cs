namespace osuLazer2TaikoNauts;

internal sealed record SyncResult(int Created, int Updated, int Deleted, int Total)
{
    public string Summary => $"同期完了: 追加 {Created} セット / 更新 {Updated} セット / 削除 {Deleted} セット / 合計 {Total} セット";
}

internal static class SyncService
{
    public static SyncResult Run(AppConfiguration config)
    {
        var current = RealmReader.ReadTaikoSets(config.RealmPath, config.RealmSchemaVersion);
        if (current.Count == 0)
            throw new InvalidOperationException("taiko 譜面が見つかりません。client.realm と osu!lazer の files フォルダーを確認してください。");

        var manifest = new ManifestManager(config.OutputPath);
        manifest.Load();
        var changes = manifest.Compare(current);
        new Linker(config.FilesPath, manifest.Root).Sync(changes);
        manifest.Save(current);
        return new SyncResult(changes.Create.Count, changes.Update.Count, changes.Delete.Count, current.Count);
    }
}
