# 判断記録（ADR）

<!--
このファイルの目的:
AIとの対話や口頭で決まったことは記録しないと消える。その受け皿。
迷ったら全部ここに書く。場所を間違えるコストがゼロになるよう設計している。

追記のみ。過去のエントリは書き換えない。判断が変わったら新しく書き、
古い方を status: Superseded by [D-YYY] にする。

並びは D 番号の昇順。日付では区切らない。日付は各項目の date: が持つ。
ID は既存の最大値 + 1。欠番・再利用はしない。
date: は判断した日。過去の判断を後から起票したときは記録した日を入れ、
判断した日と典拠のコミットを **背景** に書く。

--- テンプレート ---
## [D-XXX] タイトル
status: Proposed | Accepted | Superseded by [D-YYY]
date: YYYY-MM-DD
scope: 影響を受けるパス（CIの存在チェック対象）

**背景** — なぜ今決める必要があるか。このセクションだけで状況が再現できること
**検討した選択肢** — 却下案も Pros/Cons つきで。ここが空の記録は事後正当化
**決定** — 何を選んだか
**帰結** — 良い面、悪い面、監視すべき兆候（判断が誤りだった場合の検出条件）
-->

## [D-001] Unity 6.3 LTS (6000.3.22f1) を採用する
status: Accepted
date: 2026-08-23
scope: ProjectSettings/ProjectVersion.txt, Packages/

**背景** — 2022.3.5f1 で開発していたが、Unity CLI / com.unity.pipeline による
エージェント連携が Unity 6.0 以降を必須要件としていた。
**検討した選択肢** — (a) 2022 に留まりエージェント連携を諦める (b) Unity 6.3 LTS へ移行。
移行コストが懸案だったが、調査の結果 URP は未割り当て（Built-in RP 運用）で、
最大の難所である URP 14→17 の RenderGraph 移行を回避できることが判明した。
**決定** — (b)。6000.3.22f1 へ移行。
**帰結** — API Updater が velocity→linearVelocity、PhysicMaterial→PhysicsMaterial を自動修正。
vHierarchy が内部 API 変更で InvalidCastException を出したため削除した（[D-002] 参照）。
Play 通し・ビルド検証は未実施。壊れていた場合の検出はここが最初の砦になる。

## [D-002] バージョン管理を Git に一本化し、UVCS を廃止する
status: Accepted
date: 2026-08-23
scope: Packages/manifest.json, .plasticignore, README.md

**背景** — UVCS と Git を併用していたが、1プロジェクトを2リポジトリに分けると
コード変更とアセット変更のアトミックなコミットが不可能になり、
「この時点の状態」を再現できなくなる。
**検討した選択肢** — (a) UVCS の GitSync による併用 (b) Git 単独。
(a) は機能としては成立するが、PR / Actions / CODEOWNERS を前提にした設計と噛み合わない。
**決定** — (b)。com.unity.collab-proxy を削除し .plasticignore を除去。
**帰結** — third-party アセット 4.3 GB は .gitignore 対象のため Git では復旧できない。
物理バックアップが唯一の防御手段になる。UVCS 側のリポジトリ本体は削除していない。

## [D-003] エージェント制御を Unity 公式の Pipeline に委ね、自作の実行コマンド制限を撤回する
status: Accepted
date: 2026-08-25
scope: AGENTS.md, .claude/settings.json, CODEOWNERS

**背景** — 以前、エージェントの書き込み権限を `[CliCommand]` ホワイトリスト方式で
物理的に制限する方針を採っていた。これは当時、公式のエージェント制御面が
存在しなかったため、その不在を自作で埋める判断だった。
2026年7月に Unity CLI + com.unity.pipeline が公開され、前提が変わった。
**検討した選択肢** — (a) 自作ホワイトリストを維持 (b) 公式機構に委ねる。
(a) は公式機構と二重管理になり、かつ実装が存在しない（asmdef 0件、テスト0件の段階で
統治規則だけが積み上がっていた）。
**決定** — (b)。`set_authoring_root` による書き込み範囲の封じ込めと、
破壊的コマンドの `confirm=true` 強制を担保とする。
**帰結** — 公式機構は Unity 側で保守されるため腐らない。ただし com.unity.pipeline は
experimental (0.5.0-exp.1) で破壊的変更があり得る。
判断が誤りだった場合の検出条件: Pipeline の仕様変更で authoring_root や confirm が
弱くなったとき。その時点で (a) の再検討が要る。

