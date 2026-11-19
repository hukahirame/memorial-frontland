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
| 🟦 `Day` | **0** | **3** |
| 🟦 `Inventory` | **0** | **7** |
| 🟦 `OutField` | **0** | **4** |
| 🟦 `Wallet` | **0** | **1** |
| 🟦 `Craft` | **1** | **1** |
| 🟦 `Player` | **1** | **4** |
| 🟦 `Roots` | **1** | **8** |
| 🟦 `Scene` | **1** | **6** |
| 🟩 `Day` | **1** | **4** |
| 🟩 `Wallet` | **1** | **5** |
| 🟦 `Enemy` | **2** | **2** |
| 🟦 `Quest` | **2** | **3** |
| 🟩 `OutField` | **4** | **5** |
| 🟩 `Staging` | **4** | **8** |
| 🟩 `Inventory` | **5** | **4** |
| 🟩 `Craft` | **7** | **0** |
| 🟩 `Roots` | **9** | **4** |
| 🟩 `Scene` | **10** | **9** |
| 🟩 `Player` | **11** | **7** |
| 🟩 `Quest` | **11** | **5** |
| 🟩 `Enemy` | **12** | **4** |
| 🟩 `Save` | **12** | **1** |

## 詳細

### 🟦Craft 🔨 

↑ 🟦 `Inventory` **1** — Inventory **1**  

↓ 🟩 `Craft` **11** — Recipe **7**, Ingredient **4**  

### 🟦Day ☀️ 

↓ 🟦 `Quest` **2** — DayPlan **2**  
↓ 🟩 `Day` **4** — DayClock **2**, DayCycle **2**  
↓ 🟩 `Quest` **1** — DayPlan **1**  

### 🟦Enemy 👾 

↑ 🟦 `Player` **6** — Heading **6**  
↑ 🟦 `Roots` **4** — Root **4**  

↓ 🟦 `Roots` **4** — IRandom **4**  
↓ 🟩 `Enemy` **30** — EnemyAim **10**, AimRule **4**, SpawnRule **4**, ActionRule **3**, DropChance **3**, EnemyAction **3**, DropRule **1**, FallRule **1**, IRandom **1**  

### 🟦Inventory 🎒 

↓ 🟦 `Craft` **1** — Inventory **1**  
↓ 🟦 `Player` **2** — ItemDefinition **1**, ItemUse **1**  
↓ 🟩 `Craft` **6** — Inventory **3**, ItemDefinition **3**  
↓ 🟩 `Inventory` **38** — ItemDefinition **10**, ItemSlot **8**, Inventory **7**, ItemUse **6**, AddOutcome **2**, ItemCatalog **2**, AddResult **1**, RemoveOutcome **1**, RemoveResult **1**  
↓ 🟩 `Player` **1** — ItemDefinition **1**  
↓ 🟩 `Quest` **3** — ItemCatalog **2**, ItemDefinition **1**  
↓ 🟩 `Save` **3** — ItemSlot **3**  

### 🟦OutField 🌲 

↓ 🟩 `Enemy` **3** — FieldBounds **3**  
↓ 🟩 `OutField` **7** — FieldBounds **3**, FenceDurability **2**, FenceRule **1**, FenceSpot **1**  
↓ 🟩 `Player` **2** — FieldBounds **1**, Nudge **1**  
↓ 🟩 `Scene` **1** — FieldBounds **1**  

### 🟦Player 🚶 

↑ 🟦 `Inventory` **2** — ItemDefinition **1**, ItemUse **1**  

↓ 🟦 `Enemy` **6** — Heading **6**  
↓ 🟩 `Enemy` **8** — Health **6**, Heading **2**  
↓ 🟩 `Player` **41** — RollRule **9**, AttackTiming **8**, AttackKind **5**, AttackMove **5**, AttackRule **4**, Equipment **2**, Health **2**, Invulnerability **2**, Heading **1**, KnockbackRule **1**, MoveRule **1**, Push **1**  
↓ 🟩 `Staging` **24** — AttackMove **24**  

