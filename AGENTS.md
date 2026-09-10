<!--
このファイルはAIエージェントへの実行指示。人間向けの解説ではない。
- 書く: 実行コマンド、絶対禁止事項、他ドキュメントへのポインタ
- 書かない: asmdefの依存関係（asmdefが物理強制済み）、
           コード整形規約（.editorconfig/Analyzerが強制済み）、
           設計判断の理由（docs/DECISIONS.md）
機械が強制できるものはここに書かない。ここは「機械が表現できない意図」専用。
50行程度を上限とし、超えたら層ごとの AGENTS.md への分割を検討する。
-->

## 実行環境

Unity Editor を開いた状態で `unity command <名前>` が使える（Unity CLI + com.unity.pipeline）。

作業前に必ず `unity command editor_status` を叩き、`status: ready` かつ
`compiling` / `domainReloadInProgress` が false であることを確認する。
Editor が閉じていると全コマンドが失敗する。その場合は起動を人間に依頼する。

- コマンド一覧: `unity command`
- グループ絞り込み: `unity command --tag <tag> --detail full`

## テストの実行

`run_tests --mode <editor|playmode> --async_tests true` → `test_status` をポーリング。

- `--timeout` は付けない。キャンセルで Pipeline がゲートを握ったまま空回りする
- 詰まりの判定は `test_status` が running かつ `editor_status` の playMode が stopped。
  `cancel_tests` を叩いてから再実行すれば戻る。プロセスは殺さなくてよい

## 禁止

- `docs/JOURNAL.md` を読むこと（時系列ログ。未整理の卓上案や覆された前提を
  採用済みと誤読する。判断の正典は `docs/DECISIONS.md`）
- `.meta` を伴わないファイル移動、および GUID を照合しない移動（[D-018]）
- `ProjectSettings/` の直接編集、`.meta` の手動生成、prefab の手動マージ
- `scripts/eval` / `scripts/hotreload` グループの実行（任意コード実行に相当）
- `switch_build_target`（全再インポート + domain reload を伴う。人間の明示指示時のみ）
- `set_authoring_root` の変更（`Assets/_Project` に固定。third-party 保護のため）
- 破壊的コマンドへの `confirm=true` を自己判断で付けること。必ず人間に確認する
- `Assets/` 直下の third-party と `Assets/LegacyScenes/` への書き込み（Git 管理外＝復旧不能）

## ポインタ

- 判断の理由: `docs/DECISIONS.md`
- 取り決め: `docs/conventions.md`
- 触っていい範囲 / PR が落ちる条件: `docs/CONTRIBUTING.md`