## [D-004] エージェントの権限は「不可逆な操作のみ」を対象に絞る
status: Accepted
date: 2026-08-25
scope: .claude/settings.json, AGENTS.md

**背景** — 当初、`eval` / `reload_file` / `package_add` / `build` などを広く禁止する
deny リストを設計したが、過剰であることが判明した。
**検討した選択肢** — (a) 広範な deny (b) 不可逆な操作のみに絞る。
(a) の問題は、`eval` を禁止してもエージェントは .cs を書いて `recompile` できるため
能力が減らず、経路が回りくどくなるだけだったこと。禁止に見えて何も守っていない。
**決定** — (b)。deny は `set_authoring_root` の変更と破壊的 git 操作のみ。
ask は GUID を伴う資産操作（delete/move/rename_asset）、git push、ProjectSettings 編集。
**帰結** — permission ルールは前方一致でありサンドボックスではない。
確実な担保は authoring_root（サーバ側）、confirm=true（サーバ側）、
コミット済みの Git 状態と物理バックアップの3つ。deny は床上げであって天井ではない。

## [D-005] シーン内の参照割り当てはエージェントが行う
status: Accepted
date: 2026-08-25
scope: Assets/_Project/Scenes/, .claude/settings.json

**背景** — スクリプトに `[SerializeField]` を足すと、Inspector での参照割り当てが必ず発生する。
手作業は面倒なうえ、ドラッグ先を間違えても静かに壊れる（実行するまで気づかない）。
`Craft` に `recipeDefinitions` を追加した際に実際に発生した。
**検討した選択肢** — (a) 人間が Inspector で行う (b) エージェントが
`unity command set_component_properties` で行う。
シーン内コンポーネントへの値設定は deny にも ask にも入っておらず、境界が未定義だった。
**決定** — (b)。シーン内コンポーネントへの値・参照の設定は、確認を取らずに実行してよい。
**帰結** — [D-004] の「不可逆な操作のみ止める」に照らして矛盾しない。
値の設定は Undo 1ステップで戻せ、シーンは Git 追跡下なので `git restore` でも戻せる。
差分も読める。今回の割り当ては +5 −2 行で、`recipeDefinitions` の追加と
`craftdata` の削除がそのまま現れた。シーン全体が再シリアライズされる場合
（エディタのバージョン更新時など）を除き、変更箇所だけが差分に出る。
そのため変更履歴用の専用ログは設けない。git の差分とコミットメッセージが記録になる。
GUID を動かす操作（`move_asset` / `rename_asset` / `delete_asset`）は引き続き ask のまま。

## [D-006] `static` を状態置き場にしない
status: Accepted
date: 2026-08-25
scope: docs/conventions.md, Assets/_Project/

**背景** — 既存コードは可変な状態の多くを static フィールドに置いている
（`Player2.playerhp` / `GameManager.entered_scene` / `Sun2.daytime` /
`RootsManager.roots` / `QuestManager.quests`）。
**検討した選択肢** — (a) 現状を追認する (b) 新規コードでは禁止する。
(a) の問題は2つ。誰でもどこからでも書き換えられるため**変更の経路が追えない**こと、
そして**テストが書けない**こと（前のテストが残した状態が次に持ち越される）。
**決定** — (b)。所有者を1つ決め、参照で渡す。定数は対象外。
**帰結** — 既存コードの広い範囲を否定する規則であり、移行の方針そのものになる。
Legacy は据え置き、機能を移行するときに従う。
検出は将来 Analyzer に落とせる見込みがあり、落ちたら conventions.md から削除する。

## [D-007] public 可変フィールドを避ける。ただし ScriptableObject は例外
status: Accepted
date: 2026-08-25
scope: docs/conventions.md, .editorconfig, tests/Domain.Tests/

