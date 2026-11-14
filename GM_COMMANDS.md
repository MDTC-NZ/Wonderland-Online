# Wonderland Online - Game Master (GM) Commands & Functions

## 📋 Command Syntax

All GM commands start with `/` or `@` prefix:
```
/command parameter1 parameter2
@command parameter1 parameter2
```

---

## 👤 PLAYER MANAGEMENT COMMANDS

### User Account Commands

| Command | Syntax | Description | Example |
|---------|--------|-------------|---------|
| **kick** | `/kick <username>` | Remove player from server | `/kick player2` |
| **ban** | `/ban <username> [reason]` | Ban player account | `/ban testuser "Banned for 7 days"` |
| **unban** | `/unban <username>` | Unban player account | `/unban testuser` |
| **banlist** | `/banlist` | Show all banned accounts | `/banlist` |
| **mute** | `/mute <username> [minutes]` | Prevent player from chatting | `/mute player2 60` |
| **unmute** | `/unmute <username>` | Allow player to chat | `/unmute player2` |
| **setpassword** | `/setpassword <username> <newpass>` | Change player password | `/setpassword player2 newpass123` |

---

### Character Management

| Command | Syntax | Description | Example |
|---------|--------|-------------|---------|
| **createchar** | `/createchar <username> <charname>` | Create character for player | `/createchar player2 PlayerChar` |
| **deletechar** | `/deletechar <charname>` | Delete a character | `/deletechar PlayerChar` |
| **renamechar** | `/renamechar <oldname> <newname>` | Rename character | `/renamechar PlayerChar NewName` |
| **charinfo** | `/charinfo <charname>` | Show character details | `/charinfo PlayerChar` |
| **listchars** | `/listchars <username>` | List all characters for player | `/listchars player2` |
| **setstats** | `/setstats <charname> <stat> <value>` | Set character stat | `/setstats PlayerChar level 50` |
| **setexp** | `/setexp <charname> <amount>` | Set character experience | `/setexp PlayerChar 100000` |
| **addexp** | `/addexp <charname> <amount>` | Add experience to character | `/addexp PlayerChar 50000` |
| **sethp** | `/sethp <charname> <amount>` | Set character HP | `/sethp PlayerChar 500` |
| **setmp** | `/setmp <charname> <amount>` | Set character MP | `/setmp PlayerChar 300` |

---

## 🎁 ITEM & INVENTORY COMMANDS

### Item Management

| Command | Syntax | Description | Example |
|---------|--------|-------------|---------|
| **give** | `/give <charname> <itemid> [amount]` | Give item to character | `/give PlayerChar 1001 5` |
| **remove** | `/remove <charname> <itemid> [amount]` | Remove item from inventory | `/remove PlayerChar 1001 2` |
| **clearinv** | `/clearinv <charname>` | Clear all inventory items | `/clearinv PlayerChar` |
| **inventory** | `/inventory <charname>` | Show character inventory | `/inventory PlayerChar` |
| **getitem** | `/getitem <itemid>` | Get item for yourself (GM only) | `/getitem 1001` |
| **dropitem** | `/dropitem <itemid> [amount]` | Drop item on ground | `/dropitem 1001 1` |
| **setgold** | `/setgold <charname> <amount>` | Set character gold | `/setgold PlayerChar 50000` |
| **addgold** | `/addgold <charname> <amount>` | Add gold to character | `/addgold PlayerChar 10000` |
| **removegold** | `/removegold <charname> <amount>` | Remove gold from character | `/removegold PlayerChar 5000` |

---

### Equipment

| Command | Syntax | Description | Example |
|---------|--------|-------------|---------|
| **equip** | `/equip <charname> <slot> <itemid>` | Equip item on character | `/equip PlayerChar head 2001` |
| **unequip** | `/unequip <charname> <slot>` | Remove equipment | `/unequip PlayerChar head` |
| **getequip** | `/getequip <charname>` | Show equipped items | `/getequip PlayerChar` |

---

## 🗺️ TELEPORTATION & MOVEMENT

