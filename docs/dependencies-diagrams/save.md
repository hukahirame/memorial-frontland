<!-- 自動生成。図を手で直さない。dotnet test が作り直す。
     切り方と見出しは docs/dependencies-diagrams/slices.txt にある。
     末尾の覚え書きの節だけが手書きで、作り直しても消えない。
     枠の色: 青 = Domain / 橙 = Game / 灰 = Legacy（境界として置いているだけ）
     メンバは Domain / Game の核の公開分だけ。Legacy の中身は載せない。
     線: 太線 = 属性として保持する関係 / 点線 = 本体の中で使うだけの関係 -->

# セーブ 💾

進行状況の保存と復元。

```mermaid
graph LR
  GameManager
  OF_Spawner
  Player2
  PlayerInventory
  SaveData
  SaveSystem
  SceneFinisher
  SceneStarter
  Sun2
  OF_Spawner -.-> SaveData
  OF_Spawner -.-> SaveSystem
  SaveData -.-> GameManager
  SaveData -.-> Player2
  SaveData -.-> PlayerInventory
  SaveData -.-> Sun2
  SaveSystem ==> SaveData
  SceneFinisher -.-> SaveData
  SceneFinisher -.-> SaveSystem
  SceneStarter -.-> SaveData
  SceneStarter -.-> SaveSystem
  classDef domain fill:#e8f0fe,stroke:#1967d2,color:#174ea6;
  classDef game   fill:#fef7e0,stroke:#b06000,color:#8a5300;
  classDef legacy fill:#f1f3f4,stroke:#5f6368,color:#202124;
  class GameManager,OF_Spawner,Player2,PlayerInventory,SaveData,SaveSystem,SceneFinisher,SceneStarter,Sun2 legacy;
```

## 覚え書き

（まだ無い）
