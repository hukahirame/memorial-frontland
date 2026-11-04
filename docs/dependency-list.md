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
| 🟦 `Inventory` | **0** | **7** |
| 🟦 `OutField` | **0** | **4** |
| 🟦 `Quest` | **0** | **3** |
| 🟦 `Wallet` | **0** | **1** |
| 🟩 `Inventory` | **0** | **3** |
| 🟦 `Craft` | **1** | **2** |
| 🟦 `Player` | **1** | **4** |
| 🟦 `Roots` | **1** | **8** |
| 🟦 `Scene` | **1** | **5** |
| 🟩 `Craft` | **1** | **1** |
| 🟩 `Enemy` | **1** | **4** |
| 🟩 `Staging` | **1** | **9** |
| 🟦 `Enemy` | **2** | **4** |
| 🟩 `OutField` | **3** | **5** |
| 🟩 `Scene` | **3** | **7** |
| 🟨 `Day` | **6** | **1** |
| 🟨 `Inventory` | **7** | **3** |
| 🟨 `Craft` | **8** | **0** |
| 🟨 `Quest` | **9** | **6** |
| 🟨 `Roots` | **9** | **7** |
| 🟨 `Player` | **11** | **6** |
| 🟨 `Enemy` | **12** | **3** |
| 🟨 `Scene` | **12** | **6** |
| 🟨 `Save` | **13** | **2** |

## 詳細

### 🟦Craft 🔨 

↑ 🟦 `Inventory` **1** — Inventory **1**  

↓ 🟩 `Craft` **4** — Ingredient **2**, Recipe **2**  
↓ 🟨 `Craft` **3** — Recipe **3**  

### 🟦Day ☀️ 

↓ 🟨 `Day` **6** — DayClock **2**, DayCycle **2**, DayPlan **2**  

### 🟦Enemy 👾 

↑ 🟦 `Player` **6** — Heading **6**  
↑ 🟦 `Roots` **3** — Root **3**  

↓ 🟦 `Roots` **3** — IRandom **3**  
↓ 🟩 `Enemy` **1** — IRandom **1**  
↓ 🟨 `Enemy` **29** — EnemyAim **10**, AimRule **4**, SpawnRule **4**, ActionRule **3**, DropChance **3**, EnemyAction **3**, DropRule **1**, FallRule **1**  
↓ 🟨 `Roots` **1** — SpawnRule **1**  

### 🟦Inventory 🎒 

↓ 🟦 `Craft` **1** — Inventory **1**  
↓ 🟦 `Player` **2** — ItemDefinition **1**, ItemUse **1**  
↓ 🟨 `Craft` **4** — Inventory **2**, ItemDefinition **2**  
↓ 🟨 `Inventory` **27** — ItemSlot **9**, ItemUse **6**, Inventory **4**, ItemDefinition **4**, AddOutcome **2**, RemoveOutcome **2**  
↓ 🟨 `Quest` **1** — ItemDefinition **1**  
↓ 🟨 `Save` **3** — ItemSlot **3**  
↓ 🟨 `Scene` **2** — ItemCatalog **2**  

### 🟦OutField 🌲 

↓ 🟩 `OutField` **7** — FieldBounds **3**, FenceDurability **2**, FenceRule **1**, FenceSpot **1**  
↓ 🟩 `Scene` **1** — FieldBounds **1**  
↓ 🟨 `Enemy` **3** — FieldBounds **3**  
↓ 🟨 `Player` **3** — FieldBounds **2**, Nudge **1**  

### 🟦Player 🚶 

↑ 🟦 `Inventory` **2** — ItemDefinition **1**, ItemUse **1**  

↓ 🟦 `Enemy` **6** — Heading **6**  
↓ 🟩 `Staging` **24** — AttackMove **24**  
↓ 🟨 `Enemy` **8** — Health **6**, Heading **2**  
↓ 🟨 `Player` **41** — RollRule **9**, AttackTiming **8**, AttackKind **5**, AttackMove **5**, AttackRule **4**, Equipment **2**, Health **2**, Invulnerability **2**, Heading **1**, KnockbackRule **1**, MoveRule **1**, Push **1**  

