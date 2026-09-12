# システム一覧

1システム1行。中身はここに書かず、各ファイルを見る。

単位は `docs/slices.txt` の行と 1:1。`SystemSpecTests` が対応を見張る。

| id | スライス | 概要 |
|---|---|---|
| [roots](roots.md) | 根源 🌳 | 根源の素性と、攻略度・危険度・蓄積値の規則 |
| [inventory](inventory.md) | 持ち物 🎒 | 所持品の追加・削除。スロットと重ねの規則 |
| [craft](craft.md) | クラフト 🔨 | レシピの定義と、素材が足りているかの判定 |
| [quest](quest.md) | クエスト 📜 | 依頼の受注と達成判定、報酬の受け渡し |
| [day](day.md) | 日の進行 ☀️ | 1日を進める側。根源とクエストとセーブを同時に叩く |
| [wallet](wallet.md) | 所持金 💰 | 受け取りと支払い |
| [save](save.md) | セーブ 💾 | 進行状況の保存と復元 |
| [player](player.md) | プレイヤー 🚶 | 操作、体力、攻撃、死亡 |
| [enemy](enemy.md) | 敵とスポーン 👾 | 敵の出現位置と追跡、被弾 |
| [outfield](outfield.md) | 外フィールド 🌲 | 地形の生成と、素材が落ちるまで |
| [scene](scene.md) | シーン遷移 🚪 | どのシーンに入り、何を持ち越すか |
| [staging](staging.md) | 表示と演出 🎥 | カメラ、重ね順、効果音、共通の文字表示 |

## 書き方

`tools/flow/flow-rules/feat.md` の形式に従う。

スライスを足したら仕様書も足す。忘れると `dotnet test` が落ちる。

`体験の意図` は人間が書く。エージェントは推測しない（`docs/GDD.md`）。
