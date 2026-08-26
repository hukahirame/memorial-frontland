# 規約

<!--
掲載基準は「機械で強制できず、かつ揺れると実害があるもの」のみ。
追加する前に必ず一度問うこと: これ Analyzer / .editorconfig / asmdef に落とせないか？
落とせるならそちらに書く。ここは最後の手段。

追加のタイミングは CONTRIBUTING.md の通り、同じ指摘を2回したときだけ。

書き方:
  規則は1〜2行の言い切り。理由は書かない。
  疑われうる規則には [D-XXX] を付け、理由は docs/DECISIONS.md に置く。
  コード例は理由ではなく仕様。何が違反かを確定させるので残す。

適用範囲は `.editorconfig` と同じく Assets/_Project。
Legacy のコードは、その機能を移行するときに従う。
-->

## 状態 🗃️

### 状態の正典を UI に置かない

UI コンポーネントの値を読んで計算に使わない。UI は表示するだけ。

```csharp
// ❌ NG
for (int i = 0; i < int.Parse(demand1.text); i++) pi.UnloadInventory(target1);
coin = int.Parse(GameObject.Find("CoinText").GetComponent<TextMeshProUGUI>().text);
```

### `static` を状態置き場にしない  [D-006]

```csharp
// ❌ NG
public static int daytime;
public static string entered_scene;
public static List<string[]> quests;
```

定数（`const` / `static readonly`）は対象外。

## 参照 🔗

### 実行時に `GameObject.Find` / `FindWithTag` で探さない

Inspector で参照を張るか、生成時に渡す。やむを得ず使う場合も `Update()` の中では呼ばない。

```csharp
// ❌ NG
GameObject.FindWithTag("PlayerInventory").GetComponent<PlayerInventory>();
```

## 責務 🎯

### MonoBehaviour は1つの責務に絞る

複数の「〜する」で説明が必要になったら分割する。

### ロジックから UI を直接書き換えない

イベントで通知し、UI 側が購読する。

```csharp
// ❌ NG: 追加処理の中で UI を直書き
transform.GetChild(CHILDPLUS + index).Find("Text").GetComponent<Text>().text = ...
```

## フィールドの公開 🔓  [D-007]

書き換え可能な public インスタンスフィールドを作らない。
`const` / `static readonly` は対象外。

```csharp
// ❌ NG
public List<string> items;

// ✅ OK
[SerializeField] private List<string> _items;
public IReadOnlyList<string> Items => _items;
```

構造体も同じ。`public readonly` フィールドではなく get-only property を使う。

**例外: ScriptableObject と `[Serializable]` のデータ入れ物は public フィールドで良い。**
その代わり SO は読み取り専用として扱い、実行中に書き換えない。

## 命名 🏷️

大文字小文字は `.editorconfig` が強制する。ここには書かない。
以下は機械で判定できない部分。

### フォルダ 📁

PascalCase。層は単数、集合は複数。

```
Scripts/Domain/   Scripts/Game/Craft/   Data/Recipes/   Scenes/
```

### ファイル 📄

ファイル名は主たる型の名前。付随する enum や結果型は同居してよい。
MonoBehaviour はファイル名とクラス名を一致させる（Unity の要件）。

```
Inventory.cs  →  Inventory, AddResult, RemoveResult, AddOutcome, RemoveOutcome
```

### メソッド ⚙️

- 動詞始まり。`StartCraft()` であって `CraftStart()` ではない
- bool を返すなら `Is` / `Has` / `Can` で始める
- `Try` は付けない。`TryX` は `bool TryAdd(out T)` の形を指す

既存メソッドの改名は単独で行わない。UI の Button が Inspector に名前を文字列で
保持している場合があり、改名すると呼び出しが静かに切れる。

### アセット名 🎨

ScriptableObject のファイル名は、中身の ID と一致させる。

```
Data/Recipes/Torch.asset  ↔  productId: Torch
```

### テストメソッド 🧪  [D-008]

日本語の平叙文。振る舞いだけを書き、メソッド名や引数など呼び出し方は含めない。

```csharp
[Test] public void 同名かつ上限未満なら既存スロットに積まれる()   // ✅ OK
[Test] public void Add_WhenSlotIsEmpty_PlacesItem()              // ❌ NG
```

## 将来 Analyzer へ移すもの 🤖

実装したらこのファイルから削除する。

- `static` フィールドの検出（定数を除く）
- `GameObject.Find` / `FindWithTag` の呼び出し検出