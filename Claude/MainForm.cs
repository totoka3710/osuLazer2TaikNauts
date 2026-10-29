namespace OsuLazer2TaikoNauts;

/// <summary>ダブルクリック起動時に表示するGUI。処理本体は CLI と同じ SyncService を使う。</summary>
public sealed class MainForm : Form
{
    readonly TextBox txtRealm = new() { Dock = DockStyle.Fill };
    readonly TextBox txtFiles = new() { Dock = DockStyle.Fill };
    readonly TextBox txtOutput = new() { Dock = DockStyle.Fill };
    readonly CheckBox chkDry = new() { Text = "ドライラン（ディスクを変更しない）", AutoSize = true };
    readonly Button btnRun = new() { Text = "同期を実行", AutoSize = true, Padding = new Padding(16, 4, 16, 4) };
    readonly TextBox txtLog = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, Font = new Font("Consolas", 9F) };

    public MainForm()
    {
        Text = "osuLazer2TaikoNauts";
        Font = new Font("Yu Gothic UI", 9F);
        ClientSize = new Size(720, 480);
        MinimumSize = new Size(560, 380);
        StartPosition = FormStartPosition.CenterScreen;

        var grid = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3, Padding = new Padding(10) };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        AddRow(grid, "client.realm", txtRealm, () => PickFile(txtRealm));
        AddRow(grid, "files フォルダ", txtFiles, () => PickFolder(txtFiles));
        AddRow(grid, "出力先", txtOutput, () => PickFolder(txtOutput));

        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10, 0, 10, 6) };
        bar.Controls.AddRange(new Control[] { btnRun, chkDry });
        chkDry.Margin = new Padding(12, 8, 0, 0);

        var logPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 0, 10, 10) };
        logPanel.Controls.Add(txtLog);

        Controls.Add(logPanel);
        Controls.Add(bar);
        Controls.Add(grid);

        var cfg = ConfigManager.LoadForEdit();
        txtRealm.Text = cfg.RealmPath;
        txtFiles.Text = cfg.FilesPath;
        txtOutput.Text = cfg.OutputPath;

        btnRun.Click += async (_, _) => await RunAsync();
    }

    static void AddRow(TableLayoutPanel grid, string label, TextBox box, Action browse)
    {
        grid.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 8, 6) });
        grid.Controls.Add(box);
        var b = new Button { Text = "参照...", AutoSize = true };
        b.Click += (_, _) => browse();
        grid.Controls.Add(b);
    }

    static void PickFile(TextBox box)
    {
        using var dlg = new OpenFileDialog { Filter = "Realm (*.realm)|*.realm|All files|*.*", FileName = box.Text };
        if (dlg.ShowDialog() == DialogResult.OK) box.Text = dlg.FileName;
    }

    static void PickFolder(TextBox box)
    {
        using var dlg = new FolderBrowserDialog { SelectedPath = box.Text };
        if (dlg.ShowDialog() == DialogResult.OK) box.Text = dlg.SelectedPath;
    }

    void Log(string line)
    {
        if (InvokeRequired) { BeginInvoke(() => Log(line)); return; }
        txtLog.AppendText(line + Environment.NewLine);
    }

    async Task RunAsync()
    {
        var cfg = new Config { RealmPath = txtRealm.Text.Trim(), FilesPath = txtFiles.Text.Trim(), OutputPath = txtOutput.Text.Trim() };
        if (cfg.OutputPath.Length == 0)
        {
            MessageBox.Show(this, "出力先を指定してください。", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        bool dry = chkDry.Checked;
        ConfigManager.Save(cfg);

        try
        {
            // リンクを作れない場合のみ管理者昇格を提案（開発者モードなら不要）
            if (!dry && !Utils.CanCreateSymlink(ManifestManager.RootDir(cfg.OutputPath)))
            {
                if (!Utils.IsAdmin())
                {
                    var ans = MessageBox.Show(this,
                        "シンボリックリンクの作成には管理者権限が必要です。\n管理者として再起動しますか？\n（Windowsの開発者モードを有効にすれば不要になります）",
                        Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (ans == DialogResult.Yes && Utils.RelaunchAsAdmin("--gui")) Close();
                    return;
                }
                MessageBox.Show(this, "シンボリックリンクを作成できません。", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            btnRun.Enabled = false;
            txtLog.Clear();
            Log("開始...");
            var result = await Task.Run(() => SyncService.Run(cfg, dry, Log));
            Log($"追加: {result.Created} / 更新: {result.Updated} / 削除: {result.Deleted}{(dry ? " (dry-run)" : "")}");
            if (result.Errors.Count > 0)
            {
                Log($"エラー {result.Errors.Count} 件:");
                foreach (var e in result.Errors) Log("  " + e);
                if (!dry) SyncService.WriteErrorLog(cfg.OutputPath, result.Errors);
            }
            else Log("完了しました。");
        }
        catch (Exception e)
        {
            Log($"エラー: {e.Message}");
            Log(e.ToString());
            SyncService.WriteErrorLog(cfg.OutputPath, new[] { e.ToString() });
        }
        finally
        {
            btnRun.Enabled = true;
        }
    }
}
