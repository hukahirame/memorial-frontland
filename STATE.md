# 🗂️ 現在の状態

> 上書き専用。

## 🎯 いま

判断の記録と受け皿の整備。コードには触っていない。

- `docs/DECISIONS.md` が 16 件（D-013〜D-016 を追加）。436 行
- 目安の 500 行が近い。超えたら `docs/decisions/` へ分割し、それ自体も記録する

## ✅ 完了

- 旧 Domain クラス図を廃止。`docs/domain-class-diagram.md`、`DomainDiagramTests.cs`、
  csproj のエントリ、CONTRIBUTING の記載を削除（`1b258c5`）。`dotnet test` 57 件成功
- STATE.md / FRICTIONS.md を新設（`fddd401`）
- PlantUmlClassDiagramGenerator を評価し、図の生成には使わないと決定（[D-013]）
- 過去の判断を遡及起票。asmdef 分離 [D-014] / Notion [D-015] / PlayMode の網 [D-016]
- `.claude/hooks/adr.md` を `.claude/commands/` へ移設。フック配下では読まれていなかった

## ⏭️ 次

- **AGENTS.md の `## 禁止` に「dependencies-diagrams を読むこと」が入っている。**
  意図と逆。置き場所かコミット `bb6920c` のメッセージのどちらかが誤り
- `.claude/commands/adr.md` の出力テンプレが DECISIONS.md の実形式と不一致。
  `## [D-` と `date:` / `tags:` / `paths:` / `### Context` を指定しているが、
  実際は `### [D-` と `status:` / `scope:` / `**背景**`。手順1の grep が 0 件を返す
- `docs/conventions.md` に文字コードの節が無い。`138caed` に規則が揃っている
- `.config/dotnet-tools.json` の去就。[D-013] で図には使わないと決めたので、
  残す理由は抽出エンジンとしての再検討のみ。未コミット
- 未コミット: `docs/DECISIONS.md`、`.claude/commands/adr.md`、`.config/`

## 🚧 ブロッカー

- **ビルドを一度も通していない。**Unity 6.3 移行以降ずっと。
  PlayTests の asmdef がビルドから除外されるかも未確認
- 人の目でしか確認できないものが残っている。20 分プレイして氾濫を見る、
  クラフト画面の素材スロット、GitHub 上での図の描画
