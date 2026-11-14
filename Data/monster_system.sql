-- Wonderland Online Monster System Schema
-- Monster definitions, spawning, AI, and loot drops

-- Monster template definitions
CREATE TABLE IF NOT EXISTS monster_templates (
    monster_id INTEGER PRIMARY KEY,
    monster_name TEXT NOT NULL,
    monster_description TEXT,
    monster_type TEXT CHECK(monster_type IN ('normal', 'elite', 'boss', 'world_boss', 'dungeon_boss')),
    monster_model_id INTEGER NOT NULL,
    level INTEGER NOT NULL,
    experience_reward INTEGER NOT NULL,
    money_reward INTEGER NOT NULL,
    max_health INTEGER NOT NULL,
    armor INTEGER DEFAULT 0,
    attack_power INTEGER NOT NULL,
    attack_speed REAL DEFAULT 1.0,
    defense INTEGER DEFAULT 0,
    is_aggressive BOOLEAN DEFAULT 1,
    attack_range REAL DEFAULT 5.0,
    ai_type TEXT CHECK(ai_type IN ('passive', 'aggressive', 'mixed', 'patrol', 'boss')) DEFAULT 'aggressive',
    size_scale REAL DEFAULT 1.0,
    movement_speed REAL DEFAULT 5.0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Monster spawn points
CREATE TABLE IF NOT EXISTS monster_spawn_points (
    spawn_id INTEGER PRIMARY KEY AUTOINCREMENT,
    monster_id INTEGER NOT NULL,
    map_id INTEGER NOT NULL,
    x_coordinate REAL NOT NULL,
    y_coordinate REAL NOT NULL,
    z_coordinate REAL NOT NULL,
    spawn_radius INTEGER DEFAULT 10,
    spawn_interval_seconds INTEGER DEFAULT 300,
    max_instances INTEGER DEFAULT 5,
    spawn_time_start INTEGER DEFAULT 0,
    spawn_time_end INTEGER DEFAULT 24,
    is_active BOOLEAN DEFAULT 1,
    last_spawn TIMESTAMP,
    current_count INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (monster_id) REFERENCES monster_templates(monster_id) ON DELETE CASCADE
);

-- Active monsters in world
CREATE TABLE IF NOT EXISTS active_monsters (
    instance_id INTEGER PRIMARY KEY AUTOINCREMENT,
    monster_id INTEGER NOT NULL,
    spawn_id INTEGER NOT NULL,
    map_id INTEGER NOT NULL,
    x_coordinate REAL NOT NULL,
    y_coordinate REAL NOT NULL,
    z_coordinate REAL NOT NULL,
    current_health INTEGER NOT NULL,
    max_health INTEGER NOT NULL,
    status TEXT CHECK(status IN ('alive', 'dead', 'despawning')),
    spawned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_activity TIMESTAMP,
    killer_character_id INTEGER,
    death_time TIMESTAMP,
    FOREIGN KEY (monster_id) REFERENCES monster_templates(monster_id) ON DELETE CASCADE,
    FOREIGN KEY (spawn_id) REFERENCES monster_spawn_points(spawn_id) ON DELETE CASCADE
);

-- Monster loot drops
CREATE TABLE IF NOT EXISTS monster_loot_drops (
    loot_id INTEGER PRIMARY KEY AUTOINCREMENT,
    monster_id INTEGER NOT NULL,
    item_id INTEGER NOT NULL,
    drop_chance REAL NOT NULL,
    min_quantity INTEGER DEFAULT 1,
    max_quantity INTEGER DEFAULT 1,
    min_level INTEGER DEFAULT 1,
    max_level INTEGER DEFAULT 200,
    is_rare_drop BOOLEAN DEFAULT 0,
    rarity_chance REAL DEFAULT 0.05,
    FOREIGN KEY (monster_id) REFERENCES monster_templates(monster_id) ON DELETE CASCADE
);

-- Monster skill definitions
CREATE TABLE IF NOT EXISTS monster_skills (
    monster_skill_id INTEGER PRIMARY KEY AUTOINCREMENT,
    monster_id INTEGER NOT NULL,
    skill_id INTEGER NOT NULL,
    skill_level INTEGER DEFAULT 1,
    cast_probability REAL DEFAULT 0.5,
    cooldown_seconds INTEGER DEFAULT 30,
    min_distance REAL DEFAULT 0.0,
    max_distance REAL DEFAULT 100.0,
    FOREIGN KEY (monster_id) REFERENCES monster_templates(monster_id) ON DELETE CASCADE
);

-- Monster AI behaviors
CREATE TABLE IF NOT EXISTS monster_ai_behaviors (
    behavior_id INTEGER PRIMARY KEY AUTOINCREMENT,
    monster_id INTEGER NOT NULL,
    behavior_type TEXT CHECK(behavior_type IN ('idle', 'patrol', 'pursue', 'attack', 'retreat', 'flee')),
    trigger_condition TEXT,
    trigger_value INTEGER,
    action_sequence TEXT, -- JSON
    priority INTEGER DEFAULT 0,
    is_active BOOLEAN DEFAULT 1,
    FOREIGN KEY (monster_id) REFERENCES monster_templates(monster_id) ON DELETE CASCADE
);

-- Monster combat log
CREATE TABLE IF NOT EXISTS monster_combat_log (
    combat_log_id INTEGER PRIMARY KEY AUTOINCREMENT,
    instance_id INTEGER NOT NULL,
    character_id INTEGER,
    action_type TEXT CHECK(action_type IN ('attack', 'take_damage', 'skill_use', 'death')),
    damage_amount INTEGER,
    action_timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (instance_id) REFERENCES active_monsters(instance_id) ON DELETE CASCADE
);

-- Monster respawn history
CREATE TABLE IF NOT EXISTS monster_respawn_history (
    respawn_id INTEGER PRIMARY KEY AUTOINCREMENT,
    spawn_id INTEGER NOT NULL,
    monster_id INTEGER NOT NULL,
    spawn_time TIMESTAMP,
    respawn_time TIMESTAMP,
    was_killed BOOLEAN DEFAULT 0,
    FOREIGN KEY (spawn_id) REFERENCES monster_spawn_points(spawn_id) ON DELETE CASCADE,
    FOREIGN KEY (monster_id) REFERENCES monster_templates(monster_id) ON DELETE CASCADE
);

-- Monster summoning (player-summoned monsters)
CREATE TABLE IF NOT EXISTS monster_summoned (
    summoned_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL,
    monster_id INTEGER NOT NULL,
    instance_id INTEGER,
    summoned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    duration_seconds INTEGER,
    dismissal_time TIMESTAMP,
    FOREIGN KEY (monster_id) REFERENCES monster_templates(monster_id) ON DELETE CASCADE
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_monster_templates_level ON monster_templates(level);
CREATE INDEX IF NOT EXISTS idx_monster_templates_type ON monster_templates(monster_type);
CREATE INDEX IF NOT EXISTS idx_monster_spawn_points_monster_id ON monster_spawn_points(monster_id);
CREATE INDEX IF NOT EXISTS idx_monster_spawn_points_map_id ON monster_spawn_points(map_id);
CREATE INDEX IF NOT EXISTS idx_monster_spawn_points_active ON monster_spawn_points(is_active);
CREATE INDEX IF NOT EXISTS idx_active_monsters_map_id ON active_monsters(map_id);
CREATE INDEX IF NOT EXISTS idx_active_monsters_status ON active_monsters(status);
CREATE INDEX IF NOT EXISTS idx_active_monsters_killer ON active_monsters(killer_character_id);
CREATE INDEX IF NOT EXISTS idx_monster_loot_drops_monster_id ON monster_loot_drops(monster_id);
CREATE INDEX IF NOT EXISTS idx_monster_loot_drops_item_id ON monster_loot_drops(item_id);
CREATE INDEX IF NOT EXISTS idx_monster_skills_monster_id ON monster_skills(monster_id);
CREATE INDEX IF NOT EXISTS idx_monster_combat_log_instance ON monster_combat_log(instance_id);
CREATE INDEX IF NOT EXISTS idx_monster_combat_log_character ON monster_combat_log(character_id);
CREATE INDEX IF NOT EXISTS idx_monster_respawn_history_spawn ON monster_respawn_history(spawn_id);
