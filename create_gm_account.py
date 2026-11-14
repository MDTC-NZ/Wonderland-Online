#!/usr/bin/env python
# -*- coding: utf-8 -*-
import sqlite3
import os
from datetime import datetime

def create_gm_account():
    """Create a GM account in the unified database"""
    
    db_path = os.path.expandvars(r"%APPDATA%\PServer\unified.db")
    
    if not os.path.exists(db_path):
        print("[ERROR] Database not found at: " + db_path)
        return False
    
    try:
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        print("[OK] Connected to database")
        
        # Check if users table exists
        cursor.execute("SELECT name FROM sqlite_master WHERE type='table' AND name='users'")
        if not cursor.fetchone():
            print("[ERROR] Users table does not exist!")
            conn.close()
            return False
        
        # Check table structure
        cursor.execute("PRAGMA table_info(users)")
        columns = [col[1] for col in cursor.fetchall()]
        print("[OK] User table columns: " + ", ".join(columns))
        
        # Create GM account
        gm_username = "GM_Admin"
        gm_password = "GMPassword123"
        gm_email = "gm@wonderland.local"
        
        print("")
        print("[Creating GM Account]")
        print("  Username: " + gm_username)
        print("  Email: " + gm_email)
        print("  Password: " + gm_password)
        print("  Level: 255 (GM)")
        
        # Try to insert with available columns
        try:
            # Try extended insert with more fields
            cursor.execute("""
                INSERT INTO users (username, email, password, level, created_at, is_gm, is_online)
                VALUES (?, ?, ?, ?, ?, ?, ?)
            """, (gm_username, gm_email, gm_password, 255, datetime.now().isoformat(), 1, 1))
        except sqlite3.OperationalError as e1:
            try:
                # Try without is_online
                cursor.execute("""
                    INSERT INTO users (username, email, password, level, created_at, is_gm)
                    VALUES (?, ?, ?, ?, ?, ?)
                """, (gm_username, gm_email, gm_password, 255, datetime.now().isoformat(), 1))
            except sqlite3.OperationalError as e2:
                try:
                    # Try basic fields only
                    cursor.execute("""
                        INSERT INTO users (username, email, password, level)
                        VALUES (?, ?, ?, ?)
                    """, (gm_username, gm_email, gm_password, 255))
                except sqlite3.OperationalError as e3:
                    print("[ERROR] Could not insert: " + str(e3))
                    conn.close()
                    return False
        
        conn.commit()
        print("[OK] GM account created successfully!")
        
        # Verify it was created
        cursor.execute("SELECT * FROM users WHERE username = ?", (gm_username,))
        gm_data = cursor.fetchone()
        
        if gm_data:
            print("[OK] Verified GM account in database")
            print("  Data: " + str(gm_data))
        
        conn.close()
        
        print("")
        print("[SUCCESS] GM Account Ready!")
        print("")
        print("  Login with:")
        print("    URL: http://localhost:8080/")
        print("    Username: " + gm_username)
        print("    Password: " + gm_password)
        print("")
        print("  In-Game Name: GM_Admin")
        print("  Level: 255")
        print("  You will appear ONLINE to all players")
        print("")
        
        return True
        
    except Exception as e:
        print("[ERROR] Database error: " + str(e))
        return False

if __name__ == "__main__":
    print("="*60)
    print("  WONDERLAND ONLINE - GM ACCOUNT CREATOR")
    print("="*60)
    print("")
    
    success = create_gm_account()
    
    if not success:
        print("")
        print("[FAILED] Could not create GM account")
        print("Check the database path and try again")
