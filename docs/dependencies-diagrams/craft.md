<!-- 現状確認用。手で維持する。
     構造が変わったら tools/diagram-diff.ps1 の出力（docs/dependencies-diff-diagrams/）
     を見て、この図を更新すること。更新漏れは SliceDiagramTests が検出する。
     枠: 破線 = Domain / 太線 = Game / 細線 = Legacy（境界として置いているだけ） -->

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
  classDef domain stroke-dasharray:5;
  classDef game stroke-width:3px;
  class Recipe,Ingredient,Inventory domain;
  class RecipeDefinition,IngredientEntry game;
```

`RecipeDefinition` は ScriptableObject。`ToDomain()` で `Recipe` に変換する。
`Recipe.CanCraftWith` が `Inventory` を読むので、持ち物のスライスと接する。
