<!-- 現状確認用。手で維持する。
     構造が変わったら tools/diagram-diff.ps1 の出力（docs/dependencies-diff-diagrams/）
     を見て、この図を更新すること。更新漏れは SliceDiagramTests が検出する。
     枠の色: 青 = Domain / 橙 = Game / 灰 = Legacy（境界として置いているだけ） -->

# クラフト 🔨

レシピの定義と、素材が足りているかの判定。

```mermaid
graph LR
  subgraph Domain
    Recipe
    Ingredient
    Inventory
  end
  subgraph Game
    RecipeDefinition
    IngredientEntry
  end
  Craft
  Craft --> RecipeDefinition
  Craft --> Recipe
  Craft --> Inventory
  RecipeDefinition --> Recipe
  RecipeDefinition --> Ingredient
  IngredientEntry --> Ingredient
  IngredientEntry --> Recipe
  Recipe --> Ingredient
  Recipe --> Inventory
  classDef domain fill:#e8f0fe,stroke:#1967d2,color:#174ea6;
  classDef game   fill:#fef7e0,stroke:#b06000,color:#8a5300;
  classDef legacy fill:#f1f3f4,stroke:#5f6368,color:#202124;
  class Recipe,Ingredient,Inventory domain;
  class RecipeDefinition,IngredientEntry game;
  class Craft legacy;
```

`RecipeDefinition` は ScriptableObject。`ToDomain()` で `Recipe` に変換する。
`Recipe.CanCraftWith` が `Inventory` を読むので、持ち物のスライスと接する。
