<!--
flow の現状。種別ごとに何が機械判定され、何がされていないか。
書く: 種別ごとの判定と穴、全種別に共通の担保、改善案
書かない: 使い方（.claude/commands/flow.md）、種別ごとの手順（flow-rules/）
-->

# flow

変更の種別を宣言させ、種別ごとに機械判定を変える。検出はコミット時に行う。

## 判定が走る場所

| 場所 | いつ | 何を見る |
|---|---|---|
| commit-msg フック | コミットするたび（`--no-verify` では走らない） | 種別の宣言がちょうど1行あるか |
| reference-transaction フック | コミットがブランチに載る直前 | 種別の宣言と、種別ごとの判定（`flow verify`） |
| CI | push と PR | 種別の宣言、`dotnet test` |
| Unity | 手で走らせたときだけ | EditMode 143件 / PlayMode 3件 |

reference-transaction は出来上がったコミットを見るので、amend でも `--no-verify` でも走る。
落ちるとブランチは動かず、ステージは残る。git の呼び出しが失敗したら FLOW-BROKEN で落とす。
見ないのは、どこかの ref から既に辿れるコミット（checkout、reset、fast-forward の pull）とマージコミット。
宣言の無いコミットも CHANGE-TYPE で落とすので、`git revert` の自動のメッセージは通らない。
許す種別の一覧は `tools/flow/flow` にだけあり、commit-msg と CI もそれを呼ぶ。

`core.hooksPath` は `.git/config` の設定でバージョン管理されない。clone した直後はゲートが無い。

判定を変えたら `sh tools/flow/flow-test` を回す。使い捨てのリポジトリで57通りのコミットを試す。
自動では走らない。

## 種別ごとの一覧

| 種別 | 落とす判定 | 規則にあるが機械が見ていないもの |
|---|---|---|
| feat | 無し | 仕様書の更新（SPEC-STALE）、テストの先行 |
| fix | FIX-TEST-WEAKENED | 再現テストの有無 |
| refactor | REFACTOR-DIRTY、REFACTOR-RED、API-CHANGED | 既存テストの期待値、Unity 側が壊れていないこと |
| tune | TUNE-CODE | 値の範囲、仕様書本文の変更 |
| asset | 無し | アセット検証テストの成否 |
| test | TEST-CODE | — |
| docs | DOCS-CODE | — |
| chore | 無し | 規則ファイルが無い |

これに加えて、どの種別にも「全種別に共通」が同じように効く。

## 種別ごとの詳細

### feat

判定は無い。

| 見ていないもの | なぜ |
|---|---|
| SPEC-STALE | feat のコミットが1件も無く、掛けられる対象が無い。関心事は道のフォルダ名から引ける |
| テストを実装より先にコミットした | 判定を置いていない |

### fix

| 判定 | 落ちる条件 | 解除 |
|---|---|---|
| FIX-TEST-WEAKENED | `tests/` か `Assets/_Project/Tests/` の既存ファイルが変わった・消えた | `Test-Change:` |

テストを足すのは通る。移動は消えた扱いになる。

| 見ていないもの | なぜ |
|---|---|
| 再現テストの有無 | 静かに壊れるバグかどうかを機械が判定できない |

### refactor

| 判定 | 落ちる条件 | 解除 |
|---|---|---|
| REFACTOR-DIRTY | `docs/systems/*.md` の文が変わった。バッククォートで囲んだコード名の差し替えだけなら通る | `Spec-Changed:` |
| REFACTOR-RED | `dotnet test` が通らない。`.cs` / `docs/` / `tests/` が作業ツリーとコミットで違う | 無し |
| API-CHANGED | `docs/public-api.txt` の既存の行が消えた。既存の型に abstract の行が増えた | `Api-Changed:` |

`public-api.txt` は static・const・out・get / set・型の種類まで1行に書く。
使う側が壊れる変化は既存の行が消える形で出るので、足すだけなら API-CHANGED は落ちない。

REFACTOR-RED が通った木は `.git/flow-green` に控え、同じ木を積み直すときはテストを回さない。

| 見ていないもの | なぜ |
|---|---|
| 既存テストの期待値 | 判定を置いていない。`refactor.md` は不変条件に挙げている |
| Game / Legacy のコンパイル、EditMode / PlayMode | Unity でしか走らない |
| 振る舞いが変わらないこと | 見ているのは Domain のテストが通ることまで |

