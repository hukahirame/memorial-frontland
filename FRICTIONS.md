# やばい穴
 > 追記形式

踏んだもの、まだ残っているものを積む。直したら消さずに「直した」と書き足す。
判断の理由は `docs/DECISIONS.md`、時系列は `docs/JOURNAL.md`。ここは穴だけ。

---

## 2026-08-29 初回。既知のぶんを棚卸し

### 🩸 製品に残っている穴

- **セーブに根源とクエストが入っていない。** `SaveData.cs:13` に理由がある。
  `JsonUtility` が `List<string[]>` を保存できず、実際のセーブファイルにキー自体が
  無かった。それでもロード時に空リストで上書きしていたため、**ロードのたびに
  根源とクエストが消えていた。**同期ごと外して事故は止めたが、保存はされないまま。
  `RootRegistry` / `QuestRegistry` へ置き換えたのは並行リストの解消であって、
  この穴は塞がっていない
- **ビルドを一度も通していない。**Unity 6.3 移行以降ずっと。
  `PlayTests` の asmdef がビルドから除外されるかも未確認（`defineConstraints` を
  書いただけで確かめていない）
- 20 分プレイして氾濫を見ていない。無限ループを直したので初めて到達できるはず
- クラフト画面が未確認。Legendsword / Speedneckless で素材スロット 3・4 が隠れるか
- `git lfs prune` 未実行。未参照の LFS オブジェクトが約 461MB

### 🧪 テスト

- **`run_tests --mode all` は `--async_tests` に対応していない。**しかも外側は
  `"success": true` を返し、失敗は内側の `error` にしか出ない。
  **走っていないのに走ったように見える。**`editor` と `playmode` を別々に投げる
- **`--timeout` を付けない。**キャンセルで Pipeline がゲートを握ったまま空回りする
- **`cancel_tests` は安全なキャンセルではない。次の1回を道連れにする。**
  実測: 実行中に cancel → cancelled が返る → 再実行が running のまま固まる →
  もう一度 cancel で解ける → 再実行で完走。プロセスは殺さなくてよい
- 詰まりの判定は `test_status` が running かつ `editor_status` の playMode が
  stopped。**`editor_status` は `status: ready` を返し続けるので単体では検出できない**
- **PlayMode テストはテスト間でドメインを再読み込みしない。**static が残る。
  `GameManager.P_singleton` が true のまま残り、2 本目以降は誰も Player を
  起こさず NullReferenceException。TearDown は「Play を押し直した直後」まで戻す。
  オブジェクトの破棄では足りない（[D-006] の実例、[D-016]）
- 生の `LoadScene` では `entered_scene` が更新されず、`SceneStarter.cs:67` の
  `FindIndex` が -1 を返す。実際のゲームでは起きない壊れ方をする。
  遷移は `GameManager.SceneTrans` を通す
- **生成物をテストで検査するとき、改行を正規化しないと永久に落ちる。**
  生成側が LF で書き、git が作業コピーを CRLF に戻すため。次のチェックアウト以降
  ずっと不一致になる

### 🗑️ 生成物の廃止

- **生成物を消すときは、生成側も同時に落とす。**`docs/domain-class-diagram.md` だけ
  消しても `DomainDiagramTests` がファイルを作り直したうえで `Assert.Fail` し、
  CI が落ちる。ローカルで `dotnet test` を回した瞬間にファイルも復活する。
  消す対象は「生成物・生成器・csproj のエントリ・ドキュメントの記載」の4点セット

### 🔤 文字コードと改行

- **`.ps1` は BOM 付き UTF-8 必須。**PowerShell 5.1 が BOM 無しを CP932 として読む。
  一度直したのに再発している
- **`.cs` の BOM 無し + CP932 保存は、日本語ロケールの Windows でのみ通る。**
  Roslyn は BOM 無しを UTF-8 として読み、失敗するとシステム既定のコードページへ
  フォールバックするため。他ロケールや Linux の CI では化ける。4 件踏んだ
- `.json` `.yml` には BOM を付けない。拒否するパーサがある
- ファイル種別ごとに規則が違う。**`docs/conventions.md` に文字コードの節が無い**

