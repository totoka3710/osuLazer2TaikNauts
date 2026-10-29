using System.Text;

namespace OsuLazer2TaikoNauts;

static class Program
{
    const string Help = """
        osuLazer2TaikoNauts - osu!lazer の taiko 譜面を TaikøNauts 用にシンボリックリンクで公開します

        使い方:
          osuLazer2TaikoNauts.exe                         (ダブルクリック: GUI / コンソール・バッチから: CLI)
          osuLazer2TaikoNauts.exe --output <出力先>       (初回のみ指定。config.json に保存)

        オプション:
          --realm <path>    client.realm のパス
          --files <path>    osu!lazer の files ディレクトリ
          --output <path>   出力先（配下に osuTaiko フォルダを作成）
          --dry-run         ディスクを変更せず件数だけ表示
          --no-elevate      UAC昇格を行わない
          --pause           終了時にキー入力を待つ（昇格時に自動付与）
          --gui / --cli     表示モードを強制
        """;

    [STAThread]
    static int Main(string[] args)
    {
        // 引数なし かつ エクスプローラーから起動(ダブルクリック等)のときだけGUI。
        // cmd/PowerShell/バッチ等から起動した場合は、同じウィンドウでそのままCLIとして実行する。
        var rest = args.Where(a => a != "--gui").ToArray();
        bool gui = args.Contains("--gui") || (rest.Length == 0 && Utils.LaunchedFromExplorer());

        if (gui)
        {
            Utils.DetachConsole();          // ダブルクリック時に出た黒い窓を閉じる
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.Run(new MainForm());
            return 0;
        }
        return RunCli(rest);
    }

    static string Next(string[] a, ref int i)
    {
        if (i + 1 >= a.Length) throw new ConfigException($"{a[i]} に値がありません。");
        return a[++i];
    }

    static int RunCli(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        string? realm = null, files = null, output = null;
        bool dry = false, noElevate = false, pause = false;
        int code;
        string? errLogDir = null;

        try
        {
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--realm": realm = Next(args, ref i); break;
                    case "--files": files = Next(args, ref i); break;
                    case "--output": output = Next(args, ref i); break;
                    case "--dry-run": dry = true; break;
                    case "--no-elevate": noElevate = true; break;
                    case "--pause": pause = true; break;
                    case "--cli": break;
                    case "-h" or "--help" or "/?": Console.WriteLine(Help); return 0;
                    default: throw new ConfigException($"不明なオプション: {args[i]}\n\n{Help}");
                }
            }

            var cfg = ConfigManager.Load(realm, files, output);
            ConfigManager.Validate(cfg);
            errLogDir = cfg.OutputPath;

            // シンボリックリンクを作れない場合のみ昇格（開発者モードが有効なら昇格不要）
            if (!dry && !Utils.CanCreateSymlink(ManifestManager.RootDir(cfg.OutputPath)))
            {
                if (!noElevate && !Utils.IsAdmin())
                {
                    Console.WriteLine("シンボリックリンクの作成には管理者権限が必要です。UACで昇格します...");
                    if (Utils.RelaunchAsAdmin("--cli", "--pause")) return 0;   // 昇格した新プロセスに処理を任せる
                    throw new InvalidOperationException("管理者権限への昇格に失敗しました（キャンセルされた可能性があります）。");
                }
                throw new InvalidOperationException("シンボリックリンクを作成できません。管理者として実行するか、Windowsの開発者モードを有効にしてください。");
            }

            var result = SyncService.Run(cfg, dry, Console.WriteLine);
            Console.WriteLine($"追加: {result.Created} / 更新: {result.Updated} / 削除: {result.Deleted}{(dry ? " (dry-run)" : "")}");

            code = result.Errors.Count == 0 ? 0 : 1;
            if (result.Errors.Count > 0)
            {
                foreach (var e in result.Errors) Console.Error.WriteLine(e);
                if (!dry) SyncService.WriteErrorLog(cfg.OutputPath, result.Errors);
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"エラー: {e.Message}");
            if (e is not ConfigException) Console.Error.WriteLine(e.ToString());
            if (errLogDir is not null) SyncService.WriteErrorLog(errLogDir, new[] { e.ToString() });
            code = 1;
        }

        if (pause)
        {
            Console.WriteLine("Enterキーで終了します...");
            Console.ReadLine();
        }
        return code;
    }
}