**背景** — 今日見つけた問題の多くが「public な状態に外から直接触れる」ことに
起因していた（`pi.items = items` / `Player2.speed *= 1.3f`）。
**検討した選択肢** — (a) 全面禁止で一貫させる (b) SO のデータ入れ物は例外とする。
判断の分かれ目は「機械で強制できるか」だった。SO は `UnityEngine` 型のため
Domain に置けず、アナライザからは SO かどうかを判別できない。
つまり**どちらを選んでも Game 層では文章による担保にしかならない**ため、
一貫性を取る利得が無い。SO で public フィールドを使うのは Unity の一般的な慣習でもある。
**決定** — (b)。ただし代償として「SO は読み取り専用として扱う」を規約に加える。
実行中に SO を書き換えるとエディタ上で `.asset` に永続化され、Play を抜けても戻らない。
**帰結** — Domain 層のみ `tests/Domain.Tests` のビルドで CA1051 が機械的に検出する。
Unity のコンパイルは .NET SDK を通らないため、Game 層は検出されない。
構造体の `public readonly` フィールドも property に統一した。両者は等価
（不変性・読み方・生成コードが同じ）であり、`exclude_structs` という設定の例外を
足さずに済む方を選んだ。等価な選択肢が2つあるなら、設定を足さない方を採る。

## [D-008] テストメソッド名は日本語の平叙文にする
status: Accepted
date: 2026-08-25
scope: Assets/_Project/Tests/

**背景** — エージェントが実装とテストの両方を書く。テストはエージェントの理解を
固定するため、その理解が仕様と食い違っていても緑になる。
実例として、`Speedneckless` の処理をエージェントが「アイテム追加関数に装備効果が
直書きされた地雷」と誤認した。実際は所持しているだけで加速する意図的な仕様であり、
人間の指摘が無ければ誤解のままテストに焼き付いていた。
**検討した選択肢** — (a) `Add_WhenSlotIsEmpty_PlacesItem` 形式 (b) 日本語の平叙文。
(a) は読むのに実装の知識が要る。
**決定** — (b)。テスト名は**実装を読まずに仕様を確認できる唯一の場所**として扱う。
振る舞いだけを書き、メソッド名や引数など呼び出し方は含めない。
**帰結** — 人間はテスト一覧を眺めるだけで、エージェントの理解と仕様のずれに気づける。
この理由が失われると規則が恣意的に見えるため、必ず参照できる状態に保つこと。

## [D-009] 構造の確認は、生成した差分図と手で維持する現状図の二段で行う
status: Superseded by [D-017]（[D-011] が現状図の維持を覆し、[D-017] が図そのものを廃止した）
date: 2026-08-28
scope: docs/dependencies-diagrams, docs/dependencies-diff-diagrams, tools/diagram-diff.ps1

**背景** — 複数クラスをまたぐ変更のあと、何がどう繋がり直したのかが
git diff からは読み取れない。行の増減しか見えず、依存の増減が見えない。
一方で全型を1枚の図にすると読めない（Legacy はルート直下だけで35型ある）。

**検討した選択肢** — (a) 全体図を1枚生成する (b) クラスごとに近傍図を生成する
(c) 差分図を生成し、現状図は人が維持する。
(a) は読めない。(b) は64節になり、変更のたびに無関係な節まで動いて diff が濁った。

**決定** — (c)。役割を2つに分ける。

  docs/dependencies-diff-diagrams/  変更で何が動いたかの図。生成物。
  docs/dependencies-diagrams/       今どうなっているかの図。人が維持する。

素データ（graph.txt）は DependencyGraphTests が生成し、ソースとズレたら失敗する。
差分図は tools/diagram-diff.ps1 が graph.txt の前の版と今を比べて作る。
現状図は機能のスライス単位で人が切る。どう切るかは機械に決められないため。

**帰結** — 手で維持する現状図は放っておけば腐る。腐り方のうち2つを
SliceDiagramTests が見張る。実在しない依存が描かれていないこと、
Domain / Game の型がどれかのスライスに出ていること。
どう切るか（何を1枚にまとめるか）は見ない。そこは人の判断で、
機械が正解を持てない。

