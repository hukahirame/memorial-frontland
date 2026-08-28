<!-- 自動生成。図を手で直さない。dotnet test が作り直す。
     見出しと一行説明は .claude/skills/class-diff-diagram/SKILL.md のスライス表にある。
     末尾の覚え書きの節だけが手書きで、作り直しても消えない。
     枠の色: 青 = Domain / 橙 = Game / 灰 = Legacy（境界として置いているだけ）
     メンバは Domain / Game の核の公開分だけ。Legacy の中身は載せない。
     線: 太線 = 属性として保持する関係 / 点線 = 本体の中で使うだけの関係 -->

# 根源 🌳

根源の素性と、攻略度・危険度・蓄積値の規則。

```mermaid
graph LR
  subgraph Domain
    AccumulationLevel["AccumulationLevel<br/>_________________<br/>High<br/>Medium<br/>Minimal<br/>Small<br/>Stampede"]
    Root["Root<br/>_______________________________________________<br/>int Accumulation<br/>int DailyAccumulationGain<br/>int DailyProgressLoss<br/>int Danger<br/>bool HasSpawnPoint<br/>string Id<br/>AccumulationLevel Level<br/>string Name<br/>int Progress<br/>string Seed<br/>float SpawnX<br/>float SpawnY<br/>float SpawnZ<br/>float UiX<br/>float UiY<br/>_______________________________________________<br/>AccumulateDaily()<br/>AdvanceDay()<br/>Calm(int)<br/>DecayProgressDaily()<br/>Gain(int)<br/>PlaceSpawnPoint(float, float, float)<br/>Root(string, string, string, int, float, float)"]
    RootRegistry["RootRegistry<br/>_______________________<br/>IReadOnlyList&lt;Root&gt; All<br/>int Count<br/>_______________________<br/>AdvanceDay()<br/>Clear()<br/>Find(string) Root<br/>TryAdd(Root) bool"]
  end
  ExchangeButton
  GameManager
  MS_Spawner
  MapButton
  OF_Spawner
  QuestManager
  RewardUI
  RootUI
  RootsManager
  SceneStarter
  Slime
  Sun2
  ExchangeButton -.-> RootsManager
  MapButton -.-> RootsManager
  OF_Spawner -.-> Root
  OF_Spawner -.-> RootsManager
  QuestManager -.-> RootsManager
  RewardUI -.-> Root
  RewardUI -.-> RootsManager
  Root ==> AccumulationLevel
  RootRegistry ==> Root
  RootUI -.-> AccumulationLevel
  RootUI -.-> QuestManager
  RootUI -.-> Root
  RootUI -.-> RootsManager
  RootsManager -.-> AccumulationLevel
  RootsManager -.-> GameManager
  RootsManager -.-> MS_Spawner
  RootsManager -.-> OF_Spawner
  RootsManager -.-> Root
  RootsManager ==> RootRegistry
  SceneStarter -.-> Root
  SceneStarter -.-> RootsManager
  Slime -.-> Root
  Slime -.-> RootsManager
  Sun2 -.-> RootsManager
  classDef domain fill:#e8f0fe,stroke:#1967d2,color:#174ea6;
  classDef game   fill:#fef7e0,stroke:#b06000,color:#8a5300;
  classDef legacy fill:#f1f3f4,stroke:#5f6368,color:#202124;
  class AccumulationLevel,Root,RootRegistry domain;
  class ExchangeButton,GameManager,MS_Spawner,MapButton,OF_Spawner,QuestManager,RewardUI,RootUI,RootsManager,SceneStarter,Slime,Sun2 legacy;
```

## 覚え書き

`RootRegistry` が追加・検索・日次更新の唯一の窓口。`TryAdd` が Id の重複を
拒否するので、シーン再入場で根源が増えない。

日次の変化は2段階に分かれている。蓄積値の増加のあと氾濫を判定し、
そのあとで攻略度が減る。順序を変えると氾濫時のスポーンが読む攻略度が変わる。
