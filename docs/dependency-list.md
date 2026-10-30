# 依存関係一覧表

<!-- DependencyListTests が生成する。手で編集しない -->

- フォルダ間のクラス依存数
- 🟦 Domain ／ 🟩 Game ／ 🟨 Legacy
- ↑（fan out）：依存する
- ↓（fan in） ：依存される
- 構文木から数える。コメントと文字列は入らない。
  var で受けた依存は、そのフォルダ内のどこにも型名が出なければ見えない

## fan-out 昇順テーブル

| 節 | ↑ | ↓ |
|---|---|---|
| 🟦 `Day` | **0** | **1** |
| 🟦 `Inventory` | **0** | **6** |
| 🟦 `OutField` | **0** | **3** |
| 🟦 `Quest` | **0** | **5** |
| 🟦 `Roots` | **0** | **7** |
| 🟦 `Wallet` | **0** | **1** |
| 🟩 `Inventory` | **0** | **2** |
| 🟩 `Staging` | **0** | **7** |
| 🟦 `Craft` | **1** | **2** |
| 🟦 `Enemy` | **1** | **2** |
| 🟦 `Player` | **1** | **2** |
| 🟩 `Craft` | **1** | **1** |
| 🟩 `Enemy` | **1** | **2** |
| 🟦 `Scene` | **2** | **4** |
| 🟩 `OutField` | **2** | **3** |
| 🟩 `Scene` | **2** | **6** |
| 🟨 `Roots` | **3** | **5** |
| 🟨 `Day` | **6** | **1** |
| 🟨 `Inventory` | **6** | **4** |
| 🟨 `Craft` | **7** | **0** |
| 🟨 `Save` | **7** | **2** |
| 🟨 `Quest` | **8** | **5** |
| 🟨 `Player` | **11** | **4** |
| 🟨 `Scene` | **11** | **5** |
| 🟨 `Enemy` | **12** | **2** |

## 詳細

### 🟦Craft 🔨 

↑ 🟦 `Inventory` **1** — Inventory **1**  

↓ 🟩 `Craft` **4** — Ingredient **2**, Recipe **2**  
↓ 🟨 `Craft` **3** — Recipe **3**  

### 🟦Day ☀️ 

↓ 🟨 `Day` **6** — DayClock **2**, DayCycle **2**, DayPlan **2**  

### 🟦Enemy 👾 

↑ 🟦 `Roots` **2** — Root **2**  

↓ 🟩 `Enemy` **1** — IRandom **1**  
↓ 🟨 `Enemy` **14** — SpawnRule **4**, DropChance **3**, EnemyAction **3**, ActionRule **2**, DropRule **1**, FallRule **1**  

### 🟦Inventory 🎒 

↓ 🟦 `Craft` **1** — Inventory **1**  
↓ 🟦 `Player` **2** — ItemDefinition **1**, ItemUse **1**  
↓ 🟨 `Craft` **3** — Inventory **2**, ItemDefinition **1**  
↓ 🟨 `Inventory` **22** — ItemSlot **6**, ItemUse **5**, Inventory **4**, ItemDefinition **3**, AddOutcome **2**, RemoveOutcome **2**  
↓ 🟨 `Save` **3** — ItemSlot **3**  
↓ 🟨 `Scene` **2** — ItemCatalog **2**  

### 🟦OutField 🌲 

↓ 🟩 `OutField` **2** — FieldBounds **2**  
↓ 🟨 `Enemy` **1** — FieldBounds **1**  
↓ 🟨 `Player` **2** — FieldBounds **1**, Nudge **1**  

### 🟦Player 🚶 

↑ 🟦 `Inventory` **2** — ItemDefinition **1**, ItemUse **1**  

↓ 🟨 `Enemy` **6** — Health **6**  
↓ 🟨 `Player` **9** — AttackKind **3**, AttackRule **2**, Equipment **2**, Health **2**  

### 🟦Quest 📜 

