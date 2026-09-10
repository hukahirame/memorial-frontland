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
| 🟦 `Domain` | **0** | **9** |
| 🟩 `Inventory` | **0** | **2** |
| 🟩 `Staging` | **0** | **6** |
| 🟩 `Craft` | **1** | **1** |
| 🟩 `OutField` | **1** | **0** |
| 🟨 `Day` | **4** | **1** |
| 🟨 `Roots` | **4** | **5** |
| 🟨 `Save` | **4** | **2** |
| 🟨 `Inventory` | **5** | **4** |
| 🟨 `Quest` | **5** | **5** |
| 🟨 `Craft` | **6** | **0** |
| 🟨 `Player` | **6** | **4** |
| 🟨 `Scene` | **6** | **7** |
| 🟨 `Enemy` | **7** | **3** |

## 詳細

### 🟦Domain 💠 

↓ 🟩 `Craft` **4** — Ingredient **2**, Recipe **2**  
↓ 🟨 `Craft` **6** — Recipe **3**, Inventory **2**, ItemDefinition **1**  
↓ 🟨 `Day` **12** — Reward **3**, DayClock **2**, DayCycle **2**, DayPlan **2**, QuestKind **2**, Root **1**  
↓ 🟨 `Enemy` **2** — Root **2**  
↓ 🟨 `Inventory` **9** — ItemDefinition **3**, AddOutcome **2**, Inventory **2**, RemoveOutcome **2**  
↓ 🟨 `Player` **3** — Health **2**, Quest **1**  
↓ 🟨 `Quest` **46** — QuestKind **19**, Quest **14**, Reward **8**, QuestRegistry **2**, Root **2**, QuestId **1**  
↓ 🟨 `Roots` **13** — AccumulationLevel **8**, Root **3**, RootRegistry **2**  
↓ 🟨 `Scene` **5** — ItemCatalog **2**, Wallet **2**, Root **1**  

### 🟩Craft 🔨 

↑ 🟦 `Domain` **4** — Ingredient **2**, Recipe **2**  

↓ 🟨 `Craft` **1** — RecipeDefinition **1**  

### 🟩Inventory 🎒 

↓ 🟩 `OutField` **2** — IItemReceiver **2**  
↓ 🟨 `Inventory` **1** — IItemReceiver **1**  

### 🟩OutField 🌲 

↑ 🟩 `Inventory` **2** — IItemReceiver **2**  

### 🟩Staging 🎥 

↓ 🟨 `Craft` **2** — TempAudio **2**  
↓ 🟨 `Day` **1** — MiddleText **1**  
↓ 🟨 `Enemy` **1** — MiddleText **1**  
↓ 🟨 `Inventory` **3** — TempAudio **3**  
↓ 🟨 `Player` **6** — TempAudio **4**, BigText **1**, MiddleText **1**  
↓ 🟨 `Quest` **5** — MiddleText **3**, BigText **2**  

### 🟨Craft 🔨 

↑ 🟦 `Domain` **6** — Recipe **3**, Inventory **2**, ItemDefinition **1**  
↑ 🟩 `Craft` **1** — RecipeDefinition **1**  
↑ 🟩 `Staging` **2** — TempAudio **2**  
↑ 🟨 `Inventory` **1** — PlayerInventory **1**  
↑ 🟨 `Roots` **1** — RootsManager **1**  
↑ 🟨 `Scene` **1** — GameManager **1**  

### 🟨Day ☀️ 

↑ 🟦 `Domain` **12** — Reward **3**, DayClock **2**, DayCycle **2**, DayPlan **2**, QuestKind **2**, Root **1**  
↑ 🟩 `Staging` **1** — MiddleText **1**  
↑ 🟨 `Quest` **3** — QuestManager **3**  
↑ 🟨 `Roots` **2** — RootsManager **2**  

↓ 🟨 `Save` **2** — Sun2 **2**  

### 🟨Enemy 👾 

↑ 🟦 `Domain` **2** — Root **2**  
↑ 🟩 `Staging` **1** — MiddleText **1**  
↑ 🟨 `Player` **4** — Player2 **4**  
↑ 🟨 `Quest` **3** — QuestManager **3**  
↑ 🟨 `Roots` **2** — RootsManager **2**  
↑ 🟨 `Save` **2** — SaveData **1**, SaveSystem **1**  
↑ 🟨 `Scene` **3** — GameManager **2**, SceneStarter **1**  

↓ 🟨 `Player` **2** — OF_Spawner **1**, SpawnerCandidate **1**  
↓ 🟨 `Roots` **2** — MS_Spawner **1**, OF_Spawner **1**  
↓ 🟨 `Scene` **1** — OF_Spawner **1**  

