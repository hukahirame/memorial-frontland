# 依存関係一覧表

<!-- DependencyListTests が生成する。手で編集しない -->

- フォルダ（Craft, Inventory）単位。節は「層/直下のフォルダ」
- →（fan out）：依存"先"の数
- ←（fan in） ："被"依存の数
- 構文木から数える。コメントと文字列は入らない。
  var で受けた依存は型名が字面に出ないため見えない

## fan-out 昇順テーブル

| 節 | → | ← |
|---|---|---|
| `Domain` | 0 | 9 |
| `Legacy/Staging` | 0 | 6 |
| `Game/Craft` | 1 | 1 |
| `Legacy/OutField` | 1 | 0 |
| `Legacy/Day` | 4 | 1 |
| `Legacy/Inventory` | 4 | 5 |
| `Legacy/Roots` | 4 | 5 |
| `Legacy/Save` | 4 | 2 |
| `Legacy/Quest` | 5 | 5 |
| `Legacy/Craft` | 6 | 0 |
| `Legacy/Player` | 6 | 4 |
| `Legacy/Scene` | 6 | 7 |
| `Legacy/Enemy` | 7 | 3 |

## 詳細

### Domain

    ← Game/Craft 2
    ← Legacy/Craft 2
    ← Legacy/Day 6
    ← Legacy/Enemy 1
    ← Legacy/Inventory 3
    ← Legacy/Player 2
    ← Legacy/Quest 6
    ← Legacy/Roots 3
    ← Legacy/Scene 2

### Game/Craft

    → Domain 2

    ← Legacy/Craft 1

### Legacy/Craft

    → Domain 2
    → Game/Craft 1
    → Legacy/Inventory 1
    → Legacy/Roots 1
    → Legacy/Scene 1
    → Legacy/Staging 1

### Legacy/Day

    → Domain 6
    → Legacy/Quest 1
    → Legacy/Roots 1
    → Legacy/Staging 1

    ← Legacy/Save 1

### Legacy/Enemy

    → Domain 1
    → Legacy/Player 1
    → Legacy/Quest 1
    → Legacy/Roots 1
    → Legacy/Save 2
    → Legacy/Scene 2
    → Legacy/Staging 1

    ← Legacy/Player 2
    ← Legacy/Roots 2
    ← Legacy/Scene 1

### Legacy/Inventory

    → Domain 3
    → Legacy/Player 2
    → Legacy/Scene 1
    → Legacy/Staging 1

    ← Legacy/Craft 1
    ← Legacy/OutField 1
    ← Legacy/Player 1
    ← Legacy/Quest 1
    ← Legacy/Save 1

### Legacy/OutField

    → Legacy/Inventory 1

### Legacy/Player

    → Domain 2
    → Legacy/Enemy 2
    → Legacy/Inventory 1
    → Legacy/Quest 2
    → Legacy/Scene 1
    → Legacy/Staging 3

    ← Legacy/Enemy 1
    ← Legacy/Inventory 2
    ← Legacy/Save 1
    ← Legacy/Scene 1

### Legacy/Quest

    → Domain 6
    → Legacy/Inventory 1
    → Legacy/Roots 1
    → Legacy/Scene 1
    → Legacy/Staging 2

    ← Legacy/Day 1
    ← Legacy/Enemy 1
    ← Legacy/Player 2
    ← Legacy/Roots 1
    ← Legacy/Scene 2

### Legacy/Roots

    → Domain 3
    → Legacy/Enemy 2
    → Legacy/Quest 1
    → Legacy/Scene 1

    ← Legacy/Craft 1
    ← Legacy/Day 1
    ← Legacy/Enemy 1
    ← Legacy/Quest 1
    ← Legacy/Scene 1

### Legacy/Save

    → Legacy/Day 1
    → Legacy/Inventory 1
    → Legacy/Player 1
    → Legacy/Scene 1

    ← Legacy/Enemy 2
    ← Legacy/Scene 2

### Legacy/Scene

    → Domain 2
    → Legacy/Enemy 1
    → Legacy/Player 1
    → Legacy/Quest 2
    → Legacy/Roots 1
    → Legacy/Save 2

    ← Legacy/Craft 1
    ← Legacy/Enemy 2
    ← Legacy/Inventory 1
    ← Legacy/Player 1
    ← Legacy/Quest 1
    ← Legacy/Roots 1
    ← Legacy/Save 1

### Legacy/Staging

    ← Legacy/Craft 1
    ← Legacy/Day 1
    ← Legacy/Enemy 1
    ← Legacy/Inventory 1
    ← Legacy/Player 3
    ← Legacy/Quest 2
