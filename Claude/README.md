# osuLazer2TaikoNauts

osu!lazer に入っている **太鼓（osu!taiko）譜面だけ**を、TaikøNauts が読める形で提供するツールです。
譜面データはコピーせず**シンボリックリンク**で参照するので、ディスク容量をほとんど使いません。
2回目以降は前回との差分だけを反映します。

- Windows 10 / 11（x64）用
- 単一の exe で動作（.NET ランタイム同梱）
- ダブルクリックで GUI、コマンドラインから実行すると CLI（同じウィンドウで続行）

## ビルド

[.NET 8 SDK](https://dotnet.microsoft.com/download) をインストールして、このフォルダで次を実行します。

```
dotnet publish -c Release
```

`bin\Release\net8.0-windows\win-x64\publish\osuLazer2TaikoNauts.exe` が単一ファイルの exe です。
ランタイム同梱をやめて小さくしたい場合は `--self-contained false` を付けます（実行側に .NET 8 Desktop Runtime が必要）。

## 使い方

### GUI

exe をダブルクリックします。`client.realm`・`files` フォルダ・出力先を指定して「同期を実行」を押します。
「ドライラン」にチェックすると、ディスクを変更せずに件数だけ確認できます。

### CLI

```
osuLazer2TaikoNauts.exe --output D:\TaikoNauts\Songs     # 初回: 出力先を指定（config.json に保存）
osuLazer2TaikoNauts.exe                                  # 2回目以降: 保存済みの設定で実行
```

| オプション | 内容 |
|---|---|
| `--realm <path>` | `client.realm` のパス |
| `--files <path>` | osu!lazer の `files` フォルダ |
| `--output <path>` | 出力先（この下に `osuTaiko` フォルダを作成） |
| `--dry-run` | ディスクを変更せず件数だけ表示 |
| `--no-elevate` | UAC による管理者昇格を行わない |
| `--pause` | 終了時にキー入力を待つ（昇格時は自動で付く） |
| `--gui` / `--cli` | 起動モードを強制 |
| `-h` / `--help` | ヘルプを表示 |

終了コードは、正常終了が `0`、エラーがあった場合が `1` です。バッチファイルから使えます。

### 起動モードの判定

引数なしで、**エクスプローラーから起動された場合のみ** GUI を表示します（ダブルクリック・ショートカット・スタートメニュー）。
cmd / PowerShell / バッチ / タスクスケジューラなどから起動した場合は、新しいウィンドウを開かず、そのまま CLI として実行します。
ダブルクリック時は、起動直後にコンソールの黒い窓が一瞬表示されて閉じます。

### 設定ファイル

`config.json` が exe と同じ場所に作られます。

```json
{
  "realm_path": "C:\\Users\\<ユーザー名>\\AppData\\Roaming\\osu\\client.realm",
  "files_path": "C:\\Users\\<ユーザー名>\\AppData\\Roaming\\osu\\files",
  "output_path": "D:\\TaikoNauts\\Songs"
}
```

`realm_path` と `files_path` の初期値は `%APPDATA%\osu` 配下です。`output_path` は初回に指定が必要です。

### シンボリックリンクの権限

シンボリックリンクを作れるかどうかを実行前に試し、作れない場合だけ管理者権限で再起動を試みます（UAC の確認が出て、新しいウィンドウが開きます）。
**Windows の開発者モードを有効にすれば、管理者権限は不要**です。

## 出力

```
<output_path>\osuTaiko\
├── manifest.json                         前回の同期状態（キャッシュ）
├── error.log                             エラーがあったときだけ作成
├── <タイトル>_<アーティスト>_<UUID>\
│   ├── xxx [Oni].osu                     → osu!lazer の files 内の原本へのリンク
│   ├── audio.mp3
│   └── sb\bg.jpg                         サブフォルダも再現
└── ...
```

### 対象と命名

- **対象**: 太鼓譜面を1つ以上含むビートマップセット。osu!lazer 側で削除予定のセットは除きます。
- **リンクするファイル**: セット内の全ファイル。ただし、太鼓以外の難易度の `.osu` は除きます。
- **フォルダ名**: `[タイトル]_[アーティスト]_[UUID]`
  - タイトルとアーティストは、最初の太鼓譜面のメタデータ（ローマ字表記）を使います。空の場合は `Unknown` にします。
  - UUID はセットの UUID です。オンライン未投稿の譜面（OnlineID なし）でも付くため、命名規則は1つで済みます。
- **禁止文字**: `\ / : * ? " < > |` と制御文字を `_` に置換します。それ以外は加工しません（前後の空白やドットもそのままです）。
- **例外（Windows で作れないもの）**: ファイルパスの途中のフォルダ名の末尾にあるドットと空白は削除します。`CON` や `NUL` などの予約名の先頭には `_` を付けます。

### manifest.json

前回の状態を記録するキャッシュです。次回実行時、Realm から作った最新の状態とメモリ上で比較して、差分だけを処理します。

```json
{
  "last_synced": "2026-10-23T12:34:56",
  "beatmap_sets": {
    "301494b0-f053-4ecd-b368-7cf97081a7c8": {
      "folder_name": "Title_Artist_301494b0-f053-4ecd-b368-7cf97081a7c8",
      "files": {
        "Title [Oni].osu": "<sha256>",
        "audio.mp3": "<sha256>",
        "sb/bg.jpg": "<sha256>"
      }
    }
  }
}
```

- キーはセットの UUID（小文字、ハイフンあり）です。
- `folder_name` は実際のフォルダ名と完全に一致します。
- `files` は「リンク名（相対パス、区切りは `/`）→ 原本のハッシュ値」です。
- `beatmap_sets` は `folder_name` のABC順（大文字小文字は区別しない）で保存されます。

他のツールでこの manifest.json を共有する場合、キー・`folder_name`・`files` の値がすべて同じになるように作れば、差分がなくなり、無駄な再作成は起こりません。

### 差分の扱い

| 状況 | 処理 |
|---|---|
| 最新にだけある UUID | フォルダとリンクを新規作成 |
| 前回にだけある UUID | リンクと空フォルダを削除 |
| `folder_name` が変わった | 削除して作り直し |
| `files` の内容が変わった | 追加・変更・削除されたファイルのリンクだけ更新 |

## 安全性

- osu!lazer のデータには書き込みません。`client.realm` は一時フォルダにコピーして読み取り専用で開き、終了時にコピーを削除します（osu! の起動中でも実行できます）。
- 削除するのは `osuTaiko` 配下の**シンボリックリンク**と**空フォルダ**だけです。実ファイルや、中身のあるフォルダは消しません。
- 太鼓譜面が0件なのに前回の manifest にデータがある場合は、読み取り異常とみなして全削除せずに中断します。
- 原本が見つからないなどでリンクできなかったファイルは manifest に記録せず、次回の実行でやり直します。

## 注意

- Realm のバージョンは `osuLazer2TaikoNauts.csproj` の `Realm` パッケージで指定しています。osu!lazer が使っているバージョンより古いと、ファイル形式が新しい旨のエラーで開けません。その場合は新しいバージョンに上げてください。
- `client.realm` の読み取りは、Realm の動的 API でスキーマをファイルから読んでいます。osu!lazer 側でテーブル名やプロパティ名が変わると、読み取りの修正が必要になることがあります。

## ソース構成

| ファイル | 役割 |
|---|---|
| `Program.cs` | エントリポイント、起動モード判定、CLI |
| `MainForm.cs` | GUI |
| `SyncService.cs` | 同期処理の流れ（CLI/GUI 共通） |
| `RealmReader.cs` | `client.realm` の読み取り、太鼓譜面の抽出 |
| `ManifestManager.cs` | manifest の読み書き、差分の計算 |
| `Linker.cs` | シンボリックリンクの作成・削除 |
| `ConfigManager.cs` | `config.json` の管理 |
| `Utils.cs` | 禁止文字の置換、権限チェック、起動元の判定 |
| `Models.cs` | データ型 |