| Command | Syntax | Description | Example |
|---------|--------|-------------|---------|
| **goto** | `/goto <charname> <mapname>` | Teleport character to map | `/goto PlayerChar Ship` |
| **summon** | `/summon <charname>` | Teleport character to you | `/summon PlayerChar` |
| **teleport** | `/teleport <x> <y>` | Teleport yourself | `/teleport 100 200` |
| **tele** | `/tele <charname> <x> <y>` | Teleport character to coords | `/tele PlayerChar 100 200` |
| **setpos** | `/setpos <charname> <x> <y> <mapname>` | Set character position | `/setpos PlayerChar 150 250 Ship` |
| **mypos** | `/mypos` | Show your current position | `/mypos` |
| **getpos** | `/getpos <charname>` | Show character position | `/getpos PlayerChar` |

---

## ⚔️ COMBAT & LEVELING

| Command | Syntax | Description | Example |
|---------|--------|-------------|---------|
| **setlevel** | `/setlevel <charname> <level>` | Set character level | `/setlevel PlayerChar 99` |
| **addlevel** | `/addlevel <charname> [amount]` | Increase character level | `/addlevel PlayerChar 10` |
| **setskill** | `/setskill <charname> <skillid> <level>` | Teach skill to character | `/setskill PlayerChar 101 5` |
| **removesk** | `/removesk <charname> <skillid>` | Remove skill from character | `/removesk PlayerChar 101` |
| **skills** | `/skills <charname>` | List character skills | `/skills PlayerChar` |
| **heal** | `/heal <charname>` | Heal character HP/MP | `/heal PlayerChar` |
| **revive** | `/revive <charname>` | Revive dead character | `/revive PlayerChar` |
| **godmode** | `/godmode <charname>` | Toggle invincibility | `/godmode PlayerChar` |
| **dmg** | `/dmg <amount>` | Damage yourself for testing | `/dmg 100` |

---

## 📚 QUEST MANAGEMENT

| Command | Syntax | Description | Example |
|---------|--------|-------------|---------|
| **addquest** | `/addquest <charname> <questid>` | Add quest to character | `/addquest PlayerChar 1001` |
| **removequest** | `/removequest <charname> <questid>` | Remove quest from character | `/removequest PlayerChar 1001` |
| **completequest** | `/completequest <charname> <questid>` | Complete quest for character | `/completequest PlayerChar 1001` |
| **listquests** | `/listquests <charname>` | Show character quests | `/listquests PlayerChar` |
| **questinfo** | `/questinfo <questid>` | Show quest details | `/questinfo 1001` |
| **updatequest** | `/updatequest <charname> <questid> <progress>` | Update quest progress | `/updatequest PlayerChar 1001 5` |

---

## 🏪 SHOP MANAGEMENT

| Command | Syntax | Description | Example |
|---------|--------|-------------|---------|
| **openshop** | `/openshop <npcname>` | Open NPC shop | `/openshop Rnaya` |
| **closeshop** | `/closeshop <npcname>` | Close NPC shop | `/closeshop Rnaya` |
| **shopinfo** | `/shopinfo <npcname>` | Show shop inventory | `/shopinfo Rnaya` |
| **setprice** | `/setprice <npcname> <itemid> <price>` | Set item price in shop | `/setprice Rnaya 1001 100` |
| **additem2shop** | `/additem2shop <npcname> <itemid> [stock]` | Add item to shop | `/additem2shop Rnaya 1001 50` |
| **removefromshop** | `/removefromshop <npcname> <itemid>` | Remove item from shop | `/removefromshop Rnaya 1001` |

---

## 👹 MONSTER MANAGEMENT

| Command | Syntax | Description | Example |
|---------|--------|-------------|---------|
| **spawn** | `/spawn <monsterid> [amount] [mapname]` | Spawn monsters | `/spawn 1 5 Ship` |
| **despawn** | `/despawn <monsterid>` | Remove all monsters of type | `/despawn 1` |
| **despawnall** | `/despawnall` | Remove all monsters on map | `/despawnall` |
| **monsterinfo** | `/monsterinfo <monsterid>` | Show monster details | `/monsterinfo 1` |
| **monsters** | `/monsters` | List monsters on current map | `/monsters` |
| **killall** | `/killall [mapname]` | Kill all monsters | `/killall Ship` |