手順と書式は `.claude/skills/structure-diff/` に置き、AGENTS.md には
ポインタだけ残す。AGENTS.md は常に読み込まれるので、そこに手順を書くと
使わない回でも費用を払う。スキルは必要になったときだけ載る。

判断が誤りだった場合の検出条件: スライスが機能と対応しなくなり、
1枚に10型以上入るようになったとき。そこで切り直す。

## [D-010] 層をまたぐ変数は型を明示する。var を使わない
status: Accepted
date: 2026-08-28
scope: docs/conventions.md, .editorconfig, Assets/LegacyScripts

**背景** — `var root = RootsManager.Roots.Find(...)` と書くと、ソースの字面に
`Root` が現れない。構造の差分図は Legacy を字面で解析している（リフレクションが
使えないため）ので、この書き方だと**依存が図から消える**。
実測で、`Root` への辺が7本あるのに3本しか出ていなかった。
Legacy から Domain への移行が、実際より4本ぶん少なく見えていた。

**検討した選択肢** — (a) var を全面禁止 (b) 右辺から型が明らかなときだけ許す
(c) 図の側で var を解決する。
(c) は型推論の実装が要る。Roslyn を持ち込めば可能だが、Legacy は UnityEngine に
依存していて .NET SDK のコンパイルを通らない。それが字面解析を選んだ理由そのもの。
(a) は `var x = new Foo()` まで禁じることになり、得るものがない。

**決定** — (b)。ただし判定を「型が明らかか」ではなく
**「Domain / Game の型かどうか」**に寄せる。層をまたぐ箇所に限れば規則が短くなり、
Legacy 全体を書き換える必要もない。

**帰結** — 道具の都合が動機の半分である。ただし独立した利点もあり、
`var root = ...` からは Domain の型だと読み取れないため、
型を書けば**読み手にも層の境界が見える**。

機械で強制できるのは `_Project` だけ。Legacy では文章による担保にとどまる
（[D-007] と同じ構図）。判断が誤りだった場合の検出条件: 図に出ない依存が
再び見つかったとき。そのときは (c) を検討する。

## [D-011] 現状図も生成する。切り方は Domain のファイル分割に従う
status: Superseded by [D-017]
date: 2026-08-28
scope: docs/dependencies-diagrams, tests/Domain.Tests/SliceDiagramGenerator.cs, .claude/skills/class-diff-diagram

**背景** — [D-009] は現状図を人が維持する形にした。理由は「どう切るかは機械に
決められない」。だがこれは成り立っていなかった。維持を人に任せた図は、
更新の手間より放置の楽さが勝つので必ず腐る。実際 `roots.md` は `RewardUI` が
`Root` を使うようになったことを反映しておらず、`SliceDiagramTests` は
「描かれた依存が実在するか」しか見ないためそれを見逃していた。
検査が緩いのではなく、検査で埋め合わせようとしたのが誤りだった。

**検討した選択肢** —
(a) 現状図を人が維持し、検査を厳しくする。
    → 「型がどのスライスに属すべきか」を機械が判定できないと厳しくできない。
       判定できるならそもそも生成できる。堂々巡り。
(b) 連結成分で自動的に切る。
    → `Recipe -> Inventory` があるためクラフトと持ち物が1つに融合する。
       型を1つ足すだけで切り口が変わり、図が毎回別物になる。
(c) Domain のソースファイル1つを1枚に対応させる。

**決定** — (c)。**ファイル分割そのものが既に関心事の区切り**なので、
切り方に新しい判断が要らない。`Root.cs` が `Root` `RootRegistry`
`AccumulationLevel` を宣言しているという事実が、そのままスライスの定義になる。
切り直したければファイルを分ければよく、図は自動で追従する。

  核     そのファイルが宣言する型
  周辺   核と辺で繋がっている型
  辺     少なくとも片端が核のもの

