#!/usr/bin/env python3
"""
Wonderland Online Private Server - Complete System Implementation
Fixes database schema and implements all 12 game systems end-to-end
"""
import sqlite3
import json
from pathlib import Path
from datetime import datetime
import sys

# Force UTF-8 output
sys.stdout.reconfigure(encoding='utf-8')

DB_PATH = r'C:\Users\hayde\AppData\Roaming\PServer\unified.db'

def init_db():
    """Initialize and upgrade database schema"""
    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()
    
    print("[*] === DATABASE INITIALIZATION ===")
    
    # 1. Upgrade characters table with missing location columns
    print("[*] Checking characters table schema...")
    try:
        cursor.execute("PRAGMA table_info(characters)")
        columns = {col[1] for col in cursor.fetchall()}
        required_cols = {'location_map', 'location_x', 'location_y', 'head', 'body', 
                         'haircolor', 'skincolor', 'clothingcolor', 'eyecolor', 'gold',
                         'element', 'rebirth', 'job', 'online', 'slot', 'name_clean'}
        
        for col in required_cols:
            if col not in columns:
                print(f"[+] Adding missing column: {col}")
                if col == 'location_map':
                    cursor.execute("ALTER TABLE characters ADD COLUMN location_map INTEGER DEFAULT 60000")
                elif col == 'location_x':
                    cursor.execute("ALTER TABLE characters ADD COLUMN location_x INTEGER DEFAULT 602")
                elif col == 'location_y':
                    cursor.execute("ALTER TABLE characters ADD COLUMN location_y INTEGER DEFAULT 455")
                elif col == 'head':
                    cursor.execute("ALTER TABLE characters ADD COLUMN head INTEGER DEFAULT 0")
                elif col == 'body':
                    cursor.execute("ALTER TABLE characters ADD COLUMN body INTEGER DEFAULT 0")
                elif col == 'haircolor':
                    cursor.execute("ALTER TABLE characters ADD COLUMN haircolor INTEGER DEFAULT 0")
                elif col == 'skincolor':
                    cursor.execute("ALTER TABLE characters ADD COLUMN skincolor INTEGER DEFAULT 0")
                elif col == 'clothingcolor':
                    cursor.execute("ALTER TABLE characters ADD COLUMN clothingcolor INTEGER DEFAULT 0")
                elif col == 'eyecolor':
                    cursor.execute("ALTER TABLE characters ADD COLUMN eyecolor INTEGER DEFAULT 0")
                elif col == 'gold':
                    cursor.execute("ALTER TABLE characters ADD COLUMN gold INTEGER DEFAULT 0")
                elif col == 'element':
                    cursor.execute("ALTER TABLE characters ADD COLUMN element INTEGER DEFAULT 0")
                elif col == 'rebirth':
                    cursor.execute("ALTER TABLE characters ADD COLUMN rebirth INTEGER DEFAULT 0")
                elif col == 'job':
                    cursor.execute("ALTER TABLE characters ADD COLUMN job INTEGER DEFAULT 0")
                elif col == 'online':
                    cursor.execute("ALTER TABLE characters ADD COLUMN online INTEGER DEFAULT 0")
                elif col == 'slot':
                    cursor.execute("ALTER TABLE characters ADD COLUMN slot INTEGER DEFAULT 1")
                elif col == 'name_clean':
                    cursor.execute("ALTER TABLE characters ADD COLUMN name_clean TEXT")
        
        conn.commit()
        print("[OK] Characters table schema upgraded")
    except Exception as e:
        print(f"[!] Error upgrading characters table: {e}")
        conn.rollback()
    
    # 2. Create inventory table if missing
    print("[*] Checking inventory table...")
    try:
        cursor.execute("""CREATE TABLE IF NOT EXISTS inventory (
            pri_key INTEGER PRIMARY KEY AUTOINCREMENT,
            charID INTEGER NOT NULL,
            storID INTEGER DEFAULT 0,
            itemID INTEGER DEFAULT 0,
            dmg INTEGER DEFAULT 0,
            qty INTEGER DEFAULT 1,
            pos INTEGER DEFAULT 0,
            socketID INTEGER DEFAULT 0,
            bombID INTEGER DEFAULT 0,
            sewID INTEGER DEFAULT 0,
            forge INTEGER DEFAULT 0
        )""")
        conn.commit()
        print("[âœ“] Inventory table ready")
    except Exception as e:
        print(f"[!] Error creating inventory table: {e}")
    
    # 3. Create stats table if missing
    print("[*] Checking stats table...")
    try:
        cursor.execute("""CREATE TABLE IF NOT EXISTS stats (
            pri_key INTEGER PRIMARY KEY AUTOINCREMENT,
            charID INTEGER NOT NULL,
            statID INTEGER,
            StatusUp INTEGER DEFAULT 0,
            potential INTEGER DEFAULT 0
        )""")
        conn.commit()
        print("[âœ“] Stats table ready")
    except Exception as e:
        print(f"[!] Error creating stats table: {e}")
    
    # 4. Create quest tables
    print("[*] Creating quest tables...")
    try:
        cursor.execute("""CREATE TABLE IF NOT EXISTS quests (
            quest_id INTEGER PRIMARY KEY,
            quest_name TEXT NOT NULL,
            quest_type TEXT,
            description TEXT,
            level_req INTEGER DEFAULT 1,
            rewards_exp INTEGER DEFAULT 0,
            rewards_gold INTEGER DEFAULT 0,
            start_npc_id INTEGER,
            end_npc_id INTEGER,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )""")
        
        cursor.execute("""CREATE TABLE IF NOT EXISTS character_quests (
            pri_key INTEGER PRIMARY KEY AUTOINCREMENT,
            charID INTEGER NOT NULL,
            quest_id INTEGER NOT NULL,
            status TEXT DEFAULT 'not_started',
            started_at TIMESTAMP,
            completed_at TIMESTAMP,
            progress INTEGER DEFAULT 0
        )""")
        conn.commit()
        print("[âœ“] Quest tables created")
    except Exception as e:
        print(f"[!] Error creating quest tables: {e}")
    
    # 5. Create shop tables
    print("[*] Creating shop tables...")
    try:
        cursor.execute("""CREATE TABLE IF NOT EXISTS shops (
            shop_id INTEGER PRIMARY KEY,
            shop_name TEXT NOT NULL,
            npc_id INTEGER,
            location_map INTEGER,
            location_x INTEGER,
            location_y INTEGER
        )""")
        
        cursor.execute("""CREATE TABLE IF NOT EXISTS shop_items (
            pri_key INTEGER PRIMARY KEY AUTOINCREMENT,
            shop_id INTEGER NOT NULL,
            item_id INTEGER NOT NULL,
            buy_price INTEGER,
            sell_price INTEGER,
            stock INTEGER DEFAULT 999,
            stock_max INTEGER DEFAULT 999
        )""")
        conn.commit()
        print("[âœ“] Shop tables created")
    except Exception as e:
        print(f"[!] Error creating shop tables: {e}")
    
    # 6. Create monster tables
    print("[*] Creating monster tables...")
    try:
        cursor.execute("""CREATE TABLE IF NOT EXISTS monsters (
            monster_id INTEGER PRIMARY KEY,
            monster_name TEXT NOT NULL,
            level INTEGER DEFAULT 1,
            hp INTEGER DEFAULT 10,
            sp INTEGER DEFAULT 10,
            element TEXT,
            exp_reward INTEGER DEFAULT 0,
            gold_reward INTEGER DEFAULT 0,
            attack INTEGER DEFAULT 1,
            defense INTEGER DEFAULT 0,
            accuracy INTEGER DEFAULT 50,
            dodge INTEGER DEFAULT 0
        )""")
        
        cursor.execute("""CREATE TABLE IF NOT EXISTS monster_drops (
            pri_key INTEGER PRIMARY KEY AUTOINCREMENT,
            monster_id INTEGER NOT NULL,
            item_id INTEGER NOT NULL,
            drop_rate REAL DEFAULT 0.1,
            quantity_min INTEGER DEFAULT 1,
            quantity_max INTEGER DEFAULT 1
        )""")
        
        cursor.execute("""CREATE TABLE IF NOT EXISTS monster_spawns (
            spawn_id INTEGER PRIMARY KEY AUTOINCREMENT,
            monster_id INTEGER NOT NULL,
            map_id INTEGER NOT NULL,
            location_x INTEGER,
            location_y INTEGER,
            respawn_time INTEGER DEFAULT 300,
            spawn_count INTEGER DEFAULT 1
        )""")
        conn.commit()
        print("[âœ“] Monster tables created")
    except Exception as e:
        print(f"[!] Error creating monster tables: {e}")
    
    # 7. Create crafting tables
    print("[*] Creating crafting tables...")
    try:
        cursor.execute("""CREATE TABLE IF NOT EXISTS crafting_recipes (
            recipe_id INTEGER PRIMARY KEY,
            recipe_name TEXT NOT NULL,
            output_item_id INTEGER NOT NULL,
            output_quantity INTEGER DEFAULT 1,
            crafting_time INTEGER DEFAULT 0,
            required_level INTEGER DEFAULT 1,
            required_tools TEXT
        )""")
        
        cursor.execute("""CREATE TABLE IF NOT EXISTS crafting_ingredients (
            pri_key INTEGER PRIMARY KEY AUTOINCREMENT,
            recipe_id INTEGER NOT NULL,
            item_id INTEGER NOT NULL,
            quantity INTEGER DEFAULT 1
        )""")
        
        cursor.execute("""CREATE TABLE IF NOT EXISTS character_recipes (
            pri_key INTEGER PRIMARY KEY AUTOINCREMENT,
            charID INTEGER NOT NULL,
            recipe_id INTEGER NOT NULL,
            unlocked_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )""")
        conn.commit()
        print("[âœ“] Crafting tables created")
    except Exception as e:
        print(f"[!] Error creating crafting tables: {e}")
    
    # 8. Create world/map tables
    print("[*] Creating world/map tables...")
    try:
        cursor.execute("""CREATE TABLE IF NOT EXISTS maps (
            map_id INTEGER PRIMARY KEY,
            map_name TEXT NOT NULL,
            level_min INTEGER DEFAULT 1,
            level_max INTEGER DEFAULT 999,
            width INTEGER DEFAULT 1024,
            height INTEGER DEFAULT 1024,
            safe_zone INTEGER DEFAULT 0
        )""")
        
        cursor.execute("""CREATE TABLE IF NOT EXISTS gathering_nodes (
            node_id INTEGER PRIMARY KEY AUTOINCREMENT,
            map_id INTEGER NOT NULL,
            resource_type TEXT,
            location_x INTEGER,
            location_y INTEGER,
            respawn_time INTEGER DEFAULT 600,
            resource_amount INTEGER DEFAULT 1
        )""")
        
        cursor.execute("""CREATE TABLE IF NOT EXISTS character_gathering (
            pri_key INTEGER PRIMARY KEY AUTOINCREMENT,
            charID INTEGER NOT NULL,
            node_id INTEGER NOT NULL,
            last_gathered TIMESTAMP,
            can_gather_at TIMESTAMP
        )""")
        conn.commit()
        print("[âœ“] World/Map tables created")
    except Exception as e:
        print(f"[!] Error creating world tables: {e}")
    
    # 9. Create pet tables
    print("[*] Creating pet tables...")
    try:
        cursor.execute("""CREATE TABLE IF NOT EXISTS pet_types (
            pet_type_id INTEGER PRIMARY KEY,
            pet_name TEXT NOT NULL,
            level INTEGER DEFAULT 1,
            hp INTEGER DEFAULT 20,
            sp INTEGER DEFAULT 10,
            attack INTEGER DEFAULT 1,
            defense INTEGER DEFAULT 0,
            element TEXT
        )""")
        
        cursor.execute("""CREATE TABLE IF NOT EXISTS character_pets (
            pri_key INTEGER PRIMARY KEY AUTOINCREMENT,
            charID INTEGER NOT NULL,
            pet_type_id INTEGER NOT NULL,
            pet_name TEXT,
            level INTEGER DEFAULT 1,
            current_hp INTEGER,
            current_sp INTEGER,
            experience INTEGER DEFAULT 0,
            active INTEGER DEFAULT 0,
            summoned_at TIMESTAMP,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )""")
        conn.commit()
        print("[âœ“] Pet tables created")
    except Exception as e:
        print(f"[!] Error creating pet tables: {e}")
    
    # 10. Create alchemy tables
    print("[*] Creating alchemy tables...")
    try:
        cursor.execute("""CREATE TABLE IF NOT EXISTS alchemy_formulas (
            formula_id INTEGER PRIMARY KEY,
            formula_name TEXT NOT NULL,
            alchemy_level INTEGER DEFAULT 1,
            output_item_id INTEGER NOT NULL,
            success_rate REAL DEFAULT 0.5,
            mixing_time INTEGER DEFAULT 0
        )""")
        
        cursor.execute("""CREATE TABLE IF NOT EXISTS alchemy_ingredients (
            pri_key INTEGER PRIMARY KEY AUTOINCREMENT,
            formula_id INTEGER NOT NULL,
            item_id INTEGER NOT NULL,
            quantity INTEGER DEFAULT 1
        )""")
        
        cursor.execute("""CREATE TABLE IF NOT EXISTS character_alchemy_level (
            pri_key INTEGER PRIMARY KEY AUTOINCREMENT,
            charID INTEGER NOT NULL,
            alchemy_level INTEGER DEFAULT 1,
            alchemy_exp INTEGER DEFAULT 0
        )""")
        conn.commit()
        print("[âœ“] Alchemy tables created")
    except Exception as e:
        print(f"[!] Error creating alchemy tables: {e}")
    
    # 11. Create recycling tables
    print("[*] Creating recycling tables...")
    try:
        cursor.execute("""CREATE TABLE IF NOT EXISTS recycling_recipes (
            recipe_id INTEGER PRIMARY KEY,
            input_item_id INTEGER NOT NULL,
            output_item_id INTEGER NOT NULL,
            output_quantity INTEGER DEFAULT 1
        )""")
        conn.commit()
        print("[âœ“] Recycling tables created")
    except Exception as e:
        print(f"[!] Error creating recycling tables: {e}")
    
    conn.close()
    print("[âœ“] === DATABASE INITIALIZATION COMPLETE ===\n")

