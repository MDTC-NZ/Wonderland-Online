# Wonderland Online - Complete Implementation Guide

## Part 1: Quest System Implementation

### Current State
- Quest.cs: Has basic structure but needs enhancement
- QuestNpc.cs: Abstract base, needs implementation
- Player.cs: Needs quest manager integration
- Database: unified.db exists with test users

### Step 1: Create QuestManager.cs

Location: `wlo.pserver.core/Game/PlayerRelated/QuestManager.cs`

**Responsibilities:**
- Track all quests for a player
- Load/save quest progress to database
- Handle quest acceptance, progress update, completion
- Distribute rewards
- Track repeatable quest completion count

**Key Methods:**
```
AcceptQuest(questID) - Starts a quest
UpdateQuestProgress(questID, amount) - Increases progress
CompleteQuest(questID) - Finishes quest, gives rewards
GetPlayerQuests() - Returns all quests for player
CanAcceptQuest(questID) - Checks requirements
ResetRepeatableQuest(questID) - For repeatable quests
```

### Step 2: Create Quest Data Loader

Location: `Src/DataFiles/QuestDataLoader.cs`

**Responsibilities:**
- Parse quest data from gamedata/Quest.dat (binary format)
- Build quest definitions into memory cache
- Provide quest lookup by ID
- Handle quest NPCs and dialogue

**Data Structure:**
```
QuestDefinition:
  - QuestID
  - Name, Description, Location
  - RequiredLevel, PrerequisiteQuests
  - ObjectiveType, ObjectiveCount, TargetID
  - RewardEXP, RewardGold
  - ItemRewards (List<QuestReward>)
  - CharacterRewards (if any)
  - AmityRewards (Dictionary<CharName, Amount>)
  - IsRepeatable, TimeLimit
  - StartNpcID, EndNpcID
```

### Step 3: Enhance QuestNpc.cs

Location: `wlo.pserver.core/Game/Maps/Code/QuestNpc.cs`

**Current State:** Abstract base with virtual methods

**Enhancements:**
```csharp
public class QuestNpc : InteractableObjects
{
    private List<int> availableQuests;
    private Dictionary<int, Quest> playerActiveQuests;
    
    public override void Interact(Player player)
    {
        // Show quest dialogue
        // Allow quest acceptance
        // Show rewards for completed quests
        // Handle quest reward distribution
    }
    
    public void AcceptQuest(Player player, int questID)
    {
        // Validate quest requirements
        // Add quest to player
        // Update NPC dialogue
    }
    
    public void CompleteQuest(Player player, int questID)
    {
        // Verify quest completion
        // Distribute rewards
        // Mark as completed
    }
}
```

### Step 4: Database Schema

Create migration script: `Migrations/004_AddQuestTracking.sql`

```sql
-- Quest Log Table
CREATE TABLE quest_log (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    player_id INTEGER NOT NULL,
    quest_id INTEGER NOT NULL,
    state INTEGER NOT NULL DEFAULT 0, -- NotStarted=0, InProgress=1, Completed=2, Rewarded=3
    progress INTEGER NOT NULL DEFAULT 0,
    completed_count INTEGER DEFAULT 0, -- For repeatable quests
    accepted_date TEXT,
    completed_date TEXT,
    FOREIGN KEY(player_id) REFERENCES users(user_id)
);

-- Quest Rewards Tracking
CREATE TABLE quest_rewards (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    player_id INTEGER NOT NULL,
    quest_id INTEGER NOT NULL,
    reward_type TEXT NOT NULL, -- EXP|Gold|Item|Skill|Character|Amity
    reward_value TEXT, -- JSON for complex rewards
    earned_date TEXT,
    FOREIGN KEY(player_id) REFERENCES users(user_id)
);

-- Quest Progress Details
CREATE TABLE quest_progress (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    player_id INTEGER NOT NULL,
    quest_id INTEGER NOT NULL,
    objective_id INTEGER,
    current_value INTEGER,
    target_value INTEGER,
    FOREIGN KEY(player_id) REFERENCES users(user_id)
);
```

### Step 5: Integrate with Player.cs

**Add to Player class:**
```csharp
public class Player
{
    private QuestManager questManager;
    
    public QuestManager GetQuestManager() => questManager;
    
    public override void OnLogin()
    {
        base.OnLogin();
        // Load quests from database
        questManager.LoadPlayerQuests(this.PlayerID);
    }
    
    public override void OnLogout()
    {
        // Save quest progress
        questManager.SavePlayerQuests(this.PlayerID);
        base.OnLogout();
    }
}
```