↓ 🟦 `Scene` **1** — Quest **1**  
↓ 🟩 `Scene` **1** — Quest **1**  
↓ 🟨 `Day` **5** — Reward **3**, QuestKind **2**  
↓ 🟨 `Player` **1** — Quest **1**  
↓ 🟨 `Quest` **44** — QuestKind **19**, Quest **14**, Reward **8**, QuestRegistry **2**, QuestId **1**  

### 🟦Roots 🌳 

↓ 🟦 `Enemy` **2** — Root **2**  
↓ 🟦 `Scene` **2** — Root **2**  
↓ 🟨 `Day` **1** — Root **1**  
↓ 🟨 `Enemy` **2** — Root **2**  
↓ 🟨 `Quest` **2** — Root **2**  
↓ 🟨 `Roots` **10** — AccumulationLevel **4**, BaseRaid **2**, Root **2**, RootRegistry **2**  
↓ 🟨 `Scene` **2** — Root **2**  

### 🟦Scene 🚪 

↑ 🟦 `Quest` **1** — Quest **1**  
↑ 🟦 `Roots` **2** — Root **2**  

↓ 🟩 `Scene` **26** — Arrival **6**, Placement **5**, ExitKind **3**, Route **3**, TravelRule **3**, AreaKind **2**, PlacementLedger **2**, Whereabouts **2**  
↓ 🟨 `Quest` **2** — Whereabouts **2**  
↓ 🟨 `Save` **8** — Placement **6**, PlacementLedger **1**, Whereabouts **1**  
↓ 🟨 `Scene` **1** — AreaKind **1**  

### 🟦Wallet 💰 

↓ 🟨 `Scene` **2** — Wallet **2**  

### 🟩Craft 🔨 

↑ 🟦 `Craft` **4** — Ingredient **2**, Recipe **2**  

↓ 🟨 `Craft` **1** — RecipeDefinition **1**  

### 🟩Enemy 👾 

↑ 🟦 `Enemy` **1** — IRandom **1**  

↓ 🟨 `Enemy` **8** — IDamageable **4**, UnityRandom **4**  
↓ 🟨 `Player` **1** — IDamageable **1**  

### 🟩Inventory 🎒 

↓ 🟩 `OutField` **2** — IItemReceiver **2**  
↓ 🟨 `Inventory` **1** — IItemReceiver **1**  

### 🟩OutField 🌲 

↑ 🟦 `OutField` **2** — FieldBounds **2**  
↑ 🟩 `Inventory` **2** — IItemReceiver **2**  

↓ 🟨 `Enemy` **1** — Exposition **1**  
↓ 🟨 `Player` **1** — Exposition **1**  
↓ 🟨 `Scene` **2** — Exposition **2**  

### 🟩Scene 🚪 

↑ 🟦 `Quest` **1** — Quest **1**  
↑ 🟦 `Scene` **26** — Arrival **6**, Placement **5**, ExitKind **3**, Route **3**, TravelRule **3**, AreaKind **2**, PlacementLedger **2**, Whereabouts **2**  

↓ 🟨 `Enemy` **3** — AreaTravel **2**, Area **1**  
↓ 🟨 `Inventory` **1** — PlacedObjects **1**  
↓ 🟨 `Player` **4** — AreaTravel **2**, Exit **2**  
↓ 🟨 `Quest` **1** — AreaTravel **1**  
↓ 🟨 `Save` **5** — PlacedObjects **3**, Area **1**, AreaTravel **1**  
↓ 🟨 `Scene` **5** — AreaTravel **3**, Area **2**  

### 🟩Staging 🎥 

↓ 🟨 `Craft` **2** — TempAudio **2**  
↓ 🟨 `Day` **1** — MiddleText **1**  
↓ 🟨 `Enemy` **2** — MiddleText **2**  
↓ 🟨 `Inventory` **2** — TempAudio **2**  
↓ 🟨 `Player` **6** — TempAudio **4**, BigText **1**, MiddleText **1**  
↓ 🟨 `Quest` **5** — MiddleText **3**, BigText **2**  
↓ 🟨 `Roots` **2** — MiddleText **2**  

### 🟨Craft 🔨 