人が決めるのは見出しと一行説明だけで、それは SKILL.md のスライス表に置く。
`SliceDiagramGenerator` がそこから読む。表に行が無ければテストが落ちる。
図の下の「覚え書き」節だけは手書きで、作り直しても引き継ぐ。

**帰結** — 図は `dotnet test` のたびに作り直される。古ければ書き直したうえで
失敗するので、直す手間はゼロで、変化には気づける。
悪い面として、図が読みやすい形に収まる保証が無くなった。核の被参照が多いと
節点が増える。監視すべき兆候は 1枚が12型を超えること。そのときは Domain の
ファイルを分ける（図の都合ではなく、実際に関心事が2つあるということなので）。

素データの `[cofile]` 節は `[files]` に置き換えた。どのファイルが宣言したかを
持たないとスライスを切れないため。

手順と書式は `.claude/skills/class-diff-diagram/SKILL.md`。
[D-009] が `structure-diff/` と書いているのは誤りで、この名前が正しい。

## [D-012] Legacy の関心事は表で名指しする。ファイル構成からは導かない
status: Superseded by [D-017]
date: 2026-08-28
scope: docs/dependencies-diagrams, .claude/skills/class-diff-diagram

**背景** — [D-011] は「Domain のファイル1つ = 図1枚」で切った。Domain には
3ファイルしかないため、これだと図に出るのは62型のうち十数型で、残りの
Legacy 50型は地図のどこにも載らない。Domain 分離を進める土台としては
「今どこに何が絡んでいるか」こそ見たいので、これでは足りない。

**検討した選択肢** —
(a) Legacy もファイル1つ = 図1枚。→ 47枚になる。`Wink.cs` 1枚に意味が無い。
(b) ディレクトリで切る。→ Craft/ Inventory/ OutField/ TextLabel/ は取れるが、
    47ファイル中35が root 直下に平置きで、残りが1枚35型になる。
(c) 連結成分やクラスタリングで切る。→ [D-011] で却下済み。`dep` が
    ファイル単位の弱い辺なので、Legacy では全体が1つに繋がって切れない。
(d) 核を人が名指しし、周りは機械が描く。

**決定** — (d)。**構造が無いから Legacy なのであって**、そこから切り方を
導こうとするのは順序が逆。核の名指しだけを人が持ち、図そのものは生成する。

表の第1欄はファイルか型の並び。Domain / Game はファイルで指定して自動追従を
残し、Legacy は型を並べる。11枚に分けて62型すべてを割り当てた。

**帰結** — すべての型がちょうど1つの核に入っていることをテストが見張るので、
表はコードの索引を兼ねる。型を足して割り当てを忘れると落ちる。

Legacy を核とする図は大きい。最大で25節点になった（player.md）。[D-011] は
12型を切り直しの兆候としたが、それは Domain の話で、遠因はファイルの切り方に
あるから直せる。Legacy が大きいのは絡まっているという事実そのもので、図の
欠陥ではない。**大きさは読むべき情報**として残す。分離が進めば自然に縮む。

Legacy の公開メンバは載せない。多くが Inspector への口であって設計ではなく、
並べても関心事の輪郭が見えないため。

## [D-013] クラス図の生成に PlantUmlClassDiagramGenerator を使わない
status: Accepted
date: 2026-08-29
scope: docs/dependencies-diagrams, tests/Domain.Tests/

**背景** — 現状図は SliceDiagramTests が生成しているが、C# の読み取りは
DependencyGraphGenerator.cs の正規表現に依存している（384行中13箇所）。構文木を
使う既製ツールに置き換えられないか、PlantUmlClassDiagramGenerator（dotnet の
ローカルツール。コマンド名 puml-gen）を入れて試した。

