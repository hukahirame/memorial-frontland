# 🗂️ 現在の状態

> 上書き専用。

## 🎯 いま

判断の記録と受け皿の整備。コードには触っていない。

- `docs/DECISIONS.md` が 16 件（D-013〜D-016 を追加）。455 行
- 目安の 500 行が近い。超えたら `docs/decisions/` へ分割し、それ自体も記録する

## ✅ 完了

- 旧 Domain クラス図を廃止。`docs/domain-class-diagram.md`、`DomainDiagramTests.cs`、
  csproj のエントリ、CONTRIBUTING の記載を削除（`1b258c5`）。`dotnet test` 57 件成功
- STATE.md / FRICTIONS.md を新設（`fddd401`）
- PlantUmlClassDiagramGenerator を評価し、図の生成には使わないと決定（[D-013]）
- 過去の判断を遡及起票。asmdef 分離 [D-014] / Notion [D-015] / PlayMode の網 [D-016]
- `.claude/hooks/adr.md` を `.claude/commands/` へ移設。フック配下では読まれていなかった
- AGENTS.md の禁止に理由を追記。dep-diagrams は人間が読む用で、禁止は意図どおりだった
- DECISIONS.md を日付節のない `## [D-XXX]` の平坦な一覧に再構成。日付は各項目の
  `date:` へ移した。旧節は実態とずれており、D-003 / D-004 は 08-23 ではなく
  Pipeline 導入の 08-25、D-009〜D-012 は 08-28 が正しい
- adr.md の出力テンプレを上記の形式に合わせた。手順1の grep が 0 件から 34 件になった

## ⏭️ 次

- `docs/conventions.md` に文字コードの節が無い。`138caed` に規則が揃っている
- `.config/dotnet-tools.json` の去就。[D-013] で図には使わないと決めたので、
  残す理由は抽出エンジンとしての再検討のみ。`fd8c7aa` で追跡対象に入れた
- **AGENTS.md が 55 行。**冒頭の目安は 50 行。次に足すときは層ごとへの分割を検討する

## 🚧 ブロッカー

- **ビルドを一度も通していない。**Unity 6.3 移行以降ずっと。
  PlayTests の asmdef がビルドから除外されるかも未確認
- 人の目でしか確認できないものが残っている。20 分プレイして氾濫を見る、
  クラフト画面の素材スロット、GitHub 上での図の描画