### 🟦Quest 📜 

↑ 🟦 `Day` **2** — DayPlan **2**  
↑ 🟦 `Roots` **5** — Root **5**  

↓ 🟩 `Enemy` **1** — QuestBook **1**  
↓ 🟩 `Quest` **85** — Quest **35**, QuestKind **15**, Reward **8**, OrderResult **6**, QuestObjective **5**, QuestOutcome **5**, RewardKind **4**, QuestBook **3**, QuestRewards **3**, QuestRegistry **1**  
↓ 🟩 `Save` **9** — Quest **5**, Reward **4**  

### 🟦Roots 🌳 

↑ 🟦 `Enemy` **4** — IRandom **4**  

↓ 🟦 `Enemy` **4** — Root **4**  
↓ 🟦 `Quest` **5** — Root **5**  
↓ 🟦 `Scene` **4** — Root **4**  
↓ 🟩 `Enemy` **12** — Raider **10**, Root **2**  
↓ 🟩 `Quest` **8** — Root **8**  
↓ 🟩 `Roots` **26** — Root **17**, AccumulationLevel **5**, BaseRaid **2**, RootRegistry **2**  
↓ 🟩 `Save` **11** — Raider **6**, Root **5**  
↓ 🟩 `Scene` **3** — Root **3**  

### 🟦Scene 🚪 

↑ 🟦 `Roots` **4** — Root **4**  

↓ 🟩 `Enemy` **1** — ExitKind **1**  
↓ 🟩 `Player` **1** — Arrival **1**  
↓ 🟩 `Quest` **2** — AreaKind **2**  
↓ 🟩 `Roots` **4** — TravelRule **4**  
↓ 🟩 `Save` **8** — Placement **7**, PlacementLedger **1**  
↓ 🟩 `Scene` **46** — TravelRule **9**, ExitKind **8**, Arrival **7**, Placement **7**, Route **7**, AreaKind **4**, PlacementLedger **2**, Whereabouts **2**  

### 🟦Wallet 💰 

↓ 🟩 `Wallet` **2** — Wallet **2**  

### 🟩Craft 🔨 

↑ 🟦 `Craft` **11** — Recipe **7**, Ingredient **4**  
↑ 🟦 `Inventory` **6** — Inventory **3**, ItemDefinition **3**  
↑ 🟩 `Inventory` **3** — PlayerInventory **2**, ItemBook **1**  
↑ 🟩 `Player` **1** — PlayerMovement **1**  
↑ 🟩 `Scene` **1** — AreaTravel **1**  
↑ 🟩 `Staging` **1** — MiddleText **1**  
↑ 🟩 `Wallet` **1** — Purse **1**  

### 🟩Day ☀️ 

↑ 🟦 `Day` **4** — DayClock **2**, DayCycle **2**  

↓ 🟩 `Quest` **1** — Sun **1**  
↓ 🟩 `Roots` **1** — Sun **1**  
↓ 🟩 `Save` **1** — Sun **1**  
↓ 🟩 `Staging` **15** — Sun **12**, Daylight **3**  

### 🟩Enemy 👾 

↑ 🟦 `Enemy` **30** — EnemyAim **10**, AimRule **4**, SpawnRule **4**, ActionRule **3**, DropChance **3**, EnemyAction **3**, DropRule **1**, FallRule **1**, IRandom **1**  
↑ 🟦 `OutField` **3** — FieldBounds **3**  
↑ 🟦 `Player` **8** — Health **6**, Heading **2**  
↑ 🟦 `Quest` **1** — QuestBook **1**  
↑ 🟦 `Roots` **12** — Raider **10**, Root **2**  
↑ 🟦 `Scene` **1** — ExitKind **1**  
↑ 🟩 `OutField` **6** — Exposition **4**, Fence **2**  
↑ 🟩 `Player` **4** — PlayerHealth **4**  
↑ 🟩 `Quest` **2** — QuestManager **2**  
↑ 🟩 `Roots` **2** — RootsManager **2**  
↑ 🟩 `Scene` **8** — Area **6**, AreaTravel **2**  
↑ 🟩 `Staging` **4** — AlertMark **2**, WorldGauge **2**  

