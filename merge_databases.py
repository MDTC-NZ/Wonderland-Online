#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Wonderland Online - Database Consolidation & Merge
"""

import sqlite3
import os
import shutil
import sys
from datetime import datetime

# Fix encoding for Windows console
if sys.platform == 'win32':
    import io
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

# Database paths
PYTHON_DB = os.path.expandvars(r'%APPDATA%\PServer\registration.db')
GAME_DB_MAIN = r'C:\Users\hayde\Downloads\Wonderland-Develop\Wonderland-Develop\bin\Debug\ServerDataBase.db'
GAME_DB_CHEAT = r'C:\Users\hayde\Downloads\Wonderland-Private-Server-cheatengine-fork\Wonderland-Private-Server-cheatengine-fork\bin\Debug\ServerDataBase.db'
UNIFIED_DB = os.path.expandvars(r'%APPDATA%\PServer\unified.db')
BACKUP_DIR = os.path.expandvars(r'%APPDATA%\PServer\backups')

print("=" * 70)
print("  WONDERLAND ONLINE - DATABASE CONSOLIDATION")
print("=" * 70)
print()

# Create backup directory
os.makedirs(BACKUP_DIR, exist_ok=True)

print(f"[1] Creating backups in {BACKUP_DIR}...")
print()

# Backup existing databases
for db_path in [PYTHON_DB, GAME_DB_MAIN, GAME_DB_CHEAT]:
    if os.path.exists(db_path):
        timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
        backup_path = os.path.join(BACKUP_DIR, f"{os.path.basename(db_path)}.backup_{timestamp}")
        shutil.copy2(db_path, backup_path)
        print(f"  [OK] Backed up: {os.path.basename(db_path)}")

print()
print(f"[2] Creating unified database at {UNIFIED_DB}...")
print()

try:
    # Create unified database
    unified_conn = sqlite3.connect(UNIFIED_DB)
    uc = unified_conn.cursor()
    
    # Create unified tables
    print("  Creating user table...")
    uc.execute('''CREATE TABLE IF NOT EXISTS users (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        username TEXT UNIQUE NOT NULL,
        password TEXT NOT NULL,
        email TEXT,
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        character1 TEXT,
        character2 TEXT,
        source TEXT DEFAULT 'unified'
    )''')
    
    print("  Creating characters table...")
    uc.execute('''CREATE TABLE IF NOT EXISTS characters (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        user_id INTEGER NOT NULL,
        character_name TEXT UNIQUE NOT NULL,
        level INTEGER DEFAULT 1,
        exp INTEGER DEFAULT 0,
        hp INTEGER DEFAULT 100,
        mp INTEGER DEFAULT 100,
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        FOREIGN KEY(user_id) REFERENCES users(id)
    )''')
    
    unified_conn.commit()
    print()
    
    # Merge from Python registration DB
    if os.path.exists(PYTHON_DB):
        print(f"[3] Merging from Python registration DB...")
        python_conn = sqlite3.connect(PYTHON_DB)
        pc = python_conn.cursor()
        
        pc.execute("SELECT * FROM users")
        python_users = pc.fetchall()
        
        print(f"  Found {len(python_users)} users from Python DB")
        
        for user in python_users:
            try:
                uc.execute(
                    "INSERT OR IGNORE INTO users (username, password, email, character1, character2, source) VALUES (?, ?, ?, ?, ?, 'python_reg')",
                    (user[1], user[2], user[3], user[5], user[6])
                )
                print(f"    ✅ Merged user: {user[1]}")
            except Exception as e:
                print(f"    ⚠️  Could not merge {user[1]}: {e}")
        
        unified_conn.commit()
        python_conn.close()
        print()
    
    # Merge from Game Database (Main)
    if os.path.exists(GAME_DB_MAIN):
        print(f"[4] Merging from Game Database (Main)...")
        game_conn = sqlite3.connect(GAME_DB_MAIN)
        gc = game_conn.cursor()
        
        try:
            gc.execute("SELECT * FROM users")
            game_users = gc.fetchall()
            
            print(f"  Found {len(game_users)} users from Game DB (Main)")
            
            for user in game_users:
                try:
                    uc.execute(
                        "INSERT OR IGNORE INTO users (username, password, email, character1, character2, source) VALUES (?, ?, ?, ?, ?, 'game_main')",
                        (user[1], user[2], user[3], user[5], user[6])
                    )
                    print(f"    ✅ Merged user: {user[1]}")
                except Exception as e:
                    print(f"    ⚠️  Could not merge {user[1]}: {e}")
            
            unified_conn.commit()
        except Exception as e:
            print(f"  ⚠️  Could not access users table: {e}")
        
        game_conn.close()
        print()
    
    # Merge from Game Database (CheatEngine)
    if os.path.exists(GAME_DB_CHEAT):
        print(f"[5] Merging from Game Database (CheatEngine)...")
        cheat_conn = sqlite3.connect(GAME_DB_CHEAT)
        cc = cheat_conn.cursor()
        
        try:
            cc.execute("SELECT * FROM users")
            cheat_users = cc.fetchall()
            
            print(f"  Found {len(cheat_users)} users from Game DB (CheatEngine)")
            
            for user in cheat_users:
                try:
                    uc.execute(
                        "INSERT OR IGNORE INTO users (username, password, email, character1, character2, source) VALUES (?, ?, ?, ?, ?, 'game_cheat')",
                        (user[1], user[2], user[3], user[5], user[6])
                    )
                    print(f"    ✅ Merged user: {user[1]}")
                except Exception as e:
                    print(f"    ⚠️  Could not merge {user[1]}: {e}")
            
            unified_conn.commit()
        except Exception as e:
            print(f"  ⚠️  Could not access users table: {e}")
        
        cheat_conn.close()
        print()
    
    # Summary
    print("[6] Unified Database Summary")
    uc.execute("SELECT COUNT(*) FROM users")
    total_users = uc.fetchone()[0]
    
    print(f"  ✅ Total users consolidated: {total_users}")
    
    uc.execute("SELECT username, source FROM users ORDER BY created_at DESC")
    all_users = uc.fetchall()
    
    print()
    print("  User List:")
    for username, source in all_users:
        print(f"    • {username} (from {source})")
    
    unified_conn.close()
    
    print()
    print("=" * 70)
    print("  DATABASE CONSOLIDATION COMPLETE!")
    print("=" * 70)
    print()
    print(f"Unified Database: {UNIFIED_DB}")
    print(f"Backups Location: {BACKUP_DIR}")
    print()
    print("Next Steps:")
    print("  1. Update all servers to use: %APPDATA%\\PServer\\unified.db")
    print("  2. Point CheatEngine server to unified.db")
    print("  3. Point Python server to unified.db")
    print("  4. All registration/users/characters accessible from one DB")
    print()
    
except Exception as e:
    print(f"❌ ERROR: {e}")
    import traceback
    traceback.print_exc()

