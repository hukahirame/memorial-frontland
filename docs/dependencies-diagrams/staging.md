<!-- 自動生成。図を手で直さない。dotnet test が作り直す。
     見出しと一行説明は .claude/skills/class-diff-diagram/SKILL.md のスライス表にある。
     末尾の覚え書きの節だけが手書きで、作り直しても消えない。
     枠の色: 青 = Domain / 橙 = Game / 灰 = Legacy（境界として置いているだけ）
     メンバは Domain / Game の核の公開分だけ。Legacy の中身は載せない。
     線: 太線 = 属性として保持する関係 / 点線 = 本体の中で使うだけの関係 -->

# 表示と演出 🎥

カメラ、重ね順、効果音、共通の文字表示。

```mermaid
graph LR
  Allmaity
  BigText
  CloseInventory
  CloseManager
  DynamicLayer
  ExchangeButton
  Info_set
  MainCamera
  MiddleText
  OF_Spawner
  Player2
  PlayerDeath
  QuestButton
  QuestManager
  SortingDebuger
  Sun2
  TempAudio
  UpDownUI
  ViewSecurer
  Weapon
  Allmaity -.-> MiddleText
  CloseInventory -.-> TempAudio
  ExchangeButton -.-> TempAudio
  Info_set -.-> TempAudio
  OF_Spawner -.-> MiddleText
  Player2 -.-> TempAudio
  PlayerDeath -.-> BigText
  QuestButton -.-> MiddleText
  QuestManager -.-> BigText
  Sun2 -.-> MiddleText
  Weapon -.-> TempAudio
  classDef domain fill:#e8f0fe,stroke:#1967d2,color:#174ea6;
  classDef game   fill:#fef7e0,stroke:#b06000,color:#8a5300;
  classDef legacy fill:#f1f3f4,stroke:#5f6368,color:#202124;
  class Allmaity,BigText,CloseInventory,CloseManager,DynamicLayer,ExchangeButton,Info_set,MainCamera,MiddleText,OF_Spawner,Player2,PlayerDeath,QuestButton,QuestManager,SortingDebuger,Sun2,TempAudio,UpDownUI,ViewSecurer,Weapon legacy;
```

## 覚え書き

（まだ無い）