↓ 🟩 `Player` **5** — IDamageable **2**, RootCore **2**, SpawnerCandidate **1**  
↓ 🟩 `Roots` **2** — UnityRandom **2**  
↓ 🟩 `Save` **5** — IDamageable **2**, RaidGate **2**, RootCore **1**  
↓ 🟩 `Scene` **19** — SpawnerCandidate **9**, RootCore **4**, EnemyContext **2**, RaidGate **2**, Slime **2**  

### 🟩Inventory 🎒 

↑ 🟦 `Inventory` **38** — ItemDefinition **10**, ItemSlot **8**, Inventory **7**, ItemUse **6**, AddOutcome **2**, ItemCatalog **2**, AddResult **1**, RemoveOutcome **1**, RemoveResult **1**  
↑ 🟩 `OutField` **3** — DroppedItem **1**, Fence **1**, FenceSite **1**  
↑ 🟩 `Player` **7** — PlayerEquipment **3**, PlayerMovement **3**, PlayerHealth **1**  
↑ 🟩 `Scene` **2** — AreaTravel **1**, PlacedObjects **1**  
↑ 🟩 `Staging` **3** — KeyHint **1**, MiddleText **1**, SoundEffects **1**  

↓ 🟩 `Craft` **3** — PlayerInventory **2**, ItemBook **1**  
↓ 🟩 `Player` **2** — PlayerInventory **2**  
↓ 🟩 `Quest` **4** — ItemBook **3**, PlayerInventory **1**  
↓ 🟩 `Save` **1** — PlayerInventory **1**  

### 🟩OutField 🌲 

↑ 🟦 `OutField` **7** — FieldBounds **3**, FenceDurability **2**, FenceRule **1**, FenceSpot **1**  
↑ 🟩 `Player` **2** — PlayerReach **2**  
↑ 🟩 `Scene` **2** — Area **2**  
↑ 🟩 `Staging` **4** — SoundEffects **2**, WorldGauge **2**  

↓ 🟩 `Enemy` **6** — Exposition **4**, Fence **2**  
↓ 🟩 `Inventory` **3** — DroppedItem **1**, Fence **1**, FenceSite **1**  
↓ 🟩 `Player` **13** — Fence **11**, Exposition **2**  
↓ 🟩 `Scene` **5** — Exposition **3**, Fence **2**  
↓ 🟩 `Staging` **1** — Exposition **1**  

### 🟩Player 🚶 

↑ 🟦 `Inventory` **1** — ItemDefinition **1**  
↑ 🟦 `OutField` **2** — FieldBounds **1**, Nudge **1**  
↑ 🟦 `Player` **41** — RollRule **9**, AttackTiming **8**, AttackKind **5**, AttackMove **5**, AttackRule **4**, Equipment **2**, Health **2**, Invulnerability **2**, Heading **1**, KnockbackRule **1**, MoveRule **1**, Push **1**  
↑ 🟦 `Scene` **1** — Arrival **1**  
↑ 🟩 `Enemy` **5** — IDamageable **2**, RootCore **2**, SpawnerCandidate **1**  
↑ 🟩 `Inventory` **2** — PlayerInventory **2**  
↑ 🟩 `OutField` **13** — Fence **11**, Exposition **2**  
↑ 🟩 `Quest` **1** — QuestManager **1**  
↑ 🟩 `Scene` **7** — AreaTravel **4**, Exit **2**, Area **1**  
↑ 🟩 `Staging` **26** — HitStop **6**, MainCamera **5**, KeyHint **4**, SoundEffects **4**, AttackGuide **2**, RollStaging **2**, Allmaity **1**, BigText **1**, DamageNumber **1**  
↑ 🟩 `Wallet` **1** — Purse **1**  

