<!--
flow の現状。種別ごとに何が機械判定され、何がされていないか。
書く: 種別ごとの判定と穴、全種別に共通の担保、改善案
書かない: 使い方（.claude/commands/flow.md）、種別ごとの手順（flow-rules/）
-->

# flow

変更の種別を宣言させ、種別ごとに機械判定を変える。承認済みの計画があるあいだは、コミットを計画と
突き合わせる。検出はコミット時に行う。

## 判定が走る場所

| 場所 | いつ | 何を見る |
|---|---|---|
| commit-msg フック | コミットするたび（`--no-verify` では走らない） | 種別の宣言がちょうど1行あるか |
| reference-transaction フック | コミットがブランチに載る直前 | 種別の宣言、種別ごとの判定、計画との照合（`flow verify`） |
| reference-transaction フック | 載った・取り消された後 | 照合を通した計画を `docs/plans/` へ移す（`flow settle`） |
| CI | push と PR | 種別の宣言、`dotnet test` |
| Unity | 手で走らせたときだけ | EditMode 143件 / PlayMode 3件 |

reference-transaction は出来上がったコミットを見るので、amend でも `--no-verify` でも走る。
落ちるとブランチは動かず、ステージは残る。git の呼び出しが失敗したら FLOW-BROKEN で落とす。
見ないのは、どこかの ref から既に辿れるコミット（checkout、reset、fast-forward の pull）とマージコミット。
宣言の無いコミットも CHANGE-TYPE で落とすので、`git revert` の自動のメッセージは通らない。
許す種別の一覧は `tools/flow/flow` にだけあり、commit-msg と CI もそれを呼ぶ。
解除トレーラーは人間の承認と対になる `Api-Changed:` と `Test-Changed:` だけ。承認の無い解除は、落ちたエージェントが自分で足して通すため置かない。

`core.hooksPath` は `.git/config` の設定でバージョン管理されない。clone した直後はゲートが無い。
エージェントが外さないよう、`git config` で hooksPath に触ることと `.git/` の編集は settings で deny にしている。

判定を変えたら `sh tools/flow/flow-test` を回す。使い捨てのリポジトリで92通りのコミットを試す。
自動では走らない。

## 種別ごとの一覧

| 種別 | 落とす判定 | 規則にあるが機械が見ていないもの |
|---|---|---|
| feat | 無し | 仕様書の更新（SPEC-STALE）、テストの先行 |
| fix | TEST-CHANGED | 再現テストの有無 |
| refactor | TEST-CHANGED、REFACTOR-DIRTY、REFACTOR-RED、API-CHANGED | Unity 側が壊れていないこと |
| tune | TEST-CHANGED、TUNE-CODE | 値の範囲、仕様書本文の変更 |
| asset | TEST-CHANGED | アセット検証テストの成否 |
| chore | TEST-CHANGED、CHORE-CODE | 規則ファイルが無い |

これに加えて、どの種別にも「計画」と「全種別に共通」が同じように効く。

test と docs の種別は無い。テストと文書は変えた理由の種別と同じコミットに入れ、
単独なら chore にする。分けても機械が保証することを誰も使っておらず、
test は「先にテストを緩めるコミットを積んでから fix する」道になっていた。

## 計画

承認済みの計画（`.git/flow/active/`）があるあいだだけ掛ける。計画が無ければ何も見ないので、
`/flow` の外のコミットには掛からない。`/flow` を打てばその場で規則が読み込まれるので、
計画を飛ばして進むことは起こりにくいとみて、それ自体は判定しない。

| 判定 | 落ちる条件 | 解除 |
|---|---|---|
| PLAN-TYPE | 種別が先頭の計画と違う | 無し |
| PLAN-MESSAGE | 件名が先頭の計画と違う | 無し |
| PLAN-SCOPE | 計画の範囲に無い既存ファイルが変わった・消えた | 無し |

承認は `flow approve` で、settings の ask により許可ダイアログで止まる。承認した計画は `.git/flow/active/` に写り、
Edit / Write の deny で書き換えられない。ずれたら計画を直して承認し直す（残りごと置き換わる）。
新しいファイル、`STATE.md`、生成物、範囲の道の `.meta` は範囲に書かなくても通る。
全部の計画を使い終わるか、人間が `flow cancel`（ask）で捨てると、以後のコミットは計画と照合しない。

| 見ていないもの | なぜ |
|---|---|
| 計画の「やること」「変えないもの」の中身 | 文で、形では判定できない。人間が承認時に読む |
| 範囲の中での変え方 | 範囲はファイル単位 |
| 本文とトレーラー | 件名と種別だけ照合する |
| `/flow` で計画を出さずに進めた | 起こりにくいとみて判定しない。計画が無ければ種別の判定だけが掛かる |
| Bash の `rm` で計画を消す | deny は `rm *.git/flow*` の形だけ。書き方を変えれば消せる |
| `flow approve` / `cancel` の書き方を変えた実行 | ask は `*flow approve*` `*flow cancel*` の形だけ |
| 承認に回したファイルがチャットに貼った計画と同じか | 許可ダイアログにはコマンドしか出ない |
| `docs/plans/` を Bash で読む | deny は Read / Grep / Glob だけ。CLAUDE.md の禁止で補う |

## 種別ごとの詳細

### feat 以外すべて

