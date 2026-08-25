# 規約

<!--
掲載基準は「機械で強制できず、かつ揺れると実害があるもの」のみ。
追加する前に必ず一度問うこと: これ Analyzer / .editorconfig / asmdef に落とせないか？
落とせるならそちらに書く。ここは最後の手段。

追加のタイミングは CONTRIBUTING.md の通り、同じ指摘を2回したときだけ。
1回目は偶然、2回目は暗黙の前提が共有されていない構造的な証拠。

各項目には必ず現物の根拠を添える。抽象論だけの規約は守られない。
-->

## 状態

### 状態の正典を UI に置かない

UI コンポーネントの値を読んで計算に使わない。UI は表示するだけ。

```csharp
// NG: 必要数の正典が UI ラベル
for (int i = 0; i < int.Parse(demand1.text); i++) pi.UnloadInventory(target1);

// NG: 所持金の正典が UI ラベル
coin = int.Parse(GameObject.Find("CoinText").GetComponent<TextMeshProUGUI>().text);
```

UI を差し替えた瞬間にロジックが壊れる。テストも書けない。
値は Domain か素のクラスが持ち、UI はそれを描画する。

### `static` を状態置き場にしない

```csharp
// NG
public static int daytime;
public static string entered_scene;
public static List<string[]> quests;
```

誰でもどこからでも書き換えられるため、変更の経路が追えない。
テストも書けない（前のテストの状態が残る）。
所有者を1つ決め、参照で渡す。

定数（`const` / `static readonly`）は対象外。

## 参照

### 実行時に `GameObject.Find` / `FindWithTag` で探さない

```csharp
// NG
GameObject.FindWithTag("PlayerInventory").GetComponent<PlayerInventory>();
```

名前とタグの文字列一致なので、リネームしても実行するまで壊れたと分からない。
Inspector で参照を張るか、生成時に渡す。

やむを得ず使う場合も `Update()` の中では呼ばない。

## 責務

### MonoBehaviour は1つの責務に絞る

移動と入力と HP と装備が同居した MonoBehaviour は、テストも再利用もできない。
目安として、複数の「〜する」で説明が必要になったら分割を検討する。

### ロジックから UI を直接書き換えない

```csharp
// NG: 追加処理の中で UI を直書き
transform.GetChild(CHILDPLUS + index).Find("Text").GetComponent<Text>().text = ...
```

イベントで通知し、UI 側が購読する。
ロジックが UI の構造（子の順番、コンポーネントの種類）を知らずに済む形にする。

## 将来 Analyzer へ移すもの

以下は Roslyn Analyzer で機械的に検出できる見込みがある。
実装したらこのファイルから削除する。

- `static` フィールドの検出（定数を除く）
- `GameObject.Find` / `FindWithTag` の呼び出し検出