<!-- 自動生成。図を手で直さない。dotnet test が作り直す。
     見出しと一行説明は .claude/skills/class-diff-diagram/SKILL.md のスライス表にある。
     末尾の覚え書きの節だけが手書きで、作り直しても消えない。
     枠の色: 青 = Domain / 橙 = Game / 灰 = Legacy（境界として置いているだけ）
     メンバは Domain / Game の核の公開分だけ。Legacy の中身は載せない。
     線: 太線 = 属性として保持する関係 / 点線 = 本体の中で使うだけの関係 -->

# 持ち物 🎒

所持品の追加・削除。スロットと重ねの規則を持つ。

```mermaid
graph LR
  subgraph Domain
    AddOutcome["AddOutcome<br/>__________<br/>NoSpace<br/>Placed<br/>Stacked"]
    AddResult["AddResult<br/>_______________________________<br/>AddOutcome Outcome<br/>int SlotIndex<br/>int Stock<br/>_______________________________<br/>AddResult(AddOutcome, int, int)"]
    Ingredient
    Inventory["Inventory<br/>________________________________________________<br/>string EmptySlot<br/>int SlotCount<br/>________________________________________________<br/>Add(string, System.Func&lt;int&gt;) AddResult<br/>Add(string, int) AddResult<br/>CountOf(string) int<br/>Inventory(IList&lt;string&gt;, IList&lt;int&gt;, IList&lt;int&gt;)<br/>Remove(string) RemoveResult"]
    Recipe
    RemoveOutcome["RemoveOutcome<br/>_____________<br/>Decremented<br/>NotFound<br/>SlotCleared"]
    RemoveResult["RemoveResult<br/>_____________________________________<br/>RemoveOutcome Outcome<br/>int SlotIndex<br/>int Stock<br/>_____________________________________<br/>RemoveResult(RemoveOutcome, int, int)"]
  end
  CloseInventory
  Craft
  Dropitem
  GameManager
  Info_set
  Inventbutton
  Player2
  PlayerInventory
  RewardUI
  SaveData
  TempAudio
  Weapon
  WeaponBox
  AddResult ==> AddOutcome
  CloseInventory -.-> TempAudio
  Craft -.-> Inventory
  Craft ==> PlayerInventory
  Dropitem ==> PlayerInventory
  Info_set -.-> GameManager
  Info_set -.-> Player2
  Info_set -.-> PlayerInventory
  Info_set -.-> TempAudio
  Info_set -.-> Weapon
  Ingredient -.-> Inventory
  Inventbutton -.-> GameManager
  Inventbutton -.-> Info_set
  Inventory -.-> AddResult
  Inventory -.-> RemoveResult
  PlayerInventory -.-> AddOutcome
  PlayerInventory -.-> GameManager
  PlayerInventory -.-> Info_set
  PlayerInventory -.-> Inventbutton
  PlayerInventory -.-> Inventory
  PlayerInventory -.-> Player2
  PlayerInventory -.-> RemoveOutcome
  Recipe -.-> Inventory
  RemoveResult ==> RemoveOutcome
  RewardUI -.-> PlayerInventory
  SaveData -.-> PlayerInventory
  Weapon -.-> WeaponBox
  classDef domain fill:#e8f0fe,stroke:#1967d2,color:#174ea6;
  classDef game   fill:#fef7e0,stroke:#b06000,color:#8a5300;
  classDef legacy fill:#f1f3f4,stroke:#5f6368,color:#202124;
  class AddOutcome,AddResult,Ingredient,Inventory,Recipe,RemoveOutcome,RemoveResult domain;
  class CloseInventory,Craft,Dropitem,GameManager,Info_set,Inventbutton,Player2,PlayerInventory,RewardUI,SaveData,TempAudio,Weapon,WeaponBox legacy;
```

## 覚え書き

`PlayerInventory` は Legacy。効果の適用と UI を持ったまま、
判定だけを `Inventory` に委譲している。
