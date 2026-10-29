namespace osuLazer2TaikoNauts;

internal sealed class Linker
{
    private readonly string filesPath;
    private readonly string outputRoot;

    public Linker(string filesPath, string outputRoot)
    {
        this.filesPath = Path.GetFullPath(filesPath);
        this.outputRoot = Path.GetFullPath(outputRoot);
    }

    public void Sync(ManifestChanges changes)
    {
        Directory.CreateDirectory(outputRoot);

        foreach (var name in changes.Delete)
        {
            var folder = Utilities.EnsureChildPath(outputRoot, name);
            RemoveManagedLinksAndPrune(folder);
        }

        foreach (var rename in changes.Rename)
        {
            var oldFolder = Utilities.EnsureChildPath(outputRoot, rename.OldFolderName);
            var newFolder = Utilities.EnsureChildPath(outputRoot, rename.Current.FolderName);
            if (Directory.Exists(oldFolder) && !IsSymbolicLink(oldFolder))
            {
                if (Directory.Exists(newFolder) || File.Exists(newFolder))
                    throw new IOException($"フォルダー名の変更先が既に存在します: {newFolder}");
                Directory.Move(oldFolder, newFolder);
            }
        }

        foreach (var (_, change) in changes.Update)
        {
            var folder = Utilities.EnsureChildPath(outputRoot, change.Current.FolderName);
            foreach (var oldName in change.PreviousFiles.Keys.Except(change.Current.Files.Keys, StringComparer.OrdinalIgnoreCase))
            {
                var stale = SafeFilePath(folder, oldName);
                if (IsSymbolicLink(stale))
                    File.Delete(stale);
            }
        }

        foreach (var item in changes.Create.Values)
            LinkSet(item);
        foreach (var rename in changes.Rename)
        {
            if (!changes.Update.ContainsKey(rename.Key))
                LinkSet(rename.Current);
        }
        foreach (var (item, _) in changes.Update.Values)
            LinkSet(item);
    }

    private void LinkSet(BeatmapSetSnapshot set)
    {
        var folder = Utilities.EnsureChildPath(outputRoot, set.FolderName);
        Directory.CreateDirectory(folder);
        foreach (var (relativeName, hash) in set.Files)
        {
            var source = StoredFilePath(hash);
            if (!File.Exists(source))
                throw new FileNotFoundException($"osu!lazer の保存ファイルが見つかりません: {source}");

            var target = SafeFilePath(folder, relativeName);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            if (IsSymbolicLink(target))
            {
                var linkTarget = new FileInfo(target).LinkTarget;
                var resolved = linkTarget is null ? null : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(target)!, linkTarget));
                if (string.Equals(resolved, source, StringComparison.OrdinalIgnoreCase))
                    continue;
                File.Delete(target);
            }
            else if (File.Exists(target) || Directory.Exists(target))
            {
                throw new IOException($"リンク先に既存ファイルがあるため上書きしません: {target}");
            }

            try
            {
                File.CreateSymbolicLink(target, source);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new InvalidOperationException("シンボリックリンクを作成できません。Windows の開発者モードを有効にしてください。", ex);
            }
            catch (IOException ex) when (OperatingSystem.IsWindows())
            {
                throw new InvalidOperationException("シンボリックリンクを作成できません。Windows の開発者モードを有効にしてください。", ex);
            }
        }
    }

    private string StoredFilePath(string hash)
    {
        var normalized = hash.ToLowerInvariant();
        if (normalized.Length != 64 || normalized.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidDataException($"不正な SHA-256 hash です: {hash}");
        return Path.Combine(filesPath, normalized[..1], normalized[..2], normalized);
    }

    private static string SafeFilePath(string folder, string relativeName)
    {
        if (Path.IsPathRooted(relativeName) || relativeName.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(part => part is "" or "." or ".."))
            throw new InvalidDataException($"不正な相対ファイル名です: {relativeName}");
        var path = Path.GetFullPath(Path.Combine(folder, relativeName));
        var prefix = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"出力先フォルダーの外を指すパスです: {relativeName}");
        return path;
    }

    private static bool IsSymbolicLink(string path)
    {
        try
        {
            return new FileInfo(path).LinkTarget is not null || new DirectoryInfo(path).LinkTarget is not null;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static void RemoveManagedLinksAndPrune(string folder)
    {
        if (!Directory.Exists(folder) || IsSymbolicLink(folder))
            return;
        foreach (var entry in Directory.EnumerateFileSystemEntries(folder))
        {
            if (IsSymbolicLink(entry))
            {
                if ((File.GetAttributes(entry) & FileAttributes.Directory) != 0)
                    Directory.Delete(entry);
                else
                    File.Delete(entry);
            }
            else if (Directory.Exists(entry))
            {
                RemoveManagedLinksAndPrune(entry);
                TryDeleteEmptyDirectory(entry);
            }
        }
        TryDeleteEmptyDirectory(folder);
    }

    private static void TryDeleteEmptyDirectory(string path)
    {
        try
        {
            Directory.Delete(path);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
