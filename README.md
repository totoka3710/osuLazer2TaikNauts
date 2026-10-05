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

本システムは実行時に以下の3つのパス情報を引数（パラメータ）または設定として受け取る。

| 引数名 | 必須/任意 | 説明 | デフォルト値（想定） |
| --- | --- | --- | --- |
| `realm_path` | 必須 | osu!Lazerの `client.realm` のファイルパス

 | `%APPDATA%/osu/client.realm` |
| `files_path` | 必須 | osu!Lazerのハッシュ化ファイル格納ディレクトリ

 | `%APPDATA%/osu/files` |
| `output_path` | 必須 | リンク群を出力・生成するルートディレクトリのパス | なし（ユーザー指定） |

### 1.2 出力仕様（Output Specifications）

* **ファイルシステム出力**：指定された `output_path` 配下に、TaikøNauts参照用のシンボリックリンク構造を出力する。
* **画面出力（CLI / ログ表示）**：
* 通常時：処理完了サマリー（作成数、更新数、削除数等）のみを表示し、標準出力（Console）を汚さない。
* エラー発生時：標準エラー出力および画面上にエラー詳細（失敗理由、影響箇所）を表示する。ファイルへのログ保存はエラー発生時のみ（またはオプション指定時のみ）行う仕様とし、不要なディスク書き込みを抑止する。



---

## 2. ディレクトリ構造設計（Directory & Link Structure）

出力先ディレクトリ配下に `osuTaiko` フォルダーを自動生成（存在しない場合）し、その内部に各ビートマップセットのフォルダーを構築する。

### 2.1 フォルダ命名規則

```text
[output_path]/osuTaiko/[タイトル]_[アーティスト名]_[BeatmapID]/

```

* **区切り文字**：アンダーバー（`_`）を採用（パス解決時の誤認識を防止するため）。
* **一意性の担保**：フォルダ名末尾に `BeatmapID` を付与することで、同名曲の重複事故を防止する。

### 2.2 ディレクトリ構成イメージ

```text
[output_path]/
 └── osuTaiko/
      ├── TitleA_ArtistA_123456/
      │    ├── TitleA [Difficulty1].osu  (Symbolic Link)[cite: 5]
      │    ├── TitleA [Difficulty2].osu  (Symbolic Link)[cite: 5]
      │    ├── audio.ogg                 (Symbolic Link)[cite: 5]
      │    └── bg.jpg                    (Symbolic Link)[cite: 5]
      └── TitleB_ArtistB_789012/
           └── ...

```

---

## 3. 同期・更新ロジック設計（Synchronization Logic）

osu!Lazer側での譜面更新・削除に伴う「デッドリンク（リンク切れ）」を防止し、高速に同期を行うため、差分更新ロジックを実装する。

```
[処理フロー]
1. 出力先 (osuTaiko) 内の既存リンクを走査
2. client.realm の最新データと照合[cite: 1]
3. 参照先（ハッシュファイル）が存在しないリンク（デッドリンク）を削除
4. 走査データと client.realm の差分（未作成の新規・更新ファイル）のみを抽出[cite: 1]
5. 差分データに対してのみシンボリックリンクを作成

```

---

## 4. 例外・エラーハンドリング設計（Exception & Error Handling）

### 4.1 Windowsファイルシステム禁止文字の不活性化（Sanitization）

ファイル名およびフォルダ名に使用できない禁止文字（`\ / : * ? " < > |`）が含まれる場合、すべて半角アンダーバー（`_`）に自動置換する。

### 4.2 フォルダ名・ファイル名の衝突（Collision）制御

* `BeatmapID` をフォルダ名に含めるため、基本設計上衝突は発生しない。
* 万が一同名フォルダ／ファイルが競合した場合は、自動的に名称末尾へ連番（`_1`, `_2`...）を付与して回避し、処理完了時のエラーログに衝突検知の旨を記録する。

### 4.3 権限ハンドリング（UAC / 管理者権限昇格）

異なるドライブ（別HDD/SSD）間でのリンク作成に対応するため、**シンボリックリンク** を使用する。

* **単体起動時の自律昇格処理**：
プログラム起動時、管理者権限の有無をチェックする。権限がない場合は、WindowsのUAC（ユーザーアカウント制御）ダイアログを自動呼び出しし、管理者権限へ昇格した上で処理を継続する。
* **バッチファイル（ESE等）からの呼び出し対応**：
親バッチファイル側で事前に管理者権限を取得する記述（PowerShellを用いた `Start-Process -Verb RunAs` 等の昇格コマンド）を追加できるように配慮し、子プロセスへ権限が自動継承される設計とする。