---

## Part 2: Shop/Item Mall System

### Current State
- ShopKeeper.cs: Empty stub
- No shop inventory system
- Item Mall not implemented

### Step 1: Create Shop System Classes

**Shop.cs:**
```csharp
public class Shop
{
    public int ShopID { get; set; }
    public string ShopName { get; set; }
    public int NpcID { get; set; }
    public List<ShopItem> Inventory { get; set; }
}

public class ShopItem
{
    public int ItemID { get; set; }
    public int Quantity { get; set; }
    public int Price { get; set; }
    public int Rank { get; set; }
}
```

**ShoppingCart.cs (enhanced):**
```csharp
public class ShoppingCart
{
    public Dictionary<int, int> Items { get; set; } // ItemID, Quantity
    public int TotalPrice { get; set; }
    
    public void AddItem(int itemID, int quantity, int unitPrice)
    {
        // Add to cart with price calculation
    }
    
    public void RemoveItem(int itemID, int quantity)
    {
        // Remove from cart
    }
    
    public void Checkout(Player player, Shop shop)
    {
        // Verify player has gold
        // Deduct gold from player
        // Add items to inventory
        // Log transaction
    }
}
```

### Step 2: Implement ShopKeeper.cs

```csharp
public class ShopKeeper : InteractableObjects
{
    private Shop shopData;
    private Dictionary<int, ShopItem> inventory;
    
    public override void Interact(Player player)
    {
        // Display shop dialogue
        // Show available items
        // Allow purchase/sell interactions
    }
    
    public bool CanBuy(Player player, int itemID, int quantity)
    {
        // Check player gold
        // Check item availability
        // Check player inventory space
        return true;
    }
    
    public void CompletePurchase(Player player, int itemID, int quantity)
    {
        // Deduct gold
        // Add items to inventory
        // Log transaction
    }
}
```

### Step 3: Item Mall System

**ItemMall.cs:**
```csharp
public class ItemMall
{
    private List<ItemMallCategory> categories;
    private List<ItemMallItem> items;
    
    public List<ItemMallCategory> GetCategories() => categories;
    public List<ItemMallItem> GetItemsByCategory(int categoryID) => items.Where(x => x.CategoryID == categoryID).ToList();
}

public class ItemMallItem
{
    public int ItemID { get; set; }
    public string Name { get; set; }
    public int Price { get; set; }
    public int CategoryID { get; set; }
    public int Quantity { get; set; }
}
```

### Step 4: Shop Database Schema

```sql
CREATE TABLE shops (
    shop_id INTEGER PRIMARY KEY,
    shop_name TEXT NOT NULL,
    npc_id INTEGER NOT NULL,
    location TEXT
);

CREATE TABLE shop_inventory (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    shop_id INTEGER NOT NULL,
    item_id INTEGER NOT NULL,
    quantity INTEGER,
    price INTEGER,
    rank INTEGER,
    FOREIGN KEY(shop_id) REFERENCES shops(shop_id)
);

CREATE TABLE shop_transactions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    player_id INTEGER NOT NULL,
    shop_id INTEGER NOT NULL,
    item_id INTEGER NOT NULL,
    quantity INTEGER,
    total_price INTEGER,
    transaction_date TEXT,
    FOREIGN KEY(player_id) REFERENCES users(user_id)
);
```

### Step 5: Shop Data Loader

**ShopDataLoader.cs:**
- Parse shop data from gamedata/NPC.dat
- Load shop inventory from wlohub.com/shops reference
- Build shop definitions into cache
- Provide shop lookup by location/NPC

---

## Part 3: Spawn System

### Current Issue
- Characters select Character Select instead of spawning on ship
- No initial map/position configuration

### Step 1: Create SpawnManager.cs

```csharp
public class SpawnManager
{
    private Dictionary<string, SpawnPoint> spawnPoints;
    
    public SpawnPoint GetInitialSpawn(string characterClass)
    {
        // Return spawn point based on character class
        // Default: Ship map for all classes
    }
    
    public SpawnPoint GetRespawnPoint(Player player)
    {
        // Return nearest town/inn
        // Or last saved spawn point
    }
}

public class SpawnPoint
{
    public int MapID { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public string MapName { get; set; }
}
```

### Step 2: Update Map System

