---
id: scene
slice: シーン遷移 🚪
touches: [save]
tunables: []
---

## 体験の意図

（未記入。人間が書く）

## デフォルトと違う点

- シーンを跨いで生き残るのは `GameManager` とメインキャンバスの 2 つだけ。
  2 個目が来たら自分を消す
- いま居るシーンの名前は `GameManager.entered_scene` が持つ。シーン名そのものではなく、
  遷移のときに書かれる値
- 根源のシーンでは、根源の Id とシーン名が同じ。この一致に依存している箇所がある
- シーンに入ったときの処理は `SceneStarter` の `type` で分かれる。Root と MainSite の 2 つ
- MainSite に入るとき、立ち位置が原点なら原点のまま、違えば決められた地点へ動かす
- BGM は MainSite だけ 2 曲から 50% で選ぶ

## 相互作用

- WHEN シーンを出た、設置物の位置が控えに書かれる（[save](save.md)）
- WHEN 根源のシーンへ入った、拠点の位置が決まり、クエストが開始され、
  ジャンプボタンが繋ぎ直される（[roots](roots.md) / [quest](quest.md)）
- WHEN MainSite へ入った、報酬 UI のフラグが立っていれば報酬 UI が出る（[quest](quest.md)）
- WHEN シーンへ入った、体力の上限と表示が繋ぎ直される（[player](player.md)）
- IF 受注中のクエストが無い THEN 根源へ移動できない

## やらないこと

- シーンの動的生成。用意された 4 つを行き来する
- 遷移中の演出。読み込みは即時

## 未決

- **`GameManager` が 3 つの責務を抱えている。** 原簿と所持金の置き場、
  シングルトンの管理、シーン遷移。fan-in が 7 ある原因
- `entered_scene` が `static` のまま。[D-006] が名指しした 5 つの最後の 1 つ
- ジャンプボタンの繋ぎ直しが根源のシーンだけで起きる。MainSite では繋がない
- シーンを跨いだときの体力の引き継ぎを実機で確かめていない

## 確認

- 状態: MainSite から根源へ入り、戻ってくる
- 見るもの: （未記入。人間が書く）

## 調整値

| 値 | 居場所 |
|---|---|
| MainSite の BGM 2 曲と 50% | `SceneStarter.Start` |
| 根源の BGM | 同 |
| シーンごとの立ち位置 | `SceneStarter.playerspowner`（Inspector） |
