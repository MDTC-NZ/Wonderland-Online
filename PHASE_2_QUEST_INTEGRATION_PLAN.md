# WONDERLAND ONLINE PRIVATE SERVER - PHASE 2: QUEST SYSTEM INTEGRATION PLAN

## Objectives
1. Integrate existing Quest framework with Player class
2. Connect NPC dialogue to quest acceptance/completion
3. Implement quest tracking UI packets
4. Load 5000+ quests from WLO game data
5. Test quest lifecycle: Accept → Progress → Complete → Reward

## Current Status

### Existing Quest Framework (✓ Complete)
- ✓ Quest.cs - Quest data model with full properties
- ✓ QuestManager.cs - Quest lifecycle management
- ✓ QuestState enum - NotStarted, InProgress, Completed, Rewarded, etc.
- ✓ QuestReward class - Item/EXP/Gold rewards
- ✓ PrerequisiteQuests support
- ✓ Repeatable quests support
- ✓ Time-limited quests support

### Database Schema (✓ Complete)
- ✓ quests table - Quest templates
- ✓ character_quests table - Player quest tracking
- ✓ Columns: quest_id, status, progress, started_at, completed_at

### Missing Implementations
- ⏳ Integration with Player class
- ⏳ NPC quest dialogs (AC20/AC39 handlers)
- ⏳ Quest tracking UI packets (AC26/AC53)
- ⏳ Reward granting system
- ⏳ Quest data loading from JSON

## Integration Points

### 1. Player Class Integration

**Location**: `wlo.pserver.core/Game/Player.cs`

**Changes Needed**:
```csharp
public class Player : Game.Character
{
    private QuestManager m_questManager;
    
    public QuestManager Quests 
    { 
        get { return m_questManager; } 
    }
    
    // Constructor
    public Player(SocketClient src, DataFiles.PhxItemDat itemdat) : base(src, itemdat)
    {
        m_questManager = new QuestManager(this);
        // ... rest of initialization
    }
}
```

### 2. NPC Interaction Handler (AC20 - Door/Portal/NPC)

**Location**: `Src/Network/ActionCodes/AC20.cs`

**Current Structure**:
- Recv1() - Door/Exit interaction
- Recv6() - Shop/NPC interaction
- Recv8() - Portal/Warp
- Recv9() - Unknown

**Changes Needed**:
```csharp
void Recv6(Player p, RecievePacket r)  // NPC Interaction
{
    try
    {
        ushort npcID = r.Unpack16();
        byte dialogType = r.Unpack8();  // 1=Quest, 2=Shop, 3=Teleport, etc
        
        switch(dialogType)
        {
            case 1: HandleQuestDialog(p, npcID); break;
            case 2: HandleShopDialog(p, npcID); break;
            // ... etc
        }
    }
    catch(Exception ex) { LogServices.Log(ex); }
}

void HandleQuestDialog(Player p, ushort npcID)
{
    // 1. Get NPC's available quests
    // 2. Check player's quest status
    // 3. Send dialog packet to client
    // 4. Wait for client acceptance
}
```

### 3. Quest Action Handler (AC26/AC53 - Quest Management)

**Proposed AC26 Handler**:
- Recv1() - Accept quest
- Recv2() - Abandon quest
- Recv3() - Complete quest (hand in)
- Recv4() - Get quest list
- Recv5() - Get quest details

### 4. Quest Tracking UI Packets

**AC53 - Quest List Packet**:
```
[53][1][Count]
  For each active quest:
  [QuestID:2][Progress:1][Objective:1][QuestName:string]
```

**AC26 - Quest Details Packet**:
```
[26][1][QuestID:2]
  [Name:string][Description:string]
  [Progress:1][Objectives:1]
  [RewardExp:4][RewardGold:4]
  [ItemRewardCount:1]
    For each reward:
    [ItemID:2][Quantity:2][IsChoice:1]
```

## Implementation Schedule

### Phase 2A - Core Integration (2-3 hours)
1. Add QuestManager to Player class
2. Create AC26 quest handler stub
3. Hook NPC interaction to quest dialogs
4. Test framework compiles

### Phase 2B - Database Integration (2 hours)
1. Create QuestLoader to read from database
2. Implement GetCharacterData() quest loading
3. Implement SaveCharacterQuests() persistence
4. Test load/save cycle

### Phase 2C - Quest Data Loading (1-2 hours)
1. Parse 5000+ quests from wlohub.com JSON
2. Load into quests table
3. Create initial NPC quest assignments
4. Create quest prerequisites mapping

### Phase 2D - UI & Rewards (2-3 hours)
1. Implement quest tracking packets
2. Implement reward granting (EXP, Gold, Items)
3. Add quest completion dialog
4. Test end-to-end quest flow

### Phase 2E - Testing & Fixes (2-3 hours)
1. Create test quests (1-5 easy quests)
2. Test with GM account
3. Verify packet structures
4. Fix any issues found

## File Changes Summary

### New Files
- `initialize_quest_data.py` - Load 5000+ quests from JSON

### Modified Files
- `wlo.pserver.core/Game/Player.cs` - Add QuestManager field
- `Src/Network/ActionCodes/AC20.cs` - Add NPC quest dialog handling
- `Src/Network/ActionCodes/AC26.cs` - Create NEW quest action handler
- `wlo.pserver.core/DataBase/CharacterDataBase.cs` - Load/save quests
- `wlo.pserver.core/Game/Maps/Code/QuestNpc.cs` - Quest NPC logic

### No Changes Needed
- `wlo.pserver.core/Game/PlayerRelated/Quest.cs` - Already complete
- `wlo.pserver.core/Game/PlayerRelated/QuestManager.cs` - Already complete
- `wlo.pserver.core/DataBase/QuestDataBase.cs` - Already exists

## Risk Factors & Mitigations

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Packet structure mismatch | Game crash | Use existing AC patterns as reference |
| Quest data corruption | Invalid quests | Validate JSON before loading |
| Player quest state loss | Lost progress | Implement proper transaction handling |
| NPC dialog deadlock | Frozen player | Use timeout mechanisms |

## Success Criteria

- ✓ Player can accept quest from NPC
- ✓ Quest appears in player's quest log
- ✓ Player can update quest progress (via command or action)
- ✓ Player can complete quest and receive rewards
- ✓ Completed quests don't appear in active list
- ✓ Repeatable quests can be accepted again
- ✓ Non-repeatable quests cannot be re-accepted
- ✓ Quest state persists between logins
- ✓ 5000+ quests loaded in database
- ✓ At least 20 quests available to players initially

## Next Steps After Phase 2

Phase 3 - Inventory System:
- Item pickup/drop implementation
- Weight/slot limits
- Equipment system
- Storage/tent system

These enable the remaining systems:
- Combat (need inventory for weapons)
- Crafting (need inventory for materials)
- Shops (need inventory to sell items)

---

**Estimated Total Time**: 12-14 hours for complete integration
**Priority Level**: HIGHEST - Blocks inventory and combat systems
**Deadline**: Next session start
