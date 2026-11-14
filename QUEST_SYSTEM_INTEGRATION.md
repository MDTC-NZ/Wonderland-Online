# Quest System Integration Guide

## Overview
This document describes how to integrate the complete Quest System into the Player class and game server.

## Files Created

### 1. Core Quest System
- **Quest.cs** - Enhanced with full properties, rewards, and state management
- **QuestManager.cs** - Manages all player quests (active and completed)
- **QuestDataBase.cs** - Database persistence layer

### 2. Data Loading
- **QuestLoader.cs** - Loads quests from XML configuration
- **gamedata/quests.xml** - Sample quest configuration file
- **quest_schema.sql** - Database schema for quest storage

### 3. NPC Integration
- **QuestNpc.cs** - Enhanced abstract base for quest NPCs

## Integration Steps

### Step 1: Update Player Class

Add these fields to the Player class:
```csharp
private QuestManager m_questMgr;
private QuestDataBase m_questDB;

public QuestManager QuestMgr
{
    get { return m_questMgr; }
    set { m_questMgr = value; }
}
```

Add initialization in Player constructor:
```csharp
public Player(SocketClient src, DataFiles.PhxItemDat itemdat)
    : base(src.SendPacket, itemdat)
{
    // ... existing code ...
    
    m_questMgr = new QuestManager(this);
    m_questDB = new QuestDataBase();
}
```

### Step 2: Add Helper Methods to Player

These methods are called by QuestManager.GrantQuestRewards():
```csharp
public void AddExp(int amount)
{
    // Implement experience gain logic
    // Update player level if needed
    Exp += amount;
}

public void AddMoney(int amount)
{
    // Implement money/currency logic
    Money += amount;
}

public void AddItemToInventory(int itemID, int quantity)
{
    // Use existing inventory system
    // m_inv.Add(itemID, quantity);
}

public void LearnSkill(int skillID)
{
    // Implement skill learning logic
}
```

### Step 3: Initialize Quest System at Server Startup

In your server initialization (cGlobal.cs or Main):
```csharp
// Load quest templates from XML
DataFiles.QuestLoader questLoader = new DataFiles.QuestLoader();
bool loaded = await questLoader.Load("gamedata/quests.xml");

if (loaded)
{
    log.Info($"Quest system loaded: {questLoader.Count} quests available");
}
else
{
    log.Error("Failed to load quest system");
}
```

### Step 4: Load/Save Player Quests

When a player logs in:
```csharp
public void LoadPlayerData()
{
    // ... existing code ...
    
    // Load quest data
    m_questDB.LoadPlayerActiveQuests(this);
    m_questDB.LoadPlayerCompletedQuests(this);
}
```

When a player logs out or at periodic intervals:
```csharp
public void SavePlayerData()
{
    // ... existing code ...
    
    // Save quest data
    m_questDB.SaveAllPlayerQuests(
        this.CharID, 
        m_questMgr.GetActiveQuests(), 
        m_questMgr.GetCompletedQuests()
    );
}
```

### Step 5: Implement Quest NPC Example

Create a concrete quest NPC class:
```csharp
public class VillageEldersNpc : QuestNpc
{
    public override int NpcID { get; } = 100;
    public override string Name { get { return "Village Elder"; } }

    public VillageEldersNpc()
    {
        // Define quests offered and received
        OfferedQuests.AddRange(new[] { 1, 3 });  // Quests 1 and 3
        ReceivesQuests.AddRange(new[] { 1, 3 }); // Can complete quests 1 and 3
    }

    public override void Interact(Player src)
    {
        // Show quest menu to player
        // This would send a packet with quest options
    }

    public override void Interact(Player src, byte? answer = null)
    {
        if (answer == 1) // Accept quest
        {
            AcceptQuestFromPlayer(src, 1);
        }
        else if (answer == 2) // Complete quest
        {
            CompleteQuestWithPlayer(src, 1);
        }
    }

    protected override void ShowQuestDialog(Player src, int questID, QuestDialogType type)
    {
        // Send packet to client showing quest dialog
        // Example packet structure:
        // [Packet Type] [NPC ID] [Quest ID] [Dialog Type] [Quest Details]
    }
}
```

### Step 6: Add Quest Commands for Testing

