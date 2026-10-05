# osuLazer2TaikoNauts  
<ins>**!!コンピューター系を学び始めたばかりです。間違っていることやバカなことも言ってると思いますが、温かい目で見守ってください!!**</ins>  

osu!lazerが管理するハッシュ化されたビートマップをosuファイルの読み込みに対応しているTaikøNautsっていう太鼓シミュで読み込めるようにするためのプログラム。  
`client.realm`を読み込んで`Beatmap表`の`Ruleset欄`が`Ruleset{ShortName = taiko}`に一致するもののみTaikøNautsのSongsフォルダー内などにシンボリックリンクでリンクする。  
必要なファイルの情報(譜面(`.osu`)や音源、背景画像や動画)の情報およびハッシュ値は`BeatmapSet表`の`Files欄`を参照する。`Beatmap表`(およびそれにリンクされる`BeatmapMetadeta表`)の内容を参考にしてもよいが、今のところ`BeatmapSet表`のみ(`Ruleset欄`を除く)を参照する計画である。  
各`BeatmapSet表`の各項目ごとに一つのフォルダーを作成し、その中に`.osu`ファイルや音源などを入れ込んでいく。
曲数にもよるが、シンボリックリンクなので単純にコピーするよりも容量は大幅に削減できる。  

# 余裕があれば付けたい機能  
`BeatmapCollection表`を参考にジャンルフォルダーに対応。  
`Score表`を参照してosu!lazerでのプレイデータをTaikøNauts内で表示。  

