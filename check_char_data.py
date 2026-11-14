import sqlite3

conn = sqlite3.connect(r'C:\Users\hayde\AppData\Roaming\PServer\unified.db')
cursor = conn.cursor()

# Get sample character data
print("=== Sample Characters ===")
cursor.execute("SELECT * FROM characters LIMIT 2")
cols = [description[0] for description in cursor.description]
rows = cursor.fetchall()

print("Columns:", cols)
for row in rows:
    print(f"Row: {row}")

conn.close()
