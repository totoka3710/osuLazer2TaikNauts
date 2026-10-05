# osuLazer2TaikoNauts  
<ins>**!!コンピューター系を学び始めたばかりです。間違っていることやバカなことも言ってると思いますが、温かい目で見守ってください!!**</ins>  

osu!lazerが管理するハッシュ化されたビートマップをosuファイルの読み込みに対応しているTaikøNautsっていう太鼓シミュで読み込めるようにするためのプログラム。  
`client.realm`を読み込んで`Beatmap表`の`Ruleset欄`が`Ruleset{ShortName = taiko}`に一致するもののみTaikøNautsのSongsフォルダー内などにシンボリックリンクでリンクする。  
必要なファイルの情報(譜面(`.osu`)や音源、背景画像や動画)の情報およびハッシュ値は`BeatmapSet表`の`Files欄`を参照する。`Beatmap表`(およびそれにリンクされる`BeatmapMetadeta表`)の内容を参考にしてもよいが、今のところ`BeatmapSet表`のみ(`Ruleset欄`を除く)を参照する計画である。  
各`BeatmapSet表`の各項目ごとに一つのフォルダーを作成し、その中に`.osu`ファイルや音源などを入れ込んでいく。
曲数にもよるが、シンボリックリンクなので単純にコピーするよりも容量は大幅に削減できる。  

## 余裕があれば付けたい機能  
`BeatmapCollection表`を参考にジャンルフォルダーに対応。  
`Score表`を参照してosu!lazerでのプレイデータをTaikøNauts内で表示。  

[osu!公式サイト](https://osu.ppy.sh/ )  
[TaikøNauts公式サイト](https://taikonauts-docs.pages.dev/ )  

## 参考  
[File storage in osu!(lazer)](https://osu.ppy.sh/wiki/en/Client/Release_stream/Lazer/File_storage )  osu.ppy.sh  
[User file storage](https://github.com/ppy/osu/wiki/User-file-storage )  github.com  
[osu! file formats](https://osu.ppy.sh/wiki/en/Client/File_formats )  os.ppy.sh  
[Upgrading to lazer](https://osu.ppy.sh/wiki/en/Help_centre/Upgrading_to_lazer )  osu.ppy.sh  
[使用Realm Studio查询osu! Lazer谱面下载时间、最后游玩时间](https://post.cplus8.com/d/936 )  post.cplus8.com  


## 以下Geminiが生成してくれたREADME  
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

