<!--
flow の現状。何がどこで担保されているかの一覧。
書く: 担保の対応表、判定の一覧、設計表との差、改善案
書かない: 使い方（.claude/commands/flow.md）、種別ごとの規則（flow-rules/）
-->

# flow

変更の種別を宣言させ、種別ごとに機械判定を変える。

## 担保されているもの

| 何が | 何で | どこで |
|---|---|---|
| Domain が UnityEngine に依存しない | asmdef `noEngineReferences` | コンパイル |
| Domain の規則（140件） | `dotnet test` | CI |
| 全型がどれかのスライスに属す | `SliceCoverageTests` | CI |
| 依存一覧がソースと一致 | `DependencyListTests` | CI |
| スライスと仕様書が1対1 | `SystemSpecTests` | CI |
| 仕様書の id がファイル名と一致 | `SystemSpecTests` | CI |
| 仕様書の必須欄が揃っている | `SystemSpecTests` | CI |
| 仕様書が名指しした型とメンバが実在 | `SpecConformanceTests` | CI |
| 調整値に書いた定数がコードと一致 | `SpecConformanceTests` | CI |
| 公開API一覧がソースと一致 | `PublicApiTests` | CI |
| コミットが種別を宣言している | commit-msg / `commit-type.yml` | 手元とCI |
| 種別と差分の形が合っている | `flow verify` 6件 | 手元だけ |
| シーンとプレハブの参照が生きている | `AssetIntegrityTests` 3件 | 手元だけ |
| シーン遷移が落ちない | PlayMode 3件 | 手元だけ |

`dotnet test` 148件 / Unity EditMode 143件 / PlayMode 3件。

## 担保されていないもの

| 何が | なぜ |
|---|---|
| 仕様の文と振る舞いの一致 | 決定的に判定できない。名前と定数だけ見ている |
| 種別と中身の一致 | 差分の形しか見ていない。挙動を変える refactor も通る |
| Game と Legacy の公開API | Domain だけ固定している |
| アセットの参照切れ（リモート） | third-party が追跡外で、CI では GUID を解決できない |
| 種別ごとの6件（リモート） | フックは手元でしか走らない |
| 体験 | プレイでしか判定できない |

`core.hooksPath` は `.git/config` の設定でバージョン管理されない。
clone した直後はゲートが無い。CI が見るのは宣言の有無だけ。

## 種別ごとの判定

| 判定 | 種別 | 落ちる条件 | 解除 |
|---|---|---|---|
| REFACTOR-DIRTY | refactor | `docs/systems/` が変わった | `Spec-Changed:` |
| API-CHANGED | refactor | `docs/public-api.txt` が変わった | `Api-Changed:` |
| FIX-TEST-WEAKENED | fix | 既存のテストファイルが変わった | `Test-Change:` |
| DOCS-CODE | docs | `*.md` と `docs/` 以外が変わった | 無し |
| TEST-CODE | test | テストと生成物以外が変わった | 無し |
| TUNE-CODE | tune | `*.cs` が変わった | 無し |

生成物の見分けは先頭の「手で編集しない」。新しい生成物が増えても自動で通る。

feat / asset / chore に判定は無い。

## 設計表との差

`SPEC-GATE.md` の表で埋まっていないもの。

| 未実装 | 種別 | なぜ置いていないか |
|---|---|---|
| SPEC-STALE | feat | feat のコミットが1件も無い。掛けられる種別が無い |
| SCOPE-UNDECLARED | 全 | ファイルからスライスを引く必要があり、sh から Roslyn を呼べない |
| 値の範囲外 | tune | 調整値の表に範囲の欄が無い |
| 再現テストの有無 | fix | 静かに壊れるバグかどうかを機械が判定できない |
| アセット検証の失敗 | asset | Unity でしか走らず、フックから確かめられない |

## 改善案

効く順。

1. **`docs/systems/` の `確認` 欄を埋める。** 12本すべて「見るもの」が空。
   flow の最後の段がプレイでの判定なのに、判定の基準が無い
2. **調整値の表に範囲の欄を足す。** `値` の隣に `範囲`。tune で範囲外を弾ける。
   `SpecConformanceTests` が既に表を読んでいるので、検査はその延長
3. **file → slice の対応を生成物にする。** `SliceTable` が持っている解決を
   1ファイルに書き出せば、sh から引ける。SPEC-STALE と SCOPE-UNDECLARED が
   両方作れるようになる
4. **種別ごとの判定を CI へ。** `git diff --cached` を `<commit>^ <commit>` に
   替えれば同じ判定がコミット単位で動く。フックを通さない push を塞げる
5. **Game の公開APIも固定する。** Game が動かなくなってから

やらないと決めたもの。アセット検証の CI 移植。third-party の `.meta` が
追跡外なので、シーンの GUID 183 個のうち 113 個が CI では解決できない。
実際に壊れているのは 8 件で、誤検知が 105 件出る。
