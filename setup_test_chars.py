import sqlite3

conn = sqlite3.connect(r'C:\Users\hayde\AppData\Roaming\PServer\unified.db')
cursor = conn.cursor()

print("=== CURRENT USERS ===")
cursor.execute("SELECT * FROM users")
cols = [description[0] for description in cursor.description]
print(f"Columns: {cols}\n")
for row in cursor.fetchall():
    print(f"  User: {row}")

print("\n=== CREATING TEST CHARACTER ===")
# Create a character for the first user
cursor.execute("INSERT INTO characters (user_id, character_name, level, exp, hp, mp, "
               "location_map, location_x, location_y, head, body, haircolor, skincolor, "
               "clothingcolor, eyecolor, gold, element, slot, name_clean, rebirth, job) "
               "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
               (1, 'Adventurer', 1, 0, 100, 100,
                60000, 602, 455,  # Ship map spawn location
                0, 0, 44828, 6781, 44828, 6781, 0, 0, 1, 'adventurer', 0, 0))

conn.commit()

print("Test character created!")
print("\n=== VERIFY CHARACTER SPAWN DATA ===")
cursor.execute("SELECT id, character_name, location_map, location_x, location_y FROM characters")
for row in cursor.fetchall():
    print(f"  {row[1]:20} Map={row[2]:5} X={row[3]:4} Y={row[4]:4}")

conn.close()
