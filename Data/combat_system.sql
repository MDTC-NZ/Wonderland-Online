-- Wonderland Online Combat System Schema
-- Combat mechanics, damage, experience, and status effects

-- Combat sessions
CREATE TABLE IF NOT EXISTS combat_sessions (
    combat_id INTEGER PRIMARY KEY AUTOINCREMENT,
    initiator_character_id INTEGER NOT NULL,
    defender_character_id INTEGER,
    defender_monster_id INTEGER,
    map_id INTEGER NOT NULL,
    x_coordinate REAL,
    y_coordinate REAL,
    z_coordinate REAL,
    combat_status TEXT CHECK(combat_status IN ('active', 'ended', 'interrupted')),
    started_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    ended_at TIMESTAMP,
    duration_seconds INTEGER
);

-- Combat log entries
CREATE TABLE IF NOT EXISTS combat_log (
    log_entry_id INTEGER PRIMARY KEY AUTOINCREMENT,
    combat_id INTEGER NOT NULL,
    actor_type TEXT CHECK(actor_type IN ('character', 'monster')),
    actor_id INTEGER NOT NULL,
    action_type TEXT CHECK(action_type IN ('attack', 'skill', 'defend', 'dodge', 'parry', 'heal', 'buff', 'debuff', 'death')),
    skill_id INTEGER,
    damage_dealt INTEGER DEFAULT 0,
    damage_taken INTEGER DEFAULT 0,
    critical_hit BOOLEAN DEFAULT 0,
    accuracy_percentage INTEGER DEFAULT 100,
    dodge_chance BOOLEAN DEFAULT 0,
    action_timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (combat_id) REFERENCES combat_sessions(combat_id) ON DELETE CASCADE
);

-- Damage calculations (historical)
CREATE TABLE IF NOT EXISTS damage_calculations (
    calculation_id INTEGER PRIMARY KEY AUTOINCREMENT,
    combat_log_id INTEGER NOT NULL,
    attacker_level INTEGER,
    attacker_strength INTEGER,
    attacker_weapon_damage INTEGER,
    attacker_skill_bonus INTEGER,
    defender_level INTEGER,
    defender_armor INTEGER,
    defender_defense INTEGER,
    base_damage INTEGER,
    variance_multiplier REAL,
    final_damage INTEGER,
    calculation_timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (combat_log_id) REFERENCES combat_log(log_entry_id) ON DELETE CASCADE
);

-- Experience gained log
CREATE TABLE IF NOT EXISTS experience_log (
    experience_log_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL,
    combat_id INTEGER,
    experience_amount INTEGER NOT NULL,
    source_type TEXT CHECK(source_type IN ('monster_kill', 'quest', 'dungeon', 'skill_use', 'crafting', 'other')),
    source_id INTEGER,
    level_before INTEGER,
    level_after INTEGER,
    gained_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Character combat statistics
CREATE TABLE IF NOT EXISTS character_combat_stats (
    stat_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL UNIQUE,
    total_damage_dealt BIGINT DEFAULT 0,
    total_damage_received BIGINT DEFAULT 0,
    total_kills INTEGER DEFAULT 0,
    total_deaths INTEGER DEFAULT 0,
    total_combat_time_minutes INTEGER DEFAULT 0,
    critical_hit_count INTEGER DEFAULT 0,
    dodge_count INTEGER DEFAULT 0,
    last_updated TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Status effects (buffs/debuffs)
CREATE TABLE IF NOT EXISTS active_status_effects (
    effect_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER,
    monster_id INTEGER,
    effect_type TEXT CHECK(effect_type IN ('stun', 'slow', 'poison', 'bleed', 'freeze', 'burn', 'buff_damage', 'buff_defense', 'buff_speed')),
    effect_level INTEGER DEFAULT 1,
    effect_value INTEGER DEFAULT 0,
    source_id INTEGER,
    applied_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP,
    is_permanent BOOLEAN DEFAULT 0,
    applied_by_character_id INTEGER
);

-- Skill damage scaling
CREATE TABLE IF NOT EXISTS skill_damage_scaling (
    scaling_id INTEGER PRIMARY KEY AUTOINCREMENT,
    skill_id INTEGER NOT NULL,
    damage_base INTEGER NOT NULL,
    damage_per_level REAL DEFAULT 1.0,
    strength_scaling REAL DEFAULT 0.8,
    intelligence_scaling REAL DEFAULT 0.0,
    dexterity_scaling REAL DEFAULT 0.0,
    weapon_scaling REAL DEFAULT 1.0,
    critical_multiplier REAL DEFAULT 1.5,
    min_damage INTEGER DEFAULT 0,
    max_damage INTEGER DEFAULT 10000
);

-- PvP combat specific
CREATE TABLE IF NOT EXISTS pvp_combat_log (
    pvp_log_id INTEGER PRIMARY KEY AUTOINCREMENT,
    attacker_character_id INTEGER NOT NULL,
    defender_character_id INTEGER NOT NULL,
    damage_dealt INTEGER,
    combat_timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    result TEXT CHECK(result IN ('hit', 'miss', 'dodge', 'parry', 'critical'))
);

-- Death penalties/tracking
CREATE TABLE IF NOT EXISTS character_deaths (
    death_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL,
    killer_character_id INTEGER,
    killer_monster_id INTEGER,
    map_id INTEGER,
    death_location_x REAL,
    death_location_y REAL,
    death_location_z REAL,
    death_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    experience_lost INTEGER DEFAULT 0,
    item_dropped TEXT, -- JSON array of items
    resurrection_time TIMESTAMP,
    resurrection_location TEXT
);

-- Skill usage statistics
CREATE TABLE IF NOT EXISTS skill_usage_log (
    usage_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL,
    skill_id INTEGER NOT NULL,
    used_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    combat_id INTEGER,
    success BOOLEAN DEFAULT 1
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_combat_sessions_initiator ON combat_sessions(initiator_character_id);
CREATE INDEX IF NOT EXISTS idx_combat_sessions_defender_char ON combat_sessions(defender_character_id);
CREATE INDEX IF NOT EXISTS idx_combat_sessions_status ON combat_sessions(combat_status);
CREATE INDEX IF NOT EXISTS idx_combat_log_combat_id ON combat_log(combat_id);
CREATE INDEX IF NOT EXISTS idx_combat_log_actor ON combat_log(actor_type, actor_id);
CREATE INDEX IF NOT EXISTS idx_experience_log_character ON experience_log(character_id);
CREATE INDEX IF NOT EXISTS idx_experience_log_source ON experience_log(source_type);
CREATE INDEX IF NOT EXISTS idx_status_effects_character ON active_status_effects(character_id);
CREATE INDEX IF NOT EXISTS idx_status_effects_monster ON active_status_effects(monster_id);
CREATE INDEX IF NOT EXISTS idx_status_effects_expires ON active_status_effects(expires_at);
CREATE INDEX IF NOT EXISTS idx_pvp_combat_attacker ON pvp_combat_log(attacker_character_id);
CREATE INDEX IF NOT EXISTS idx_pvp_combat_defender ON pvp_combat_log(defender_character_id);
CREATE INDEX IF NOT EXISTS idx_character_deaths_character ON character_deaths(character_id);
CREATE INDEX IF NOT EXISTS idx_skill_usage_log_character ON skill_usage_log(character_id);
