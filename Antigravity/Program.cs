using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace osuLazer2TaikoNauts
{
    static class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(uint nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool WriteConsoleInput(
            IntPtr hConsoleInput,
            INPUT_RECORD[] lpBuffer,
            uint nLength,
            out uint lpNumberOfEventsWritten
        );

        private const int ATTACH_PARENT_PROCESS = -1;
        private const uint STD_INPUT_HANDLE = unchecked((uint)-10);
        private const ushort KEY_EVENT = 0x0001;

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUT_RECORD
        {
            [FieldOffset(0)]
            public ushort EventType;
            [FieldOffset(4)]
            public KEY_EVENT_RECORD KeyEvent;
        }

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
        private struct KEY_EVENT_RECORD
        {
            [FieldOffset(0)]
            public bool bKeyDown;
            [FieldOffset(4)]
            public ushort wRepeatCount;
            [FieldOffset(6)]
            public short wVirtualKeyCode;
            [FieldOffset(8)]
            public short wVirtualScanCode;
            [FieldOffset(10)]
            public char UnicodeChar;
            [FieldOffset(12)]
            public uint dwControlKeyState;
        }

        /// <summary>
        /// CLI終了時に親コンソールへENTERキー入力を送り、プロンプトを再描画させて次の入力待機状態に戻します。
        /// </summary>
        private static void SendEnterToConsole()
        {
            try
            {
                IntPtr hInput = GetStdHandle(STD_INPUT_HANDLE);
                if (hInput == IntPtr.Zero || hInput == new IntPtr(-1)) return;

                var records = new INPUT_RECORD[2];

                // Key Down
                records[0].EventType = KEY_EVENT;
                records[0].KeyEvent.bKeyDown = true;
                records[0].KeyEvent.wRepeatCount = 1;
                records[0].KeyEvent.wVirtualKeyCode = 0x0D; // VK_RETURN
                records[0].KeyEvent.wVirtualScanCode = 0x1C;
                records[0].KeyEvent.UnicodeChar = '\r';
                records[0].KeyEvent.dwControlKeyState = 0;

                // Key Up
                records[1].EventType = KEY_EVENT;
                records[1].KeyEvent.bKeyDown = false;
                records[1].KeyEvent.wRepeatCount = 1;
                records[1].KeyEvent.wVirtualKeyCode = 0x0D;
                records[1].KeyEvent.wVirtualScanCode = 0x1C;
                records[1].KeyEvent.UnicodeChar = '\r';
                records[1].KeyEvent.dwControlKeyState = 0;

                WriteConsoleInput(hInput, records, (uint)records.Length, out _);
            }
            catch
            {
            }
        }

        [STAThread]
        static void Main(string[] args)
        {
            // エクスプローラーからの起動（ダブルクリック）ならGUI、それ以外（cmd, powershell, バッチ等）ならCLI
            bool isGui = string.Equals(Utils.GetParentProcessName(), "explorer", StringComparison.OrdinalIgnoreCase);

            if (isGui)
            {
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetColorMode(SystemColorMode.Classic);

                Application.Run(new MainForm());
            }
            else
            {
                // CLI mode
                bool attached = AttachConsole(ATTACH_PARENT_PROCESS);
                if (attached)
                {
                    Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
                    Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
                }

                RunCLI(args);

                if (attached)
                {
                    SendEnterToConsole();
                    FreeConsole();
                }
            }
        }

        static void RunCLI(string[] args)
        {
            var configMgr = new ConfigManager();
            configMgr.LoadConfig(args);
            var config = configMgr.CurrentConfig;

            if (string.IsNullOrEmpty(config.OutputPath))
            {
                Console.WriteLine("エラー: output_path が設定されていません。");
                Console.WriteLine("引数 (--output_path <パス>) を指定するか、config.json を設定してください。");
                return;
            }

            // シンボリックリンク作成権限の事前確認（開発者モード有効なら管理者権限不要）
            if (!Utils.CanCreateSymlink(config.OutputPath))
            {
                Console.WriteLine("シンボリックリンクの作成権限がありません。管理者権限に昇格します...");
                if (!Utils.RelaunchAsAdmin(args))
                {
                    Console.WriteLine("管理者権限での再起動に失敗しました。Windowsの「開発者モード」を有効にするか、管理者として実行してください。");
                }
                return;
            }

            Console.WriteLine("設定を読み込みました。");
            Console.WriteLine($"Realm Path : {config.RealmPath}");
            Console.WriteLine($"Files Path : {config.FilesPath}");
            Console.WriteLine($"Output Path: {config.OutputPath}");
            Console.WriteLine(new string('-', 30));

            var reader = new RealmReader(config.RealmPath);
            var currentData = reader.ExtractTaikoBeatmaps();

            if (currentData == null)
            {
                Console.WriteLine("エラー: Realmデータベースの読み込みに失敗しました。");
                return;
            }

            var manifestMgr = new ManifestManager(config.OutputPath);
            manifestMgr.LoadManifest();

            var (createList, updateList, deleteList) = manifestMgr.Compare(currentData);

            Console.WriteLine($"総数: {currentData.Count} 件");
            Console.WriteLine($"追加: {createList.Count} 件");
            Console.WriteLine($"更新: {updateList.Count} 件");
            Console.WriteLine($"削除: {deleteList.Count} 件");

            if (createList.Count == 0 && updateList.Count == 0 && deleteList.Count == 0)
            {
                Console.WriteLine("変更はありません。");
                return;
            }

            var linker = new Linker(config.FilesPath, config.OutputPath);
            linker.ProcessDeletions(deleteList);
            linker.ProcessCreationsAndUpdates(createList);
            linker.ProcessCreationsAndUpdates(updateList);

            manifestMgr.SaveManifest(currentData);
            Console.WriteLine("同期が完了しました。");
        }
    }
}
