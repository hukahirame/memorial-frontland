<!-- tools/diagram-diff.ps1 が生成する。手で編集しない -->

# 依存の差分  HEAD -> 作業ツリー  (2026-08-29)

型 +3 / -0　　辺 +12 / -5 / 種類変化 1　　メンバが動いた型 3

**色が変化** — 緑が追加、赤が削除、橙が関連と依存の入れ替わり、灰が変わっていない
**線種が関係** — 太線が関連（フィールドで保持）、点線が依存（signature に出るだけ）
緑の枠が現れた型、赤の枠が消えた型。塗りは白で統一。
メンバは文字色で示す。緑が追加、赤が削除、橙が変更。

```mermaid
graph LR
  subgraph Domain
    Quest["<span style='color:#202124'>Quest</span><br/><span style='color:#5f6368'>___________________________________________________________</span><br/><span style='color:#202124'>+ int Amount</span><br/><span style='color:#202124'>+ string Id</span><br/><span style='color:#202124'>+ bool IsComplete</span><br/><span style='color:#202124'>+ QuestKind Kind</span><br/><span style='color:#202124'>+ int Progress</span><br/><span style='color:#202124'>+ IReadOnlyList&lt;Reward&gt; Rewards</span><br/><span style='color:#202124'>+ string RootId</span><br/><span style='color:#202124'>+ string Target</span><br/><span style='color:#5f6368'>___________________________________________________________</span><br/><span style='color:#202124'>+ Advance()</span><br/><span style='color:#202124'>+ Quest(string, string, string, int, IReadOnlyList&lt;Reward&gt;)</span><br/><span style='color:#202124'>+ SetProgress(int)</span>"]
    QuestId["<span style='color:#202124'>QuestId</span>"]
    QuestKind["<span style='color:#202124'>QuestKind</span>"]
    QuestProgress["<span style='color:#202124'>QuestProgress</span>"]
    QuestRegistry["<span style='color:#202124'>QuestRegistry</span><br/><span style='color:#5f6368'>_____________________________________________________________________</span><br/><span style='color:#202124'>- List&lt;Quest&gt; _quests</span><br/><span style='color:#202124'>+ IReadOnlyList&lt;Quest&gt; All</span><br/><span style='color:#202124'>+ int Count</span><br/><span style='color:#5f6368'>_____________________________________________________________________</span><br/><span style='color:#202124'>+ Clear()</span><br/><span style='color:#202124'>+ Create(QuestKind, string, string, int, IReadOnlyList&lt;Reward&gt;) Quest</span><br/><span style='color:#202124'>+ Find(string) Quest</span><br/><span style='color:#202124'>+ HasMainFor(string) bool</span><br/><span style='color:#202124'>+ Remove(string) bool</span>"]
    Reward["<span style='color:#202124'>Reward</span><br/><span style='color:#5f6368'>_____________________</span><br/><span style='color:#202124'>+ int Amount</span><br/><span style='color:#202124'>+ string Kind</span><br/><span style='color:#5f6368'>_____________________</span><br/><span style='color:#202124'>+ Reward(string, int)</span>"]
  end
  subgraph Legacy
    Allmaity["<span style='color:#202124'>Allmaity</span>"]
    QuestManager["<span style='color:#202124'>QuestManager</span><br/><span style='color:#5f6368'>_____________________________________________</span><br/><span style='color:#c5221f'>+ List&lt;string[]&gt; quests</span><br/><span style='color:#137333'>+ QuestRegistry Quests</span><br/><span style='color:#c5221f'>+ List&lt;string[]&gt; rewards</span><br/><span style='color:#137333'>- int ShownAtOnce</span><br/><span style='color:#137333'>- QuestKind[] ShowOrder</span><br/><span style='color:#5f6368'>_____________________________________________</span><br/><span style='color:#b06000'>- ClearUI(string, string)</span><br/><span style='color:#c5221f'>+ CreateQuest(string, string, string, string)</span><br/><span style='color:#137333'>- Headline(Quest) string</span><br/><span style='color:#137333'>+ RewardName(string) string</span><br/><span style='color:#b06000'>+ SyncQuestSub(Quest)</span>"]
    RewardUI["<span style='color:#202124'>RewardUI</span><br/><span style='color:#5f6368'>______________________</span><br/><span style='color:#137333'>+ string rewardUI_id</span><br/><span style='color:#c5221f'>+ int rewardUI_index</span><br/><span style='color:#5f6368'>______________________</span><br/><span style='color:#137333'>- Grant(Quest, Reward)</span>"]
    SaveData["<span style='color:#202124'>SaveData</span><br/><span style='color:#5f6368'>________________________</span><br/><span style='color:#c5221f'>+ List&lt;string[]&gt; quests</span><br/><span style='color:#c5221f'>+ List&lt;string[]&gt; rewards</span>"]
    Sun2["<span style='color:#202124'>Sun2</span>"]
  end

  Allmaity -.->|✚| Quest
  Quest ==>|✚| QuestKind
  Quest ==>|✚| Reward
  QuestManager -.->|✚| Quest
  QuestManager ==>|✚| QuestRegistry
  QuestManager -.->|✚| Reward
  QuestRegistry ==>|✚| Quest
  QuestRegistry -.->|✚| QuestKind
  QuestRegistry -.->|✚| Reward
  RewardUI -.->|✚| Quest
  RewardUI -.->|✚| Reward
  Sun2 -.->|✚| Reward
  QuestManager -.->|✖| QuestId
  QuestManager -.->|✖| QuestProgress
  RewardUI -.->|✖| QuestProgress
  SaveData -.->|✖| QuestManager
  Sun2 -.->|✖| QuestId
  QuestManager ==>|⟳| QuestKind
  Allmaity -.-> QuestManager
  QuestId -.-> QuestKind
  QuestManager -.-> RewardUI
  RewardUI -.-> QuestManager
  SaveData -.-> Sun2
  Sun2 -.-> QuestKind
  Sun2 -.-> QuestManager

  linkStyle 0,1,2,3,4,5,6,7,8,9,10,11 stroke:#137333
  linkStyle 12,13,14,15,16 stroke:#c5221f
  linkStyle 17 stroke:#b06000
  linkStyle 18,19,20,21,22,23,24 stroke:#9aa0a6
  classDef default fill:#ffffff,stroke:#5f6368
  classDef added fill:#ffffff,stroke:#137333,stroke-width:4px
  class Quest,QuestRegistry,Reward added
```

この図を見て `docs/dependencies-diagrams/` の現状図を更新すること。
