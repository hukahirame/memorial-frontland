<!-- 自動生成。図を手で直さない。dotnet test が作り直す。
     切り方と見出しは docs/dependencies-diagrams/slices.txt にある。
     末尾の覚え書きの節だけが手書きで、作り直しても消えない。
     枠の色: 青 = Domain / 橙 = Game / 灰 = Legacy（境界として置いているだけ）
     メンバは Domain / Game の核の公開分だけ。Legacy の中身は載せない。
     線: 太線 = 属性として保持する関係 / 点線 = 本体の中で使うだけの関係 -->

# シーン遷移 🚪

どのシーンに入り、何を持ち越すか。

```mermaid
graph LR
  subgraph Domain
    Root
  end
  Allmaity
  Craft_set
  GameManager
  Info_set
  Inventbutton
  MapButton
  OF_Spawner
  Player2
  PlayerDeath
  PlayerInventory
  QuestButton
  QuestManager
  RewardUI
  RootsManager
  SaveData
  SaveSystem
  SceneFinisher
  SceneStarter
  Slime
  Title
  Allmaity -.-> GameManager
  Craft_set -.-> GameManager
  GameManager -.-> SceneFinisher
  Info_set -.-> GameManager
  Inventbutton -.-> GameManager
  MapButton -.-> RootsManager
  OF_Spawner -.-> GameManager
  OF_Spawner -.-> SceneStarter
  PlayerDeath -.-> GameManager
  PlayerInventory -.-> GameManager
  QuestButton -.-> GameManager
  QuestManager -.-> GameManager
  RootsManager -.-> GameManager
  SaveData -.-> GameManager
  SceneFinisher -.-> GameManager
  SceneFinisher -.-> SaveData
  SceneFinisher -.-> SaveSystem
  SceneStarter -.-> GameManager
  SceneStarter -.-> OF_Spawner
  SceneStarter -.-> Player2
  SceneStarter -.-> QuestManager
  SceneStarter -.-> RewardUI
  SceneStarter -.-> Root
  SceneStarter -.-> RootsManager
  SceneStarter -.-> SaveData
  SceneStarter -.-> SaveSystem
  Slime -.-> GameManager
  classDef domain fill:#e8f0fe,stroke:#1967d2,color:#174ea6;
  classDef game   fill:#fef7e0,stroke:#b06000,color:#8a5300;
  classDef legacy fill:#f1f3f4,stroke:#5f6368,color:#202124;
  class Root domain;
  class Allmaity,Craft_set,GameManager,Info_set,Inventbutton,MapButton,OF_Spawner,Player2,PlayerDeath,PlayerInventory,QuestButton,QuestManager,RewardUI,RootsManager,SaveData,SaveSystem,SceneFinisher,SceneStarter,Slime,Title legacy;
```

## 覚え書き

（まだ無い）
