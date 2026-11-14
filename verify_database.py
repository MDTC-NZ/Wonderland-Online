import sqlite3

db_path = r"C:\Users\hayde\Downloads\Wonderland-Develop\Wonderland-Develop\ServerDataBase.db"
conn = sqlite3.connect(db_path)
cursor = conn.cursor()

print("\n=== DATABASE CONTENT VERIFICATION ===\n")

# Count records in each table
tables = ["quests", "items", "npcs", "skills", "recipes", "maps"]
for table in tables:
    try:
        cursor.execute(f"SELECT COUNT(*) FROM {table}")
        count = cursor.fetchone()[0]
        print(f"  {table}: {count} records")
    except:
        print(f"  {table}: (not available)")

# Sample items
print("\n=== SAMPLE ITEMS ===")
cursor.execute("SELECT item_id, name, type FROM items LIMIT 5")
for row in cursor.fetchall():
    print(f"  Item {row[0]}: {row[1]} (Type: {row[2]})")

# Sample quests
print("\n=== SAMPLE QUESTS ===")
cursor.execute("SELECT quest_id, name, required_level FROM quests LIMIT 3")
for row in cursor.fetchall():
    print(f"  Quest {row[0]}: {row[1]} (Level {row[2]})")

# Sample NPCs
print("\n=== SAMPLE NPCs ===")
cursor.execute("SELECT npc_id, name, type FROM npcs LIMIT 5")
for row in cursor.fetchall():
    print(f"  NPC {row[0]}: {row[1]} ({row[2]})")

# Sample maps
print("\n=== MAPS ===")
cursor.execute("SELECT map_id, name FROM maps")
for row in cursor.fetchall():
    print(f"  Map {row[0]}: {row[1]}")

conn.close()
print("\n")
