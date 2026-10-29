using System.Drawing;

namespace osuLazer2TaikoNauts;

internal sealed class MainForm : Form
{
    private readonly TextBox realmPath = new();
    private readonly TextBox filesPath = new();
    private readonly TextBox outputPath = new();
    private readonly RichTextBox results = new();
    private readonly Button runButton = new();
    private readonly AppConfiguration savedSettings;

    public MainForm()
    {
        Text = "osu!lazer → TaikoNauts";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(720, 520);
        Size = new Size(820, 610);
        Font = SystemFonts.MessageBoxFont;

        try
        {
            savedSettings = Configuration.LoadSaved();
        }
        catch (Exception ex)
        {
            savedSettings = CreateDefaults();
            AppendResult($"設定ファイルを読み込めませんでした。パスを確認して保存してください。\r\n{ex.Message}");
        }

        realmPath.Text = savedSettings.RealmPath;
        filesPath.Text = savedSettings.FilesPath;
        outputPath.Text = savedSettings.OutputPath;

        BuildLayout();
    }

    private void BuildLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 5
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var heading = new Label
        {
            Text = "osu!lazer の taiko 譜面を TaikoNauts に同期します。",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font(Font, FontStyle.Bold)
        };
        layout.Controls.Add(heading, 0, 0);
        layout.Controls.Add(BuildPathFields(), 0, 1);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 7, 0, 0)
        };
        runButton.Text = "リンクを実行";
        runButton.AutoSize = true;
        runButton.MinimumSize = new Size(130, 34);
        runButton.Click += RunButton_Click;
        buttonPanel.Controls.Add(runButton);
        layout.Controls.Add(buttonPanel, 0, 2);

        var resultsLabel = new Label
        {
            Text = "実行結果",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        layout.Controls.Add(resultsLabel, 0, 3);

        results.Dock = DockStyle.Fill;
        results.ReadOnly = true;
        results.BackColor = SystemColors.Window;
        results.DetectUrls = true;
        layout.Controls.Add(results, 0, 4);

        Controls.Add(layout);
    }

    private Control BuildPathFields()
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 3,
            Margin = Padding.Empty
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        for (var row = 0; row < 3; row++)
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 33.333f));

        AddPathRow(table, 0, "client.realm", realmPath, BrowseRealmFile);
        AddPathRow(table, 1, "osu!lazer の files", filesPath, BrowseFilesFolder);
        AddPathRow(table, 2, "TaikoNauts の Songs", outputPath, BrowseOutputFolder);
        return table;
    }

    private static void AddPathRow(TableLayoutPanel table, int row, string labelText, TextBox textBox, Action browse)
    {
        var label = new Label
        {
            Text = labelText,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Margin = new Padding(0, 3, 10, 3)
        };
        textBox.Dock = DockStyle.Fill;
        textBox.Margin = new Padding(0, 6, 8, 6);
        var browseButton = new Button
        {
            Text = "参照...",
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 4)
        };
        browseButton.Click += (_, _) => browse();

        table.Controls.Add(label, 0, row);
        table.Controls.Add(textBox, 1, row);
        table.Controls.Add(browseButton, 2, row);
    }

    private void BrowseRealmFile()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "osu!lazer の client.realm を選択",
            Filter = "Realm データベース (client.realm)|client.realm|Realm ファイル (*.realm)|*.realm|すべてのファイル (*.*)|*.*",
            CheckFileExists = true,
            FileName = File.Exists(realmPath.Text) ? Path.GetFileName(realmPath.Text) : "client.realm"
        };
        if (Directory.Exists(Path.GetDirectoryName(realmPath.Text)))
            dialog.InitialDirectory = Path.GetDirectoryName(realmPath.Text);
        if (dialog.ShowDialog(this) == DialogResult.OK)
            realmPath.Text = dialog.FileName;
    }

    private void BrowseFilesFolder() => BrowseFolder(filesPath, "osu!lazer の files フォルダーを選択");
    private void BrowseOutputFolder() => BrowseFolder(outputPath, "TaikoNauts の Songs フォルダーを選択");

    private void BrowseFolder(TextBox target, string description)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = description,
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false,
            SelectedPath = Directory.Exists(target.Text) ? target.Text : ""
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            target.Text = dialog.SelectedPath;
    }

    private async void RunButton_Click(object? sender, EventArgs e)
    {
        var config = new AppConfiguration
        {
            RealmPath = realmPath.Text.Trim(),
            FilesPath = filesPath.Text.Trim(),
            OutputPath = outputPath.Text.Trim(),
            RealmSchemaVersion = savedSettings.RealmSchemaVersion
        };

        try
        {
            Configuration.Save(config);
        }
        catch (Exception ex)
        {
            AppendResult($"エラー: {ex.Message}");
            return;
        }

        runButton.Enabled = false;
        AppendResult("同期を開始しました...");
        try
        {
            var result = await Task.Run(() => SyncService.Run(config));
            AppendResult(result.Summary);
            AppendResult("TaikoNauts の選曲画面で F2 を押すと曲リストを再読み込みできます。");
        }
        catch (Exception ex)
        {
            AppendResult($"エラー: {ex.Message}");
        }
        finally
        {
            runButton.Enabled = true;
        }
    }

    private void AppendResult(string message)
    {
        if (results.IsDisposed)
            return;
        results.AppendText($"{message}{Environment.NewLine}");
        results.SelectionStart = results.TextLength;
        results.ScrollToCaret();
    }

    private static AppConfiguration CreateDefaults()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return new AppConfiguration
        {
            RealmPath = Path.Combine(appData, "osu", "client.realm"),
            FilesPath = Path.Combine(appData, "osu", "files")
        };
    }
}