### 🟨Inventory 🎒 

↑ 🟦 `Domain` **9** — ItemDefinition **3**, AddOutcome **2**, Inventory **2**, RemoveOutcome **2**  
↑ 🟩 `Inventory` **1** — IItemReceiver **1**  
↑ 🟩 `Staging` **3** — TempAudio **3**  
↑ 🟨 `Player` **6** — Player2 **4**, Weapon **2**  
↑ 🟨 `Scene` **3** — GameManager **3**  

↓ 🟨 `Craft` **1** — PlayerInventory **1**  
↓ 🟨 `Player` **1** — WeaponBox **1**  
↓ 🟨 `Quest` **1** — PlayerInventory **1**  
↓ 🟨 `Save` **4** — PlayerInventory **4**  

### 🟨Player 🚶 

↑ 🟦 `Domain` **3** — Health **2**, Quest **1**  
↑ 🟩 `Staging` **6** — TempAudio **4**, BigText **1**, MiddleText **1**  
↑ 🟨 `Enemy` **2** — OF_Spawner **1**, SpawnerCandidate **1**  
↑ 🟨 `Inventory` **1** — WeaponBox **1**  
↑ 🟨 `Quest` **6** — QuestManager **4**, RewardUI **2**  
↑ 🟨 `Scene` **5** — GameManager **5**  

↓ 🟨 `Enemy` **4** — Player2 **4**  
↓ 🟨 `Inventory` **6** — Player2 **4**, Weapon **2**  
↓ 🟨 `Save` **3** — Player2 **3**  
↓ 🟨 `Scene` **5** — Player2 **5**  

### 🟨Quest 📜 

↑ 🟦 `Domain` **46** — QuestKind **19**, Quest **14**, Reward **8**, QuestRegistry **2**, Root **2**, QuestId **1**  
↑ 🟩 `Staging` **5** — MiddleText **3**, BigText **2**  
↑ 🟨 `Inventory` **1** — PlayerInventory **1**  
↑ 🟨 `Roots` **3** — RootsManager **3**  
↑ 🟨 `Scene` **5** — GameManager **5**  

↓ 🟨 `Day` **3** — QuestManager **3**  
↓ 🟨 `Enemy` **3** — QuestManager **3**  
↓ 🟨 `Player` **6** — QuestManager **4**, RewardUI **2**  
↓ 🟨 `Roots` **1** — QuestManager **1**  
↓ 🟨 `Scene` **3** — RewardUI **2**, QuestManager **1**  

### 🟨Roots 🌳 

↑ 🟦 `Domain` **13** — AccumulationLevel **8**, Root **3**, RootRegistry **2**  
↑ 🟨 `Enemy` **2** — MS_Spawner **1**, OF_Spawner **1**  
↑ 🟨 `Quest` **1** — QuestManager **1**  
↑ 🟨 `Scene` **1** — GameManager **1**  

↓ 🟨 `Craft` **1** — RootsManager **1**  
↓ 🟨 `Day` **2** — RootsManager **2**  
↓ 🟨 `Enemy` **2** — RootsManager **2**  
↓ 🟨 `Quest` **3** — RootsManager **3**  
↓ 🟨 `Scene` **2** — RootsManager **2**  

### 🟨Save 💾 

↑ 🟨 `Day` **2** — Sun2 **2**  
↑ 🟨 `Inventory` **4** — PlayerInventory **4**  
↑ 🟨 `Player` **3** — Player2 **3**  
↑ 🟨 `Scene` **5** — GameManager **5**  

↓ 🟨 `Enemy` **2** — SaveData **1**, SaveSystem **1**  
↓ 🟨 `Scene` **4** — SaveData **2**, SaveSystem **2**  

### 🟨Scene 🚪 

↑ 🟦 `Domain` **5** — ItemCatalog **2**, Wallet **2**, Root **1**  
↑ 🟨 `Enemy` **1** — OF_Spawner **1**  
↑ 🟨 `Player` **5** — Player2 **5**  
↑ 🟨 `Quest` **3** — RewardUI **2**, QuestManager **1**  
↑ 🟨 `Roots` **2** — RootsManager **2**  
↑ 🟨 `Save` **4** — SaveData **2**, SaveSystem **2**  

↓ 🟨 `Craft` **1** — GameManager **1**  
↓ 🟨 `Enemy` **3** — GameManager **2**, SceneStarter **1**  
↓ 🟨 `Inventory` **3** — GameManager **3**  
↓ 🟨 `Player` **5** — GameManager **5**  
↓ 🟨 `Quest` **5** — GameManager **5**  
↓ 🟨 `Roots` **1** — GameManager **1**  
↓ 🟨 `Save` **5** — GameManager **5**  