### 🐚 シェル

- **通ったように見えて中身が違う形で 3 回壊した。**
  改行が潰れて C# の文字列リテラルが実改行になった /
  `` `u{1} `` は PowerShell 7 の記法で 5.1 では展開されず `"u"` で分割していた /
  BOM 付きを `utf-8` で読んで二重 BOM を作り構文エラー。
  **このとき古い出力が表示され続け、成功したように見えていた**
- 引用符を挟む操作は `Write` / `Edit` に切り替える。解釈が入らないので再発しない
- `git add <消したパス>` は失敗する。既に index に `D` で入っているため。
  `git add -A <パス>` を使う

### 📊 図

- `classDiagram` は辺を着色できない。`linkStyle` が無く、`themeCSS` は描画側の
  `securityLevel` に止められる。**メンバの区画と辺の装飾は両立しない**
- `classDef` の `color:` は `!important` が付き、ラベル内の `span` を潰す
- ラベルを `["..."]` で囲むので `style` の引用符はシングルでないと閉じる
- `<>` は HTML タグと解釈される。素データは C# のまま持ち、描画側で逃がす
- **`var` で受けた依存は字面に型名が出ないので図から消える。**`Root` への辺は
  3 本に見えて実際は 7 本だった。**Legacy → Domain の移行が 4 本ぶん少なく
  見えていた**（[D-010] で `var` を禁じた背景）
- 括弧を次の行に置くプロパティは正規表現から系統的に漏れる。
  `Root.Level` / `RootRegistry.All` / `Count` が図に出ていなかった
- 図が GitHub 上で実際に描画されるか未確認

### 🛠️ ツールとエージェント運用

- **`.claude/hooks/` に .md を置いてもスラッシュコマンドとして読まれない。**
  コマンドは `.claude/commands/` のみ。フックは settings.json から実行可能
  コマンドを呼ぶ仕組みで、.md をプロンプトとして解釈しない。adr.md が
  これで動いていなかった
- **禁止の理由を書かないと、禁止そのものが誤りに見える。**AGENTS.md の
  「`docs/dependencies-diagrams` を読むこと」は意図どおりの禁止（人間が読む用の
  生成物）だが、1つ上の JOURNAL の行だけが括弧で理由を持ち、この行は持たなかった。
  **理由の無い1行だけが浮き、誤記だと判断してしまった。**
  → 直した。理由を括弧で追記し、同じ様式に揃えた
- `.claude/commands/adr.md` の出力テンプレが DECISIONS.md の実形式と不一致だった。
  手順1の `grep "^## \[D-"` が 0 件を返し、**既存 ID を1つも見ずに採番しかけた。**
  **無言で 0 件を返す grep は、成功と区別がつかない。**
  → 直した。DECISIONS.md 側を日付節のない `## [D-XXX]` の平坦な一覧に変え、
  adr.md をそれに合わせた。日付は各項目の `date:` が持つ
- Unity のプロセスは `unity pipeline list` の PID で特定する。
  `Get-Process` の先頭は AssetImportWorker のことがある
- フリーズの判定は CPU ではなくログ行数。非フォアグラウンドだと無限ループでも
  CPU が立たない
- Unity Editor が閉じていると `unity command` が全滅する
- **検証せずに手順を書いて 3 回踏んだ**（.editorconfig、フック、`run_tests --mode all`）。
  「発火するところまで確認する」が守れていない
- **手段が無いと思い込んで 10 回以上、人に確認を依頼していた。**実際は
  `.claude/launch.json` に静的サーバを足して SVG の計算後スタイルを DOM から
  読めばよかった。「自分で確かめられないか」を先に疑う

### 📦 外部ツール

- `puml-gen` は `-h` / `--help` を受け付けない。「そのパスは存在しない」と返すだけで
  使い方が出ない
- `puml-gen -dir` は入力のディレクトリ階層を出力先にそのまま再現する
- winget で入れた JDK / Graphviz は、**起動済みのプロセスの PATH には反映されない。**
  VS Code もエージェントも再起動が要る
- `.puml` の描画には Java が要る。Mermaid は GitHub と VS Code が標準で描く（[D-013]）
