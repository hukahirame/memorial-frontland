<!-- tools/diagram-diff.ps1 が生成する。手で編集しない -->

# 依存の差分  HEAD -> 作業ツリー  (2026-08-29)

型 +3 / -0　　辺 +9 / -0 / 種類変化 0　　メンバが動いた型 1

**色が変化** — 緑が追加、赤が削除、橙が関連と依存の入れ替わり、灰が変わっていない
**線種が関係** — 太線が関連（フィールドで保持）、点線が依存（signature に出るだけ）
緑の枠が現れた型、赤の枠が消えた型。塗りは白で統一。
メンバは文字色で示す。緑が追加、赤が削除、橙が変更。

```mermaid
graph LR
  subgraph Domain
    QuestId["<span style='color:#202124'>QuestId</span><br/><span style='color:#5f6368'>_____________________________________</span><br/><span style='color:#202124'>- string Letters</span><br/><span style='color:#5f6368'>_____________________________________</span><br/><span style='color:#202124'>+ Is(string, QuestKind) bool</span><br/><span style='color:#202124'>+ LetterOf(QuestKind) char</span><br/><span style='color:#202124'>+ TryReadKind(string, QuestKind) bool</span>"]
    QuestKind["<span style='color:#202124'>QuestKind</span><br/><span style='color:#5f6368'>_________</span><br/><span style='color:#202124'>Breach</span><br/><span style='color:#202124'>Common</span><br/><span style='color:#202124'>Main</span><br/><span style='color:#202124'>Sub</span>"]
    QuestProgress["<span style='color:#202124'>QuestProgress</span><br/><span style='color:#5f6368'>___________________________</span><br/><span style='color:#202124'>+ Advance(int, int) int</span><br/><span style='color:#202124'>+ Clamp(int, int) int</span><br/><span style='color:#202124'>+ IsComplete(int, int) bool</span>"]
  end
  subgraph Legacy
    QuestButton["<span style='color:#202124'>QuestButton</span>"]
    QuestManager["<span style='color:#202124'>QuestManager</span><br/><span style='color:#5f6368'>______________________________</span><br/><span style='color:#b06000'>+ SyncQuestSub(int, QuestKind)</span>"]
    RewardUI["<span style='color:#202124'>RewardUI</span>"]
    Sun2["<span style='color:#202124'>Sun2</span>"]
  end

  QuestButton -.->|✚| QuestId
  QuestButton -.->|✚| QuestKind
  QuestId -.->|✚| QuestKind
  QuestManager -.->|✚| QuestId
  QuestManager -.->|✚| QuestKind
  QuestManager -.->|✚| QuestProgress
  RewardUI -.->|✚| QuestProgress
  Sun2 -.->|✚| QuestId
  Sun2 -.->|✚| QuestKind
  QuestButton -.-> QuestManager
  QuestManager -.-> RewardUI
  RewardUI -.-> QuestManager
  Sun2 -.-> QuestManager

  linkStyle 0,1,2,3,4,5,6,7,8 stroke:#137333
  linkStyle 9,10,11,12 stroke:#9aa0a6
  classDef default fill:#ffffff,stroke:#5f6368
  classDef added fill:#ffffff,stroke:#137333,stroke-width:4px
  class QuestId,QuestKind,QuestProgress added
```

この図を見て `docs/dependencies-diagrams/` の現状図を更新すること。
