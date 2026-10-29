using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace OsuLazer2TaikoNauts;

public static class Utils
{
    static readonly Regex InvalidChars = new(@"[\\/:*?""<>|\x00-\x1f]", RegexOptions.Compiled);
    static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1","COM2","COM3","COM4","COM5","COM6","COM7","COM8","COM9",
        "LPT1","LPT2","LPT3","LPT4","LPT5","LPT6","LPT7","LPT8","LPT9",
    };

    /// <summary>Windows禁止文字( \ / : * ? " &lt; &gt; | )を '_' に置換する（前後の空白・ドットは触らない）。</summary>
    public static string SanitizeFilename(string? name) => InvalidChars.Replace(name ?? "", "_");

    /// <summary>'sb/image.png' のような相対パスを要素ごとにサニタイズ（区切りは '/'）。</summary>
    public static string SanitizeRelPath(string? path)
    {
        var parts = Regex.Split(path ?? "", @"[\\/]").Where(p => p is not ("" or "." or "..")).ToList();
        for (int i = 0; i < parts.Count; i++)
        {
            var p = SanitizeFilename(parts[i]);
            if (i < parts.Count - 1) p = p.TrimEnd('.', ' ');   // 末尾がドット/空白のフォルダはWindowsで作れない
            if (p.Length == 0) p = "_";
            if (Reserved.Contains(p.Split('.')[0])) p = "_" + p; // CON, NUL 等の予約名
            parts[i] = p;
        }
        return string.Join('/', parts);
    }

    public static bool IsAdmin()
    {
        using var id = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
    }

    /// <summary>
    /// 実際にシンボリックリンクを1つ作れるか試す。
    /// 開発者モードが有効なら管理者でなくても成功する（その場合UAC昇格は不要）。
    /// </summary>
    public static bool CanCreateSymlink(string dir)
    {
        string target = Path.Combine(dir, ".probe_target"), link = Path.Combine(dir, ".probe_link");
        try
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(target, "");
            if (File.Exists(link)) File.Delete(link);
            File.CreateSymbolicLink(link, target);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
        finally
        {
            try { if (File.Exists(link)) File.Delete(link); } catch { }
            try { if (File.Exists(target)) File.Delete(target); } catch { }
        }
    }

    /// <summary>UACで自分自身を管理者として再起動。起動できたらtrue（ユーザーがキャンセルしたらfalse）。</summary>
    public static bool RelaunchAsAdmin(params string[] args)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Environment.ProcessPath!,
                Arguments = string.Join(' ', args.Select(Quote)),
                UseShellExecute = true,
                Verb = "runas",
            });
            return true;
        }
        catch (Win32Exception) { return false; }
    }

    static string Quote(string a) => a.Contains(' ') || a.Contains('"') ? "\"" + a.Replace("\"", "\\\"") + "\"" : a;

    // ---- 起動元判定（ダブルクリック or CLI） ----
    [StructLayout(LayoutKind.Sequential)]
    struct ProcessBasicInformation
    {
        public IntPtr ExitStatus, PebBaseAddress, AffinityMask, BasePriority, UniqueProcessId, InheritedFromUniqueProcessId;
    }

    [DllImport("ntdll.dll")]
    static extern int NtQueryInformationProcess(IntPtr handle, int infoClass, ref ProcessBasicInformation info, int size, out int returned);

    [DllImport("kernel32.dll")] static extern bool FreeConsole();

    /// <summary>親プロセス名（取得できなければnull）。例: explorer / cmd / pwsh / WindowsTerminal</summary>
    public static string? ParentProcessName()
    {
        try
        {
            using var self = Process.GetCurrentProcess();
            var info = new ProcessBasicInformation();
            if (NtQueryInformationProcess(self.Handle, 0, ref info, Marshal.SizeOf<ProcessBasicInformation>(), out _) != 0) return null;
            using var parent = Process.GetProcessById((int)info.InheritedFromUniqueProcessId);
            return parent.ProcessName;
        }
        catch { return null; }
    }

    /// <summary>エクスプローラー(ダブルクリック・ショートカット・スタートメニュー)から起動されたか。</summary>
    public static bool LaunchedFromExplorer() =>
        string.Equals(ParentProcessName(), "explorer", StringComparison.OrdinalIgnoreCase);

    public static void DetachConsole() => FreeConsole();
}
