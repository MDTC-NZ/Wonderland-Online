#!/usr/bin/env python3
import sqlite3
import os
from datetime import datetime

db_path = os.path.expandvars(r'%APPDATA%\PServer\registration.db')
db_dir = os.path.dirname(db_path)

# Create directory if needed
os.makedirs(db_dir, exist_ok=True)

print("Database path:", db_path)
print()

try:
    conn = sqlite3.connect(db_path)
    c = conn.cursor()
    
    # Create tables if needed
    c.execute('''CREATE TABLE IF NOT EXISTS users (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        username TEXT UNIQUE NOT NULL,
        password TEXT NOT NULL,
        email TEXT,
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        character1 TEXT,
        character2 TEXT
    )''')
    conn.commit()
    
    # Check if testuser exists
    c.execute('SELECT id FROM users WHERE username = ?', ('testuser',))
    user = c.fetchone()
    
    if user:
        print("[OK] User exists: testuser (ID: {})".format(user[0]))
        user_id = user[0]
    else:
        # Create new user
        c.execute('INSERT INTO users (username, password) VALUES (?, ?)', ('testuser', 'test1234'))
        conn.commit()
        user_id = c.lastrowid
        print("[+] Created user: testuser (ID: {})".format(user_id))
    
    # Update character field
    c.execute('UPDATE users SET character1 = ? WHERE id = ?', ('TestCharacter', user_id))
    conn.commit()
    print("[+] Set character1 = TestCharacter")
    
    # Verify
    c.execute('SELECT username, character1 FROM users WHERE id = ?', (user_id,))
    result = c.fetchone()
    print()
    print("User Info:")
    print("  Username: {}".format(result[0]))
    print("  Character: {}".format(result[1]))
    print("  Ready to login with username: testuser, password: test1234")
    
    conn.close()
    
except Exception as e:
    print("Error: {}".format(e))
    import traceback
    traceback.print_exc()
