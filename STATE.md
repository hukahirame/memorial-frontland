# 🗂️ 現在の状態

> 上書き専用。

## 🎯 いま

Legacy から Domain / Game への切り出し。順序は `docs/dependency-list.md` の
fan-out 昇順で読む。

- Domain 8 ファイル / Game 14 ファイル / Legacy 38 ファイル・9 フォルダ
- dotnet 93 件 / Unity EditMode 91 件
- ADR 20 件・511 行。目安の 500 行を超えたので `docs/decisions/` への分割が近い

## ✅ 完了

**Domain へ切り出したもの**（[D-006] が名指しした 5 つのうち 4 つを解消）

- 根源 `RootRegistry` / クエスト `QuestRegistry` / 日の進行 `DayClock` `DayCycle` `DayPlan`
- 所持金 `Wallet` — UI の文字列が正典で `int.Parse` が 5 箇所あった
- 体力 `Health` — UI の Slider が正典で値が黙って丸められていた
- アイテム原簿 `ItemCatalog` — `List<string[]>` の添字読みが 5 ファイル 15 箇所。
  見つからないと配列の末尾を越えて例外になるバグごと解消

**Legacy から Game へ移したもの**

- `Staging` 8 ファイル。Legacy に依存していなかったのでそのまま移せた
- `OutField` 4 ファイル。`Dropitem` だけ `PlayerInventory` に依存していたため、
  `IItemReceiver` を Game に置いて向きを反転した

**仕組み**

- 依存図の生成を廃止し、型の被覆検査だけ残した（[D-017]）
- ソースの解析を正規表現から Roslyn へ（537 行 → 191 行）
- `docs/dependency-list.md` を生成。フォルダ間の fan-in / fan-out
- Legacy を関心事ごとのフォルダに分割。直下 35 ファイルが 0 に
- ファイル移動の禁止を手順に置き換えた（[D-018]）
- ゲームデータの置き場をテーブルごとに決める（[D-019]）。中間形式の xlsx を削除

## ⏭️ 次

- **`SaveData` の並行 3 リスト**（`items` / `stocks` / `maxstocks`）。
  `Inventory` の Registry 化はここが前提。ただし `Save()` の呼び出しは今も 0 件
- **`GameManager` の責務分解。**`Items` `Coins` シングルトン管理 シーン遷移を
  1 つで抱えており、fan-in 7 の原因になっている
- `Root.cs:5` のコメントに「二重定義されていた」が残っている（コメント整理の取りこぼし）
- `.claude/settings.json` の `Write(ProjectSettings/**)` は効いていない。
  `Edit(ProjectSettings/**)` が覆うので削除でよい
- `docs/conventions.md` に文字コードの節が無い（規則は `138caed` に揃っている）
- `.config/dotnet-tools.json` の去就。[D-013] で図には使わないと決めており、
  現時点で puml-gen を呼ぶ者はいない

`-warnaserror` は見送り。CI がコンパイルするのは Domain 7 ファイルだけで
警告は 2 回測って 0 件、有効な規則が一度も発火していない。`AnalysisLevel: latest`
との組み合わせは SDK 更新で無関係に赤くなる経路も作る。

## 🚧 ブロッカー

- **ビルドを一度も通していない。**Unity 6.3 移行以降ずっと。
  `PlayTests` の asmdef がビルドから除外されるかも未確認
- **実機でしか確かめられないものが 2 件。**どちらもテストでは検証できない
  - シーンを跨いだときの体力。`1f4bfdc` で挙動が変わった可能性がある
  - クエストの発行が翌日になる件（`dd810a1`）
- 20 分プレイして氾濫を見る / クラフト画面の素材スロット
