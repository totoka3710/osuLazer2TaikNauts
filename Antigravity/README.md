# osuLazer2TaikoNauts (Antigravity Edition)

osu!lazer が管理するハッシュ化されたビートマップ群から **osu!taiko 譜面** を自動検出し、太鼓シミュレーター [TaikøNauts](https://taikonauts-docs.pages.dev/) の `Songs` フォルダーへシンボリックリンクで同期・インポートするためのユーティリティです。

実体ファイルを複製せずシンボリックリンクで参照するため、**ストレージ容量をほとんど消費せず** に大量の譜面を瞬時に同期できます。

---

## 🌟 主な特徴

- **GUI / CLI ハイブリッド動作**:
  - エクスプローラーからダブルクリックで起動すると、黒い画面を出さずに直感的な **GUIウィンドウ** が表示されます。
  - コマンドプロンプト、PowerShell、バッチファイルから呼び出された場合は、自動的に **CLIモード** としてバックグラウンド・コンソール動作します（バッチでの自動化・放置実行に対応）。
- **洗練されたGUI & ダークモード対応**:
  - 各フォルダーのパス入力欄とエクスプローラーによる「参照」ダイアログを完備。
  - Windowsのテーマ設定（ライト/ダーク）を自動検出し、手動切り替えチェックボックスも搭載。
- **完全な単一ファイルEXE (ポータブル)**:
  - .NET ランタイムや Realm の C++ ネイティブライブラリ（`realm-wrappers.dll`）をすべて1本のEXEに内包。
  - インストーラー不要で、生成された `osuLazer2TaikoNauts.exe` 単体だけで他のPCでもそのまま動作します。
- **スマートな権限管理**:
  - Windows の「開発者モード」が有効な環境であれば、**管理者権限（UACダイアログ）なし** でスムーズに実行できます。
- **taiko専用最適化**:
  - スタンダードやmaniaなどの他モード専用 `.osu` 難易度ファイルを自動除外（TaikøNauts内で他モードが表示されるのを防止）。
  - `manifest.json` による高速な差分検知（追加・更新・削除・総数表示）。
  - フォルダー名順（大文字小文字を区別しないアルファベット順）でのソートとアトミック保存。

---

## 💻 動作環境

- **OS**: Windows 10 (バージョン 1703 以降) / Windows 11 (64bit)
- **推奨設定**: Windowsの **「開発者モード」** を有効化（シンボリックリンク作成時にUAC管理者昇格が不要になります）
- **ビルド時のみ**: [.NET 10 SDK](https://dotnet.microsoft.com/download)

---

## 🛠️ ビルド方法

リポジトリをクローン後、`Antigravity` フォルダーで以下のコマンドを実行します：

```powershell
cd Antigravity
dotnet publish -c Release
```

ビルドが完了すると、下記に単一の実行ファイルが出力されます：
```text
Antigravity\bin\Release\net10.0-windows\win-x64\publish\osuLazer2TaikoNauts.exe
```
この `osuLazer2TaikoNauts.exe` をお好きな場所へコピーしてご使用いただけます。

---

## 🚀 使い方

### 1. GUI モード（通常利用）

`osuLazer2TaikoNauts.exe` をエクスプローラーからダブルクリックして起動します。

1. **client.realm パス**: osu!lazer のデータベースファイルのパスを指定（初期値: `%APPDATA%\osu\client.realm`）。
2. **files フォルダー**: osu!lazer の実体ファイルフォルダーを指定（初期値: `%APPDATA%\osu\files`）。
3. **Songs フォルダー**: TaikøNauts の `Songs` フォルダーを指定（例: `D:\TaikøNauts\Songs`）。
4. **「リンク作成を実行」ボタン** をクリックすると、同期処理が開始されます。

※ 設定したパスは同階層の `config.json` に自動保存され、次回起動時に入力済みとなります。

---

### 2. CLI モード（コマンドライン / バッチファイル利用）

コマンドプロンプトや PowerShell、または `.bat` ファイルから実行すると、GUIを開かずにCLIモードで同期が完了します。

```powershell
# 設定ファイル (config.json) の内容でそのまま実行
.\osuLazer2TaikoNauts.exe

# 出力先を指定して実行
.\osuLazer2TaikoNauts.exe --output_path "D:\TaikøNauts\Songs"

# パスをすべて指定して実行
.\osuLazer2TaikoNauts.exe --realm_path "C:\path\to\client.realm" --files_path "C:\path\to\files" --output_path "D:\TaikøNauts\Songs"
```

#### CLIオプション一覧

| オプション | 説明 |
| :--- | :--- |
| `--output_path <パス>` | 同期先の TaikøNauts の `Songs` フォルダーパス |
| `--realm_path <パス>` | osu!lazer の `client.realm` のパス |
| `--files_path <パス>` | osu!lazer の `files` フォルダーのパス |
| `--cli` | 明示的にCLIモードで実行するフラグ |

#### 出力例
```text
設定を読み込みました。
Realm Path : C:\Users\<User>\AppData\Roaming\osu\client.realm
Files Path : C:\Users\<User>\AppData\Roaming\osu\files
Output Path: D:\TaikøNauts\Songs
------------------------------
総数: 350 件
追加: 12 件
更新: 0 件
削除: 0 件
同期が完了しました。
```

---

## 📂 生成されるフォルダー構造

指定した `Songs` フォルダーの直下に `osuTaiko` フォルダーが作成され、その中に各曲のフォルダーとキャッシュ用マニフェストが生成されます。

```text
Songs/
 └── osuTaiko/
      ├── manifest.json
      ├── Title_Artist_UUID1/
      │    ├── song.mp3 (シンボリックリンク)
      │    ├── bg.jpg   (シンボリックリンク)
      │    └── diff.osu (シンボリックリンク)
      └── Title_Artist_UUID2/
           └── ...
```

- **TaikøNauts での反映**:
  - 同期完了後、TaikøNauts の選曲画面で **F2 キー** を押すと曲リストが再読み込みされ、即座にプレイ可能になります。

---

## ❓ トラブルシューティング

### Q. 「シンボリックリンクの作成権限がありません」と表示される
- **解決策 1 (推奨)**: Windowsの **[設定] > [更新とセキュリティ] (または [プライバシーとセキュリティ]) > [開発者向け]** で **「開発者モード」をオン** にしてください。管理者権限なしで実行できるようになります。
- **解決策 2**: 開発者モードを有効にしない場合は、右クリックして「管理者として実行」するか、ダイアログで「はい」を選択して管理者権限を許可してください。

### Q. osu!lazer 側で譜面を追加・削除した場合は？
- 本ツールを再度実行してください。`manifest.json` との差分を自動比較し、新しく追加された曲のみをリンク、osu!側で削除された曲のリンクは自動的にクリーンアップされます。
