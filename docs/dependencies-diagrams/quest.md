<!-- 自動生成。図を手で直さない。dotnet test が作り直す。
     見出しと一行説明は .claude/skills/class-diff-diagram/SKILL.md のスライス表にある。
     末尾の覚え書きの節だけが手書きで、作り直しても消えない。
     枠の色: 青 = Domain / 橙 = Game / 灰 = Legacy（境界として置いているだけ）
     メンバは Domain / Game の核の公開分だけ。Legacy の中身は載せない。
     線: 太線 = 属性として保持する関係 / 点線 = 本体の中で使うだけの関係 -->

# クエスト 📜

依頼の受注と達成判定、報酬の受け渡し。

```mermaid
graph LR
  subgraph Domain
    Quest["Quest<br/>_________________________________________________________<br/>int Amount<br/>string Id<br/>bool IsComplete<br/>QuestKind Kind<br/>int Progress<br/>IReadOnlyList&lt;Reward&gt; Rewards<br/>string RootId<br/>string Target<br/>_________________________________________________________<br/>Advance()<br/>Quest(string, string, string, int, IReadOnlyList&lt;Reward&gt;)<br/>SetProgress(int)"]
    QuestId["QuestId<br/>___________________________________<br/>Is(string, QuestKind) bool<br/>LetterOf(QuestKind) char<br/>TryReadKind(string, QuestKind) bool"]
    QuestKind["QuestKind<br/>_________<br/>Breach<br/>Common<br/>Main<br/>Sub"]
    QuestProgress["QuestProgress<br/>_________________________<br/>Advance(int, int) int<br/>Clamp(int, int) int<br/>IsComplete(int, int) bool"]
    QuestRegistry["QuestRegistry<br/>___________________________________________________________________<br/>IReadOnlyList&lt;Quest&gt; All<br/>int Count<br/>___________________________________________________________________<br/>Clear()<br/>Create(QuestKind, string, string, int, IReadOnlyList&lt;Reward&gt;) Quest<br/>Find(string) Quest<br/>HasMainFor(string) bool<br/>Remove(string) bool"]
    Reward["Reward<br/>___________________<br/>int Amount<br/>string Kind<br/>___________________<br/>Reward(string, int)"]
    Root
  end
  Allmaity
  BigText
  Enemy
  GameManager
  MiddleText
  OF_Spawner
  PlayerDeath
  PlayerInventory
  QuestButton
  QuestManager
  RewardUI
  RootUI
  RootsManager
  SceneStarter
  Slime
  Sun2
  Allmaity -.-> Quest
  Allmaity -.-> QuestManager
  Enemy -.-> QuestManager
  OF_Spawner -.-> QuestManager
  PlayerDeath -.-> RewardUI
  Quest ==> QuestKind
  Quest ==> Reward
  QuestButton -.-> GameManager
  QuestButton -.-> MiddleText
  QuestButton -.-> QuestId
  QuestButton -.-> QuestKind
  QuestButton -.-> QuestManager
  QuestId -.-> QuestKind
  QuestManager -.-> BigText
  QuestManager -.-> GameManager
  QuestManager -.-> Quest
  QuestManager ==> QuestKind
  QuestManager ==> QuestRegistry
  QuestManager -.-> Reward
  QuestManager -.-> RewardUI
  QuestManager -.-> RootsManager
  QuestRegistry ==> Quest
  QuestRegistry -.-> QuestKind
  QuestRegistry -.-> Reward
  RewardUI -.-> PlayerInventory
  RewardUI -.-> Quest
  RewardUI -.-> QuestManager
  RewardUI -.-> Reward
  RewardUI -.-> Root
  RewardUI -.-> RootsManager
  RootUI -.-> QuestManager
  SceneStarter -.-> QuestManager
  SceneStarter -.-> RewardUI
  Slime -.-> QuestManager
  Sun2 -.-> QuestKind
  Sun2 -.-> QuestManager
  Sun2 -.-> Reward
  classDef domain fill:#e8f0fe,stroke:#1967d2,color:#174ea6;
  classDef game   fill:#fef7e0,stroke:#b06000,color:#8a5300;
  classDef legacy fill:#f1f3f4,stroke:#5f6368,color:#202124;
  class Quest,QuestId,QuestKind,QuestProgress,QuestRegistry,Reward,Root domain;
  class Allmaity,BigText,Enemy,GameManager,MiddleText,OF_Spawner,PlayerDeath,PlayerInventory,QuestButton,QuestManager,RewardUI,RootUI,RootsManager,SceneStarter,Slime,Sun2 legacy;
```

## 覚え書き

（まだ無い）