### 🟦Quest 📜 

↓ 🟨 `Day` **1** — QuestKind **1**  
↓ 🟨 `Quest` **79** — Quest **39**, QuestKind **24**, Reward **13**, QuestRegistry **2**, QuestRewards **1**  
↓ 🟨 `Save` **9** — Quest **5**, Reward **4**  

### 🟦Roots 🌳 

↑ 🟦 `Enemy` **3** — IRandom **3**  

↓ 🟦 `Enemy` **3** — Root **3**  
↓ 🟦 `Scene` **4** — Root **4**  
↓ 🟨 `Day` **1** — Root **1**  
↓ 🟨 `Enemy` **12** — Raider **10**, Root **2**  
↓ 🟨 `Quest` **7** — Root **7**  
↓ 🟨 `Roots` **14** — Root **6**, AccumulationLevel **4**, BaseRaid **2**, RootRegistry **2**  
↓ 🟨 `Save` **11** — Raider **6**, Root **5**  
↓ 🟨 `Scene` **2** — Root **2**  

### 🟦Scene 🚪 

↑ 🟦 `Roots` **4** — Root **4**  

↓ 🟩 `Scene` **33** — Arrival **7**, Placement **7**, Route **5**, TravelRule **5**, ExitKind **3**, AreaKind **2**, PlacementLedger **2**, Whereabouts **2**  
↓ 🟨 `Quest` **4** — AreaKind **2**, Whereabouts **2**  
↓ 🟨 `Roots` **4** — TravelRule **4**  
↓ 🟨 `Save` **9** — Placement **7**, PlacementLedger **1**, Whereabouts **1**  
↓ 🟨 `Scene` **3** — TravelRule **2**, AreaKind **1**  

### 🟦Wallet 💰 

↓ 🟨 `Scene` **2** — Wallet **2**  

### 🟩Craft 🔨 

↑ 🟦 `Craft` **4** — Ingredient **2**, Recipe **2**  

↓ 🟨 `Craft` **1** — RecipeDefinition **1**  

### 🟩Enemy 👾 

↑ 🟦 `Enemy` **1** — IRandom **1**  

↓ 🟨 `Enemy` **14** — IDamageable **10**, UnityRandom **4**  
↓ 🟨 `Player` **2** — IDamageable **2**  
↓ 🟨 `Roots` **1** — UnityRandom **1**  
↓ 🟨 `Save` **2** — IDamageable **2**  

### 🟩Inventory 🎒 

↓ 🟩 `OutField` **2** — IItemReceiver **2**  
↓ 🟨 `Inventory` **1** — IItemReceiver **1**  
↓ 🟨 `Quest` **2** — IItemReceiver **2**  

### 🟩OutField 🌲 

↑ 🟦 `OutField` **7** — FieldBounds **3**, FenceDurability **2**, FenceRule **1**, FenceSpot **1**  
↑ 🟩 `Inventory` **2** — IItemReceiver **2**  
↑ 🟩 `Staging` **6** — TempAudio **4**, WorldGauge **2**  

↓ 🟩 `Scene` **2** — Fence **2**  
↓ 🟨 `Enemy` **6** — Exposition **4**, Fence **2**  
↓ 🟨 `Inventory` **2** — Fence **1**, FenceSite **1**  
↓ 🟨 `Player` **13** — Fence **11**, Exposition **2**  
↓ 🟨 `Scene` **1** — Exposition **1**  

### 🟩Scene 🚪 

↑ 🟦 `OutField` **1** — FieldBounds **1**  
↑ 🟦 `Scene` **33** — Arrival **7**, Placement **7**, Route **5**, TravelRule **5**, ExitKind **3**, AreaKind **2**, PlacementLedger **2**, Whereabouts **2**  
↑ 🟩 `OutField` **2** — Fence **2**  

