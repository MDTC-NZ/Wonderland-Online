import sqlite3

conn = sqlite3.connect(r'C:\Users\hayde\AppData\Roaming\PServer\unified.db')
cursor = conn.cursor()

# Get all tables
cursor.execute("SELECT name FROM sqlite_master WHERE type='table'")
tables = cursor.fetchall()

print("=== All tables ===")
for table in tables:
    print(f"  {table[0]}")

# Check character/player table structure
print("\n=== Checking character storage ===")
cursor.execute("PRAGMA table_info(characters)")
cols = cursor.fetchall()
if cols:
    print("Characters table columns:")
    for col in cols:
        print(f"  {col[1]} ({col[2]})")
else:
    # Try other names
    cursor.execute("PRAGMA table_info(players)")
    cols = cursor.fetchall()
    if cols:
        print("Players table columns:")
        for col in cols:
            print(f"  {col[1]} ({col[2]})")
    else:
        # List all columns from first table
        for table_name in tables:
            print(f"\n{table_name[0]} columns:")
            cursor.execute(f"PRAGMA table_info({table_name[0]})")
            cols = cursor.fetchall()
            for col in cols:
                print(f"  {col[1]} ({col[2]})")

conn.close()