[osu!公式サイト](https://osu.ppy.sh/ )  
[TaikøNauts公式サイト](https://taikonauts-docs.pages.dev/ )  

# 参考  
[File storage in osu!(lazer)](https://osu.ppy.sh/wiki/en/Client/Release_stream/Lazer/File_storage )  osu.ppy.sh  
[User file storage](https://github.com/ppy/osu/wiki/User-file-storage )  github.com  
[osu! file formats](https://osu.ppy.sh/wiki/en/Client/File_formats )  os.ppy.sh  
[Upgrading to lazer](https://osu.ppy.sh/wiki/en/Help_centre/Upgrading_to_lazer )  osu.ppy.sh  
[使用Realm Studio查询osu! Lazer谱面下载时间、最后游玩时间](https://post.cplus8.com/d/936 )  post.cplus8.com  


# Geminiが生成してくれたREADME  
#### 1. 機能要件（システムが実行すべき機能）

* **`client.realm` の解析機能**
  * Read-OnlyでRealmデータベースを開き、`BeatmapSet` および `Files`（`RealmNamedFileUsage`）から情報を抽出する。


  * `Ruleset` が `taiko`（osu!taiko）に該当する譜面データのみをフィルタリング対象とする。




* **仮想ライブラリ（リンク）生成機能**
  * `BeatmapSet` ごとに独立したフォルダを作成する。


  * 原本（`files/` フォルダ内のハッシュ化ファイル）を参照するシンボリックリンク（またはハードリンク）を、元のファイル名（`.osu`, `.ogg`, `.jpg` 等）で作成する。




* **TaikøNauts連携機能**
  * 生成したフォルダ群を TaikøNauts の `songPath` から読み込める構造にする。



#### 2. 非機能要件（性能・安全性・運用などの制約）

* **データ保護・安全性（最重要）**
  * osu!Lazerの既存データ（`files/` や `client.realm`）に対して**書き込み・削除命令を一切行わない**（完全読み取り専用）。




* **ストレージ効率性**
  * 実体ファイルの複製を行わず、リンク生成のみとすることで**ディスク追加容量を0にする**。


* **動作環境・パフォーマンス**
  * Windows環境で動作すること（管理者権限または開発者モードでのシンボリックリンク作成に対応）。
  * 10万曲規模のデータベース処理でも、メモリを圧迫せずに安定して高速に処理が完了すること。




# 外部設計書（System External Design Document）

## 1. 入出力設計（Interface Design）

### 1.1 入力仕様（Input Specifications）

本システムは起動時、設定ファイル（`config.json`）またはコマンドライン引数（パラメータ）からパス情報を取得する。

| パラメータ名 | 必須/任意 | 説明 | デフォルト値（`config.json` 自動生成時） |
| --- | --- | --- | --- |
| `realm_path` | 必須 | osu!Lazerの `client.realm` パス| `%APPDATA%/osu/client.realm` |
| `files_path` | 必須 | osu!Lazerのハッシュファイル格納ディレクトリパス| `%APPDATA%/osu/files` |
| `output_path` | 必須 | TaikøNauts参照用（`osuTaiko`）出力先ディレクトリ | なし（初回設定時に保存） |

  * **設定保持機能（`config.json`）**：初回実行時に指定されたパス情報を保存する。2回目以降の起動時は引数を省略しても `config.json` の内容を自動参照し、将来的なGUI化時の初期入力値としても流用する。

### 1.2 出力仕様（Output Specifications）

* **ファイルシステム出力**：
  * `output_path` 配下にTaikøNauts参照用のシンボリックリンク構造を出力する。
  * `output_path/osuTaiko/manifest.json`（管理用キャッシュファイル）を出力・更新する。


* **画面出力（CLI / ログ表示）**：
  * 通常時：更新処理結果のサマリー（追加数、更新数、削除数等）のみを出力し、標準出力を汚さない。
  * エラー発生時：標準エラー出力および画面上に詳細を表示し、エラー時のみファイルへのログ出力を行う（不要なディスク書き込みを抑止）。



---

## 2. ディレクトリ構造設計（Directory & Link Structure）

出力先ディレクトリ配下に `osuTaiko` フォルダーを自動生成（存在しない場合）し、その内部に各ビートマップセットのフォルダーおよび同期管理キャッシュを配置する。

### 2.1 フォルダ命名規則

```text
[output_path]/osuTaiko/[タイトル]_[アーティスト名]_[BeatmapID]/

```

* **区切り文字**：アンダーバー（`_`）を採用（パス解決時の誤認識を防止）。
* **一意性の担保**：末尾に `BeatmapID` を付与することで同名曲の重複事故を防止する。

### 2.2 ディレクトリ構成イメージ

```text
[output_path]/
 └── osuTaiko/
      ├── manifest.json                  # 管理用キャッシュファイル（新規追加）
      ├── TitleA_ArtistA_123456/
      │    ├── TitleA [Difficulty1].osu  (Symbolic Link)[cite: 5]
      │    ├── TitleA [Difficulty2].osu  (Symbolic Link)[cite: 5]
      │    ├── audio.ogg                 (Symbolic Link)[cite: 5]
      │    └── bg.jpg                    (Symbolic Link)[cite: 5]
      └── TitleB_ArtistB_789012/
           └── ...

```

---

## 3. 同期・高速差分検出ロジック設計（Synchronization Logic）

HDDの寿命保護および処理速度極大化のため、ディスク全走査を行わず `manifest.json` を活用したメモリ上での高速差分比較を行う。

### 3.1 管理用キャッシュ（`manifest.json`）のデータ構造

各 `BeatmapSetID` に対するフォルダ名、および含まれるファイルの「リンク名 ➔ 原本ハッシュ値」のペアを保持する。

```json
{
  "last_synced": "2026-10-05T13:45:00",
  "beatmap_sets": {
    "2506999": {
      "folder_name": "Watashi wa..._Kaguya_2506999",
      "files": {
        "Futsuu.osu": "56df732197794cc20cdf108e473914bd1f20d6573bd4f95efd0e083112413407",
        "audio.ogg": "cdce4e91d5dcb39a65769c024dc69be6bf9337d12dc4818806059e637ec52be3"
      }
    }
  }
}

```

### 3.2 処理フロー

1. `client.realm` を読み込み、最新の `taiko` 譜面データおよびハッシュ一覧（A）を抽出する。


2. `manifest.json`（B）をメモリ上に読み込む（存在しない場合は新規作成）。
3. **メモリ上で A と B を高速照合**：

    * **Bに存在せずAに存在** ➔ 新規リンクフォルダ作成
    * **AとBでハッシュ値が変更** ➔ 該当ファイルのリンク更新（古いリンクを削除し再作成）
    * **Bに存在しAから消滅** ➔ デッドリンクおよびフォルダを実ディスクから削除


    * **AとBのハッシュ値が完全一致** ➔ **ディスク処理なし（スキップ）**


4. 処理完了後、最新の状態を `manifest.json` に保存する。

---

## 4. 例外・エラーハンドリング設計（Exception & Error Handling）

### 4.1 Windowsファイルシステム禁止文字の不活性化（Sanitization）

ファイル名およびフォルダ名に使用できない禁止文字（`\ / : * ? " < > |`）が含まれる場合、すべて半角アンダーバー（`_`）に自動置換する。

### 4.2 フォルダ名・ファイル名の衝突（Collision）制御

* `BeatmapID` をフォルダ名に含めるため基本設計上衝突は発生しない。
* 万が一衝突した場合は名称末尾へ連番（`_1`, `_2`...）を付与して回避し、エラーログに記録する。

### 4.3 権限ハンドリング（UAC / 管理者権限昇格）

異なるドライブ（別HDD/SSD）間でのリンク作成に対応するため **シンボリックリンク** を使用する。

* **単体起動時**：管理者権限の有無をチェックし、権限がない場合はWindowsのUACダイアログを呼び出して自動昇格・再起動する。
* **バッチファイル（ESE等）呼び出し時**：親バッチファイル側で事前昇格処理（PowerShell等の `RunAs`）を行える設計とし、起動時のポーズ（一時停止）を防止する。




# 内部設計書（System Internal Design Document）

## 1. モジュール構成設計（Module Architecture）

コードの保守性と単体テストの容易性を担保するため、機能を役割ごとに分離する。
※将来的な単一ファイル化（`main.py` への統合）および EXE化を見越し、各モジュールは独立したクラス／関数として設計する。

```text
osuLazer2TaikNauts/
 ├── config_manager.py     # 設定ファイル（config.json）管理モジュール
 ├── realm_reader.py       # client.realm 解析・抽出モジュール
 ├── manifest_manager.py   # キャッシュ（manifest.json）比較・更新モジュール
 ├── linker.py             # シンボリックリンク生成・削除実行モジュール
 ├── utils.py              # 共通ユーティリティ（文字置換・管理者権限チェック等）
 └── main.py               # 全体制御エントリポイント

```

---

## 2. 各モジュールの役割・機能詳細

### 2.1 `config_manager.py`（設定管理）

* `config.json` の読み込みおよび保存を担当する。
* `config.json` が存在しない場合、デフォルト値（`%APPDATA%/osu/client.realm` 等）を設定したファイルを自動作成する。


* CLIの引数でパスが渡された場合は、それを優先しつつ `config.json` を更新する。

### 2.2 `realm_reader.py`（DB解析エンジン）

* `client.realm` のロックスキップ（osu!起動時対策）のため、一時フォルダーへ安全に複製してから読み込む。


* `Ruleset.ShortName == "taiko"` に該当する `Beatmap` データをフィルタリング抽出する。


* 抽出した `BeatmapSet` から、以下の辞書型データ構造を構築して返す。


* `beatmap_set_id`: フォルダ名（サニタイズ済み）、および各ファイル（`.osu`, 音声, 画像等）の「相対パス ➔ 原本ハッシュ値」





### 2.3 `manifest_manager.py`（高速差分比較エンジン）

* `output_path/osuTaiko/manifest.json` のロードおよび保存を担当する。
* `realm_reader` から取得した最新データ（A）と `manifest.json` 内のデータ（B）をメモリ上でメモリ比較し、以下の3つのリストを生成する。
1. **`create_list`**：新規作成が必要なリンク・フォルダ（Bに存在しないAの項目）
2. **`update_list`**：更新が必要なリンク（AとBでハッシュ値が異なる項目）
3. **`delete_list`**：削除が必要なデッドリンク・空フォルダ（Aに存在しないBの項目）



### 2.4 `linker.py`（リンク同期実行エンジン）

* `manifest_manager` から受け取った 3 種類のリスト（`create`, `update`, `delete`）に基づき、実際のディスク操作を実行する。
* **削除処理**：`delete_list` および `update_list` の対象となる旧シンボリックリンクを物理削除（`os.unlink`）。


* **作成処理**：`create_list` および `update_list` の対象に対して `os.symlink()` でシンボリックリンクを作成。
* **スキップ処理**：変更がない項目は一切のファイルアクセスを行わない。

### 2.5 `utils.py`（共通ユーティリティ）

* `sanitize_filename(name)`: ファイル名・フォルダ名に使用できない禁止文字（`\ / : * ? " < > |`）を `_` に置換する。
* `is_admin()`: `ctypes.windll.shell32.IsUserAnAdmin()` を使用して現在の実行プロセスが管理者権限を持っているか判定する。
* `elevate_privileges()`: 管理者権限がない場合、`ShellExecuteEx` (UAC) を呼び出して自プロセスを管理者権限で再起動する。

### 2.6 `main.py`（エントリポイント）

* 全体の処理フローを制御し、例外ハンドリング（エラー時のログ出力）を行う。

---

## 3. メイン処理フロー（Main Execution Sequence）

```
[開始]
  │
  ├── 1. 管理者権限の判定 (utils.is_admin)
  │     ├── 権限なし ──> UAC昇格ダイアログを表示して再起動 (utils.elevate_privileges) -> [旧プロセス終了]
  │     └── 権限あり ──> 処理継続
  │
  ├── 2. 設定のロード (config_manager)
  │     └── config.json から各パスを取得 (CLI引数があれば上書き)
  │
  ├── 3. client.realm から taiko 譜面データの抽出 (realm_reader)[cite: 1]
  │     ├── client.realm を一時フォルダへコピー[cite: 1]
  │     └── ルールセット taiko の BeatmapSet / ハッシュマップをメモリ構築[cite: 1, 5]
  │
  ├── 4. キャッシュ比較と差分リスト算出 (manifest_manager)
  │     ├── manifest.json をロード (存在しなければ空として処理)
  │     └── メモリ上で照合し、[作成] [更新] [削除] の3リストを生成
  │
  ├── 5. ディスク同期の実行 (linker)
  │     ├── デッドリンク・更新対象の旧リンク削除
  │     └── 新規・更新リンクの作成 (os.symlink)
  │
  ├── 6. キャッシュの更新 & 後処理
  │     ├── 最新の同期状態を manifest.json に保存
  │     ├── 一時コピーした client.realm の削除[cite: 1]
  │     └── 実行結果サマリー（追加・更新・削除数）のコンソール表示
  │
[終了]

```

