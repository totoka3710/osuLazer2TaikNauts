# osuLazer2TaikoNauts

osu!lazer に保存されている taiko 譜面を、TaikoNauts の Songs フォルダーから読み込めるようにする Windows 用ツールです。

譜面ファイルそのものはコピーせず、osu!lazer の保存先を指すシンボリックリンクを作成します。初回同期後も再実行すると差分だけを反映し、`manifest.json` を使って前回の状態と比較します。

## 動作環境

- Windows 10 / 11
- 実行ファイルをビルドする場合は .NET 10 SDK
- シンボリックリンク作成のため Windows の開発者モード
- 初回のビルド・復元時は NuGet パッケージを取得できるインターネット接続

同期を始める前に osu!lazer を終了してください。起動中は `client.realm` がロックされ、読み取りに失敗する場合があります。

## 使い方

### GUI

`osuLazer2TaikoNauts.exe` をエクスプローラーから開きます。次の3つの場所を指定し、「リンクを実行」を押してください。

1. osu!lazer の `client.realm`
2. osu!lazer の `files` フォルダー
3. TaikoNauts の `Songs` フォルダー

パスの「参照...」ボタンからファイルやフォルダーを選べます。実行結果やエラーはウィンドウ下部に表示されます。指定したパスは、実行ファイルと同じフォルダーにある `config.json` に保存されます。

### コマンドライン

PowerShell などのターミナルから起動すると、GUIではなくその場で同期し、結果をターミナルに表示します。実行ファイルを直接起動する例です。

```powershell
& "C:\Tools\osuLazer2TaikoNauts\osuLazer2TaikoNauts.exe" --output-path "D:\TaikoNauts\Songs"
```

開発中にソースから実行する場合は、リポジトリのルートで次を実行します。

```powershell
dotnet run --project .\Codex\osuLazer2TaikoNauts.csproj -- --output-path "D:\TaikoNauts\Songs"
```

`--help` を指定すると、利用できるオプションを表示します。

| オプション | 内容 |
| --- | --- |
| `--realm-path PATH` | osu!lazer の `client.realm` の場所 |
| `--files-path PATH` | osu!lazer の `files` フォルダー |
| `--output-path PATH` | TaikoNauts の `Songs` フォルダー |
| `--realm-schema-version NUMBER` | Realm のスキーマバージョン。通常は自動検出されます |

`realm_path` と `files_path` の既定値は `%APPDATA%\osu\client.realm` と `%APPDATA%\osu\files` です。`output_path` は初回に指定してください。CLIで指定した値も `config.json` に保存され、次回起動時に使われます。オプションに値を渡すと、その項目だけ保存値を上書きします。

## TaikoNauts 側の出力

指定した `Songs` フォルダー内に、次の構成を作成します。

```text
Songs\
└─ osuTaiko\
   ├─ manifest.json
   └─ 曲名_アーティスト名_譜面セットUUID\
      ├─ 譜面ファイル.osu
      └─ 音源や画像などの関連ファイルへのシンボリックリンク
```

各セットには UUID を使った固有の名前を付けます。taiko 以外の譜面ファイルは出力対象から除きます。`manifest.json` には同期日時、譜面セットの UUID、フォルダー名、関連ファイル名と osu!lazer 内のファイルハッシュを記録します。

再同期では新規追加・変更・削除・名前変更を反映します。削除対象になるのはツールが管理するシンボリックリンクと空になったフォルダーです。通常ファイルは上書き・削除せず、同名のファイルがある場合はエラーにします。出力先として `Songs` の中の `osuTaiko` フォルダー自体を指定せず、`Songs` を指定してください。

同期後、TaikoNauts の選曲画面で **F2** を押して曲リストを再読み込みします。

## ビルドと配布

リポジトリのルートで次を実行します。

```powershell
dotnet publish .\Codex\osuLazer2TaikoNauts.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

生成先は `Codex\bin\Release\net10.0-windows\win-x64\publish\` です。公開用に配布するのは `osuLazer2TaikoNauts.exe` です。設定ファイル `config.json` は、配布後に初めて実行したとき実行ファイルの隣に作られます。

## 困ったときは

- **シンボリックリンクを作成できない**: Windows の設定で開発者モードを有効にして、もう一度実行してください。
- **Realm を開けない / ファイルが見つからない**: osu!lazer を終了し、GUIまたは CLI で `client.realm` と `files` の場所を確認してください。
- **NuGet ソースに接続できない**: 初回ビルド時のパッケージ復元にはインターネット接続が必要です。ネットワーク接続を確認してから `dotnet publish` を再実行してください。
- **TaikoNauts に曲が表示されない**: 出力先に `Songs` フォルダーを指定したことを確認し、TaikoNauts の選曲画面で F2 を押してください。
