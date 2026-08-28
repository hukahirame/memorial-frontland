<!-- tools/diagram-diff.ps1 が生成する。手で編集しない -->

# 依存の差分  HEAD -> 作業ツリー  (2026-08-28)

型 +0 / -0　　辺 +4 / -0 / 種類変化 1　　メンバが動いた型 1

**色が変化** — 緑が追加、赤が削除、橙が関連と依存の入れ替わり、灰が変わっていない
**線種が関係** — 太線が関連（フィールドで保持）、点線が依存（signature に出るだけ）
緑の枠が現れた型、赤の枠が消えた型。塗りは白で統一。
メンバは文字色で示す。緑が追加、赤が削除、橙が変更。

```mermaid
graph LR
  subgraph Domain
    Root["<span style='color:#202124'>Root</span>"]
  end
  subgraph Legacy
    OF_Spawner["<span style='color:#202124'>OF_Spawner</span><br/><span style='color:#5f6368'>___________</span><br/><span style='color:#c5221f'>- Root root</span>"]
    RewardUI["<span style='color:#202124'>RewardUI</span>"]
    RootUI["<span style='color:#202124'>RootUI</span>"]
    SceneStarter["<span style='color:#202124'>SceneStarter</span>"]
    Slime["<span style='color:#202124'>Slime</span>"]
  end

  RewardUI -.->|✚| Root
  RootUI -.->|✚| Root
  SceneStarter -.->|✚| Root
  Slime -.->|✚| Root
  OF_Spawner -.->|⟳| Root
  OF_Spawner -.-> SceneStarter
  SceneStarter -.-> OF_Spawner
  SceneStarter -.-> RewardUI

  linkStyle 0,1,2,3 stroke:#137333
  linkStyle 4 stroke:#b06000
  linkStyle 5,6,7 stroke:#9aa0a6
  classDef default fill:#ffffff,stroke:#5f6368
```

この図を見て `docs/dependencies-diagrams/` の現状図を更新すること。
