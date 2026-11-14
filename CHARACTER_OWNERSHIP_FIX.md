# Character Ownership Fix

## Problem
When creating an account, it already has a character there, and when another user creates a character, it goes on all accounts in the database. **Characters were being shared across all user accounts.**

## Root Cause
The database schema for the `characters` table was missing the `userID` column. Without this relationship, there was no way to distinguish which user owned which character. The system loaded **any character matching the calculated charID**, regardless of which user it belonged to.

### How it worked (BROKEN):
1. User gmone (DataBaseID=1) logs in
2. System calculates Character1ID = UserID (1+10000) = 10001
3. Database query: `SELECT * FROM characters WHERE charID = 10001`
4. Returns ANY character with charID=10001, doesn't matter who owns it!

## Solution

### Step 1: Add userID Column to Database
```sql
ALTER TABLE characters ADD COLUMN userID INTEGER
```

Then linked existing character to correct user:
```sql
UPDATE characters SET userID = 1 WHERE charID = 10000
```

### Step 2: Update C# Code - Add Character Validation

#### File: `wlo.pserver.core\DataBase\CharacterDataBase.cs`

Added new overload:
```csharp
public Character GetCharacterData(uint charID, uint userID)
{
    // Query now includes userID validation
    src = GetDataTable("SELECT * FROM characters where charID = '" + charID + "' AND userID = '" + userID + "'");
    // Returns null if character doesn't belong to this user
}
```

Updated existing overload:
```csharp
public bool GetCharacterData(uint charID, ref Player t)
{
    uint userID = t.UserAcc?.DataBaseID + 10000 ?? 0;
    return GetCharacterData(charID, userID, ref t);  // Now validates userID
}
```

#### File: `Src\Network\ActionCodes\AC63.cs`

Updated character list sending to validate ownership:
```csharp
Character char1 = cGlobal.gCharacterDataBase.GetCharacterData(p.UserAcc.Character1ID, p.UserAcc.DataBaseID + 10000);
Character char2 = cGlobal.gCharacterDataBase.GetCharacterData(p.UserAcc.Character2ID, p.UserAcc.DataBaseID + 10000);
if (char1 != null) tmp.PackArray(char1.ToArray());
if (char2 != null) tmp.PackArray(char2.ToArray());
```

## Result

### New Behavior (FIXED):
1. User gmone (DataBaseID=1, UserID=10001) logs in
2. System calculates Character1ID = 10001
3. Database query: `SELECT * FROM characters WHERE charID = 10001 AND userID = 10001`
4. Returns ONLY gmone's characters!
5. User gmone1 (DataBaseID=2, UserID=10002) logs in
6. Database query: `SELECT * FROM characters WHERE charID = 10002 AND userID = 10002`
7. Returns ONLY gmone1's characters!

## Files Modified
- `ServerDataBase.db` - Added userID column to characters table
- `wlo.pserver.core\DataBase\CharacterDataBase.cs` - Added userID validation overloads
- `Src\Network\ActionCodes\AC63.cs` - Updated to validate character ownership

## Testing
After rebuild and restart:
1. Create new account "testuser" / "testpass"
2. Create character "test1" on slot 1
3. Create account "testuser2" / "testpass2"
4. Create character "test2" on slot 1
5. Login with testuser → should only see "test1"
6. Login with testuser2 → should only see "test2"
7. Characters should NOT be shared!