Add admin commands for quest management:
```csharp
// Accept a quest
public void AcceptQuest(int questID)
{
    if (m_questMgr.AcceptQuest(questID))
    {
        SendMessage($"Quest accepted: {questID}");
    }
}

// Complete a quest
public void CompleteQuest(int questID)
{
    Quest q = m_questMgr.GetActiveQuest(questID);
    if (q != null)
    {
        q.CurrentProgress = q.ObjectiveCount;
        if (m_questMgr.CompleteQuest(questID))
        {
            SendMessage($"Quest completed: {questID}");
        }
    }
}

// Show active quests
public void ListActiveQuests()
{
    var quests = m_questMgr.GetActiveQuests();
    SendMessage($"Active quests: {quests.Count}");
    foreach (var q in quests.Values)
    {
        SendMessage($"  [{q.QuestID}] {q.QuestName} - {q.CurrentProgress}/{q.ObjectiveCount}");
    }
}
```

## Database Setup

1. Apply the quest schema:
```bash
mysql -u root -p < quest_schema.sql
```

Or execute the queries in your database management tool.

## Usage Examples

### Accept a Quest
```csharp
player.QuestMgr.AcceptQuest(1);  // Accept quest ID 1
```

### Progress a Quest
```csharp
player.QuestMgr.UpdateQuestProgress(3, 1);  // Increment progress by 1 for quest 3
```

### Complete a Quest
```csharp
Quest q = player.QuestMgr.GetActiveQuest(3);
if (q != null && q.CurrentProgress >= q.ObjectiveCount)
{
    player.QuestMgr.CompleteQuest(3);  // Complete and grant rewards
}
```

### Check Quest Status
```csharp
bool completed = player.QuestMgr.IsQuestCompleted(1);
var activeQuests = player.QuestMgr.GetActiveQuests();
int count = player.QuestMgr.GetActiveQuestCount();
```

## Quest Objectives Handling

### Kill Mob Objective
When a mob dies:
```csharp
private void OnMobKilled(int mobID)
{
    var activeQuests = player.QuestMgr.GetActiveQuests();
    foreach (var quest in activeQuests.Values)
    {
        if (quest.ObjectiveType == "KillMob" && 
            quest.ObjectiveTargetID == mobID &&
            !quest.IsCompleted)
        {
            player.QuestMgr.UpdateQuestProgress(quest.QuestID, 1);
        }
    }
}
```

### Collect Item Objective
When an item is added to inventory:
```csharp
private void OnItemCollected(int itemID, int quantity)
{
    var activeQuests = player.QuestMgr.GetActiveQuests();
    foreach (var quest in activeQuests.Values)
    {
        if (quest.ObjectiveType == "CollectItem" && 
            quest.ObjectiveTargetID == itemID &&
            !quest.IsCompleted)
        {
            player.QuestMgr.UpdateQuestProgress(quest.QuestID, quantity);
        }
    }
}
```

### Deliver Item Objective
When a player talks to delivery NPC:
```csharp
private void OnDeliveryInteraction(Player player, QuestNpc npc)
{
    var activeQuests = player.QuestMgr.GetActiveQuests();
    foreach (var quest in activeQuests.Values)
    {
        if (quest.ObjectiveType == "DeliverItem" && 
            quest.EndNpcID == npc.NpcID &&
            !quest.IsCompleted)
        {
            player.QuestMgr.UpdateQuestProgress(quest.QuestID, 1);
            npc.CompleteQuestWithPlayer(player, quest.QuestID);
        }
    }
}
```

## Configuration

### Adding New Quests
1. Add quest to `gamedata/quests.xml`
2. Server will automatically load on startup
3. Quests are registered in QuestManager.m_questDatabase

### Quest Chain Prerequisites
Quests can have prerequisites using the `Prerequisites` section in XML:
```xml
<Prerequisites>
  <Quest>1</Quest>
  <Quest>2</Quest>
</Prerequisites>
```

### Repeatable Quests
Set `<IsRepeatable>true</IsRepeatable>` for repeatable quests.
Time limit in hours is set in `<TimeLimit>24</TimeLimit>`

## Thread Safety
- QuestManager uses locks for thread-safe access
- QuestLoader uses locks when loading quest data
- QuestDataBase uses existing database connection pool

## Performance Considerations
1. Quests are cached in memory after loading
2. Database queries use indexed columns (charID, questID, state)
3. Active quest count is typically small (< 20 per player)
4. Completed quests are only loaded once per session

## Future Enhancements
1. Quest log display system
2. Quest tracker UI
3. Quest markers on map
4. Dynamic quest generation
5. Quest difficulty scaling
6. Group quest support
7. Quest timer/expiration system
8. Quest statistics and achievements

## Troubleshooting

### Quests not loading
- Check `gamedata/quests.xml` exists and is valid XML
- Check database schema is applied
- Review log files for load errors

### Player quests not persisting
- Ensure SaveQuestsToDatabse() is called on logout
- Check database connection is working
- Verify quest tables have correct permissions

### Performance issues
- Monitor query performance on character_active_quests table
- Consider archiving old completed quests
- Use query explain to optimize indexes