↓ 🟩 `Craft` **1** — PlayerMovement **1**  
↓ 🟩 `Enemy` **4** — PlayerHealth **4**  
↓ 🟩 `Inventory` **7** — PlayerEquipment **3**, PlayerMovement **3**, PlayerHealth **1**  
↓ 🟩 `OutField` **2** — PlayerReach **2**  
↓ 🟩 `Roots` **2** — PlayerMovement **2**  
↓ 🟩 `Save` **2** — PlayerEquipment **1**, PlayerHealth **1**  
↓ 🟩 `Scene` **1** — PlayerHealth **1**  

### 🟩Quest 📜 

↑ 🟦 `Day` **1** — DayPlan **1**  
↑ 🟦 `Inventory` **3** — ItemCatalog **2**, ItemDefinition **1**  
↑ 🟦 `Quest` **85** — Quest **35**, QuestKind **15**, Reward **8**, OrderResult **6**, QuestObjective **5**, QuestOutcome **5**, RewardKind **4**, QuestBook **3**, QuestRewards **3**, QuestRegistry **1**  
↑ 🟦 `Roots` **8** — Root **8**  
↑ 🟦 `Scene` **2** — AreaKind **2**  
↑ 🟩 `Day` **1** — Sun **1**  
↑ 🟩 `Inventory` **4** — ItemBook **3**, PlayerInventory **1**  
↑ 🟩 `Roots` **4** — RootsManager **4**  
↑ 🟩 `Scene` **3** — AreaTravel **3**  
↑ 🟩 `Staging` **5** — MiddleText **4**, BigText **1**  
↑ 🟩 `Wallet` **1** — Purse **1**  

↓ 🟩 `Enemy` **2** — QuestManager **2**  
↓ 🟩 `Player` **1** — QuestManager **1**  
↓ 🟩 `Roots` **2** — QuestBoard **1**, QuestManager **1**  
↓ 🟩 `Save` **1** — QuestManager **1**  
↓ 🟩 `Scene` **3** — QuestManager **2**, RewardUI **1**  

### 🟩Roots 🌳 

↑ 🟦 `Roots` **26** — Root **17**, AccumulationLevel **5**, BaseRaid **2**, RootRegistry **2**  
↑ 🟦 `Scene` **4** — TravelRule **4**  
↑ 🟩 `Day` **1** — Sun **1**  
↑ 🟩 `Enemy` **2** — UnityRandom **2**  
↑ 🟩 `Player` **2** — PlayerMovement **2**  
↑ 🟩 `Quest` **2** — QuestBoard **1**, QuestManager **1**  
↑ 🟩 `Scene` **3** — AreaTravel **3**  
↑ 🟩 `Staging` **8** — CloseManager **5**, MiddleText **2**, SoundEffects **1**  
↑ 🟩 `Wallet` **1** — Purse **1**  

↓ 🟩 `Enemy` **2** — RootsManager **2**  
↓ 🟩 `Quest` **4** — RootsManager **4**  
↓ 🟩 `Save` **1** — RootsManager **1**  
↓ 🟩 `Scene` **1** — RootsManager **1**  

### 🟩Save 💾 

↑ 🟦 `Inventory` **3** — ItemSlot **3**  
↑ 🟦 `Quest` **9** — Quest **5**, Reward **4**  
↑ 🟦 `Roots` **11** — Raider **6**, Root **5**  
↑ 🟦 `Scene` **8** — Placement **7**, PlacementLedger **1**  
↑ 🟩 `Day` **1** — Sun **1**  
↑ 🟩 `Enemy` **5** — IDamageable **2**, RaidGate **2**, RootCore **1**  
↑ 🟩 `Inventory` **1** — PlayerInventory **1**  
↑ 🟩 `Player` **2** — PlayerEquipment **1**, PlayerHealth **1**  
↑ 🟩 `Quest` **1** — QuestManager **1**  
↑ 🟩 `Roots` **1** — RootsManager **1**  
↑ 🟩 `Scene` **9** — Area **6**, PlacedObjects **2**, AreaTravel **1**  
↑ 🟩 `Wallet` **1** — Purse **1**  