↓ 🟨 `Enemy` **9** — Area **5**, AreaTravel **4**  
↓ 🟨 `Inventory` **2** — PlacedObjects **2**  
↓ 🟨 `Player` **5** — AreaTravel **2**, Exit **2**, Area **1**  
↓ 🟨 `Quest` **2** — AreaTravel **2**  
↓ 🟨 `Roots` **2** — AreaTravel **2**  
↓ 🟨 `Save` **5** — PlacedObjects **3**, Area **1**, AreaTravel **1**  
↓ 🟨 `Scene` **6** — Area **3**, AreaTravel **3**  

### 🟩Staging 🎥 

↑ 🟦 `Player` **24** — AttackMove **24**  

↓ 🟩 `OutField` **6** — TempAudio **4**, WorldGauge **2**  
↓ 🟨 `Craft` **4** — TempAudio **2**, CloseManager **1**, MiddleText **1**  
↓ 🟨 `Day` **1** — MiddleText **1**  
↓ 🟨 `Enemy` **6** — AlertMark **2**, MiddleText **2**, WorldGauge **2**  
↓ 🟨 `Inventory` **4** — TempAudio **2**, KeyHint **1**, MiddleText **1**  
↓ 🟨 `Player` **27** — TempAudio **7**, HitStop **6**, MainCamera **5**, KeyHint **4**, AttackGuide **2**, RollStaging **2**, BigText **1**  
↓ 🟨 `Quest` **5** — BigText **3**, MiddleText **2**  
↓ 🟨 `Roots` **7** — CloseManager **4**, MiddleText **3**  
↓ 🟨 `Scene` **1** — PressFeedback **1**  

### 🟨Craft 🔨 

↑ 🟦 `Craft` **3** — Recipe **3**  
↑ 🟦 `Inventory` **4** — Inventory **2**, ItemDefinition **2**  
↑ 🟩 `Craft` **1** — RecipeDefinition **1**  
↑ 🟩 `Staging` **4** — TempAudio **2**, CloseManager **1**, MiddleText **1**  
↑ 🟨 `Inventory` **1** — PlayerInventory **1**  
↑ 🟨 `Player` **2** — Player2 **2**  
↑ 🟨 `Roots` **1** — RootsManager **1**  
↑ 🟨 `Scene` **3** — GameManager **3**  

### 🟨Day ☀️ 

↑ 🟦 `Day` **6** — DayClock **2**, DayCycle **2**, DayPlan **2**  
↑ 🟦 `Quest` **1** — QuestKind **1**  
↑ 🟦 `Roots` **1** — Root **1**  
↑ 🟩 `Staging` **1** — MiddleText **1**  
↑ 🟨 `Quest` **3** — QuestManager **3**  
↑ 🟨 `Roots` **2** — RootsManager **2**  

↓ 🟨 `Save` **4** — Sun2 **4**  

### 🟨Enemy 👾 

↑ 🟦 `Enemy` **29** — EnemyAim **10**, AimRule **4**, SpawnRule **4**, ActionRule **3**, DropChance **3**, EnemyAction **3**, DropRule **1**, FallRule **1**  
↑ 🟦 `OutField` **3** — FieldBounds **3**  
↑ 🟦 `Player` **8** — Health **6**, Heading **2**  
↑ 🟦 `Roots` **12** — Raider **10**, Root **2**  
↑ 🟩 `Enemy` **14** — IDamageable **10**, UnityRandom **4**  
↑ 🟩 `OutField` **6** — Exposition **4**, Fence **2**  
↑ 🟩 `Scene` **9** — Area **5**, AreaTravel **4**  
↑ 🟩 `Staging` **6** — AlertMark **2**, MiddleText **2**, WorldGauge **2**  
↑ 🟨 `Player` **3** — Player2 **3**  
↑ 🟨 `Quest` **2** — QuestManager **2**  
↑ 🟨 `Roots` **5** — RootsManager **5**  
↑ 🟨 `Save` **2** — SaveData **1**, SaveSystem **1**  

