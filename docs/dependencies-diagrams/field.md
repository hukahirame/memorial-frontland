<!-- 自動生成。図を手で直さない。dotnet test が作り直す。
     見出しと一行説明は .claude/skills/class-diff-diagram/SKILL.md のスライス表にある。
     末尾の覚え書きの節だけが手書きで、作り直しても消えない。
     枠の色: 青 = Domain / 橙 = Game / 灰 = Legacy（境界として置いているだけ）
     メンバは Domain / Game の核の公開分だけ。Legacy の中身は載せない。
     線: 太線 = 属性として保持する関係 / 点線 = 本体の中で使うだけの関係 -->

# 外フィールド 🌲

地形の生成と、木を切って素材が落ちるまで。

```mermaid
graph LR
  BranchFallSystem
  Dropitem
  FieldCreator
  FirstSeedSet
  OutField
  PlayerInventory
  Dropitem ==> PlayerInventory
  FieldCreator -.-> FirstSeedSet
  classDef domain fill:#e8f0fe,stroke:#1967d2,color:#174ea6;
  classDef game   fill:#fef7e0,stroke:#b06000,color:#8a5300;
  classDef legacy fill:#f1f3f4,stroke:#5f6368,color:#202124;
  class BranchFallSystem,Dropitem,FieldCreator,FirstSeedSet,OutField,PlayerInventory legacy;
```

## 覚え書き

（まだ無い）
