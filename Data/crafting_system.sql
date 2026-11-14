-- Wonderland Online Crafting System Schema
-- Crafting recipes, queues, and result tracking

-- Crafting recipes
CREATE TABLE IF NOT EXISTS crafting_recipes (
    recipe_id INTEGER PRIMARY KEY,
    recipe_name TEXT NOT NULL,
    recipe_description TEXT,
    crafting_category TEXT CHECK(crafting_category IN ('weapon', 'armor', 'accessory', 'consumable', 'quest_item', 'material')),
    required_level INTEGER DEFAULT 1,
    required_skill_id INTEGER,
    required_skill_level INTEGER DEFAULT 1,
    result_item_id INTEGER NOT NULL,
    result_quantity INTEGER DEFAULT 1,
    crafting_time_seconds INTEGER DEFAULT 10,
    success_rate INTEGER DEFAULT 100,
    is_repeatable BOOLEAN DEFAULT 1,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Recipe ingredients
CREATE TABLE IF NOT EXISTS recipe_ingredients (
    ingredient_id INTEGER PRIMARY KEY AUTOINCREMENT,
    recipe_id INTEGER NOT NULL,
    material_item_id INTEGER NOT NULL,
    required_quantity INTEGER NOT NULL,
    ingredient_index INTEGER,
    UNIQUE(recipe_id, ingredient_index),
    FOREIGN KEY (recipe_id) REFERENCES crafting_recipes(recipe_id) ON DELETE CASCADE
);

-- Crafting queue (active crafting jobs)
CREATE TABLE IF NOT EXISTS crafting_queue (
    queue_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL,
    recipe_id INTEGER NOT NULL,
    queue_position INTEGER NOT NULL,
    quantity_to_craft INTEGER DEFAULT 1,
    quantity_completed INTEGER DEFAULT 0,
    queue_status TEXT CHECK(queue_status IN ('pending', 'in_progress', 'completed', 'failed', 'cancelled')) DEFAULT 'pending',
    started_at TIMESTAMP,
    completed_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (recipe_id) REFERENCES crafting_recipes(recipe_id) ON DELETE CASCADE
);

-- Crafting results
CREATE TABLE IF NOT EXISTS crafting_results (
    result_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL,
    recipe_id INTEGER NOT NULL,
    result_item_id INTEGER NOT NULL,
    quantity_crafted INTEGER DEFAULT 1,
    success BOOLEAN DEFAULT 1,
    failure_reason TEXT,
    crafted_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    item_quality_tier TEXT CHECK(item_quality_tier IN ('normal', 'superior', 'masterwork', 'exceptional'))
);

-- Character crafting skills
CREATE TABLE IF NOT EXISTS character_crafting_skills (
    crafting_skill_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL,
    skill_category TEXT CHECK(skill_category IN ('weapon_crafting', 'armor_crafting', 'jewelry_crafting', 'potion_crafting', 'material_processing')),
    skill_level INTEGER DEFAULT 1,
    skill_experience BIGINT DEFAULT 0,
    items_crafted INTEGER DEFAULT 0,
    UNIQUE(character_id, skill_category)
);

-- Crafting patterns (unlocked recipes)
CREATE TABLE IF NOT EXISTS character_crafting_patterns (
    pattern_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL,
    recipe_id INTEGER NOT NULL,
    unlocked_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    times_crafted INTEGER DEFAULT 0,
    favorite_recipe BOOLEAN DEFAULT 0,
    UNIQUE(character_id, recipe_id),
    FOREIGN KEY (recipe_id) REFERENCES crafting_recipes(recipe_id) ON DELETE CASCADE
);

-- Crafting stations
CREATE TABLE IF NOT EXISTS crafting_stations (
    station_id INTEGER PRIMARY KEY AUTOINCREMENT,
    station_name TEXT NOT NULL,
    station_type TEXT CHECK(station_type IN ('anvil', 'loom', 'cauldron', 'forge', 'workbench')),
    map_id INTEGER NOT NULL,
    x_coordinate REAL NOT NULL,
    y_coordinate REAL NOT NULL,
    z_coordinate REAL NOT NULL,
    efficiency_bonus REAL DEFAULT 1.0,
    max_concurrent_crafts INTEGER DEFAULT 1,
    is_active BOOLEAN DEFAULT 1,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Crafting statistics
CREATE TABLE IF NOT EXISTS crafting_statistics (
    stat_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL UNIQUE,
    total_items_crafted INTEGER DEFAULT 0,
    successful_crafts INTEGER DEFAULT 0,
    failed_crafts INTEGER DEFAULT 0,
    total_crafting_time_hours REAL DEFAULT 0.0,
    last_craft_time TIMESTAMP,
    favorite_recipe_id INTEGER
);

-- Crafting materials inventory
CREATE TABLE IF NOT EXISTS crafting_materials (
    material_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL,
    material_item_id INTEGER NOT NULL,
    quantity_available INTEGER DEFAULT 0,
    last_restocked TIMESTAMP,
    UNIQUE(character_id, material_item_id)
);

-- Blueprint/schematic definitions
CREATE TABLE IF NOT EXISTS crafting_blueprints (
    blueprint_id INTEGER PRIMARY KEY AUTOINCREMENT,
    blueprint_name TEXT NOT NULL,
    recipe_id INTEGER NOT NULL,
    blueprint_level_required INTEGER DEFAULT 1,
    cost_to_learn INTEGER DEFAULT 0,
    vendor_id INTEGER,
    is_purchasable BOOLEAN DEFAULT 1,
    is_learned BOOLEAN DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (recipe_id) REFERENCES crafting_recipes(recipe_id) ON DELETE CASCADE
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_crafting_recipes_category ON crafting_recipes(crafting_category);
CREATE INDEX IF NOT EXISTS idx_recipe_ingredients_recipe_id ON recipe_ingredients(recipe_id);
CREATE INDEX IF NOT EXISTS idx_recipe_ingredients_material ON recipe_ingredients(material_item_id);
CREATE INDEX IF NOT EXISTS idx_crafting_queue_character ON crafting_queue(character_id);
CREATE INDEX IF NOT EXISTS idx_crafting_queue_status ON crafting_queue(queue_status);
CREATE INDEX IF NOT EXISTS idx_crafting_queue_recipe ON crafting_queue(recipe_id);
CREATE INDEX IF NOT EXISTS idx_crafting_results_character ON crafting_results(character_id);
CREATE INDEX IF NOT EXISTS idx_crafting_results_recipe ON crafting_results(recipe_id);
CREATE INDEX IF NOT EXISTS idx_character_crafting_skills_character ON character_crafting_skills(character_id);
CREATE INDEX IF NOT EXISTS idx_character_crafting_patterns_character ON character_crafting_patterns(character_id);
CREATE INDEX IF NOT EXISTS idx_crafting_stations_map ON crafting_stations(map_id);
CREATE INDEX IF NOT EXISTS idx_crafting_materials_character ON crafting_materials(character_id);
CREATE INDEX IF NOT EXISTS idx_crafting_blueprints_recipe ON crafting_blueprints(recipe_id);