| 判定 | 落ちる条件 | 解除 |
|---|---|---|
| TEST-CHANGED | `tests/` か `Assets/_Project/Tests/` の既存ファイルの行が消えた・書き換わった。ファイルが消えた。足した行に `Ignore` `Explicit` `Assert.Pass` `Assert.Ignore` `Assert.Inconclusive` `Assume.That` がある | `Test-Changed:`（人間の承認） |

新しいファイル、既存ファイルへの行の追加、中身そのままの移動、`.meta` は通る。
feat は振る舞いが変わるので、テストが付いて変わるのが正しい。仕様書は書いた直後に人間が確認している。

| 見ていないもの | なぜ |
|---|---|
| 足した行で期待を緩めた（`if` で飛ばす、`Assert` を足さない） | 行が消えない緩め方は形で見分けられない |
| feat での緩め | 仕様書の確認に任せている |

### feat

判定は無い。

| 見ていないもの | なぜ |
|---|---|
| SPEC-STALE | feat のコミットが1件も無く、掛けられる対象が無い。関心事は道のフォルダ名から引ける |
| テストを実装より先にコミットした | 判定を置いていない |

### fix

TEST-CHANGED だけ。

| 見ていないもの | なぜ |
|---|---|
| 再現テストの有無 | 静かに壊れるバグかどうかを機械が判定できない |

### refactor

| 判定 | 落ちる条件 | 解除 |
|---|---|---|
| REFACTOR-DIRTY | `docs/systems/*.md` の文が変わった。バッククォートで囲んだコード名の差し替えだけなら通る | 無し |
| REFACTOR-RED | `dotnet test` が通らない。`.cs` / `docs/` / `tests/` が作業ツリーとコミットで違う | 無し |
| API-CHANGED | `docs/public-api.txt` の既存の行が消えた。既存の型に abstract の行が増えた | `Api-Changed:` |

`public-api.txt` は static・const・out・get / set・型の種類まで1行に書く。
使う側が壊れる変化は既存の行が消える形で出るので、足すだけなら API-CHANGED は落ちない。

REFACTOR-RED が通った木は `.git/flow-green` に控え、同じ木を積み直すときはテストを回さない。

| 見ていないもの | なぜ |
|---|---|
| Game / Legacy のコンパイル、EditMode / PlayMode | Unity でしか走らない |
| 振る舞いが変わらないこと | 見ているのは Domain のテストが通ることまで |

コード名の差し替えで、意味の違うものに付け替えても REFACTOR-DIRTY は通る。
付け替え先が実在しない、または調整値の定数と値が合わなければ REFACTOR-RED で落ちる。

### tune

| 判定 | 落ちる条件 | 解除 |
|---|---|---|
| TUNE-CODE | `*.cs` が変わった。調整値はテストで固定しないので、テストの `.cs` も数える | 無し |

| 見ていないもの | なぜ |
|---|---|
| 値が範囲内か | 調整値の表に範囲の欄が無い |
| 仕様書本文の変更 | 判定を置いていない。`tune.md` は feat になると書いている |
| `.cs` 以外のコードやシーン | 落とすのが `.cs` だけ |

### asset

TEST-CHANGED だけ。

| 見ていないもの | なぜ |
|---|---|
| アセット検証テストの成否 | Unity でしか走らず、フックから確かめられない |

### chore

| 判定 | 落ちる条件 | 解除 |
|---|---|---|
| CHORE-CODE | `Assets/` が変わった。`Assets/_Project/Tests/` は除く | 無し |

| 見ていないもの | なぜ |
|---|---|
| `ProjectSettings/`、`Packages/` | 設定は chore の持ち物 |
| 仕様書 | feat で自由に変えられるので、chore で止めても守れない |

## 全種別に共通

| 何が | 何で | どこで |
|---|---|---|
| コミットが種別をちょうど1つ宣言している（CHANGE-TYPE） | commit-msg / reference-transaction / `commit-type.yml` | コミット時とCI |
| Domain が UnityEngine に依存しない | asmdef `noEngineReferences` | コンパイル |
| Domain の規則 | `dotnet test` | CI |
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
| 種別と中身の一致 | 差分の形しか見ていない。挙動を変える refactor も Domain のテストが通れば通る |
| 仕様の文と振る舞いの一致 | 決定的に判定できない。名前と定数だけ見ている |
| Game と Legacy の公開API | Domain だけ固定している |
| 体験 | プレイでしか判定できない |

## 改善案

効く順。

1. **判定の無い種別を塞ぐ。** asset で `.cs` が変わったら落とす、など。今は feat と宣言すれば何でも通る
2. **`docs/systems/` の `確認` 欄を埋める。** 12本すべて「見るもの」が空。
   flow の最後の段がプレイでの判定なのに、判定の基準が無い
3. **調整値の表に範囲の欄を足す。** tune で範囲外を弾ける。
   `SpecConformanceTests` が既に表を読んでいるので、検査はその延長
4. **SPEC-STALE を置く。** 関心事は道のフォルダ名なので sh だけで引ける
5. **Game の公開APIも固定する。** Game が動かなくなってから

やらないと決めたもの。アセット検証の CI 移植。third-party の `.meta` が
追跡外なので、シーンの GUID 183 個のうち 113 個が CI では解決できない。
実際に壊れているのは 8 件で、誤検知が 105 件出る。
