<!-- 現状確認用。手で維持する。
     構造が変わったら tools/diagram-diff.ps1 の出力（docs/dependencies-diff-diagrams/）
     を見て、この図を更新すること。更新漏れは SliceDiagramTests が検出する。
     枠の色: 青 = Domain / 橙 = Game / 灰 = Legacy（境界として置いているだけ） -->

# 持ち物 🎒

所持品の追加・削除。スロットと重ねの規則を持つ。

```mermaid
graph LR
  subgraph Domain
    Inventory
    AddResult
    AddOutcome
    RemoveResult
    RemoveOutcome
  end
  PlayerInventory
  PlayerInventory --> Inventory
  PlayerInventory --> AddOutcome
  PlayerInventory --> RemoveOutcome
  Inventory --> AddResult
  Inventory --> RemoveResult
  AddResult --> AddOutcome
  RemoveResult --> RemoveOutcome
  classDef domain fill:#e8f0fe,stroke:#1967d2,color:#174ea6;
  classDef game   fill:#fef7e0,stroke:#b06000,color:#8a5300;
  classDef legacy fill:#f1f3f4,stroke:#5f6368,color:#202124;
  class Inventory,AddResult,AddOutcome,RemoveResult,RemoveOutcome domain;
  class PlayerInventory legacy;
```

`PlayerInventory` は Legacy。効果の適用と UI を持ったまま、
判定だけを `Inventory` に委譲している。
