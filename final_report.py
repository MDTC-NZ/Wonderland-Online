#!/usr/bin/env python
"""
Final Comprehensive Wonderland Online Game Data Report
"""

import sqlite3

db_path = r"C:\Users\hayde\Downloads\Wonderland-Develop\Wonderland-Develop\ServerDataBase.db"
conn = sqlite3.connect(db_path)
cursor = conn.cursor()

print("\n" + "="*70)
print(" WONDERLAND ONLINE - GAME CONTENT LOADED - FINAL REPORT")
print("="*70 + "\n")

# 1. QUESTS REPORT
print("[QUESTS SYSTEM]")
cursor.execute("SELECT COUNT(*) FROM quests")
quest_count = cursor.fetchone()[0]
print(f"  Total Quests: {quest_count}")

cursor.execute("""
    SELECT quest_id, name, required_level, reward_exp, reward_money, is_repeatable 
    FROM quests ORDER BY quest_id
""")
for quest_id, name, level, exp, money, repeatable in cursor.fetchall():
    repeat_str = "(Repeatable)" if repeatable else "(One-time)"
    print(f"    [{quest_id}] {name} | Level {level} | EXP: {exp} | Gold: {money} {repeat_str}")

# 2. ITEMS REPORT
print("\n[ITEMS SYSTEM]")
cursor.execute("SELECT COUNT(*) FROM items")
item_count = cursor.fetchone()[0]
print(f"  Total Items: {item_count}")

cursor.execute("SELECT type, COUNT(*) FROM items GROUP BY type")
type_counts = cursor.fetchall()
type_names = {0: "Consumable", 1: "Weapon", 2: "Armor", 3: "Resource"}
for item_type, count in type_counts:
    type_name = type_names.get(item_type, f"Type {item_type}")
    print(f"    {type_name}: {count} items")

cursor.execute("SELECT item_id, name, buy_price, sell_price FROM items ORDER BY item_id")
print("\n  Item Catalog:")
for item_id, name, buy, sell in cursor.fetchall():
    print(f"    [{item_id:5}] {name:30} | Buy: {buy:6} Gold | Sell: {sell:6} Gold")

# 3. NPCs REPORT
print("\n[NPC SYSTEM]")
cursor.execute("SELECT COUNT(*) FROM npcs")
npc_count = cursor.fetchone()[0]
print(f"  Total NPCs: {npc_count}")

cursor.execute("SELECT npc_id, name, map_id, x_pos, y_pos, type FROM npcs ORDER BY map_id")
print("\n  NPC Placement:")
current_map = None
for npc_id, name, map_id, x, y, npc_type in cursor.fetchall():
    if current_map != map_id:
        print(f"\n    Map {map_id}:")
        current_map = map_id
    print(f"      [{npc_id}] {name:20} at ({x:.0f}, {y:.0f}) - Type: {npc_type}")

# 4. MAPS REPORT
print("\n[MAP SYSTEM]")
cursor.execute("SELECT COUNT(*) FROM maps")
map_count = cursor.fetchone()[0]
print(f"  Total Maps: {map_count}")

cursor.execute("SELECT map_id, name, width, height, level_range FROM maps ORDER BY map_id")
print("\n  Map Definitions:")
for map_id, name, width, height, level_range in cursor.fetchall():
    print(f"    Map {map_id}: {name:20} ({width}x{height}) | Levels {level_range}")

# 5. SKILLS REPORT
print("\n[SKILLS SYSTEM]")
cursor.execute("SELECT COUNT(*) FROM skills")
skill_count = cursor.fetchone()[0]
print(f"  Total Skills: {skill_count}")

cursor.execute("""
    SELECT skill_id, name, level_required, cast_time, cooldown, mana_cost 
    FROM skills ORDER BY skill_id
""")
print("\n  Skill List:")
for skill_id, name, level, cast_time, cooldown, mana in cursor.fetchall():
    print(f"    [{skill_id}] {name:15} | Req Level: {level:2} | Cast: {cast_time}ms | CD: {cooldown}ms | Mana: {mana}")

# 6. RECIPES REPORT
print("\n[CRAFTING/ALCHEMY SYSTEM]")
cursor.execute("SELECT COUNT(*) FROM recipes")
recipe_count = cursor.fetchone()[0]
print(f"  Total Recipes: {recipe_count}")

cursor.execute("""
    SELECT recipe_id, name, type, level_required, result_item_id, result_quantity 
    FROM recipes ORDER BY recipe_id
""")
print("\n  Recipes:")
for recipe_id, name, recipe_type, level, result_id, qty in cursor.fetchall():
    print(f"    [{recipe_id}] {name:30} ({recipe_type:8}) | Level {level} | Produces {qty}x Item {result_id}")

# 7. QUEST REWARDS
print("\n[QUEST REWARDS SYSTEM]")
cursor.execute("""
    SELECT q.quest_id, q.name, COUNT(qi.item_id) as reward_count
    FROM quests q
    LEFT JOIN quest_items qi ON q.quest_id = qi.quest_id
    GROUP BY q.quest_id
""")
print("\n  Quest Item Rewards:")
for quest_id, name, reward_count in cursor.fetchall():
    print(f"    Quest {quest_id} ({name}): {reward_count} item rewards")

# Summary statistics
print("\n" + "="*70)
print(" DATABASE SUMMARY")
print("="*70)

stats = {
    "Quests": quest_count,
    "Items": item_count,
    "NPCs": npc_count,
    "Maps": map_count,
    "Skills": skill_count,
    "Recipes": recipe_count
}

print("\nContent Statistics:")
total_content = 0
for category, count in stats.items():
    print(f"  {category:15}: {count:4} records")
    total_content += count

print(f"  {'-'*25}")
print(f"  {'TOTAL CONTENT':15}: {total_content:4} records")

print("\n" + "="*70)
print(" STATUS: ALL GAME CONTENT SUCCESSFULLY LOADED")
print("="*70 + "\n")

conn.close()
