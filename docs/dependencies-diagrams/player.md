<!-- 自動生成。図を手で直さない。dotnet test が作り直す。
     切り方と見出しは docs/dependencies-diagrams/slices.txt にある。
     末尾の覚え書きの節だけが手書きで、作り直しても消えない。
     枠の色: 青 = Domain / 橙 = Game / 灰 = Legacy（境界として置いているだけ）
     メンバは Domain / Game の核の公開分だけ。Legacy の中身は載せない。
     線: 太線 = 属性として保持する関係 / 点線 = 本体の中で使うだけの関係 -->

# プレイヤー 🚶

操作、体力、攻撃、死亡。

```mermaid
graph LR
  subgraph Domain
    Quest
  end
  Allmaity
  BigText
  DamageSet
  Enemy
  GameManager
  Info_set
  JoystickEffect
  JoystickEffect_ATK
  MiddleText
  OF_Spawner
  Player2
  PlayerDeath
  PlayerHp
  PlayerInventory
  QuestManager
  RewardUI
  SaveData
  SceneStarter
  SideJab
  Slime
  SpawnerCandidate
  TempAudio
  Weapon
  WeaponBox
  Wink
  Allmaity -.-> GameManager
  Allmaity -.-> MiddleText
  Allmaity -.-> Quest
  Allmaity -.-> QuestManager
  Enemy ==> Player2
  Info_set -.-> Player2
  Info_set -.-> Weapon
  JoystickEffect -.-> Player2
  Player2 ==> Allmaity
  Player2 -.-> PlayerDeath
  Player2 -.-> SideJab
  Player2 -.-> TempAudio
  Player2 -.-> Weapon
  Player2 -.-> Wink
  PlayerDeath -.-> BigText
  PlayerDeath -.-> GameManager
  PlayerDeath ==> Player2
  PlayerDeath -.-> RewardUI
  PlayerDeath -.-> Wink
  PlayerInventory -.-> Player2
  SaveData -.-> Player2
  SceneStarter -.-> Player2
  Slime ==> Player2
  Weapon -.-> DamageSet
  Weapon -.-> OF_Spawner
  Weapon -.-> SpawnerCandidate
  Weapon -.-> TempAudio
  Weapon -.-> WeaponBox
  classDef domain fill:#e8f0fe,stroke:#1967d2,color:#174ea6;
  classDef game   fill:#fef7e0,stroke:#b06000,color:#8a5300;
  classDef legacy fill:#f1f3f4,stroke:#5f6368,color:#202124;
  class Quest domain;
  class Allmaity,BigText,DamageSet,Enemy,GameManager,Info_set,JoystickEffect,JoystickEffect_ATK,MiddleText,OF_Spawner,Player2,PlayerDeath,PlayerHp,PlayerInventory,QuestManager,RewardUI,SaveData,SceneStarter,SideJab,Slime,SpawnerCandidate,TempAudio,Weapon,WeaponBox,Wink legacy;
```

## 覚え書き

（まだ無い）
