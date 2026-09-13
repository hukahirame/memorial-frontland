---
description: 変更の種別を判定し、対応する規則に従って作業を進める
---

変更を始める前に種別を判定し、該当する規則ファイルを読んでから作業する。

## 種別の判定

| 種別 | 条件 | 読むファイル |
|---|---|---|
| feat | 振る舞いが増える・変わる | @tools/flow/flow-rules/feat.md |
| refactor | 振る舞いが変わらない | @tools/flow/flow-rules/refactor.md |
| fix | 不具合の修正 | @tools/flow/flow-rules/fix.md |
| tune | ScriptableObjectの値のみ | @tools/flow/flow-rules/tune.md |
| asset | シーン・プレハブ・アセット | @tools/flow/flow-rules/asset.md |
| test | テストだけを足す・直す | — |
| docs | ドキュメントだけの変更 | — |
| chore | 仕組み・設定・ツールの変更 | — |

判定が曖昧な場合は、人間に確認してから進める。

下の3種別に規則ファイルは無い。ゲートが見るのは `Change-Type` の宣言だけ。
規則を書くのは、同じ迷いが2回出たときにする。

## 1コミット1種別

複数の種別が混ざる場合は、コミットを分割する。

順序: **fix → refactor → feat**

- 修正を先にコミットしておけば、リファクタで壊した場合に切り分けられる
- リファクタ中にバグを見つけたら、リファクタを中断し、バグ修正を先にコミットしてから再開する
- 「ついでに直す」をしない

## コミット手順

```
git add -A
git commit -m "<メッセージ>

Change-Type: <種別>"
```

- `Change-Type:` トレーラーはちょうど1行必須。commit-msg フックが先に検証する。
- ブランチに載る直前に、reference-transaction フックが `tools/flow/flow verify` を
  呼び、宣言と、宣言した種別とコミットの差分を突き合わせる。amend でも `--no-verify` でも走る。
- `git revert` が作るメッセージには宣言が無く落ちる。`--no-commit` で戻してから種別を書いてコミットする。

| 判定 | 落ちる条件 | 解除 |
|---|---|---|
| CHANGE-TYPE | `Change-Type:` がちょうど1行でない、または許す種別でない | 無し |
| REFACTOR-DIRTY | refactor で `docs/systems/` の文が変わった。バッククォートで囲んだコード名の差し替えだけなら通る | `Spec-Changed: <理由>` |
| REFACTOR-RED | refactor で `dotnet test` が通らない。テストが読むものが作業ツリーとコミットで違う | 無し |
| API-CHANGED | refactor で `docs/public-api.txt` の既存の行が消えた。既存の型に abstract の行が増えた | `Api-Changed: <理由>` |
| FIX-TEST-WEAKENED | fix で既存のテストファイルが変わった・消えた | `Test-Change: <理由>` |
| DOCS-CODE | docs で `*.md` と `docs/` 以外が変わった | 無し |
| TEST-CODE | test でテストと生成物以外が変わった | 無し |
| TUNE-CODE | tune で `*.cs` が変わった | 無し |

  解除トレーラーのある判定が限られるのは、残りは種別を変えるかコミットを割るのが
  正しい直し方だから。逃げ道を作ると誤ラベルを誘う。
- **`--no-verify` は使わない。**
- ゲートが落ちたら、出力の指示に従って修正し、再度コミットする。人間に確認を求めない。
- **push はしない。** 人間が打つ。

## 人間が確認する地点

以下の2か所では作業を止めて人間の応答を待つ。それ以外では止めない。

1. **仕様書を書いた直後**(feat のみ) — 意図の解釈が合っているか
2. **動くものができた後** — プレイして判定

## 原則

- 自明なことは仕様書に書かない。AIのデフォルトとズレる部分だけ書く。
- 指示から決まらないことは推測で埋めず、`未決` に落とす。
- 遊べばすぐ気づくバグのためにテストを書かない。
