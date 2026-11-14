-- Wonderland Online Pet System Schema
-- Pet ownership, combat, and progression

-- Pet templates
CREATE TABLE IF NOT EXISTS pet_templates (
    pet_id INTEGER PRIMARY KEY,
    pet_name TEXT NOT NULL,
    pet_description TEXT,
    pet_model_id INTEGER NOT NULL,
    pet_type TEXT CHECK(pet_type IN ('attack', 'defense', 'support', 'tank', 'healer', 'utility')),
    acquisition_method TEXT CHECK(acquisition_method IN ('quest_reward', 'purchase', 'wild_capture', 'breeding', 'event', 'summon')),
    base_level INTEGER DEFAULT 1,
    max_level INTEGER DEFAULT 100,
    base_experience_to_level_up INTEGER DEFAULT 1000,
    base_attack INTEGER NOT NULL,
    base_defense INTEGER NOT NULL,
    base_health INTEGER NOT NULL,
    growth_attack REAL DEFAULT 1.0,
    growth_defense REAL DEFAULT 1.0,
    growth_health REAL DEFAULT 1.0,
    attack_speed REAL DEFAULT 1.0,
    movement_speed REAL DEFAULT 5.0,
    size_scale REAL DEFAULT 1.0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Pet skills
CREATE TABLE IF NOT EXISTS pet_skills (
    pet_skill_id INTEGER PRIMARY KEY AUTOINCREMENT,
    pet_id INTEGER NOT NULL,
    skill_id INTEGER NOT NULL,
    skill_level INTEGER DEFAULT 1,
    learn_level INTEGER NOT NULL,
    cooldown_seconds INTEGER DEFAULT 10,
    FOREIGN KEY (pet_id) REFERENCES pet_templates(pet_id) ON DELETE CASCADE
);

-- Owned pets
CREATE TABLE IF NOT EXISTS owned_pets (
    owned_pet_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL,
    pet_id INTEGER NOT NULL,
    pet_name TEXT NOT NULL,
    current_level INTEGER DEFAULT 1,
    current_experience BIGINT DEFAULT 0,
    current_health INTEGER NOT NULL,
    max_health INTEGER NOT NULL,
    captured_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_activity TIMESTAMP,
    is_active BOOLEAN DEFAULT 0,
    is_in_combat BOOLEAN DEFAULT 0,
    pet_happiness INTEGER DEFAULT 100,
    pet_loyalty INTEGER DEFAULT 50,
    FOREIGN KEY (pet_id) REFERENCES pet_templates(pet_id) ON DELETE CASCADE
);

-- Pet stats (current)
CREATE TABLE IF NOT EXISTS pet_stats (
    stat_id INTEGER PRIMARY KEY AUTOINCREMENT,
    owned_pet_id INTEGER NOT NULL UNIQUE,
    attack_power INTEGER NOT NULL,
    defense_power INTEGER NOT NULL,
    health_points INTEGER NOT NULL,
    experience_total BIGINT DEFAULT 0,
    experience_next_level BIGINT DEFAULT 1000,
    skills_learned INTEGER DEFAULT 0,
    last_calculated TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (owned_pet_id) REFERENCES owned_pets(owned_pet_id) ON DELETE CASCADE
);

-- Pet abilities
CREATE TABLE IF NOT EXISTS pet_learned_skills (
    learned_skill_id INTEGER PRIMARY KEY AUTOINCREMENT,
    owned_pet_id INTEGER NOT NULL,
    skill_id INTEGER NOT NULL,
    learned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    times_used INTEGER DEFAULT 0,
    UNIQUE(owned_pet_id, skill_id),
    FOREIGN KEY (owned_pet_id) REFERENCES owned_pets(owned_pet_id) ON DELETE CASCADE
);

-- Pet combat log
CREATE TABLE IF NOT EXISTS pet_combat_log (
    combat_log_id INTEGER PRIMARY KEY AUTOINCREMENT,
    owned_pet_id INTEGER NOT NULL,
    character_id INTEGER NOT NULL,
    opponent_pet_id INTEGER,
    opponent_monster_id INTEGER,
    combat_duration_seconds INTEGER,
    pet_damage_dealt INTEGER,
    pet_damage_taken INTEGER,
    pet_won BOOLEAN DEFAULT 0,
    combat_timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (owned_pet_id) REFERENCES owned_pets(owned_pet_id) ON DELETE CASCADE
);

-- Pet breeding
CREATE TABLE IF NOT EXISTS pet_breeding (
    breeding_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL,
    parent_pet_1_id INTEGER NOT NULL,
    parent_pet_2_id INTEGER NOT NULL,
    breeding_status TEXT CHECK(breeding_status IN ('pending', 'breeding', 'ready', 'hatched')),
    breeding_start TIMESTAMP,
    expected_hatch_time TIMESTAMP,
    hatched_at TIMESTAMP,
    offspring_pet_id INTEGER,
    FOREIGN KEY (parent_pet_1_id) REFERENCES owned_pets(owned_pet_id) ON DELETE CASCADE,
    FOREIGN KEY (parent_pet_2_id) REFERENCES owned_pets(owned_pet_id) ON DELETE CASCADE
);

-- Pet items/equipment
CREATE TABLE IF NOT EXISTS pet_equipment (
    equipment_id INTEGER PRIMARY KEY AUTOINCREMENT,
    owned_pet_id INTEGER NOT NULL,
    pet_equipment_slot TEXT CHECK(pet_equipment_slot IN ('collar', 'armor', 'weapon', 'accessory')),
    item_id INTEGER NOT NULL,
    equipped_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(owned_pet_id, pet_equipment_slot),
    FOREIGN KEY (owned_pet_id) REFERENCES owned_pets(owned_pet_id) ON DELETE CASCADE
);

-- Pet statistics
CREATE TABLE IF NOT EXISTS pet_statistics (
    stat_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL UNIQUE,
    total_pets_owned INTEGER DEFAULT 0,
    pets_trained_to_max_level INTEGER DEFAULT 0,
    total_pet_combat_time_hours REAL DEFAULT 0.0,
    total_pet_victories INTEGER DEFAULT 0,
    total_offspring BIGINT DEFAULT 0
);

-- Pet food/care
CREATE TABLE IF NOT EXISTS pet_care_log (
    care_id INTEGER PRIMARY KEY AUTOINCREMENT,
    owned_pet_id INTEGER NOT NULL,
    care_type TEXT CHECK(care_type IN ('fed', 'played', 'groomed', 'trained', 'healed')),
    care_timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    happiness_change INTEGER DEFAULT 0,
    loyalty_change INTEGER DEFAULT 0,
    FOREIGN KEY (owned_pet_id) REFERENCES owned_pets(owned_pet_id) ON DELETE CASCADE
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_pet_templates_type ON pet_templates(pet_type);
CREATE INDEX IF NOT EXISTS idx_pet_skills_pet_id ON pet_skills(pet_id);
CREATE INDEX IF NOT EXISTS idx_owned_pets_character ON owned_pets(character_id);
CREATE INDEX IF NOT EXISTS idx_owned_pets_active ON owned_pets(is_active);
CREATE INDEX IF NOT EXISTS idx_pet_stats_owned_pet ON pet_stats(owned_pet_id);
CREATE INDEX IF NOT EXISTS idx_pet_learned_skills_owned_pet ON pet_learned_skills(owned_pet_id);
CREATE INDEX IF NOT EXISTS idx_pet_combat_log_owned_pet ON pet_combat_log(owned_pet_id);
CREATE INDEX IF NOT EXISTS idx_pet_combat_log_character ON pet_combat_log(character_id);
CREATE INDEX IF NOT EXISTS idx_pet_breeding_character ON pet_breeding(character_id);
CREATE INDEX IF NOT EXISTS idx_pet_breeding_status ON pet_breeding(breeding_status);
CREATE INDEX IF NOT EXISTS idx_pet_equipment_owned_pet ON pet_equipment(owned_pet_id);
CREATE INDEX IF NOT EXISTS idx_pet_statistics_character ON pet_statistics(character_id);
CREATE INDEX IF NOT EXISTS idx_pet_care_log_owned_pet ON pet_care_log(owned_pet_id);
