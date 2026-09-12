---
id: outfield
slice: 外フィールド 🌲
touches: []
tunables: []
---

## 体験の意図

（未記入。人間が書く）

## デフォルトと違う点

- 場の範囲はシーンが `Vector4` に XL, XS, ZL, ZS の順で詰めて持っている。
  どの成分がどの辺かを知っているのは `Exposition` 1 か所だけ
- 木の枝は切って落とすのではなく、**勝手に落ちてくる**。最初は 10〜20 秒後、
  以降は 20〜36 秒ごと
- 落ちる場所は範囲内で毎回引き直す。高さ 5 から落ちる
- 落ちているものは触れると拾う。持ち物が一杯なら落ちたまま残る
- 拾えるものの名前は、スプライトの名前で決まる。原簿の ID と一致している必要がある

## 相互作用

- WHEN 落ちているものに触れた、持ち物へ入る（[inventory](inventory.md)）
- IF 持ち物に空きが無い THEN 拾わず、その場に残る
- WHEN 場の外へ出た、押し戻される（[player](player.md) / [enemy](enemy.md)）
- WHEN 根源のシーンへ初めて入った、拠点の位置が候補から選ばれる（[roots](roots.md)）

## やらないこと

- 地形の生成。`FieldCreator` と `OutField` は中身が空のまま
- シード値からの復元。`RootDecode` も空
- 木を切ること。スライスの説明には「木を切って素材が落ちる」とあるが、
  実装は時間で落ちるだけ

## 未決

- **地形生成が無い。** シードは根源が持っているが、使う者が居ない。
  手で置いたシーンを使っている
- 枝以外が落ちてこない。`BranchFallSystem` は枝 1 種だけ
- 落ちたものが消えない。拾わなければ増え続ける
- 木を切る操作を入れるか。入れるならスライスの説明が実態に追いつく

## 確認

- 状態: 外フィールドで 40 秒待つ
- 見るもの: （未記入。人間が書く）

## 調整値

| 項目 | 値 | 居場所 |
|---|---|---|
| 最初の落下までの秒数 | 10〜20 | `BranchFallSystem.Start` |
| 以降の間隔（秒） | 20〜36 | `BranchFallSystem.BranchDrop` |
| 落ちる範囲 | Inspector を見る | `BranchFallSystem.minusrange` |
| 場の範囲 | Inspector を見る | `SceneStarter.exposition` |
| 場外の猶予 | 0.4 | `FieldBounds.EscapeMargin` |
| 場外の高さ | 4 | `FieldBounds.EscapeHeight` |