---

## 🎣 GATHERING & COLLECTION

| Command | Syntax | Description | Example |
|---------|--------|-------------|---------|
| **respawnres** | `/respawnres <resourceid>` | Respawn resource node | `/respawnres 1` |
| **respawnall** | `/respawnall` | Respawn all resources | `/respawnall` |
| **resources** | `/resources` | List resources on map | `/resources` |

---

## 🐾 PET MANAGEMENT

| Command | Syntax | Description | Example |
|---------|--------|-------------|---------|
| **addpet** | `/addpet <charname> <petid>` | Give pet to character | `/addpet PlayerChar 1` |
| **removepet** | `/removepet <charname> <petid>` | Remove pet from character | `/removepet PlayerChar 1` |
| **listpets** | `/listpets <charname>` | Show character pets | `/listpets PlayerChar` |
| **petinfo** | `/petinfo <petid>` | Show pet details | `/petinfo 1` |
| **summonpet** | `/summonpet <charname> <petid>` | Summon pet for character | `/summonpet PlayerChar 1` |

---

## 🔧 SERVER & SYSTEM COMMANDS

### Server Management

| Command | Syntax | Description | Example |
|---------|--------|-------------|---------|
| **broadcast** | `/broadcast <message>` | Send message to all players | `/broadcast "Server maintenance in 5 minutes"` |
| **announce** | `/announce <message>` | Send colored announcement | `/announce "New event started!"` |
| **serverstatus** | `/serverstatus` | Show server info | `/serverstatus` |
| **playercount** | `/playercount` | Show online player count | `/playercount` |
| **onlinelist** | `/onlinelist` | List all online players | `/onlinelist` |
| **lag** | `/lag` | Show current server lag | `/lag` |
| **uptime** | `/uptime` | Show server uptime | `/uptime` |

### Database & Saving

| Command | Syntax | Description | Example |
|---------|--------|-------------|---------|
| **save** | `/save` | Force save all data | `/save` |
| **reload** | `/reload [section]` | Reload configuration | `/reload quests` |
| **backup** | `/backup` | Create database backup | `/backup` |
| **dbstats** | `/dbstats` | Show database statistics | `/dbstats` |

### Debug & Logging

| Command | Syntax | Description | Example |
|---------|--------|-------------|---------|
| **debug** | `/debug [on/off]` | Toggle debug mode | `/debug on` |
| **log** | `/log [level]` | Set logging level | `/log verbose` |
| **dumplog** | `/dumplog [lines]` | Show recent logs | `/dumplog 50` |
| **trace** | `/trace <player>` | Trace player actions | `/trace PlayerChar` |

---

## 🎭 COSMETIC & SPECIAL EFFECTS

| Command | Syntax | Description | Example |
|---------|--------|-------------|---------|
| **effect** | `/effect <effectid>` | Play visual effect | `/effect 1` |
| **animation** | `/animation <animid>` | Play animation | `/animation 5` |
| **particle** | `/particle <particleid>` | Show particle effect | `/particle 10` |
| **sound** | `/sound <soundid>` | Play sound effect | `/sound 1` |

---

## 📊 STATISTICS & REPORTING

| Command | Syntax | Description | Example |
|---------|--------|-------------|---------|
| **stats** | `/stats` | Show your statistics | `/stats` |
| **playerstats** | `/playerstats <username>` | Show player statistics | `/playerstats player2` |
| **monsterstats** | `/monsterstats` | Show monster spawn stats | `/monsterstats` |
| **economy** | `/economy` | Show economy statistics | `/economy` |
| **report** | `/report` | Generate server report | `/report` |

---

## 🔐 SECURITY & PERMISSIONS

| Command | Syntax | Description | Example |
|---------|--------|-------------|---------|
| **gmlist** | `/gmlist` | List all GM accounts | `/gmlist` |
| **gmadmin** | `/gmadmin <username> [on/off]` | Grant/revoke GM status | `/gmadmin player2 on` |
| **viewperms** | `/viewperms <username>` | Show user permissions | `/viewperms player2` |
| **grantperm** | `/grantperm <username> <permission>` | Grant permission | `/grantperm player2 ban_players` |
| **revokeperm** | `/revokeperm <username> <permission>` | Revoke permission | `/revokeperm player2 ban_players` |
| **auditlog** | `/auditlog [username]` | Show audit log | `/auditlog` |