**検討した選択肢** —
(a) 図の生成そのものを puml-gen に置き換える。→ 却下。`-createAssociation` を
付けて初めて関連線が出るが、その線はコレクション型を節点として作る。private
フィールドも関連線になる。保持と使用の区別（太線・点線）が無い。**型の構造を
そのまま図にする道具は、図に出したい関係と型の構造が一致しない限り使えない。**
ここでは「Quest が Reward を持つ」が「Quest → ``IReadOnlyList`1`` → Reward」に化ける。

(b) 抽出だけ puml-gen に任せ、.puml を中間表現として Mermaid 生成に食わせる。
→ 今回は採らない。``IReadOnlyList`1`` から要素型を取り出す正規化を自前で書く必要が
あり、**正規表現が消えるのではなく置き場所が変わるだけになる**。構文解析の
堅牢さと引き換えに、出力の正規化という別の脆さを買うことになる。

(c) 描画のため Java と Graphviz を各人の環境に入れる。→ (a) を却下したので不要。
Mermaid は GitHub と VS Code が標準で描くため閲覧環境の要求が発生しない。
**閲覧に追加インストールを要する形式は、閲覧されなくなる。**

**決定** — 現状図・差分図の生成に puml-gen を使わない。図の形式は Mermaid の
ままとする。抽出エンジンとしての利用（b）は、正規表現が実際に取りこぼした
事例が出てから再検討する。

**帰結** — 正規表現による C# 読み取りが残る。判断が誤りだった場合の検出条件:
DependencyGraphGenerator が式形式メンバ・ネストしたジェネリクス・複数行
シグネチャのいずれかを取りこぼし、図が実態とずれること。そのとき (b) を実装する。

## [D-014] Domain を UnityEngine 非依存にし、asmdef で物理強制する
status: Accepted
date: 2026-08-29
scope: Assets/_Project/Scripts/Domain/, Assets/_Project/Scripts/Game/, .github/workflows/domain-tests.yml

**背景** — 2026-08-25（4c5cc3a）の判断を後から起票する。`Assets/_Project/Scripts`
を分割するにあたり、Domain がエンジンに依存しないことをどう担保するかを決める
必要があった。

**検討した選択肢** —
(a) 名前空間とレビューで分ける。→ 却下。`using UnityEngine` を1行足した時点で
崩れ、しかも**崩れたことが誰にも通知されない**。強制力の無い層分けは、破った
コストがゼロなので必ず破られる。
(b) 単一アセンブリのまま置く。→ 却下。Domain のテストに Unity Editor の起動が
必要になる。**エンジンを起動しないと回らないテストは、CI でも手元でも回す頻度が
落ちる。**

**決定** — Domain の asmdef は `noEngineReferences: true`、`references: []` とする。
依存は Game → Domain の一方向のみ。Domain は UnityEngine を参照しない。

**帰結** — Domain だけを素の `dotnet test` で回せるようになり、CI
（domain-tests.yml）は Unity を起動しない。
代償として、asmdef 側から Legacy（Assembly-CSharp）を参照できない。参照は
一方向で、Legacy への asmdef 付与は HeroEditor が gitignore されているため今は
行えない。この制約は既に実害を出しており、PlayMode テストは `GameManager.SceneTrans`
をリフレクションで呼んでいる（[D-016] 参照）。判断が誤りだった場合の検出条件:
リフレクション経由の呼び出しが増え、改名がコンパイルで止まらなくなること。

## [D-015] ゲーム設計は Notion に置く。docs/GDD.md は消さずに残す
status: Accepted
date: 2026-08-29
scope: docs/GDD.md, README.md

**背景** — 2026-08-27（c4df091）の判断を後から起票する。分厚い GDD は複数人の
ズレを防ぐ道具であり、現在1人では防ぐべきズレが無い。実際 Inventory と Craft の
Domain 分離で仕様のすり合わせは起きたが、着地したのはテスト名と ScriptableObject
のフィールドで、GDD は要らなかった。

**検討した選択肢** —
(a) 分厚い GDD をリポジトリで維持する。→ 却下。**読み手のいない文書は更新されず、
更新されない文書は誤りを配る。**
(b) `docs/GDD.md` を削除する。→ 却下。消すと「一度考えて、置かないと決めた」
事実が失われる。**却下の記録が無い決定は、同じ議論をゼロからやり直す。**

