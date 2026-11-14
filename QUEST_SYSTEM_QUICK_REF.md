# Quest System Quick Reference

## Quest States
```csharp
public enum QuestState
{
    NotStarted = 0,    // Quest available but not accepted
    InProgress = 1,    // Quest accepted, objectives pending
    Completed = 2,     // Objectives complete, ready for turn-in
    Rewarded = 3,      // Rewards granted
    Failed = 4,        // Quest failed
    Abandoned = 5      // Player abandoned quest
}
```

## Objective Types
```csharp
public enum QuestObjectiveType
{
    KillMob = 0,           // Kill X monsters
    CollectItem = 1,       // Collect X items
    TalkToNpc = 2,         // Talk to NPC
    ReachLocation = 3,     // Reach a map location
    UseItem = 4,           // Use an item
    EquipItem = 5,         // Equip an item
    DeliverItem = 6        // Deliver item to NPC
}
```

## Core Methods

### Accept Quest
```csharp
player.QuestMgr.AcceptQuest(questID);
```

### Update Progress
```csharp
player.QuestMgr.UpdateQuestProgress(questID, amount);
```

### Complete Quest
```csharp
player.QuestMgr.CompleteQuest(questID);
```

### Abandon Quest
```csharp
player.QuestMgr.AbandonQuest(questID);
```

### Get Active Quest
```csharp
Quest quest = player.QuestMgr.GetActiveQuest(questID);
```

### Get All Active Quests
```csharp
Dictionary<int, Quest> quests = player.QuestMgr.GetActiveQuests();
```

### Check if Completed
```csharp
bool done = player.QuestMgr.IsQuestCompleted(questID);
```

## Quest Properties

```csharp
public class Quest
{
    public int QuestID { get; set; }
    public string QuestName { get; set; }
    public string Description { get; set; }
    public byte Level { get; set; }
    public int RequiredLevel { get; set; }
    
    public int ObjectiveCount { get; set; }
    public int CurrentProgress { get; set; }
    public string ObjectiveType { get; set; }
    public int ObjectiveTargetID { get; set; }
    
    public int StartNpcID { get; set; }
    public int EndNpcID { get; set; }
    
    public int RewardEXP { get; set; }
    public int RewardMoney { get; set; }
    public List<QuestReward> ItemRewards { get; set; }
    public List<int> RewardSkills { get; set; }
    
    public QuestState State { get; set; }
    public bool IsActive { get; }
    public bool IsCompleted { get; }
    public int ProgressPercentage { get; }
}
```

## Quest Rewards

```csharp
public class QuestReward
{
    public int ItemID { get; set; }
    public int Quantity { get; set; }
    public bool IsChoice { get; set; }  // Player chooses this reward
}
```

## Common Patterns

### Mob Kill Handler
```csharp
public void OnMobKilled(int mobID, Player player)
{
    var active = player.QuestMgr.GetActiveQuests();
    foreach (var q in active.Values)
    {
        if (q.ObjectiveType == "KillMob" && 
            q.ObjectiveTargetID == mobID)
        {
            player.QuestMgr.UpdateQuestProgress(q.QuestID);
        }
    }
}
```

### Item Collect Handler
```csharp
public void OnItemCollected(int itemID, int qty, Player player)
{
    var active = player.QuestMgr.GetActiveQuests();
    foreach (var q in active.Values)
    {
        if (q.ObjectiveType == "CollectItem" && 
            q.ObjectiveTargetID == itemID)
        {
            player.QuestMgr.UpdateQuestProgress(q.QuestID, qty);
        }
    }
}
```

### NPC Interaction
```csharp
public class MyQuestNpc : QuestNpc
{
    public override void Interact(Player src)
    {
        // Show menu
    }

    public override void Interact(Player src, byte? answer = null)
    {
        if (answer == 1)
            AcceptQuestFromPlayer(src, 1);
        else if (answer == 2)
            CompleteQuestWithPlayer(src, 1);
    }
}
```

## Database Tables

```
quests                          - Quest templates
quest_item_rewards              - Item rewards
quest_skill_rewards             - Skill rewards
quest_prerequisites             - Quest chains
character_active_quests         - Player active quests
character_completed_quests      - Player completed quests
quest_objective_progress        - Multi-stage quest tracking
```

## Initialization

```csharp
// Server startup
DataFiles.QuestLoader loader = new DataFiles.QuestLoader();
await loader.Load("gamedata/quests.xml");

// Player login
player.QuestMgr = new QuestManager(player);
questDB.LoadPlayerActiveQuests(player);
questDB.LoadPlayerCompletedQuests(player);

// Player logout
questDB.SaveAllPlayerQuests(player.CharID, 
    player.QuestMgr.GetActiveQuests(),
    player.QuestMgr.GetCompletedQuests());
```

## XML Quest Format

```xml
<Quest>
    <QuestID>1</QuestID>
    <QuestName>Quest Name</QuestName>
    <Description>Quest description</Description>
    <Level>10</Level>
    <RequiredLevel>5</RequiredLevel>
    <StartNpcID>100</StartNpcID>
    <EndNpcID>101</EndNpcID>
    <ObjectiveCount>5</ObjectiveCount>
    <ObjectiveType>KillMob</ObjectiveType>
    <ObjectiveTargetID>1001</ObjectiveTargetID>
    <RewardEXP>1000</RewardEXP>
    <RewardMoney>500</RewardMoney>
    <IsRepeatable>false</IsRepeatable>
    <TimeLimit>0</TimeLimit>
    <Prerequisites>
        <Quest>5</Quest>
    </Prerequisites>
    <ItemRewards>
        <Item>
            <ItemID>2001</ItemID>
            <Quantity>1</Quantity>
            <IsChoice>false</IsChoice>
        </Item>
    </ItemRewards>
    <SkillRewards>
        <Skill>1001</Skill>
    </SkillRewards>
</Quest>
```

## Debug Commands

```csharp
// Accept quest
/quest accept <questID>

// Complete quest
/quest complete <questID>

// List active quests
/quest list

// Fail quest
/quest fail <questID>

// Abandon quest
/quest abandon <questID>
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Quest not loading | Check quests.xml format |
| Rewards not granted | Verify Player has AddExp, AddMoney methods |
| Quest state not saving | Call SaveAllPlayerQuests on logout |
| Database errors | Check quest_schema.sql applied |
| NPC quests not showing | Verify NPC.OfferedQuests/ReceivesQuests filled |

## Performance Tips

1. Active quests cached in memory
2. Database indexed on charID, questID
3. Quest templates loaded once at startup
4. Use GetActiveQuest() not GetActiveQuests().Keys
5. Archive old completed quests periodically
