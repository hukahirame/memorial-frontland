# 🗂️ 現在の状態

> 上書き専用。

## 🎯 いま

Legacy から Domain への切り出しと、その順序を測る仕組みの整備。

- Domain は 8 型ファイル。dotnet 86 件 / Unity EditMode 84 件
- 次にどこへ手を付けるかは `docs/dependency-list.md` の fan-out 昇順で読む

## ✅ 完了

- **[D-006] が名指しした 5 つのうち 4 つを解消。**
  `RootsManager.roots` → `RootRegistry` / `QuestManager.quests` → `QuestRegistry` /
  `Sun2.daytime` → `DayCycle` / `Player2.playerhp` → `Health`
- 所持金を `Wallet` へ。UI の文字列が正典で `int.Parse` が 5 箇所あった
- 体力を `Health` へ。UI の Slider が正典で値が黙って丸められていた
- 日の進行を `DayClock` / `DayCycle` / `DayPlan` へ
- 依存図の生成を廃止し、型の被覆検査だけ残した（[D-017]）
- ソースの解析を正規表現から Roslyn へ（537 行 → 191 行）
- **Legacy を 11 フォルダに分割。**直下 35 件が 0 件に。51 件すべて GUID 保持
- `docs/dependency-list.md` を生成。フォルダ間の fan-in / fan-out

## ⏭️ 次

- **`warnaserror`。**アナライザは既に走っているが警告が CI を落とさない。
  いま警告 0 件なので、上げるなら無料の今
- **移行の次の候補**（fan-out 昇順より）
  - `Legacy/OutField` — 出 1 / 入 0。中身が空で移行ではなく削除。
    4 クラスともシーンに貼り付いているので剥がす作業が先
  - `Legacy/Craft` — 出 6 / **入 0**。誰からも参照されておらず波及しない
- `docs/slices.txt` の Legacy の核が型名の羅列のまま。フォルダができたので
  `Legacy/Player` のような指定へ寄せられるが、`Resolve` のフォルダ対応が要る
- `Root.cs:5` のコメントに「二重定義されていたものを1つにした」が残っている
- `docs/conventions.md` に文字コードの節が無い（規則は `138caed` に揃っている）
- `GameManager.entered_scene` — [D-006] が名指しした最後の 1 つ

## 🚧 ブロッカー

- **ビルドを一度も通していない。**Unity 6.3 移行以降ずっと。
  `PlayTests` の asmdef がビルドから除外されるかも未確認
- **実機でしか確かめられないものが 2 件。**どちらもテストでは検証できない
  - シーンを跨いだときの体力。`1f4bfdc` で挙動が変わった可能性がある
  - クエストの発行が翌日になる件（`dd810a1`）
- 20 分プレイして氾濫を見る / クラフト画面の素材スロット
