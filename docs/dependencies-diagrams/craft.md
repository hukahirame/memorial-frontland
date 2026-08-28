<!-- 自動生成。図を手で直さない。dotnet test が作り直す。
     切り方と見出しは docs/dependencies-diagrams/slices.txt にある。
     末尾の覚え書きの節だけが手書きで、作り直しても消えない。
     枠の色: 青 = Domain / 橙 = Game / 灰 = Legacy（境界として置いているだけ）
     メンバは Domain / Game の核の公開分だけ。Legacy の中身は載せない。
     線: 太線 = 属性として保持する関係 / 点線 = 本体の中で使うだけの関係 -->

# クラフト 🔨

レシピの定義と、素材が足りているかの判定。

```mermaid
graph LR
  subgraph Domain
    Ingredient["Ingredient<br/>_______________________________<br/>int Amount<br/>string DisplayName<br/>string ItemId<br/>_______________________________<br/>Ingredient(string, string, int)"]
    Inventory
    Recipe["Recipe<br/>_________________________________________<br/>IReadOnlyList&lt;Ingredient&gt; Ingredients<br/>string ProductId<br/>_________________________________________<br/>CanCraftWith(Inventory) bool<br/>Recipe(string, IReadOnlyList&lt;Ingredient&gt;)"]
  end
  subgraph Game
    IngredientEntry["IngredientEntry<br/>__________________<br/>int amount<br/>string displayName<br/>string itemId"]
    RecipeDefinition["RecipeDefinition<br/>_________________________________<br/>List&lt;IngredientEntry&gt; ingredients<br/>string productId<br/>_________________________________<br/>ToDomain() Recipe"]
  end
  Craft
  CraftButton
  Craft_set
  ExchangeButton
  GameManager
  PlayerInventory
  RootsManager
  TempAudio
  Craft -.-> Craft_set
  Craft -.-> Inventory
  Craft ==> PlayerInventory
  Craft ==> Recipe
  Craft ==> RecipeDefinition
  CraftButton ==> Craft
  Craft_set -.-> GameManager
  ExchangeButton -.-> RootsManager
  ExchangeButton -.-> TempAudio
  Ingredient -.-> Inventory
  IngredientEntry -.-> Ingredient
  IngredientEntry -.-> Recipe
  Recipe ==> Ingredient
  Recipe -.-> Inventory
  RecipeDefinition -.-> Ingredient
  RecipeDefinition ==> IngredientEntry
  RecipeDefinition -.-> Recipe
  classDef domain fill:#e8f0fe,stroke:#1967d2,color:#174ea6;
  classDef game   fill:#fef7e0,stroke:#b06000,color:#8a5300;
  classDef legacy fill:#f1f3f4,stroke:#5f6368,color:#202124;
  class Ingredient,Inventory,Recipe domain;
  class IngredientEntry,RecipeDefinition game;
  class Craft,CraftButton,Craft_set,ExchangeButton,GameManager,PlayerInventory,RootsManager,TempAudio legacy;
```

## 覚え書き

`RecipeDefinition` は ScriptableObject。`ToDomain()` で `Recipe` に変換する。
