-- Wonderland Online Skill System Schema
-- Character skills, skill trees, and progression

-- Skill definitions
CREATE TABLE IF NOT EXISTS skill_definitions (
    skill_id INTEGER PRIMARY KEY,
    skill_name TEXT NOT NULL,
    skill_description TEXT NOT NULL,
    skill_type TEXT CHECK(skill_type IN ('physical', 'magic', 'support', 'passive', 'toggle')),
    skill_category TEXT CHECK(skill_category IN ('combat', 'utility', 'crafting', 'gathering', 'movement', 'special')),
    skill_tier INTEGER DEFAULT 1,
    required_level INTEGER DEFAULT 1,
    required_skill_points INTEGER DEFAULT 1,
    max_level INTEGER DEFAULT 20,
    cast_time_ms INTEGER DEFAULT 0,
    cooldown_ms INTEGER DEFAULT 1000,
    mana_cost INTEGER DEFAULT 0,
    stamina_cost INTEGER DEFAULT 0,
    range_meters REAL DEFAULT 0.0,
    requires_weapon_type TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Skill tree definitions
CREATE TABLE IF NOT EXISTS skill_trees (
    tree_id INTEGER PRIMARY KEY,
    tree_name TEXT NOT NULL,
    tree_type TEXT CHECK(tree_type IN ('class_specific', 'universal', 'hybrid')),
    tree_description TEXT,
    class_id INTEGER,
    max_tier INTEGER DEFAULT 5,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Skill tree nodes
CREATE TABLE IF NOT EXISTS skill_tree_nodes (
    node_id INTEGER PRIMARY KEY AUTOINCREMENT,
    tree_id INTEGER NOT NULL,
    skill_id INTEGER NOT NULL,
    node_tier INTEGER NOT NULL,
    node_position_x INTEGER,
    node_position_y INTEGER,
    prerequisite_node_id INTEGER,
    node_cost_points INTEGER DEFAULT 1,
    is_passive BOOLEAN DEFAULT 0,
    FOREIGN KEY (tree_id) REFERENCES skill_trees(tree_id) ON DELETE CASCADE,
    FOREIGN KEY (skill_id) REFERENCES skill_definitions(skill_id) ON DELETE CASCADE,
    FOREIGN KEY (prerequisite_node_id) REFERENCES skill_tree_nodes(node_id) ON DELETE SET NULL
);

-- Character learned skills
CREATE TABLE IF NOT EXISTS character_skills (
    character_skill_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL,
    skill_id INTEGER NOT NULL,
    skill_level INTEGER DEFAULT 1,
    skill_experience BIGINT DEFAULT 0,
    learned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_used TIMESTAMP,
    cooldown_until TIMESTAMP,
    is_active BOOLEAN DEFAULT 1,
    times_used INTEGER DEFAULT 0,
    UNIQUE(character_id, skill_id),
    FOREIGN KEY (skill_id) REFERENCES skill_definitions(skill_id) ON DELETE CASCADE
);

-- Character skill progression
CREATE TABLE IF NOT EXISTS character_skill_progression (
    progression_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL,
    tree_id INTEGER NOT NULL,
    total_points_available INTEGER DEFAULT 0,
    points_spent INTEGER DEFAULT 0,
    tree_tier_reached INTEGER DEFAULT 1,
    last_reset_at TIMESTAMP,
    UNIQUE(character_id, tree_id),
    FOREIGN KEY (tree_id) REFERENCES skill_trees(tree_id) ON DELETE CASCADE
);

-- Character unlocked tree nodes
CREATE TABLE IF NOT EXISTS character_unlocked_nodes (
    unlock_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL,
    node_id INTEGER NOT NULL,
    unlocked_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_unlocked BOOLEAN DEFAULT 1,
    points_invested INTEGER DEFAULT 0,
    UNIQUE(character_id, node_id),
    FOREIGN KEY (node_id) REFERENCES skill_tree_nodes(node_id) ON DELETE CASCADE
);

-- Skill synergies (skill combinations)
CREATE TABLE IF NOT EXISTS skill_synergies (
    synergy_id INTEGER PRIMARY KEY AUTOINCREMENT,
    primary_skill_id INTEGER NOT NULL,
    secondary_skill_id INTEGER NOT NULL,
    synergy_bonus_type TEXT CHECK(synergy_bonus_type IN ('damage', 'cooldown_reduction', 'cost_reduction', 'range_increase')),
    bonus_value REAL DEFAULT 0.1,
    requirement_text TEXT,
    FOREIGN KEY (primary_skill_id) REFERENCES skill_definitions(skill_id) ON DELETE CASCADE,
    FOREIGN KEY (secondary_skill_id) REFERENCES skill_definitions(skill_id) ON DELETE CASCADE
);

-- Skill usage log
CREATE TABLE IF NOT EXISTS skill_usage_log (
    usage_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL,
    skill_id INTEGER NOT NULL,
    used_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    combat_id INTEGER,
    target_type TEXT,
    target_id INTEGER,
    was_successful BOOLEAN DEFAULT 1,
    damage_dealt INTEGER DEFAULT 0,
    experience_gained INTEGER DEFAULT 0
);

-- Skill level requirements
CREATE TABLE IF NOT EXISTS skill_level_requirements (
    requirement_id INTEGER PRIMARY KEY AUTOINCREMENT,
    skill_id INTEGER NOT NULL,
    skill_level INTEGER NOT NULL,
    experience_required BIGINT NOT NULL,
    UNIQUE(skill_id, skill_level),
    FOREIGN KEY (skill_id) REFERENCES skill_definitions(skill_id) ON DELETE CASCADE
);

-- Skill modifiers (buffs applied to skills)
CREATE TABLE IF NOT EXISTS skill_modifiers (
    modifier_id INTEGER PRIMARY KEY AUTOINCREMENT,
    skill_id INTEGER NOT NULL,
    modifier_type TEXT CHECK(modifier_type IN ('damage_bonus', 'cooldown_reduction', 'range_increase', 'cost_reduction', 'effect_extend')),
    modifier_source TEXT CHECK(modifier_source IN ('equipment', 'buff', 'passive_skill', 'special')),
    modifier_value REAL DEFAULT 0.0,
    FOREIGN KEY (skill_id) REFERENCES skill_definitions(skill_id) ON DELETE CASCADE
);

-- Skill hotkey assignments
CREATE TABLE IF NOT EXISTS skill_hotkeys (
    hotkey_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL,
    hotkey_key TEXT NOT NULL,
    skill_id INTEGER NOT NULL,
    hotbar_position INTEGER,
    UNIQUE(character_id, hotkey_key),
    FOREIGN KEY (skill_id) REFERENCES skill_definitions(skill_id) ON DELETE CASCADE
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_skill_definitions_type ON skill_definitions(skill_type);
CREATE INDEX IF NOT EXISTS idx_skill_definitions_category ON skill_definitions(skill_category);
CREATE INDEX IF NOT EXISTS idx_skill_trees_type ON skill_trees(tree_type);
CREATE INDEX IF NOT EXISTS idx_skill_tree_nodes_tree ON skill_tree_nodes(tree_id);
CREATE INDEX IF NOT EXISTS idx_skill_tree_nodes_skill ON skill_tree_nodes(skill_id);
CREATE INDEX IF NOT EXISTS idx_character_skills_character ON character_skills(character_id);
CREATE INDEX IF NOT EXISTS idx_character_skills_skill ON character_skills(skill_id);
CREATE INDEX IF NOT EXISTS idx_character_skill_progression_character ON character_skill_progression(character_id);
CREATE INDEX IF NOT EXISTS idx_character_unlocked_nodes_character ON character_unlocked_nodes(character_id);
CREATE INDEX IF NOT EXISTS idx_skill_usage_log_character ON skill_usage_log(character_id);
CREATE INDEX IF NOT EXISTS idx_skill_usage_log_skill ON skill_usage_log(skill_id);
CREATE INDEX IF NOT EXISTS idx_skill_hotkeys_character ON skill_hotkeys(character_id);
