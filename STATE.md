# 🗂️ 現在の状態

> 上書き専用。

## 🎯 いま

Legacy から Domain / Game への切り出し。順序は `docs/dependency-list.md` の
fan-out 昇順で読む。

- Domain 13 ファイル / Game 17 ファイル / Legacy 37 ファイル・9 フォルダ
- dotnet 142 件 / Unity EditMode 140 件 / PlayMode 3 件。すべて緑
- ADR 20 件・511 行。目安の 500 行を超えたので `docs/decisions/` への分割が近い

## ✅ 完了

**Domain へ切り出したもの**（[D-006] が名指しした 5 つのうち 4 つを解消）

- 根源 `RootRegistry` / クエスト `QuestRegistry` / 日の進行 `DayClock` `DayCycle` `DayPlan`
- 所持金 `Wallet` — UI の文字列が正典で `int.Parse` が 5 箇所あった
- 体力 `Health` — UI の Slider が正典で値が黙って丸められていた
- インベントリの並行 3 リストを `ItemSlot` 1 本に。保存の形が揃い、参照ごとの
  差し替えも消えた
- 敵の湧きと行動の規則を `SpawnRule` `ActionRule` へ。`IRandom` で乱数を
  差し替えられるようにし、境界をテストで固定した
- 拠点と候補の耐久を `Health` に。`OF_Spawner.spawnerhp` の public static を解消
- 敵の体力を `Health` に。`Weapon` が相手の `Canvas/Slider` を名前で探して
  直接減算していたのを `IDamageable` で切った。Slider は表示に戻った
- 敵のドロップを `DropRule` へ。割合が prefab に移り、敵ごとに変えられる
- 敵の行動秒数を `ActionRule.Duration` へ。`IRandom` に小数の口を足した。
  場外の高さは `FallRule`。Slime に残る規則は無くなった
- アイテム原簿 `ItemCatalog` — `List<string[]>` の添字読みが 5 ファイル 15 箇所。
  見つからないと配列の末尾を越えて例外になるバグごと解消
- 場の範囲を `FieldBounds` へ。シーンが Vector4 に XL, XS, ZL, ZS の順で詰めており、
  どの成分がどの辺かを `Player2` と `Seeker` が各々知っていた。読むときだけ
  `Exposition` で名前へ移す。`if (expos == null)` は Vector4 相手で常に偽だった
- 攻撃の選び分けを `AttackRule` へ。閾値 0.5 が `Player2.Attack` に4か所あった
- 武器を `Equipment` へ。攻撃力の正典が static で、値は表示名に「鉄」「伝」が
  含まれるかで決まっていた。耐久の正典は static な Slider だった。
  原簿に 攻撃力・吹き飛ばし・使い道・回復量 の4列を足し、素性をそこから取る。
  情報枠のボタンの文言も直った（「食べる」が必ず「設置」に上書きされていた）

**Legacy から Game へ移したもの**

- `Staging` 8 ファイル。Legacy に依存していなかったのでそのまま移せた
- `OutField` 4 ファイル。`Dropitem` だけ `PlayerInventory` に依存していたため、
  `IItemReceiver` を Game に置いて向きを反転した

**仕組み**

- 依存図の生成を廃止し、型の被覆検査だけ残した（[D-017]）
- ソースの解析を正規表現から Roslyn へ（537 行 → 191 行）
- `docs/dependency-list.md` を生成。フォルダ間の fan-in / fan-out
- Legacy を関心事ごとのフォルダに分割。直下 35 ファイルが 0 に
- Slime へ移行済みで参照の無くなった `Enemy` を削除（GUID で全アセットを照合）
- ファイル移動の禁止を手順に置き換えた（[D-018]）
- ゲームデータの置き場をテーブルごとに決める（[D-019]）。中間形式の xlsx を削除

## ⏭️ 次

- **`PlayerHp` の自然回復が Health を迂回している。**5 秒ごとに `slider.value += 1`
  していて、次の被弾で `RefreshHpView` に消される。Health を正典にしたときの
  取りこぼし。プレイヤーの残りではこれが一番小さい
- **吹き飛ばしの値が決まっていない。**原簿に列は空いたが全品目 0 で、
  これまでと同じく誰も飛ばない。消えていた `Inventbutton` の式は
  `0.07 * n + 7` だった
- **武器の耐久も 0 のまま。**`Equipment` は減らせるが、原簿の武器がすべて
  最大耐久値 0 なので減らない。1 以上を入れると `Weapon` が
  `FindWithTag("PlayerInventory/WeaponBox")` を呼ぶ。この綴りのタグは
  無い見込みで、その時に初めて例外になる
- **セーブの修正。**根源とクエストが保存されていない。`ItemSlot` で形は
  分かったので、`RootSave` / `QuestSave` を同じ形にすれば塞がる。ただし Domain の
  `Root` / `Quest` は get-only プロパティで `JsonUtility` が読めず、DTO が要る。
  実機でしか検証できないため、ビルドとプレイ確認の後に置く
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
- **実機でしか確かめられないものが増えている。**どれもテストでは検証できない
  - シーンを跨いだときの体力。`1f4bfdc` で挙動が変わった可能性がある
  - クエストの発行が翌日になる件（`dd810a1`）
  - 敵に当たること自体。`d55d59a` でダメージの経路を `IDamageable` に替えた
  - 拠点の耐久がシーンを跨ぐと戻る（`cfc32c6`）
  - 装備したときの持ち絵とボタンの文言。原簿の列で決まるようになった
- 20 分プレイして氾濫を見る / クラフト画面の素材スロット