**In MapManager.cs:**
```csharp
public void LoadMap(int mapID)
{
    // Load map data
    // Initialize spawn points
    // Initialize NPCs
    // Initialize shops
    // Initialize quest givers
}
```

### Step 3: Fix Character Creation Flow

**In character creation completion:**
```csharp
public void FinalizeCharacter(Player player)
{
    // Set initial map to "Ship" map
    SpawnPoint spawn = SpawnManager.GetInitialSpawn(player.Class);
    player.CurrentMapID = spawn.MapID;
    player.Position = new Vector2(spawn.X, spawn.Y);
    
    // Skip character select, auto-spawn
    player.EnterWorld();
}
```

---

## Part 4: Data File Investigation

### Files to Analyze
1. **Item.dat** - Item properties, bases, attributes
2. **NPC.dat** - NPC locations, shops, dialogues
3. **Quest.dat** - Quest definitions
4. **Talk.dat** - NPC dialogue text
5. **Skill.dat** - Skill definitions
6. **Spawn.dat** or Map files - Spawn points

### Binary Parser Strategy
1. Use hex editor to examine file headers
2. Identify record structure and size
3. Build parser class with BinaryReader
4. Test with known NPCs/items
5. Create data loader class

---

## Part 5: Testing Strategy

### Unit Tests
1. Quest acceptance/progress/completion
2. Shop purchase transactions
3. Spawn point loading
4. Reward distribution

### Integration Tests
1. Full quest flow (accept→progress→complete→rewards)
2. Shop purchase flow
3. Character creation to spawn
4. Database persistence

### Gameplay Tests
1. Accept quest from NPC
2. Complete quest objective
3. Receive quest rewards
4. Purchase items from shop
5. New character spawns on correct map

---

## Implementation Order

### Priority 1: Quest System (8-10 hours)
1. Create QuestManager.cs
2. Enhance Quest.cs
3. Implement QuestNpc.cs
4. Create database schema
5. Test quest flow

### Priority 2: Spawn System (2-3 hours)
1. Create SpawnManager.cs
2. Fix character creation flow
3. Test character spawning

### Priority 3: Shop System (6-8 hours)
1. Implement ShopKeeper.cs
2. Create Shop classes
3. Create database schema
4. Test purchases

### Priority 4: Integration & Polish (4-6 hours)
1. End-to-end testing
2. Performance optimization
3. Database verification
4. UI/Feedback improvements

---

## Data Files from WLO Databases

### Quests (from wlohub.com/quests)
**Sample Data Structure:**
- ID: 1-5000+
- Types: Main (0), Optional (1), Recruitment (2), Amity (3)
- Rewards: EXP (100-50000), Items (various IDs), Characters
- Prerequisites: Level requirements, quest chains

### Items (6,800+ items)
**Properties:**
- ID: 1-6800+
- Rank: 1-50
- Bases: Wood, Iron, Gold, Leaf, etc.
- Attributes: ATK, DEF, MATK, MDEF, SPD, HP, SP

### Shops (Oslya, Kelan, Chang'an, Holy Village, Persia)
**Item Examples:**
- Bread (10g) - Kelan Village
- Iron Sword (70g) - Holy Village
- Pure Iron Saber (510g) - Chang'an
- Materials 30-130g range

### Monsters (200+ types)
**Properties:**
- Level: 1-150+
- HP, SP, STR, CON, INT, WIS, AGI, SPD
- Element: Fire, Water, Wind, Earth, None
- Drops: Materials, quest items
- Locations: Multiple maps per monster

### Locations (40+ maps)
**Major Towns:**
- Welling Village (starter area)
- Kelan Village (beginner quests)
- Holy Village (low-level area)
- Persia (early quests)
- China/Egypt/etc. (end-game)

---

## Key Implementation Notes

1. **Database-First Approach**: All persistent data saved immediately
2. **Modular Design**: Each system independent, can test separately
3. **Data-Driven**: Quest/shop data loaded from files, not hardcoded
4. **Scalable**: Can handle 6800+ items, 5000+ quests
5. **Reference WLO Hub**: Use wlohub.com databases as source of truth

---

## Success Metrics

- ✅ Players can accept/complete quests
- ✅ Quest rewards distributed correctly
- ✅ Shop items purchaseable with gold
- ✅ Characters spawn on correct map
- ✅ All data persists across login/logout
- ✅ 100+ quests implemented
- ✅ 500+ shop items available
- ✅ Zero data loss on crash
