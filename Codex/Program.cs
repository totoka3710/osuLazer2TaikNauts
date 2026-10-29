namespace osuLazer2TaikoNauts;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (StartupMode.ShouldOpenGui())
        {
            StartupMode.HideConsole();
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
            return 0;
        }

        try
        {
            var config = Configuration.Load(args);
            var result = SyncService.Run(config);
            Console.WriteLine(result.Summary);
            Console.WriteLine("TaikoNauts の選曲画面で F2 を押すと曲リストを再生成できます。");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"エラー: {ex.Message}");
            return 1;
        }
    }
}
