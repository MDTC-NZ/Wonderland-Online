# Wonderland Online Server - Accounts & Credentials

## GM Account (Administrator)

```
Username: GM
Password: Admin123
```

**Use this account for:**
- Game Master functions
- Administrative commands
- Testing game systems
- Debugging issues

---

## Test Player Accounts

| Username | Password | Source | Status |
|----------|----------|--------|--------|
| testuser123 | Test1234 | Python Registration | Active |
| player2 | Pass1234 | Python Registration | Active |
| user_1794658300 | Test123 | Python Registration | Active |
| test123 | Pass123 | Python Registration | Active |
| testuser | test1234 | Python Registration | Has character |

---

## Database Location

```
C:\Users\hayde\AppData\Roaming\PServer\unified.db
```

**Tables:**
- `users` - Player accounts
- `characters` - Character data
- `sqlite_sequence` - Auto-increment tracking

---

## How to Connect

### Registration Server (Python)
- URL: http://localhost:8090
- Registration interface for new accounts

### Game Server (to be implemented)
- Port: 6414 (game server)
- Port: 8080 (game launcher)

---

## Next Steps

1. **Start game server** with the core quest/shop/monster systems
2. **Login with GM account** to test administrative functions
3. **Create character** with test player account
4. **Begin gameplay testing** with systems coming online

---

## Database Schema Notes

**Current Structure:**
- `users.id` - Primary key
- `users.username` - Login username (UNIQUE)
- `users.password` - Plain text password (TODO: hash these!)
- `users.email` - Email (optional)
- `users.created_at` - Account creation timestamp
- `users.character1`, `character2` - Character names
- `users.source` - Account source (python_reg or gm_account)

**TODO: Security Improvements**
- Hash passwords with bcrypt/SHA256
- Add password salt
- Add admin flag to schema
- Add account ban/suspension tracking
- Add login history
- Add two-factor authentication

---

## Account Management Commands

To create additional GM accounts or test accounts in the future:

```python
import sqlite3

db_path = r'C:\Users\hayde\AppData\Roaming\PServer\unified.db'
conn = sqlite3.connect(db_path)
cursor = conn.cursor()

# Create new account
cursor.execute(
    "INSERT INTO users (username, password, source) VALUES (?, ?, ?)",
    ('newuser', 'password123', 'manual')
)
conn.commit()
conn.close()
```

---

## Security Notes

⚠️ **WARNING:** This server is using plain-text passwords. Before production deployment:

1. Hash all passwords with bcrypt
2. Add salt to password hashing
3. Implement account lockout after failed logins
4. Add password change functionality
5. Add email verification
6. Add password recovery system
7. Implement HTTPS/TLS
8. Add logging and auditing
9. Add rate limiting on login attempts
10. Add session token system

---

## Status

- ✅ Database created and initialized
- ✅ 6 accounts total (1 GM + 5 test players)
- ✅ Database consolidation complete
- ⏳ Game server systems (quests, shops, monsters) - In Progress
- ⏳ Login/authentication system - To Do
- ⏳ Character creation UI - To Do
- ⏳ Game client connection - To Do

