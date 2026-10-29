using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace osuLazer2TaikoNauts
{
    public partial class MainForm : Form
    {
        private TextBox txtRealmPath = null!;
        private TextBox txtFilesPath = null!;
        private TextBox txtOutputPath = null!;
        private Button btnBrowseRealm = null!;
        private Button btnBrowseFiles = null!;
        private Button btnBrowseOutput = null!;
        private Button btnRun = null!;
        private Label lblStatus = null!;
        private Label lblRealm = null!;
        private Label lblFiles = null!;
        private Label lblOutput = null!;
        private CheckBox chkDarkMode = null!;
        private ConfigManager _configMgr;

        public MainForm()
        {
            _configMgr = new ConfigManager();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "osuLazer2TaikoNauts - GUI";
            this.Size = new Size(600, 320);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            int labelX = 20, textX = 140, btnX = 500;
            int yPos = 30, yStep = 50;

            lblRealm = new Label { Text = "client.realm パス:", Location = new Point(labelX, yPos), AutoSize = true };
            txtRealmPath = new TextBox { Location = new Point(textX, yPos - 3), Width = 350 };
            btnBrowseRealm = new Button { Text = "参照", Location = new Point(btnX, yPos - 5), Width = 60 };
            btnBrowseRealm.Click += (s, e) => BrowseFile(txtRealmPath, "Realm Database|*.realm|All Files|*.*");

            yPos += yStep;

            lblFiles = new Label { Text = "files フォルダー:", Location = new Point(labelX, yPos), AutoSize = true };
            txtFilesPath = new TextBox { Location = new Point(textX, yPos - 3), Width = 350 };
            btnBrowseFiles = new Button { Text = "参照", Location = new Point(btnX, yPos - 5), Width = 60 };
            btnBrowseFiles.Click += (s, e) => BrowseFolder(txtFilesPath);

            yPos += yStep;

            lblOutput = new Label { Text = "Songs フォルダー:", Location = new Point(labelX, yPos), AutoSize = true };
            txtOutputPath = new TextBox { Location = new Point(textX, yPos - 3), Width = 350 };
            btnBrowseOutput = new Button { Text = "参照", Location = new Point(btnX, yPos - 5), Width = 60 };
            btnBrowseOutput.Click += (s, e) => BrowseFolder(txtOutputPath);

            yPos += yStep + 10;

            lblStatus = new Label { Text = "準備完了", Location = new Point(labelX, yPos + 8), AutoSize = true };

            chkDarkMode = new CheckBox
            {
                Text = "ダークモード",
                Location = new Point(btnX - 180, yPos + 6),
                AutoSize = true
            };
            chkDarkMode.CheckedChanged += (s, e) => ApplyTheme(chkDarkMode.Checked);

            btnRun = new Button
            {
                Text = "リンク作成を実行",
                Location = new Point(btnX - 60, yPos),
                Width = 120,
                Height = 35,
                Font = new Font(this.Font, FontStyle.Bold)
            };
            btnRun.Click += BtnRun_Click;

            this.Controls.Add(lblRealm);
            this.Controls.Add(txtRealmPath);
            this.Controls.Add(btnBrowseRealm);

            this.Controls.Add(lblFiles);
            this.Controls.Add(txtFilesPath);
            this.Controls.Add(btnBrowseFiles);

            this.Controls.Add(lblOutput);
            this.Controls.Add(txtOutputPath);
            this.Controls.Add(btnBrowseOutput);

            this.Controls.Add(lblStatus);
            this.Controls.Add(chkDarkMode);
            this.Controls.Add(btnRun);

            this.Load += MainForm_Load;
        }

        private void MainForm_Load(object? sender, EventArgs e)
        {
            _configMgr.LoadConfig(Array.Empty<string>());
            var config = _configMgr.CurrentConfig;

            txtRealmPath.Text = config.RealmPath;
            txtFilesPath.Text = config.FilesPath;
            txtOutputPath.Text = config.OutputPath;

            // Windowsのテーマ設定からダークモードかどうかを初期判定
            bool isDark = false;
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key?.GetValue("AppsUseLightTheme") is int val && val == 0)
                {
                    isDark = true;
                }
            }
            catch { }

            chkDarkMode.Checked = isDark;
            ApplyTheme(isDark);
        }

        private void ApplyTheme(bool isDark)
        {
            Color formBack = isDark ? Color.FromArgb(32, 32, 32) : Color.FromArgb(240, 240, 240);
            Color formFore = isDark ? Color.FromArgb(235, 235, 235) : Color.FromArgb(30, 30, 30);

            Color txtBack = isDark ? Color.FromArgb(48, 48, 52) : Color.White;
            Color txtFore = isDark ? Color.White : Color.Black;

            Color btnBrowseBack = isDark ? Color.FromArgb(60, 60, 65) : Color.FromArgb(235, 235, 235);
            Color btnBrowseFore = isDark ? Color.White : Color.Black;

            Color runBtnBack = isDark ? Color.FromArgb(38, 130, 65) : Color.FromArgb(40, 167, 69);
            Color runBtnFore = Color.White;

            this.BackColor = formBack;
            this.ForeColor = formFore;

            lblRealm.ForeColor = formFore;
            lblFiles.ForeColor = formFore;
            lblOutput.ForeColor = formFore;
            chkDarkMode.ForeColor = formFore;

            var textboxes = new[] { txtRealmPath, txtFilesPath, txtOutputPath };
            foreach (var txt in textboxes)
            {
                txt.BackColor = txtBack;
                txt.ForeColor = txtFore;
                txt.BorderStyle = isDark ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;
            }

            var browseButtons = new[] { btnBrowseRealm, btnBrowseFiles, btnBrowseOutput };
            foreach (var btn in browseButtons)
            {
                btn.BackColor = btnBrowseBack;
                btn.ForeColor = btnBrowseFore;
                btn.UseVisualStyleBackColor = !isDark;
                btn.FlatStyle = isDark ? FlatStyle.Flat : FlatStyle.Standard;
                if (isDark)
                {
                    btn.FlatAppearance.BorderColor = Color.FromArgb(90, 90, 95);
                }
            }

            btnRun.BackColor = runBtnBack;
            btnRun.ForeColor = runBtnFore;
            btnRun.FlatStyle = FlatStyle.Flat;
            btnRun.FlatAppearance.BorderColor = isDark ? Color.FromArgb(50, 160, 80) : Color.FromArgb(30, 140, 55);

            if (lblStatus.Text == "準備完了")
            {
                lblStatus.ForeColor = isDark ? Color.DarkGray : Color.DimGray;
            }
        }

        private void BrowseFile(TextBox target, string filter)
        {
            using var ofd = new OpenFileDialog { Filter = filter, CheckFileExists = true };
            if (!string.IsNullOrEmpty(target.Text) && File.Exists(target.Text))
            {
                ofd.InitialDirectory = Path.GetDirectoryName(target.Text);
                ofd.FileName = Path.GetFileName(target.Text);
            }

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                target.Text = ofd.FileName;
            }
        }

        private void BrowseFolder(TextBox target)
        {
            using var fbd = new FolderBrowserDialog();
            if (!string.IsNullOrEmpty(target.Text) && Directory.Exists(target.Text))
            {
                fbd.SelectedPath = target.Text;
            }

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                target.Text = fbd.SelectedPath;
            }
        }

        private async void BtnRun_Click(object? sender, EventArgs e)
        {
            var config = _configMgr.CurrentConfig;
            config.RealmPath = txtRealmPath.Text;
            config.FilesPath = txtFilesPath.Text;
            config.OutputPath = txtOutputPath.Text;
            _configMgr.SaveConfig();

            if (string.IsNullOrWhiteSpace(config.OutputPath))
            {
                MessageBox.Show("Songs フォルダーのパスを設定してください。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // シンボリックリンク作成権限の確認
            if (!Utils.CanCreateSymlink(config.OutputPath))
            {
                var dialogResult = MessageBox.Show(
                    "シンボリックリンクの作成権限がありません。\n管理者権限でアプリを再起動しますか？\n（Windowsの「開発者モード」を有効にすると、管理者権限なしでも実行できるようになります）",
                    "権限が必要です",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (dialogResult == DialogResult.Yes)
                {
                    if (Utils.RelaunchAsAdmin(Array.Empty<string>()))
                    {
                        Application.Exit();
                        return;
                    }
                }
                return;
            }

            btnRun.Enabled = false;
            lblStatus.Text = "処理中...";
            lblStatus.ForeColor = chkDarkMode.Checked ? Color.LightSkyBlue : Color.Blue;

            try
            {
                await Task.Run(() => RunSyncProcess(config));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"エラーが発生しました:\n{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "エラー発生";
                lblStatus.ForeColor = chkDarkMode.Checked ? Color.LightCoral : Color.Red;
            }
            finally
            {
                btnRun.Enabled = true;
            }
        }

        private void RunSyncProcess(Config config)
        {
            var reader = new RealmReader(config.RealmPath);
            var currentData = reader.ExtractTaikoBeatmaps();

            if (currentData == null)
            {
                Invoke((MethodInvoker)(() =>
                {
                    MessageBox.Show("Realmデータベースの読み込みに失敗しました。\nパスが正しいか確認してください。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    lblStatus.Text = "読み込み失敗";
                    lblStatus.ForeColor = chkDarkMode.Checked ? Color.LightCoral : Color.Red;
                }));
                return;
            }

            var manifestMgr = new ManifestManager(config.OutputPath);
            manifestMgr.LoadManifest();

            var (createList, updateList, deleteList) = manifestMgr.Compare(currentData);

            if (createList.Count == 0 && updateList.Count == 0 && deleteList.Count == 0)
            {
                Invoke((MethodInvoker)(() =>
                {
                    lblStatus.Text = $"完了: 総数 {currentData.Count} 件 (変更なし)";
                    lblStatus.ForeColor = chkDarkMode.Checked ? Color.LightGreen : Color.Green;
                }));
                return;
            }

            var linker = new Linker(config.FilesPath, config.OutputPath);
            linker.ProcessDeletions(deleteList);
            linker.ProcessCreationsAndUpdates(createList);
            linker.ProcessCreationsAndUpdates(updateList);

            manifestMgr.SaveManifest(currentData);

            Invoke((MethodInvoker)(() =>
            {
                lblStatus.Text = $"完了: 総数 {currentData.Count} / 追加 {createList.Count} / 更新 {updateList.Count} / 削除 {deleteList.Count}";
                lblStatus.ForeColor = chkDarkMode.Checked ? Color.LightGreen : Color.Green;
                MessageBox.Show($"同期が完了しました。\n総数: {currentData.Count}\n追加: {createList.Count}\n更新: {updateList.Count}\n削除: {deleteList.Count}", "完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }));
        }
    }
}
