<!-- 自動生成。図を手で直さない。dotnet test が作り直す。
     見出しと一行説明は .claude/skills/class-diff-diagram/SKILL.md のスライス表にある。
     末尾の覚え書きの節だけが手書きで、作り直しても消えない。
     枠の色: 青 = Domain / 橙 = Game / 灰 = Legacy（境界として置いているだけ）
     メンバは Domain / Game の核の公開分だけ。Legacy の中身は載せない。
     線: 太線 = 属性として保持する関係 / 点線 = 本体の中で使うだけの関係 -->

# 日の進行 ☀️

1日を進める側。根源とクエストとセーブを同時に叩く。

```mermaid
graph LR
  MiddleText
  QuestManager
  RootsManager
  SaveData
  Sun2
  SaveData -.-> Sun2
  Sun2 -.-> MiddleText
  Sun2 -.-> QuestManager
  Sun2 -.-> RootsManager
  classDef domain fill:#e8f0fe,stroke:#1967d2,color:#174ea6;
  classDef game   fill:#fef7e0,stroke:#b06000,color:#8a5300;
  classDef legacy fill:#f1f3f4,stroke:#5f6368,color:#202124;
  class MiddleText,QuestManager,RootsManager,SaveData,Sun2 legacy;
```

## 覚え書き

（まだ無い）
