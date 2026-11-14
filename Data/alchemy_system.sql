-- Wonderland Online Alchemy System Schema
-- Alchemy formulas, rank progression, and potion crafting

-- Alchemy formulas
CREATE TABLE IF NOT EXISTS alchemy_formulas (
    formula_id INTEGER PRIMARY KEY,
    formula_name TEXT NOT NULL,
    formula_description TEXT,
    result_item_id INTEGER NOT NULL,
    result_quantity INTEGER DEFAULT 1,
    formula_level INTEGER DEFAULT 1,
    success_rate INTEGER DEFAULT 80,
    required_alchemy_rank INTEGER DEFAULT 1,
    mixing_time_seconds INTEGER DEFAULT 15,
    cooldown_seconds INTEGER DEFAULT 30,
    is_repeatable BOOLEAN DEFAULT 1,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Alchemy formula ingredients
CREATE TABLE IF NOT EXISTS alchemy_ingredients (
    ingredient_id INTEGER PRIMARY KEY AUTOINCREMENT,
    formula_id INTEGER NOT NULL,
    ingredient_item_id INTEGER NOT NULL,
    required_quantity INTEGER NOT NULL,
    ingredient_index INTEGER,
    is_catalyst BOOLEAN DEFAULT 0,
    UNIQUE(formula_id, ingredient_index),
    FOREIGN KEY (formula_id) REFERENCES alchemy_formulas(formula_id) ON DELETE CASCADE
);

-- Character alchemy rank
CREATE TABLE IF NOT EXISTS character_alchemy_rank (
    alchemy_rank_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL UNIQUE,
    current_rank INTEGER DEFAULT 1,
    rank_experience BIGINT DEFAULT 0,
    rank_mastery INTEGER DEFAULT 0,
    last_updated TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (character_id) REFERENCES characters(character_id) ON DELETE CASCADE
);

-- Alchemy mixing queue
CREATE TABLE IF NOT EXISTS alchemy_mixing_queue (
    mixing_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL,
    formula_id INTEGER NOT NULL,
    quantity_to_mix INTEGER DEFAULT 1,
    quantity_completed INTEGER DEFAULT 0,
    queue_status TEXT CHECK(queue_status IN ('pending', 'mixing', 'completed', 'failed', 'cancelled')) DEFAULT 'pending',
    started_at TIMESTAMP,
    completed_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (formula_id) REFERENCES alchemy_formulas(formula_id) ON DELETE CASCADE
);

-- Alchemy mixing results
CREATE TABLE IF NOT EXISTS alchemy_results (
    result_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL,
    formula_id INTEGER NOT NULL,
    result_item_id INTEGER NOT NULL,
    quantity_produced INTEGER DEFAULT 1,
    success BOOLEAN DEFAULT 1,
    critical_success BOOLEAN DEFAULT 0,
    failure_reason TEXT,
    mixed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    potency_level INTEGER DEFAULT 1
);

-- Alchemical catalysts
CREATE TABLE IF NOT EXISTS alchemical_catalysts (
    catalyst_id INTEGER PRIMARY KEY AUTOINCREMENT,
    catalyst_name TEXT NOT NULL,
    catalyst_type TEXT CHECK(catalyst_type IN ('universal', 'fire', 'water', 'earth', 'air', 'life', 'death')),
    catalyst_strength INTEGER DEFAULT 1,
    success_rate_bonus INTEGER DEFAULT 5,
    critical_chance_bonus REAL DEFAULT 0.05
);

-- Character learned formulas
CREATE TABLE IF NOT EXISTS character_alchemy_formulas (
    learned_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL,
    formula_id INTEGER NOT NULL,
    learned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    times_mixed INTEGER DEFAULT 0,
    is_favorite BOOLEAN DEFAULT 0,
    UNIQUE(character_id, formula_id),
    FOREIGN KEY (formula_id) REFERENCES alchemy_formulas(formula_id) ON DELETE CASCADE
);

-- Alchemy stations
CREATE TABLE IF NOT EXISTS alchemy_stations (
    station_id INTEGER PRIMARY KEY AUTOINCREMENT,
    station_name TEXT NOT NULL,
    map_id INTEGER NOT NULL,
    x_coordinate REAL NOT NULL,
    y_coordinate REAL NOT NULL,
    z_coordinate REAL NOT NULL,
    station_level INTEGER DEFAULT 1,
    success_rate_bonus REAL DEFAULT 1.0,
    max_concurrent_mixes INTEGER DEFAULT 1,
    is_active BOOLEAN DEFAULT 1,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Alchemy statistics
CREATE TABLE IF NOT EXISTS alchemy_statistics (
    stat_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL UNIQUE,
    total_potions_mixed INTEGER DEFAULT 0,
    successful_mixes INTEGER DEFAULT 0,
    failed_mixes INTEGER DEFAULT 0,
    critical_successes INTEGER DEFAULT 0,
    total_mixing_time_hours REAL DEFAULT 0.0,
    last_mix_time TIMESTAMP,
    highest_rank_achieved INTEGER DEFAULT 1
);

-- Rank progression requirements
CREATE TABLE IF NOT EXISTS alchemy_rank_requirements (
    rank_id INTEGER PRIMARY KEY,
    rank_level INTEGER NOT NULL,
    experience_required BIGINT NOT NULL,
    formulas_to_learn INTEGER DEFAULT 0,
    previous_rank_requirement INTEGER
);

-- Potion effects
CREATE TABLE IF NOT EXISTS potion_effects (
    effect_id INTEGER PRIMARY KEY AUTOINCREMENT,
    potion_item_id INTEGER NOT NULL,
    effect_type TEXT CHECK(effect_type IN ('heal', 'buff', 'debuff_cure', 'temporary_stat_boost', 'status_cure')),
    effect_value INTEGER,
    effect_duration_seconds INTEGER DEFAULT 0,
    is_permanent BOOLEAN DEFAULT 0,
    FOREIGN KEY (potion_item_id) REFERENCES item_definitions(item_id) ON DELETE CASCADE
);

-- Alchemy experimentation log
CREATE TABLE IF NOT EXISTS alchemy_experimentation_log (
    experimentation_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL,
    formula_id INTEGER,
    experiment_type TEXT CHECK(experiment_type IN ('pure_formula', 'modified_formula', 'unknown_formula')),
    ingredients_used TEXT, -- JSON
    result_item_id INTEGER,
    success BOOLEAN DEFAULT 0,
    experimented_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_alchemy_formulas_level ON alchemy_formulas(formula_level);
CREATE INDEX IF NOT EXISTS idx_alchemy_ingredients_formula ON alchemy_ingredients(formula_id);
CREATE INDEX IF NOT EXISTS idx_character_alchemy_rank_character ON character_alchemy_rank(character_id);
CREATE INDEX IF NOT EXISTS idx_alchemy_mixing_queue_character ON alchemy_mixing_queue(character_id);
CREATE INDEX IF NOT EXISTS idx_alchemy_mixing_queue_status ON alchemy_mixing_queue(queue_status);
CREATE INDEX IF NOT EXISTS idx_alchemy_results_character ON alchemy_results(character_id);
CREATE INDEX IF NOT EXISTS idx_alchemy_results_formula ON alchemy_results(formula_id);
CREATE INDEX IF NOT EXISTS idx_character_alchemy_formulas_character ON character_alchemy_formulas(character_id);
CREATE INDEX IF NOT EXISTS idx_alchemy_stations_map ON alchemy_stations(map_id);
CREATE INDEX IF NOT EXISTS idx_alchemy_statistics_character ON alchemy_statistics(character_id);
CREATE INDEX IF NOT EXISTS idx_alchemy_experimentation_character ON alchemy_experimentation_log(character_id);
