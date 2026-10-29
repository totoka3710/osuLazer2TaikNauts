using System.Diagnostics;
using System.Runtime.InteropServices;

namespace osuLazer2TaikoNauts;

internal static class StartupMode
{
    private const uint SnapshotProcesses = 0x00000002;
    private static readonly IntPtr InvalidHandleValue = new(-1);
    private const int HideWindow = 0;

    public static bool ShouldOpenGui() => string.Equals(GetParentProcessName(), "explorer", StringComparison.OrdinalIgnoreCase);

    public static void HideConsole()
    {
        var consoleWindow = GetConsoleWindow();
        if (consoleWindow != IntPtr.Zero)
            ShowWindow(consoleWindow, HideWindow);
        FreeConsole();
    }

    private static string? GetParentProcessName()
    {
        var snapshot = CreateToolhelp32Snapshot(SnapshotProcesses, 0);
        if (snapshot == InvalidHandleValue)
            return null;

        try
        {
            var entry = new ProcessEntry32 { Size = (uint)Marshal.SizeOf<ProcessEntry32>() };
            if (!Process32First(snapshot, ref entry))
                return null;

            var currentId = (uint)Environment.ProcessId;
            do
            {
                if (entry.ProcessId != currentId)
                    continue;

                try
                {
                    return Process.GetProcessById((int)entry.ParentProcessId).ProcessName;
                }
                catch (ArgumentException)
                {
                    return null;
                }
                catch (InvalidOperationException)
                {
                    return null;
                }
            }
            while (Process32Next(snapshot, ref entry));

            return null;
        }
        finally
        {
            CloseHandle(snapshot);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ProcessEntry32
    {
        public uint Size;
        public uint Usage;
        public uint ProcessId;
        public UIntPtr DefaultHeapId;
        public uint ModuleId;
        public uint Threads;
        public uint ParentProcessId;
        public int BasePriority;
        public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string ExecutableFile;
    }

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("kernel32.dll")]
    private static extern bool FreeConsole();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr window, int command);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint processId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool Process32First(IntPtr snapshot, ref ProcessEntry32 entry);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool Process32Next(IntPtr snapshot, ref ProcessEntry32 entry);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);
}
