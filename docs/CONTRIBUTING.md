# 開発者向け

<!--
想定読み手: 3週間ぶりに戻ってきた協力者。30分で手を動かせる状態にすることが目標。
「セットアップ」節は1画面に収める。長くなったら手順ではなくスクリプトに落とす。

- 書く: セットアップ〜テスト通過までのコマンド列、触っていい層、
       PRが落ちる条件、ドキュメント運用のループ（誰が・いつ・どこに書くか）
- 書かない: 各ドキュメントの書き方の詳細（各ファイルのヘッダーコメントが正典）、
           規約の中身（docs/conventions.md）、判断の理由（docs/DECISIONS.md）

原則: 文章で書いた手順は腐る。手順が長くなったら、まずスクリプト化できないかを疑う。
-->

## セットアップ

1. clone の前に `git lfs install`。忘れると画像がポインタ文字列のまま展開され、壊れたアセットになる
2. Unity をインストール（バージョンは `ProjectSettings/ProjectVersion.txt` が正典）
3. シーンとプレハブのマージ用に、Unity 付属の UnityYAMLMerge を `merge.unityyamlmerge.driver` に設定する。
   `.gitattributes` が指定しているが、ドライバ自体は各マシンで設定しないと通常のテキストマージになる
4. `dotnet test tests/Domain.Tests/Domain.Tests.csproj` が緑になれば完了。
   EditMode / PlayMode のテストは Unity の Test Runner から回す

サードパーティのアセットは含まれていないため、clone しただけではシーンが完全には開かない（README 参照）。

詰まったら Issue を立ててください。解決策はこの節に反映します。

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

CI（`.github/workflows/domain-tests.yml`）は Domain のテストを `dotnet test` で回す。

- テストが赤
- Domain が UnityEngine を参照した。Domain は UnityEngine 無しでコンパイルされるため、ここで止まる
- 生成物（`docs/public-api.txt`、`docs/dependency-list.md`）がソースとずれている

Unity 側（Game / Legacy のコンパイル、EditMode / PlayMode）は CI で走らない。PR の前に手元で回す。

## ドキュメント運用のループ

AI との対話で決まったことは、書かなければ消えます。
これは全員が回すループです。

### AIセッションの終わりに

`/decision` を実行すると、そのセッションで触ったものと食い違う設計判断と、新しい設計判断の候補が挙がる。
承認したものだけが `docs/DECISIONS.md` に反映される。候補が無いのが普通。

### 判断の行き先

| 性質 | 行き先 |
|---|---|
| 複数の選択肢から 1 つを選び、その選択で複数の振る舞いや作りが決まる判断 | `docs/DECISIONS.md` |
| 1 つの振る舞いの条件（受け入れ条件）と、まだ決めていない振る舞い（未決） | `docs/systems/`（理由は残さない） |
| 「毎回こう書く」という取り決め | `docs/conventions.md` |
| 道具やエンジンの落とし穴 | `FRICTIONS.md` |
| 検証可能な振る舞い | テストコード |
| 企画（何のゲームか・コアループ） | `docs/private/concept.md`（git に入れない。練るのは Notion） |
| エージェントへの実行指示・禁止事項 | `/CLAUDE.md` |

**迷ったら書かない。**

### レビュー中に規約を足すとき

**同じ指摘を2回したときだけ** `docs/conventions.md` に1行追加する。
1回目は偶然、2回目は暗黙の前提が共有されていない構造的な証拠です。

追加する前に必ず一度問うこと: **これ Analyzer で検出できないか？**
できるなら conventions.md ではなく Analyzer に書く。

### 大きめのタスクを始める前

`docs/DECISIONS.md` の関連する決定をエージェントに渡す。
同じ設計を再発明させないためです。

## ドキュメントの入口

| ファイル | 中身 |
|---|---|
| `docs/DECISIONS.md` | なぜそう作られているか。判断の記録 |
| `docs/conventions.md` | 機械で強制できない取り決め |
| `docs/systems/` | 関心事ごとの仕様書（受け入れ条件）。関心事のフォルダと1対1。書き方は `INDEX.md` |
| `docs/private/concept.md` | 企画。公開しないので git に入れず、作った人の手元にしか無い（[D-25]） |
| `.spec/admin.html` | 人の対応が要るものを先に出すページ。仕様書も全体から細部へ読める。`python tools/spec/build.py` で仕様書から作る（git には入れない） |
| `/CLAUDE.md` | エージェントへの実行指示 |
| `/README.md` | プロジェクトの概要（外部向け） |

各ファイルの冒頭に、何を書き何を書かないかのコメントがあります。

生成物は手で編集しません。`dotnet test` が型とソースから作り直し、
ズレていれば書き換えたうえで失敗します。

## 型を足したとき 🔍

層（Domain / Game / Legacy）の直下にある関心事のフォルダに置きます。
層の直下に置くと `dotnet test` が落ちます。フォルダの名前がそのまま関心事の名前で、
層をまたいで同じ名前のフォルダが1つの関心事になります。

新しいフォルダを作ったら、`docs/systems/<フォルダ名を小文字にしたもの>.md` も足します。
仕様書の無いフォルダがあると `dotnet test` が落ちます。