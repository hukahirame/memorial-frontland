<!-- 自動生成。図を手で直さない。dotnet test が作り直す。
     見出しと一行説明は .claude/skills/class-diff-diagram/SKILL.md のスライス表にある。
     末尾の覚え書きの節だけが手書きで、作り直しても消えない。
     枠の色: 青 = Domain / 橙 = Game / 灰 = Legacy（境界として置いているだけ）
     メンバは Domain / Game の核の公開分だけ。Legacy の中身は載せない。
     線: 太線 = 属性として保持する関係 / 点線 = 本体の中で使うだけの関係 -->

# クエスト 📜

依頼の受注と達成判定、報酬の受け渡し。

```mermaid
graph LR
  subgraph Domain
    Root
  end
  Allmaity
  BigText
  Enemy
  GameManager
  MiddleText
  OF_Spawner
  PlayerDeath
  PlayerInventory
  QuestButton
  QuestManager
  RewardUI
  RootUI
  RootsManager
  SaveData
  SceneStarter
  Slime
  Sun2
  Allmaity -.-> QuestManager
  Enemy -.-> QuestManager
  OF_Spawner -.-> QuestManager
  PlayerDeath -.-> RewardUI
  QuestButton -.-> GameManager
  QuestButton -.-> MiddleText
  QuestButton -.-> QuestManager
  QuestManager -.-> BigText
  QuestManager -.-> GameManager
  QuestManager -.-> RewardUI
  QuestManager -.-> RootsManager
  RewardUI -.-> PlayerInventory
  RewardUI -.-> QuestManager
  RewardUI -.-> Root
  RewardUI -.-> RootsManager
  RootUI -.-> QuestManager
  SaveData -.-> QuestManager
  SceneStarter -.-> QuestManager
  SceneStarter -.-> RewardUI
  Slime -.-> QuestManager
  Sun2 -.-> QuestManager
  classDef domain fill:#e8f0fe,stroke:#1967d2,color:#174ea6;
  classDef game   fill:#fef7e0,stroke:#b06000,color:#8a5300;
  classDef legacy fill:#f1f3f4,stroke:#5f6368,color:#202124;
  class Root domain;
  class Allmaity,BigText,Enemy,GameManager,MiddleText,OF_Spawner,PlayerDeath,PlayerInventory,QuestButton,QuestManager,RewardUI,RootUI,RootsManager,SaveData,SceneStarter,Slime,Sun2 legacy;
```

## 覚え書き

（まだ無い）
