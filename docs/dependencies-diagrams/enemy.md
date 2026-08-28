<!-- 自動生成。図を手で直さない。dotnet test が作り直す。
     切り方と見出しは docs/dependencies-diagrams/slices.txt にある。
     末尾の覚え書きの節だけが手書きで、作り直しても消えない。
     枠の色: 青 = Domain / 橙 = Game / 灰 = Legacy（境界として置いているだけ）
     メンバは Domain / Game の核の公開分だけ。Legacy の中身は載せない。
     線: 太線 = 属性として保持する関係 / 点線 = 本体の中で使うだけの関係 -->

# 敵とスポーン 👾

敵の出現位置と追跡、被弾。

```mermaid
graph LR
  subgraph Domain
    Root
  end
  Enemy
  GameManager
  MS_Spawner
  MiddleText
  OF_Spawner
  Player2
  QuestManager
  RootsManager
  SaveData
  SaveSystem
  SceneStarter
  Seeker
  Slime
  SpawnerCandidate
  Weapon
  Enemy ==> Player2
  Enemy -.-> QuestManager
  OF_Spawner -.-> GameManager
  OF_Spawner -.-> MiddleText
  OF_Spawner -.-> QuestManager
  OF_Spawner -.-> Root
  OF_Spawner -.-> RootsManager
  OF_Spawner -.-> SaveData
  OF_Spawner -.-> SaveSystem
  OF_Spawner -.-> SceneStarter
  OF_Spawner ==> Seeker
  RootsManager -.-> MS_Spawner
  RootsManager -.-> OF_Spawner
  SceneStarter -.-> OF_Spawner
  Seeker -.-> OF_Spawner
  Slime -.-> GameManager
  Slime ==> Player2
  Slime -.-> QuestManager
  Slime -.-> Root
  Slime -.-> RootsManager
  Weapon -.-> OF_Spawner
  Weapon -.-> SpawnerCandidate
  classDef domain fill:#e8f0fe,stroke:#1967d2,color:#174ea6;
  classDef game   fill:#fef7e0,stroke:#b06000,color:#8a5300;
  classDef legacy fill:#f1f3f4,stroke:#5f6368,color:#202124;
  class Root domain;
  class Enemy,GameManager,MS_Spawner,MiddleText,OF_Spawner,Player2,QuestManager,RootsManager,SaveData,SaveSystem,SceneStarter,Seeker,Slime,SpawnerCandidate,Weapon legacy;
```

## 覚え書き

（まだ無い）
