using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace osuLazer2TaikoNauts
{
    public static class Utils
    {
        private static readonly Regex InvalidChars = new(@"[\\/:*?""<>|\x00-\x1f]", RegexOptions.Compiled);

        /// <summary>
        /// Windows/JSONの仕様上使えない禁止文字のみを '_' に置き換えます。
        /// </summary>
        public static string SanitizeFilename(string? name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "Unknown";
            }
            return InvalidChars.Replace(name, "_");
        }

        /// <summary>
        /// 現在の権限（開発者モード有効または管理者権限）でシンボリックリンクが作成可能かをテストします。
        /// </summary>
        public static bool CanCreateSymlink(string dir)
        {
            string target = Path.Combine(dir, ".symlink_probe_target");
            string link = Path.Combine(dir, ".symlink_probe_link");
            try
            {
                Directory.CreateDirectory(dir);
                File.WriteAllText(target, "");
                if (File.Exists(link)) File.Delete(link);
                File.CreateSymbolicLink(link, target);
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                try { if (File.Exists(link)) File.Delete(link); } catch { }
                try { if (File.Exists(target)) File.Delete(target); } catch { }
            }
        }

        /// <summary>
        /// 必要時にUAC昇格（管理者として再起動）を行います。
        /// </summary>
        public static bool RelaunchAsAdmin(string[] args)
        {
            try
            {
                string exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? "";
                var psi = new ProcessStartInfo(exePath)
                {
                    UseShellExecute = true,
                    Verb = "runas",
                    Arguments = string.Join(" ", args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a))
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 起動元の親プロセス名（例: explorer, cmd, powershell など）を取得します。
        /// </summary>
        public static string GetParentProcessName()
        {
            try
            {
                uint myPid = (uint)Environment.ProcessId;
                IntPtr hSnapshot = CreateToolhelp32Snapshot(0x00000002, 0); // TH32CS_SNAPPROCESS
                if (hSnapshot == IntPtr.Zero || hSnapshot == new IntPtr(-1))
                    return "";

                try
                {
                    var pe = new PROCESSENTRY32 { dwSize = (uint)Marshal.SizeOf<PROCESSENTRY32>() };
                    if (Process32First(hSnapshot, ref pe))
                    {
                        do
                        {
                            if (pe.th32ProcessID == myPid)
                            {
                                try
                                {
                                    using var parent = Process.GetProcessById((int)pe.th32ParentProcessID);
                                    return parent.ProcessName;
                                }
                                catch
                                {
                                    return "";
                                }
                            }
                        } while (Process32Next(hSnapshot, ref pe));
                    }
                }
                finally
                {
                    CloseHandle(hSnapshot);
                }
            }
            catch
            {
            }
            return "";
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct PROCESSENTRY32
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExeFile;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

        [DllImport("kernel32.dll")]
        private static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32.dll")]
        private static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);
    }
}
