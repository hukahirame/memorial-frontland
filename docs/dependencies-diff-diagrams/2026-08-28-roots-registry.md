<!-- tools/diagram-diff.ps1 が生成する。手で編集しない -->

# 依存の差分  c2fcb5d -> 作業ツリー  (2026-08-28)

型 +3 / -0　　辺 +7 / -3 / 種類変化 0　　メンバが動いた型 4

**色が変化** — 緑が追加、赤が削除、橙が関連と依存の入れ替わり、灰が変わっていない
**線種が関係** — 太線が関連（フィールドで保持）、点線が依存（signature に出るだけ）
緑の枠が現れた型、赤の枠が消えた型。塗りは白で統一。
メンバは文字色で示す。緑が追加、赤が削除、橙が変更。

```mermaid
graph LR
  subgraph Domain
    AccumulationLevel["<span style='color:#202124'>AccumulationLevel</span><br/><span style='color:#5f6368'>_________________</span><br/><span style='color:#202124'>High</span><br/><span style='color:#202124'>Medium</span><br/><span style='color:#202124'>Minimal</span><br/><span style='color:#202124'>Small</span><br/><span style='color:#202124'>Stampede</span>"]
    Root["<span style='color:#202124'>Root</span><br/><span style='color:#5f6368'>_________________________________________________</span><br/><span style='color:#202124'>+ int Accumulation</span><br/><span style='color:#202124'>+ int DailyAccumulationGain</span><br/><span style='color:#202124'>+ int DailyProgressLoss</span><br/><span style='color:#202124'>+ int Danger</span><br/><span style='color:#202124'>+ bool HasSpawnPoint</span><br/><span style='color:#202124'>+ string Id</span><br/><span style='color:#202124'>+ AccumulationLevel Level</span><br/><span style='color:#202124'>+ string Name</span><br/><span style='color:#202124'>+ int Progress</span><br/><span style='color:#202124'>+ string Seed</span><br/><span style='color:#202124'>+ float SpawnX</span><br/><span style='color:#202124'>+ float SpawnY</span><br/><span style='color:#202124'>+ float SpawnZ</span><br/><span style='color:#202124'>+ float UiX</span><br/><span style='color:#202124'>+ float UiY</span><br/><span style='color:#5f6368'>_________________________________________________</span><br/><span style='color:#202124'>+ AccumulateDaily()</span><br/><span style='color:#202124'>+ AdvanceDay()</span><br/><span style='color:#202124'>+ Calm(int)</span><br/><span style='color:#202124'>- Clamp0(int) int</span><br/><span style='color:#202124'>+ DecayProgressDaily()</span><br/><span style='color:#202124'>+ Gain(int)</span><br/><span style='color:#202124'>+ PlaceSpawnPoint(float, float, float)</span><br/><span style='color:#202124'>+ Root(string, string, string, int, float, float)</span>"]
    RootRegistry["<span style='color:#202124'>RootRegistry</span><br/><span style='color:#5f6368'>_________________________</span><br/><span style='color:#202124'>- List&lt;Root&gt; _roots</span><br/><span style='color:#202124'>+ IReadOnlyList&lt;Root&gt; All</span><br/><span style='color:#202124'>+ int Count</span><br/><span style='color:#5f6368'>_________________________</span><br/><span style='color:#202124'>+ AdvanceDay()</span><br/><span style='color:#202124'>+ Clear()</span><br/><span style='color:#202124'>+ Find(string) Root</span><br/><span style='color:#202124'>+ TryAdd(Root) bool</span>"]
  end
  subgraph Legacy
    FieldCreator["<span style='color:#202124'>FieldCreator</span>"]
    MS_Spawner["<span style='color:#202124'>MS_Spawner</span><br/><span style='color:#5f6368'>___________</span><br/><span style='color:#c5221f'>- int index</span>"]
    OF_Spawner["<span style='color:#202124'>OF_Spawner</span><br/><span style='color:#5f6368'>___________</span><br/><span style='color:#c5221f'>- int index</span><br/><span style='color:#137333'>- Root root</span>"]
    RootsManager["<span style='color:#202124'>RootsManager</span><br/><span style='color:#5f6368'>_______________________________________________________</span><br/><span style='color:#c5221f'>+ FieldCreator creator</span><br/><span style='color:#c5221f'>- int d</span><br/><span style='color:#c5221f'>+ List&lt;int[]&gt; parameta</span><br/><span style='color:#c5221f'>+ List&lt;float[]&gt; pos</span><br/><span style='color:#c5221f'>+ Text power</span><br/><span style='color:#c5221f'>+ List&lt;string[]&gt; roots</span><br/><span style='color:#137333'>+ RootRegistry Roots</span><br/><span style='color:#c5221f'>+ GameObject slime</span><br/><span style='color:#5f6368'>_______________________________________________________</span><br/><span style='color:#b06000'>+ RootCreate(string, string, string, float, float, int)</span><br/><span style='color:#b06000'>+ StampedeJudge(Root)</span>"]
    RootUI["<span style='color:#202124'>RootUI</span>"]
    SaveData["<span style='color:#202124'>SaveData</span><br/><span style='color:#5f6368'>______________________</span><br/><span style='color:#c5221f'>+ List&lt;string[]&gt; roots</span>"]
  end

  OF_Spawner ==>|✚| Root
  Root ==>|✚| AccumulationLevel
  RootRegistry ==>|✚| Root
  RootsManager -.->|✚| AccumulationLevel
  RootsManager -.->|✚| Root
  RootsManager ==>|✚| RootRegistry
  RootUI -.->|✚| AccumulationLevel
  MS_Spawner -.->|✖| RootsManager
  RootsManager ==>|✖| FieldCreator
  SaveData -.->|✖| RootsManager
  OF_Spawner -.-> RootsManager
  OF_Spawner -.-> SaveData
  RootsManager -.-> MS_Spawner
  RootsManager -.-> OF_Spawner
  RootUI -.-> RootsManager

  linkStyle 0,1,2,3,4,5,6 stroke:#137333
  linkStyle 7,8,9 stroke:#c5221f
  linkStyle 10,11,12,13,14 stroke:#9aa0a6
  classDef default fill:#ffffff,stroke:#5f6368
  classDef added fill:#ffffff,stroke:#137333,stroke-width:4px
  class AccumulationLevel,Root,RootRegistry added
```

この図を見て `docs/dependencies-diagrams/` の現状図を更新すること。
