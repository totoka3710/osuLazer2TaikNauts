using System.Text.RegularExpressions;
using Realms;
using Realms.Exceptions;

namespace osuLazer2TaikoNauts;

internal static class RealmReader
{
    public static Dictionary<string, BeatmapSetSnapshot> ReadTaikoSets(string realmPath, ulong schemaVersion)
    {
        var absoluteRealmPath = Path.GetFullPath(realmPath);
        var realm = OpenReadOnly(absoluteRealmPath, schemaVersion);
        using (realm)
        {
            var sets = new Dictionary<string, BeatmapSetSnapshot>(StringComparer.Ordinal);
            foreach (var set in realm.All<BeatmapSet>())
            {
                var allBeatmaps = set.Beatmaps.ToArray();
                var taikoBeatmaps = allBeatmaps
                    .Where(beatmap => string.Equals(beatmap.Ruleset.ShortName, "taiko", StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                if (taikoBeatmaps.Length == 0)
                    continue;

                var taikoHashes = taikoBeatmaps.Select(beatmap => beatmap.Hash).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var otherRulesetHashes = allBeatmaps
                    .Where(beatmap => !string.Equals(beatmap.Ruleset.ShortName, "taiko", StringComparison.OrdinalIgnoreCase))
                    .Select(beatmap => beatmap.Hash)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var usage in set.Files)
                {
                    var relativeName = Utilities.SanitizeRelativePath(usage.Filename);
                    if (relativeName is null)
                        continue;
                    if (relativeName.EndsWith(".osu", StringComparison.OrdinalIgnoreCase)
                        && otherRulesetHashes.Contains(usage.File.Hash)
                        && !taikoHashes.Contains(usage.File.Hash))
                        continue;
                    files[relativeName] = usage.File.Hash.ToLowerInvariant();
                }

                if (!files.Keys.Any(name => name.EndsWith(".osu", StringComparison.OrdinalIgnoreCase)))
                    continue;

                var metadata = taikoBeatmaps[0].Metadata;
                var key = set.ID.ToString("D").ToLowerInvariant();
                var folderName = string.Join('_',
                    Utilities.SanitizeFolderPart(metadata.Title),
                    Utilities.SanitizeFolderPart(metadata.Artist),
                    Utilities.SanitizeFileName(key));
                sets[key] = new BeatmapSetSnapshot { FolderName = folderName, Files = files };
            }

            return sets;
        }
    }

    private static Realm OpenReadOnly(string realmPath, ulong schemaVersion)
    {
        Realm Open(ulong version) => Realm.GetInstance(new RealmConfiguration(realmPath)
        {
            IsReadOnly = true,
            SchemaVersion = version,
            Schema = new[] { typeof(Beatmap), typeof(BeatmapSet), typeof(BeatmapMetadata),
                typeof(RealmFile), typeof(RealmNamedFileUsage), typeof(Ruleset) }
        });

        try
        {
            return Open(schemaVersion);
        }
        catch (RealmException ex)
        {
            // Read-only Realm requires the schema version already stored in the file.
            var match = Regex.Match(ex.Message, @"last set version (\d+)");
            if (!match.Success || !ulong.TryParse(match.Groups[1].Value, out var storedVersion))
                throw;
            return Open(storedVersion);
        }
    }
}

public class Beatmap : RealmObject
{
    [PrimaryKey] public Guid ID { get; set; }
    public Ruleset Ruleset { get; set; } = null!;
    public BeatmapMetadata Metadata { get; set; } = null!;
    public BeatmapSet? BeatmapSet { get; set; }
    public string Hash { get; set; } = "";
}

public class BeatmapSet : RealmObject
{
    [PrimaryKey] public Guid ID { get; set; }
    public IList<Beatmap> Beatmaps { get; } = null!;
    public IList<RealmNamedFileUsage> Files { get; } = null!;
}

public class BeatmapMetadata : RealmObject
{
    public string Title { get; set; } = "";
    public string Artist { get; set; } = "";
}

public class Ruleset : RealmObject
{
    [PrimaryKey] public string ShortName { get; set; } = "";
}

[MapTo("File")]
public class RealmFile : RealmObject
{
    [PrimaryKey] public string Hash { get; set; } = "";
}

public class RealmNamedFileUsage : RealmObject
{
    public RealmFile File { get; set; } = null!;
    public string Filename { get; set; } = "";
}
