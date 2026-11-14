import sqlite3

conn = sqlite3.connect(r'C:\Users\hayde\AppData\Roaming\PServer\unified.db')
cursor = conn.cursor()

print("=== CHARACTERS TABLE SCHEMA ===")
cursor.execute("PRAGMA table_info(characters)")
cols = cursor.fetchall()
for col in cols:
    print(f"  {col[1]:20} {col[2]}")

print("\n=== CHECKING OTHER TABLES ===")
cursor.execute("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name")
tables = cursor.fetchall()
for table in tables:
    cursor.execute(f"SELECT COUNT(*) FROM {table[0]}")
    count = cursor.fetchone()[0]
    print(f"  {table[0]:30} ({count} rows)")

conn.close()