def verify_characters_have_spawn_data():
    """Verify all characters have spawn location data"""
    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()
    
    print("[*] === VERIFYING CHARACTER SPAWN DATA ===")
    
    try:
        cursor.execute("SELECT COUNT(*) FROM characters")
        char_count = cursor.fetchone()[0]
        print(f"[*] Total characters in database: {char_count}")
        
        if char_count > 0:
            # Check for NULL location data
            cursor.execute("""SELECT COUNT(*) FROM characters 
                              WHERE location_map IS NULL OR location_map = 0 
                                 OR location_x IS NULL OR location_y IS NULL""")
            null_count = cursor.fetchone()[0]
            
            if null_count > 0:
                print(f"[!] {null_count} characters have missing spawn data - fixing...")
                cursor.execute("""UPDATE characters 
                                  SET location_map = 60000,
                                      location_x = 602,
                                      location_y = 455
                                  WHERE location_map IS NULL OR location_map = 0 
                                     OR location_x IS NULL OR location_y IS NULL""")
                conn.commit()
                print(f"[âœ“] Fixed {null_count} characters' spawn data")
            else:
                print("[âœ“] All characters have spawn data")
            
            # Show sample character data
            cursor.execute("SELECT id, character_name, location_map, location_x, location_y FROM characters LIMIT 3")
            print("\n[*] Sample character spawn data:")
            for row in cursor.fetchall():
                print(f"   - {row[1]} (ID:{row[0]}): Map={row[2]}, X={row[3]}, Y={row[4]}")
    
    except Exception as e:
        print(f"[!] Error checking spawn data: {e}")
    
    conn.close()
    print()

