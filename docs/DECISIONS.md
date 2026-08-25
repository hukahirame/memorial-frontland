# 決定記録

<!--
このファイルの目的:
AIとの対話や口頭で決まったことは記録しないと消える。その受け皿。
迷ったら全部ここに書く。場所を間違えるコストがゼロになるよう設計している。

書き方は2種類あり、重要度に応じて使い分ける:

  [軽い記録] 箇条書き1〜3行。判断の質を問わない。整形しない。
             例: ライブラリ選定の小さいもの、局所的な方針

  [重い記録] 「### [決定] タイトル」の見出しを立て、下記テンプレで書く。
             目安は「構造を変える」「後戻りが高コスト」「同じ説明を2回した」

昇格: 軽い記録が2回以上参照されたら、重い記録として書き直す。
      元の行は消さず「→ 下記[決定]参照」を追記する。

追記のみ。過去のエントリは書き換えない。判断が変わったら新しく書く。
新しい日付を上に追加する。

このファイルが読みづらくなったら（目安500行、または目的の記録を
Ctrl+Fで探し始めたら）docs/decisions/ に分割する。
その分割自体もこのファイルに記録すること。

--- 重い記録のテンプレート ---
### [決定] タイトル
status: Proposed | Accepted | Superseded by <日付・タイトル>
scope: 影響を受けるパス（CIの存在チェック対象）

**背景** — なぜ今決める必要があるか。このセクションだけで状況が再現できること
**検討した選択肢** — 却下案も Pros/Cons つきで。ここが空の記録は事後正当化
**決定** — 何を選んだか
**帰結** — 良い面、悪い面、監視すべき兆候（判断が誤りだった場合の検出条件）
-->

## 2026-08-25

### [D-005] シーン内の参照割り当てはエージェントが行う
status: Accepted
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
## 2026-08-23

### [D-001] Unity 6.3 LTS (6000.3.22f1) を採用する
status: Accepted
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

### [D-002] バージョン管理を Git に一本化し、UVCS を廃止する
status: Accepted
scope: Packages/manifest.json, .plasticignore, README.md

**背景** — UVCS と Git を併用していたが、1プロジェクトを2リポジトリに分けると
コード変更とアセット変更のアトミックなコミットが不可能になり、
「この時点の状態」を再現できなくなる。
**検討した選択肢** — (a) UVCS の GitSync による併用 (b) Git 単独。
(a) は機能としては成立するが、PR / Actions / CODEOWNERS を前提にした設計と噛み合わない。
**決定** — (b)。com.unity.collab-proxy を削除し .plasticignore を除去。
**帰結** — third-party アセット 4.3 GB は .gitignore 対象のため Git では復旧できない。
物理バックアップが唯一の防御手段になる。UVCS 側のリポジトリ本体は削除していない。

### [D-003] エージェント制御を Unity 公式の Pipeline に委ね、自作の実行コマンド制限を撤回する
status: Accepted
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

### [D-004] エージェントの権限は「不可逆な操作のみ」を対象に絞る
status: Accepted
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