↑ 🟦 `Craft` **3** — Recipe **3**  
↑ 🟦 `Inventory` **3** — Inventory **2**, ItemDefinition **1**  
↑ 🟩 `Craft` **1** — RecipeDefinition **1**  
↑ 🟩 `Staging` **2** — TempAudio **2**  
↑ 🟨 `Inventory` **1** — PlayerInventory **1**  
↑ 🟨 `Roots` **1** — RootsManager **1**  
↑ 🟨 `Scene` **1** — GameManager **1**  

### 🟨Day ☀️ 

↑ 🟦 `Day` **6** — DayClock **2**, DayCycle **2**, DayPlan **2**  
↑ 🟦 `Quest` **5** — Reward **3**, QuestKind **2**  
↑ 🟦 `Roots` **1** — Root **1**  
↑ 🟩 `Staging` **1** — MiddleText **1**  
↑ 🟨 `Quest` **3** — QuestManager **3**  
↑ 🟨 `Roots` **2** — RootsManager **2**  

↓ 🟨 `Save` **2** — Sun2 **2**  

### 🟨Enemy 👾 

↑ 🟦 `Enemy` **14** — SpawnRule **4**, DropChance **3**, EnemyAction **3**, ActionRule **2**, DropRule **1**, FallRule **1**  
↑ 🟦 `OutField` **1** — FieldBounds **1**  
↑ 🟦 `Player` **6** — Health **6**  
↑ 🟦 `Roots` **2** — Root **2**  
↑ 🟩 `Enemy` **8** — IDamageable **4**, UnityRandom **4**  
↑ 🟩 `OutField` **1** — Exposition **1**  
↑ 🟩 `Scene` **3** — AreaTravel **2**, Area **1**  
↑ 🟩 `Staging` **2** — MiddleText **2**  
↑ 🟨 `Player` **2** — Player2 **2**  
↑ 🟨 `Quest` **2** — QuestManager **2**  
↑ 🟨 `Roots` **5** — RootsManager **5**  
↑ 🟨 `Save` **2** — SaveData **1**, SaveSystem **1**  

↓ 🟨 `Player` **2** — OF_Spawner **1**, SpawnerCandidate **1**  
↓ 🟨 `Scene` **1** — OF_Spawner **1**  

### 🟨Inventory 🎒 

↑ 🟦 `Inventory` **22** — ItemSlot **6**, ItemUse **5**, Inventory **4**, ItemDefinition **3**, AddOutcome **2**, RemoveOutcome **2**  
↑ 🟩 `Inventory` **1** — IItemReceiver **1**  
↑ 🟩 `Scene` **1** — PlacedObjects **1**  
↑ 🟩 `Staging` **2** — TempAudio **2**  
↑ 🟨 `Player` **5** — Player2 **4**, Weapon **1**  
↑ 🟨 `Scene` **3** — GameManager **3**  

↓ 🟨 `Craft` **1** — PlayerInventory **1**  
↓ 🟨 `Player` **1** — WeaponBox **1**  
↓ 🟨 `Quest` **1** — PlayerInventory **1**  
↓ 🟨 `Save` **4** — PlayerInventory **4**  

### 🟨Player 🚶 

↑ 🟦 `OutField` **2** — FieldBounds **1**, Nudge **1**  
↑ 🟦 `Player` **9** — AttackKind **3**, AttackRule **2**, Equipment **2**, Health **2**  
↑ 🟦 `Quest` **1** — Quest **1**  
↑ 🟩 `Enemy` **1** — IDamageable **1**  
↑ 🟩 `OutField` **1** — Exposition **1**  
↑ 🟩 `Scene` **4** — AreaTravel **2**, Exit **2**  
↑ 🟩 `Staging` **6** — TempAudio **4**, BigText **1**, MiddleText **1**  
↑ 🟨 `Enemy` **2** — OF_Spawner **1**, SpawnerCandidate **1**  
↑ 🟨 `Inventory` **1** — WeaponBox **1**  
↑ 🟨 `Quest` **3** — QuestManager **2**, RewardUI **1**  
↑ 🟨 `Scene` **2** — GameManager **2**  