**決定** — ゲーム設計は Notion（個人 Wiki）に置く。柱とコアループはそのトップ
ページ。リポジトリ側の正典は従来どおり、挙動 = テスト / 数値 = ScriptableObject /
理由 = DECISIONS.md とする。Notion での議論がコードの形を決めるなら、その時点で
DECISIONS.md へ移す。`docs/GDD.md` は残す。

**帰結** — 設計の一部がリポジトリ外の外部サービスに載る。公開リポジトリからは
辿れず、Notion が失われれば設計意図も失われる。複数人になった時点で方針を
見直す。判断が誤りだった場合の検出条件: Notion を参照しないとコードの意図が
読めない箇所が現れること。そのとき DECISIONS.md へ移す。

## [D-016] PlayMode テストは判定せず、例外が出ないことだけを見る
status: Accepted
date: 2026-08-29
scope: Assets/_Project/Tests/Play/

**背景** — 2026-08-27（9b1233e）の判断を後から起票する。Domain 分割が作る壊れ方は
「コンパイルは通るが Play すると null」であり、EditMode / dotnet テストでは
素通りする。一方 Legacy は static と `DontDestroyOnLoad` での持ち回りに依存して
おり、細かい判定を書ける状態にない。

**検討した選択肢** —
(a) 挙動を判定する PlayMode テストを書く。→ 却下。**テストは触れた構造を固定
する。** 作り直す予定の Legacy の内部状態に判定を書けば、リファクタのたびに
テストを直すことになり、リファクタを妨げる資産になる。
(b) PlayMode テストを持たない。→ 却下。(a) の理由は「判定を書かない」根拠には
なるが、「網を張らない」根拠にはならない。起動時の null は最も安く捕まえられる。

**決定** — PlayMode テストは判定を書かず、例外が出ないことだけを見る。Unity Test
Framework が LogError と例外を自動で失敗にするため、これだけで網になる。TearDown
は「Play を押し直した直後の状態に戻す」まで行う。オブジェクトの破棄では足りない。
static のフラグとリストが残り、2本目以降が落ちるため（[D-006] の実例）。

**帰結** — シーン遷移は `GameManager.SceneTrans` をリフレクションで呼ぶ（[D-014]
の一方向依存による）。改名がコンパイルではなく実行時に落ちる点はスモークテスト
として許容する。判断が誤りだった場合の検出条件: 例外は出ないが壊れている
不具合を、この網が続けて見逃すこと。

## [D-017] 依存を図にする仕組みを廃止し、型の被覆検査だけ残す
status: Accepted
date: 2026-08-30
scope: docs/slices.txt, tests/Domain.Tests/

**背景** — 現状図11枚と差分図を生成し、`dotnet test` が作り直していた。
図は人間が読むためのもので、エージェントには読ませない前提だった（AGENTS.md の禁止）。
つまり生成物の読み手は1人しかおらず、その1人も参照していなかった。
一方で生成器は 712 行あり、C# の読み取りを正規表現で行っていた。

**検討した選択肢** —
(a) 図の生成を続ける。→ 却下。**読み手が実際に読んでいない生成物は、
正しさを誰も検証しないまま古くなる。**図の再生成が毎回の差分に出るぶん、
コミットの意味を薄める代償だけが残る。
(b) 検査ごと全部やめる。→ 却下。型を足したとき関心事への割り当てを忘れる事故は
実際に起きており（`Day.cs` の3型）、検査がそれを止めた。**出力をやめる判断は、
その出力を作る過程で得ていた検査までやめる理由にはならない。**

**決定** — 現状図・差分図の生成を廃止する。`docs/dependencies-diagrams/`、
`docs/dependencies-diff-diagrams/`、`tools/diagram-diff.ps1` を削除する。
スライス表は `docs/slices.txt` に移し、「すべての型がちょうど1つの核に入っている」
検査だけを残す。`DependencyGraphGenerator.Generate()` は検査が型の一覧を得るために
残すが、ファイルには書き出さない。

**帰結** — 構造の変化を1枚で見る手段が無くなる。git diff から読み取るしかない。
判断が誤りだった場合の検出条件: 複数クラスをまたぐ変更で「何を参照しなくなったか」が
分からず、同じ調査を繰り返すこと。