↓ 🟨 `Player` **2** — OF_Spawner **1**, SpawnerCandidate **1**  
↓ 🟨 `Save` **2** — MS_Spawner **2**  
↓ 🟨 `Scene` **1** — OF_Spawner **1**  

### 🟨Inventory 🎒 

↑ 🟦 `Inventory` **27** — ItemSlot **9**, ItemUse **6**, Inventory **4**, ItemDefinition **4**, AddOutcome **2**, RemoveOutcome **2**  
↑ 🟩 `Inventory` **1** — IItemReceiver **1**  
↑ 🟩 `OutField` **2** — Fence **1**, FenceSite **1**  
↑ 🟩 `Scene` **2** — PlacedObjects **2**  
↑ 🟩 `Staging` **4** — TempAudio **2**, KeyHint **1**, MiddleText **1**  
↑ 🟨 `Player` **11** — Player2 **8**, Weapon **3**  
↑ 🟨 `Scene` **6** — GameManager **6**  

↓ 🟨 `Craft` **1** — PlayerInventory **1**  
↓ 🟨 `Player` **3** — PlayerInventory **2**, WeaponBox **1**  
↓ 🟨 `Save` **4** — PlayerInventory **4**  

### 🟨Player 🚶 

↑ 🟦 `OutField` **3** — FieldBounds **2**, Nudge **1**  
↑ 🟦 `Player` **41** — RollRule **9**, AttackTiming **8**, AttackKind **5**, AttackMove **5**, AttackRule **4**, Equipment **2**, Health **2**, Invulnerability **2**, Heading **1**, KnockbackRule **1**, MoveRule **1**, Push **1**  
↑ 🟩 `Enemy` **2** — IDamageable **2**  
↑ 🟩 `OutField` **13** — Fence **11**, Exposition **2**  
↑ 🟩 `Scene` **5** — AreaTravel **2**, Exit **2**, Area **1**  
↑ 🟩 `Staging` **27** — TempAudio **7**, HitStop **6**, MainCamera **5**, KeyHint **4**, AttackGuide **2**, RollStaging **2**, BigText **1**  
↑ 🟨 `Enemy` **2** — OF_Spawner **1**, SpawnerCandidate **1**  
↑ 🟨 `Inventory` **3** — PlayerInventory **2**, WeaponBox **1**  
↑ 🟨 `Quest` **2** — QuestManager **1**, RewardUI **1**  
↑ 🟨 `Roots` **1** — RootsManager **1**  
↑ 🟨 `Scene` **1** — GameManager **1**  

↓ 🟨 `Craft` **2** — Player2 **2**  
↓ 🟨 `Enemy` **3** — Player2 **3**  
↓ 🟨 `Inventory` **11** — Player2 **8**, Weapon **3**  
↓ 🟨 `Roots` **2** — Player2 **2**  
↓ 🟨 `Save` **6** — Player2 **5**, Weapon **1**  
↓ 🟨 `Scene` **2** — Player2 **2**  

### 🟨Quest 📜 

↑ 🟦 `Inventory` **1** — ItemDefinition **1**  
↑ 🟦 `Quest` **79** — Quest **39**, QuestKind **24**, Reward **13**, QuestRegistry **2**, QuestRewards **1**  
↑ 🟦 `Roots` **7** — Root **7**  
↑ 🟦 `Scene` **4** — AreaKind **2**, Whereabouts **2**  
↑ 🟩 `Inventory` **2** — IItemReceiver **2**  
↑ 🟩 `Scene` **2** — AreaTravel **2**  
↑ 🟩 `Staging` **5** — BigText **3**, MiddleText **2**  
↑ 🟨 `Roots` **5** — RootsManager **5**  
↑ 🟨 `Scene` **2** — GameManager **2**  

↓ 🟨 `Day` **3** — QuestManager **3**  
↓ 🟨 `Enemy` **2** — QuestManager **2**  
↓ 🟨 `Player` **2** — QuestManager **1**, RewardUI **1**  
↓ 🟨 `Roots` **4** — QuestManager **4**  
↓ 🟨 `Save` **7** — QuestManager **7**  
↓ 🟨 `Scene` **4** — QuestManager **2**, RewardUI **2**  