↓ 🟨 `Enemy` **2** — Player2 **2**  
↓ 🟨 `Inventory` **5** — Player2 **4**, Weapon **1**  
↓ 🟨 `Save` **3** — Player2 **3**  
↓ 🟨 `Scene` **2** — Player2 **2**  

### 🟨Quest 📜 

↑ 🟦 `Quest` **44** — QuestKind **19**, Quest **14**, Reward **8**, QuestRegistry **2**, QuestId **1**  
↑ 🟦 `Roots` **2** — Root **2**  
↑ 🟦 `Scene` **2** — Whereabouts **2**  
↑ 🟩 `Scene` **1** — AreaTravel **1**  
↑ 🟩 `Staging` **5** — MiddleText **3**, BigText **2**  
↑ 🟨 `Inventory` **1** — PlayerInventory **1**  
↑ 🟨 `Roots` **3** — RootsManager **3**  
↑ 🟨 `Scene` **2** — GameManager **2**  

↓ 🟨 `Day` **3** — QuestManager **3**  
↓ 🟨 `Enemy` **2** — QuestManager **2**  
↓ 🟨 `Player` **3** — QuestManager **2**, RewardUI **1**  
↓ 🟨 `Roots` **1** — QuestManager **1**  
↓ 🟨 `Scene` **3** — RewardUI **2**, QuestManager **1**  

### 🟨Roots 🌳 

↑ 🟦 `Roots` **10** — AccumulationLevel **4**, BaseRaid **2**, Root **2**, RootRegistry **2**  
↑ 🟩 `Staging` **2** — MiddleText **2**  
↑ 🟨 `Quest` **1** — QuestManager **1**  

↓ 🟨 `Craft` **1** — RootsManager **1**  
↓ 🟨 `Day` **2** — RootsManager **2**  
↓ 🟨 `Enemy` **5** — RootsManager **5**  
↓ 🟨 `Quest` **3** — RootsManager **3**  
↓ 🟨 `Scene` **2** — RootsManager **2**  

### 🟨Save 💾 

↑ 🟦 `Inventory` **3** — ItemSlot **3**  
↑ 🟦 `Scene` **8** — Placement **6**, PlacementLedger **1**, Whereabouts **1**  
↑ 🟩 `Scene` **5** — PlacedObjects **3**, Area **1**, AreaTravel **1**  
↑ 🟨 `Day` **2** — Sun2 **2**  
↑ 🟨 `Inventory` **4** — PlayerInventory **4**  
↑ 🟨 `Player` **3** — Player2 **3**  
↑ 🟨 `Scene` **3** — GameManager **3**  

↓ 🟨 `Enemy` **2** — SaveData **1**, SaveSystem **1**  
↓ 🟨 `Scene` **1** — SaveSystem **1**  

### 🟨Scene 🚪 

↑ 🟦 `Inventory` **2** — ItemCatalog **2**  
↑ 🟦 `Roots` **2** — Root **2**  
↑ 🟦 `Scene` **1** — AreaKind **1**  
↑ 🟦 `Wallet` **2** — Wallet **2**  
↑ 🟩 `OutField` **2** — Exposition **2**  
↑ 🟩 `Scene` **5** — AreaTravel **3**, Area **2**  
↑ 🟨 `Enemy` **1** — OF_Spawner **1**  
↑ 🟨 `Player` **2** — Player2 **2**  
↑ 🟨 `Quest` **3** — RewardUI **2**, QuestManager **1**  
↑ 🟨 `Roots` **2** — RootsManager **2**  
↑ 🟨 `Save` **1** — SaveSystem **1**  

↓ 🟨 `Craft` **1** — GameManager **1**  
↓ 🟨 `Inventory` **3** — GameManager **3**  
↓ 🟨 `Player` **2** — GameManager **2**  
↓ 🟨 `Quest` **2** — GameManager **2**  
↓ 🟨 `Save` **3** — GameManager **3**  