コード名の差し替えで、意味の違うものに付け替えても REFACTOR-DIRTY は通る。
付け替え先が実在しない、または調整値の定数と値が合わなければ REFACTOR-RED で落ちる。

### tune

| 判定 | 落ちる条件 | 解除 |
|---|---|---|
| TUNE-CODE | `*.cs` が変わった | 無し |

| 見ていないもの | なぜ |
|---|---|
| 値が範囲内か | 調整値の表に範囲の欄が無い |
| 仕様書本文の変更 | 判定を置いていない。`tune.md` は feat になると書いている |
| `.cs` 以外のコードやシーン | 落とすのが `.cs` だけ |

### asset

判定は無い。

| 見ていないもの | なぜ |
|---|---|
| アセット検証テストの成否 | Unity でしか走らず、フックから確かめられない |

### test

| 判定 | 落ちる条件 | 解除 |
|---|---|---|
| TEST-CODE | `tests/`、`Assets/_Project/Tests/`、生成物のどれでもないファイルが変わった | 無し |

生成物の見分けはコミットの中身の先頭5行の「手で編集しない」。新しい生成物が増えても自動で通る。

### docs

| 判定 | 落ちる条件 | 解除 |
|---|---|---|
| DOCS-CODE | `*.md` でも `docs/` 以下でもないファイルが変わった | 無し |

`docs/` 以下の生成物を手で書き換えても通る。

### chore

判定も規則ファイルも無い。どんな差分でも chore と宣言すれば通る。

## 全種別に共通

| 何が | 何で | どこで |
|---|---|---|
| コミットが種別をちょうど1つ宣言している（CHANGE-TYPE） | commit-msg / reference-transaction / `commit-type.yml` | コミット時とCI |
| Domain が UnityEngine に依存しない | asmdef `noEngineReferences` | コンパイル |
| Domain の規則（140件） | `dotnet test` | CI |
| 全型が関心事のフォルダに入っている | `SystemSpecTests` | CI |
| 依存一覧がソースと一致 | `DependencyListTests` | CI |
| 関心事のフォルダと仕様書が1対1 | `SystemSpecTests` | CI |
| 仕様書の id がファイル名と一致 | `SystemSpecTests` | CI |
| 仕様書の必須欄が揃っている | `SystemSpecTests` | CI |
| 仕様書が名指しした型とメンバが実在 | `SpecConformanceTests` | CI |
| 調整値に書いた定数がコードと一致 | `SpecConformanceTests` | CI |
| 公開API一覧がソースと一致 | `PublicApiTests` | CI |
| シーンとプレハブの参照が生きている | `AssetIntegrityTests` 3件 | 手で走らせたとき |
| シーン遷移が落ちない | PlayMode 3件 | 手で走らせたとき |

`dotnet test` は148件。refactor ではコミット時にも走る。

| 見ていないもの | なぜ |
|---|---|
| 変更した関心事を宣言している（SCOPE-UNDECLARED） | 判定を置いていない。関心事は道のフォルダ名から引ける |
| 種別と中身の一致 | 差分の形しか見ていない。挙動を変える refactor も Domain のテストが通れば通る |
| 仕様の文と振る舞いの一致 | 決定的に判定できない。名前と定数だけ見ている |
| Game と Legacy の公開API | Domain だけ固定している |
| 体験 | プレイでしか判定できない |

## 改善案

効く順。

1. **判定の無い種別を塞ぐ。** chore で `Assets/` や `tests/` が変わったら落とす、
   feat で既存テストが変わったら落とす、など。今は chore と宣言すれば何でも通る
2. **`docs/systems/` の `確認` 欄を埋める。** 12本すべて「見るもの」が空。
   flow の最後の段がプレイでの判定なのに、判定の基準が無い
3. **調整値の表に範囲の欄を足す。** tune で範囲外を弾ける。
   `SpecConformanceTests` が既に表を読んでいるので、検査はその延長
4. **SPEC-STALE と SCOPE-UNDECLARED を置く。** 関心事は道のフォルダ名なので sh だけで引ける
5. **Game の公開APIも固定する。** Game が動かなくなってから

やらないと決めたもの。アセット検証の CI 移植。third-party の `.meta` が
追跡外なので、シーンの GUID 183 個のうち 113 個が CI では解決できない。
実際に壊れているのは 8 件で、誤検知が 105 件出る。