### 🟨Roots 🌳 

↑ 🟦 `Enemy` **1** — SpawnRule **1**  
↑ 🟦 `Roots` **14** — Root **6**, AccumulationLevel **4**, BaseRaid **2**, RootRegistry **2**  
↑ 🟦 `Scene` **4** — TravelRule **4**  
↑ 🟩 `Enemy` **1** — UnityRandom **1**  
↑ 🟩 `Scene` **2** — AreaTravel **2**  
↑ 🟩 `Staging` **7** — CloseManager **4**, MiddleText **3**  
↑ 🟨 `Player` **2** — Player2 **2**  
↑ 🟨 `Quest` **4** — QuestManager **4**  
↑ 🟨 `Scene` **2** — GameManager **2**  

↓ 🟨 `Craft` **1** — RootsManager **1**  
↓ 🟨 `Day` **2** — RootsManager **2**  
↓ 🟨 `Enemy` **5** — RootsManager **5**  
↓ 🟨 `Player` **1** — RootsManager **1**  
↓ 🟨 `Quest` **5** — RootsManager **5**  
↓ 🟨 `Save` **7** — RootsManager **7**  
↓ 🟨 `Scene` **2** — RootsManager **2**  

### 🟨Save 💾 

↑ 🟦 `Inventory` **3** — ItemSlot **3**  
↑ 🟦 `Quest` **9** — Quest **5**, Reward **4**  
↑ 🟦 `Roots` **11** — Raider **6**, Root **5**  
↑ 🟦 `Scene` **9** — Placement **7**, PlacementLedger **1**, Whereabouts **1**  
↑ 🟩 `Enemy` **2** — IDamageable **2**  
↑ 🟩 `Scene` **5** — PlacedObjects **3**, Area **1**, AreaTravel **1**  
↑ 🟨 `Day` **4** — Sun2 **4**  
↑ 🟨 `Enemy` **2** — MS_Spawner **2**  
↑ 🟨 `Inventory` **4** — PlayerInventory **4**  
↑ 🟨 `Player` **6** — Player2 **5**, Weapon **1**  
↑ 🟨 `Quest` **7** — QuestManager **7**  
↑ 🟨 `Roots` **7** — RootsManager **7**  
↑ 🟨 `Scene` **2** — GameManager **2**  

↓ 🟨 `Enemy` **2** — SaveData **1**, SaveSystem **1**  
↓ 🟨 `Scene` **17** — SaveSystem **15**, SaveData **2**  

### 🟨Scene 🚪 

↑ 🟦 `Inventory` **2** — ItemCatalog **2**  
↑ 🟦 `Roots` **2** — Root **2**  
↑ 🟦 `Scene` **3** — TravelRule **2**, AreaKind **1**  
↑ 🟦 `Wallet` **2** — Wallet **2**  
↑ 🟩 `OutField` **1** — Exposition **1**  
↑ 🟩 `Scene` **6** — Area **3**, AreaTravel **3**  
↑ 🟩 `Staging` **1** — PressFeedback **1**  
↑ 🟨 `Enemy` **1** — OF_Spawner **1**  
↑ 🟨 `Player` **2** — Player2 **2**  
↑ 🟨 `Quest` **4** — QuestManager **2**, RewardUI **2**  
↑ 🟨 `Roots` **2** — RootsManager **2**  
↑ 🟨 `Save` **17** — SaveSystem **15**, SaveData **2**  

↓ 🟨 `Craft` **3** — GameManager **3**  
↓ 🟨 `Inventory` **6** — GameManager **6**  
↓ 🟨 `Player` **1** — GameManager **1**  
↓ 🟨 `Quest` **2** — GameManager **2**  
↓ 🟨 `Roots` **2** — GameManager **2**  
↓ 🟨 `Save` **2** — GameManager **2**  