↓ 🟩 `Scene` **17** — SaveSlots **11**, SaveSystem **4**, SaveData **2**  

### 🟩Scene 🚪 

↑ 🟦 `OutField` **1** — FieldBounds **1**  
↑ 🟦 `Roots` **3** — Root **3**  
↑ 🟦 `Scene` **46** — TravelRule **9**, ExitKind **8**, Arrival **7**, Placement **7**, Route **7**, AreaKind **4**, PlacementLedger **2**, Whereabouts **2**  
↑ 🟩 `Enemy` **19** — SpawnerCandidate **9**, RootCore **4**, EnemyContext **2**, RaidGate **2**, Slime **2**  
↑ 🟩 `OutField` **5** — Exposition **3**, Fence **2**  
↑ 🟩 `Player` **1** — PlayerHealth **1**  
↑ 🟩 `Quest` **3** — QuestManager **2**, RewardUI **1**  
↑ 🟩 `Roots` **1** — RootsManager **1**  
↑ 🟩 `Save` **17** — SaveSlots **11**, SaveSystem **4**, SaveData **2**  
↑ 🟩 `Staging` **8** — PressFeedback **4**, Music **2**, KeyHint **1**, UiStage **1**  

↓ 🟩 `Craft` **1** — AreaTravel **1**  
↓ 🟩 `Enemy` **8** — Area **6**, AreaTravel **2**  
↓ 🟩 `Inventory` **2** — AreaTravel **1**, PlacedObjects **1**  
↓ 🟩 `OutField` **2** — Area **2**  
↓ 🟩 `Player` **7** — AreaTravel **4**, Exit **2**, Area **1**  
↓ 🟩 `Quest` **3** — AreaTravel **3**  
↓ 🟩 `Roots` **3** — AreaTravel **3**  
↓ 🟩 `Save` **9** — Area **6**, PlacedObjects **2**, AreaTravel **1**  
↓ 🟩 `Staging` **11** — Area **10**, AreaTravel **1**  

### 🟩Staging 🎥 

↑ 🟦 `Player` **24** — AttackMove **24**  
↑ 🟩 `Day` **15** — Sun **12**, Daylight **3**  
↑ 🟩 `OutField` **1** — Exposition **1**  
↑ 🟩 `Scene` **11** — Area **10**, AreaTravel **1**  

↓ 🟩 `Craft` **1** — MiddleText **1**  
↓ 🟩 `Enemy` **4** — AlertMark **2**, WorldGauge **2**  
↓ 🟩 `Inventory` **3** — KeyHint **1**, MiddleText **1**, SoundEffects **1**  
↓ 🟩 `OutField` **4** — SoundEffects **2**, WorldGauge **2**  
↓ 🟩 `Player` **26** — HitStop **6**, MainCamera **5**, KeyHint **4**, SoundEffects **4**, AttackGuide **2**, RollStaging **2**, Allmaity **1**, BigText **1**, DamageNumber **1**  
↓ 🟩 `Quest` **5** — MiddleText **4**, BigText **1**  
↓ 🟩 `Roots` **8** — CloseManager **5**, MiddleText **2**, SoundEffects **1**  
↓ 🟩 `Scene` **8** — PressFeedback **4**, Music **2**, KeyHint **1**, UiStage **1**  

### 🟩Wallet 💰 

↑ 🟦 `Wallet` **2** — Wallet **2**  

↓ 🟩 `Craft` **1** — Purse **1**  
↓ 🟩 `Player` **1** — Purse **1**  
↓ 🟩 `Quest` **1** — Purse **1**  
↓ 🟩 `Roots` **1** — Purse **1**  
↓ 🟩 `Save` **1** — Purse **1**  
