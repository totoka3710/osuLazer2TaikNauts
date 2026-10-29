using System.Text.RegularExpressions;

namespace osuLazer2TaikoNauts;

internal static partial class Utilities
{
    private static readonly Regex invalidFileCharacters = InvalidFileCharactersRegex();
    private static readonly HashSet<string> reservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    public static string SanitizeFileName(string value)
    {
        var safe = invalidFileCharacters.Replace(value, "_");
        safe = safe.TrimEnd(' ', '.');
        if (safe.Length != value.Length && safe.Length < value.Length)
            safe += new string('_', value.Length - safe.Length);
        if (string.IsNullOrEmpty(safe))
            safe = "Unknown";
        if (reservedNames.Contains(Path.GetFileNameWithoutExtension(safe)))
            safe = "_" + safe;
        return safe;
    }

    public static string SanitizeFolderPart(string value)
    {
        var safe = invalidFileCharacters.Replace(value, "_");
        return string.IsNullOrEmpty(safe) ? "Unknown" : safe;
    }

    public static string? SanitizeRelativePath(string value)
    {
        var parts = value.Replace('\\', '/').Split('/');
        if (string.IsNullOrWhiteSpace(value) || value.StartsWith('/') || parts.Any(p => p is "" or "." or ".."))
            return null;
        return string.Join('/', parts.Select(SanitizeFileName));
    }

    public static string EnsureChildPath(string root, string childName)
    {
        var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var child = Path.GetFullPath(Path.Combine(normalizedRoot, childName));
        var prefix = normalizedRoot + Path.DirectorySeparatorChar;
        if (!child.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"管理対象外のパスを指定しています: {childName}");
        return child;
    }

    [GeneratedRegex("[\\\\/:*?\"<>|\\x00-\\x1f]")]
    private static partial Regex InvalidFileCharactersRegex();
}