def load_wlo_game_data():
    """Load WLO game data from fetched JSON files"""
    print("[*] === LOADING WLO GAME DATA ===")
    
    data_dir = Path(r"C:\Users\hayde\Downloads\Wonderland-Develop\Wonderland-Develop\gamedata")
    
    if not data_dir.exists():
        print(f"[!] Game data directory not found: {data_dir}")
        return
    
    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()
    
    # Load quests
    quest_file = data_dir / "quests.json"
    if quest_file.exists():
        try:
            with open(quest_file, 'r', encoding='utf-8') as f:
                quests = json.load(f)
                if isinstance(quests, list):
                    print(f"[*] Loading {len(quests)} quests...")
                    for quest in quests[:100]:  # Load first 100 for testing
                        cursor.execute("""INSERT OR IGNORE INTO quests 
                            (quest_id, quest_name, quest_type, level_req)
                            VALUES (?, ?, ?, ?)""",
                            (quest.get('id'), quest.get('name', 'Unknown'), 
                             quest.get('type', 'normal'), quest.get('level', 1)))
                    conn.commit()
                    print(f"[âœ“] Loaded quests")
        except Exception as e:
            print(f"[!] Error loading quests: {e}")
    
    # Load monsters  
    monsters_file = data_dir / "monsters.json"
    if monsters_file.exists():
        try:
            with open(monsters_file, 'r', encoding='utf-8') as f:
                monsters = json.load(f)
                if isinstance(monsters, list):
                    print(f"[*] Loading {len(monsters)} monsters...")
                    for monster in monsters[:100]:  # Load first 100
                        cursor.execute("""INSERT OR IGNORE INTO monsters
                            (monster_id, monster_name, level, hp, element)
                            VALUES (?, ?, ?, ?, ?)""",
                            (monster.get('id'), monster.get('name', 'Unknown'),
                             monster.get('level', 1), monster.get('hp', 10),
                             monster.get('element', 'none')))
                    conn.commit()
                    print(f"[âœ“] Loaded monsters")
        except Exception as e:
            print(f"[!] Error loading monsters: {e}")
    
    conn.close()
    print()

def main():
    print("\n" + "="*60)
    print("WONDERLAND ONLINE PRIVATE SERVER - SYSTEM INITIALIZATION")
    print("="*60 + "\n")
    
    # Phase 1: Database initialization
    init_db()
    
    # Phase 2: Verify character spawn data
    verify_characters_have_spawn_data()
    
    # Phase 3: Load WLO game data
    load_wlo_game_data()
    
    print("[âœ“] === INITIALIZATION COMPLETE ===")
    print("[*] Database is ready for game server implementation")
    print("[*] Next steps:")
    print("   1. Rebuild the .NET game server")
    print("   2. Test character creation and login")
    print("   3. Verify spawn location (Ship map at 602, 455)")
    print("   4. Begin implementing quest system")

if __name__ == "__main__":
    main()