---

## 💬 CHAT & COMMUNICATION

| Command | Syntax | Description | Example |
|---------|--------|-------------|---------|
| **whisper** | `/w <player> <message>` | Private message | `/w player2 "Hello"` |
| **channel** | `/channel <channelname>` | Join chat channel | `/channel trade` |
| **channels** | `/channels` | List available channels | `/channels` |
| **announce** | `/announce <message>` | Server announcement | `/announce "Maintenance time!"` |

---

## 🛠️ DEVELOPER COMMANDS

| Command | Syntax | Description | Example |
|---------|--------|-------------|---------|
| **reload** | `/reload <section>` | Reload data section | `/reload quests` |
| **clearmap** | `/clearmap [mapname]` | Clear all entities on map | `/clearmap Ship` |
| **warp** | `/warp <x> <y>` | Warp to coordinates | `/warp 100 200` |
| **test** | `/test <testname>` | Run system test | `/test spawn` |
| **perftest** | `/perftest` | Run performance test | `/perftest` |

---

## 🎯 QUICK REFERENCE

### Most Used Commands
```
/goto PlayerChar Ship          # Teleport player to map
/give PlayerChar 1001 5        # Give 5 items to player
/setlevel PlayerChar 50        # Set player level
/spawn 1 10 Ship               # Spawn 10 monsters
/broadcast "Server message"    # Announce to all
/createchar player2 NewChar    # Create character
/kick player2                  # Remove from server
```

### Testing & Debugging
```
/mypos                         # Check your position
/playercount                   # How many online
/serverstatus                  # Server info
/monsters                      # List map monsters
/inventory PlayerChar          # Check inventory
/listquests PlayerChar         # Show player quests
```

### Maintenance
```
/save                          # Save database
/backup                        # Backup all data
/broadcast "Message"           # Notify players
/kick username                 # Remove player
/ban username                  # Ban account
```

---

## 📝 COMMAND IMPLEMENTATION CHECKLIST

- [ ] Player Management (kick, ban, create char, delete char)
- [ ] Item Management (give, remove, inventory, gold)
- [ ] Teleportation (goto, teleport, summon)
- [ ] Combat & Leveling (setlevel, setskill, heal, godmode)
- [ ] Quest Management (addquest, completequest, listquests)
- [ ] Shop Management (openshop, setprice, additem2shop)
- [ ] Monster Management (spawn, despawn, monsterinfo)
- [ ] Gathering & Resources (respawnres, respawnall)
- [ ] Pet Management (addpet, removepet, summonpet)
- [ ] Server Management (broadcast, serverstatus, playercount)
- [ ] Database (save, backup, reload)
- [ ] Security (gmlist, gmadmin, grantperm)

---

## 🔐 PERMISSION LEVELS

### Level 1 - Trial GM
- Basic commands: broadcast, lag, uptime
- Cannot ban/kick players

### Level 2 - Full GM
- All Level 1 commands
- Player management: kick, create char, teleport
- Item management: give items
- Monster spawning

### Level 3 - Admin
- All Level 2 commands
- Ban/unban players
- Database operations (save, backup, reload)
- Set permissions for other GMs

### Level 4 - Super Admin
- All commands
- Server shutdown/restart
- Emergency procedures
- Configuration changes

---

## 📚 USAGE EXAMPLES

### Creating a New Test Player
```
/createchar player2 TestCharacter
/setlevel TestCharacter 10
/give TestCharacter 1001 50
/goto TestCharacter Ship
```

### Testing Quest System
```
/addquest TestCharacter 1001
/updatequest TestCharacter 1001 5
/completequest TestCharacter 1001
```

### Testing Combat
```
/spawn 1 10 Ship
/godmode TestCharacter
/heal TestCharacter
```

### Server Maintenance
```
/broadcast "Server maintenance begins in 5 minutes"
/kick offline_player
/save
/backup
```

---

Generated: 2026-11-15
Database: C:\Users\hayde\AppData\Roaming\PServer\unified.db
