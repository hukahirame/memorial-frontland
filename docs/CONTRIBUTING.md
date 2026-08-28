# 開発者向け

<!--
想定読み手: 3週間ぶりに戻ってきた協力者。30分で手を動かせる状態にすることが目標。
「セットアップ」節は1画面に収める。長くなったら手順ではなくスクリプトに落とす。

- 書く: セットアップ〜テスト通過までのコマンド列、触っていい層、
       PRが落ちる条件、ドキュメント運用のループ（誰が・いつ・どこに書くか）
- 書かない: 環境構築の詳細説明（スクリプト化してCIで毎回実行する）、
           各ドキュメントの書き方の詳細（各ファイルのヘッダーコメントが正典）、
           規約の中身（docs/conventions.md）、判断の理由（docs/DECISIONS.md）

原則: 文章で書いた手順は腐る。CIで実行している手順は腐らない。
      説明を足したくなったら、まずスクリプト化できないかを疑う。
-->

## セットアップ

1. Unity をインストール（バージョンは `ProjectSettings/ProjectVersion.txt` が正典）
2. `./scripts/setup.ps1` を実行
3. `./scripts/test.ps1` が緑になれば完了

詰まったら、手順を口頭で聞くのではなく Issue を立ててください。
その解決策はここに追記するのではなく、可能な限り `setup.ps1` に反映します。

## 触っていい範囲

| 範囲 | 扱い |
|---|---|
| `Assets/_Project/Scripts/<層>/` | 自由に。PRベース。**新規スクリプトは必ずここに作る** |
| `Assets/LegacyScripts/` | Legacy。既存ファイルの修正のみ。新規ファイルを追加しない。ロジックは順次 Domain へ抜く |
| `Assets/_Project/` のその他（`Scenes/` `Prefabs/` `Art/` `Resources/` 等） | 自由に。ただし操作は Unity Editor 上で |
| `Assets/` 直下のサードパーティ、`Assets/Resources/`、`Assets/LegacyScenes/` | 追跡対象外。コミットに含めない |
| `*.asmdef` | CODEOWNERS 対象。レビュー必須 |
| `.github/workflows/` | CODEOWNERS 対象。レビュー必須 |
| `ProjectSettings/` | 直接編集しない |

ファイルの移動・リネームは必ず Unity Editor 上で行うこと。
エクスプローラで動かすと `.meta` が追従せず GUID が壊れます。

`Assets/` 配下は `.gitignore` で既定拒否（ホワイトリスト方式）です。
サードパーティ素材を誤ってコミットしないための構造なので、緩めないこと。
`Assets/` 直下に新しいディレクトリを作っても、そのままでは追跡されません。
追跡が必要なら `Assets/_Project/` の下に作るか、`.gitignore` に
ディレクトリと `.meta` を対で追加してください（対を忘れると GUID が振り直されます）。

## PRが自動で落ちる条件

- テストが赤
- asmdef の参照違反
- Analyzer のエラー

ローカルで `./scripts/test.ps1` を通してから出せば、ほぼ落ちません。

## ドキュメント運用のループ

AI との対話で決まったことは、書かなければ消えます。
これは全員が回すループです。

### AIセッションの終わりに（30秒）

エージェントに投げる:

> このセッションで決まった設計判断を docs/DECISIONS.md 形式で3行以内に
> まとめて。判断がなければ「なし」と答えて。

出力を `docs/DECISIONS.md` の先頭に貼り、日付だけ確認する。以上。
却下した案があれば1行足すと、後で価値が出ます。

### 判断の行き先

| 性質 | 行き先 |
|---|---|
| 構造を変える / 後戻りが高コスト | `docs/DECISIONS.md` の `### [決定]` |
| 判断したが影響が局所的 | `docs/DECISIONS.md` の箇条書き |
| 「毎回こう書く」という取り決め | `docs/conventions.md` |
| 検証可能な振る舞い | テストコード |
| エージェントへの実行指示・禁止事項 | `/AGENTS.md` |

**迷ったら `docs/DECISIONS.md`。** 場所を間違えるコストがゼロになるよう
設計してあります。後で昇格・移動させれば済みます。

### レビュー中に規約を足すとき

**同じ指摘を2回したときだけ** `docs/conventions.md` に1行追加する。
1回目は偶然、2回目は暗黙の前提が共有されていない構造的な証拠です。

追加する前に必ず一度問うこと: **これ Analyzer で検出できないか？**
できるなら conventions.md ではなく Analyzer に書く。

### 大きめのタスクを始める前

`docs/DECISIONS.md` の関連する決定をエージェントに渡す。
同じ設計を再発明させないためです。

### 週次（10分・拓海）

- `docs/DECISIONS.md` を上から読み、2回以上参照された箇条書きを
  `### [決定]` に昇格させる
- `status: Proposed` のまま止まっているものを確認する

## ドキュメントの入口

| ファイル | 中身 |
|---|---|
| `docs/DECISIONS.md` | なぜそう作られているか。判断の記録 |
| `docs/conventions.md` | 機械で強制できない取り決め |
| `docs/domain-class-diagram.md` | Domain 層のクラス図（生成物） |
| `docs/dependencies-diagrams/` | 今どうなっているか。関心事ごとに11枚（生成物） |
| `docs/dependencies-diff-diagrams/` | 変更で何が動いたか（生成物） |
| `/AGENTS.md` | エージェントへの実行指示 |
| `/README.md` | プロジェクトの概要（外部向け） |

各ファイルの冒頭に、何を書き何を書かないかのコメントがあります。

生成物は手で編集しません。`dotnet test` が型とソースから作り直し、
ズレていれば書き換えたうえで失敗します。

## 構造の変化を見る 🔍

複数クラスをまたぐ変更のあと、依存がどう動いたかを1枚の図にします。
git diff では読み取れないため（[D-009]）。

```
dotnet test tests/Domain.Tests/Domain.Tests.csproj   # 素データを最新にする
./tools/diagram-diff.ps1                             # 差分図を出す
```

差分図は `docs/dependencies-diff-diagrams/` に出ます。**現状図
（`docs/dependencies-diagrams/`）は `dotnet test` が作り直すので手で触りません**（[D-011]）。
古ければ書き直したうえで失敗するので、1回目で更新され2回目で緑になります。

型を足したときだけ、skill のスライス表のどれかの行に足します。すべての型が
ちょうど1つのスライスの核に入っていることをテストが見張るので、忘れると落ちます。
表はコードの索引を兼ねています。

スライスの切り方は `docs/dependencies-diagrams/slices.txt` が定義です。
差分図の書式と抽出のアルゴリズムの解説は
`.claude/skills/class-diff-diagram/broken.md` にありますが、この skill は
費用に見合わないため停止中です。置き換えを用意するまで呼びません。