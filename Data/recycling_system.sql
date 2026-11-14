-- Wonderland Online Recycling System Schema
-- Item recycling and material extraction

-- Recycling recipes
CREATE TABLE IF NOT EXISTS recycling_recipes (
    recipe_id INTEGER PRIMARY KEY AUTOINCREMENT,
    recipe_name TEXT NOT NULL,
    input_item_id INTEGER NOT NULL,
    success_rate INTEGER DEFAULT 80,
    extraction_materials TEXT, -- JSON array of {material_id, min_qty, max_qty}
    required_level INTEGER DEFAULT 1,
    cooldown_seconds INTEGER DEFAULT 5,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Recycling outputs
CREATE TABLE IF NOT EXISTS recycling_outputs (
    output_id INTEGER PRIMARY KEY AUTOINCREMENT,
    recipe_id INTEGER NOT NULL,
    material_item_id INTEGER NOT NULL,
    min_quantity INTEGER DEFAULT 1,
    max_quantity INTEGER DEFAULT 3,
    output_chance REAL DEFAULT 1.0,
    output_index INTEGER,
    UNIQUE(recipe_id, output_index),
    FOREIGN KEY (recipe_id) REFERENCES recycling_recipes(recipe_id) ON DELETE CASCADE
);

-- Character recycling activity
CREATE TABLE IF NOT EXISTS character_recycling_log (
    recycling_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL,
    recipe_id INTEGER NOT NULL,
    input_item_id INTEGER NOT NULL,
    materials_received TEXT, -- JSON array of items
    recycled_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    success BOOLEAN DEFAULT 1,
    failure_reason TEXT
);

-- Recycling statistics
CREATE TABLE IF NOT EXISTS recycling_statistics (
    stat_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL UNIQUE,
    total_items_recycled INTEGER DEFAULT 0,
    successful_recycles INTEGER DEFAULT 0,
    failed_recycles INTEGER DEFAULT 0,
    total_materials_extracted BIGINT DEFAULT 0,
    last_recycle_time TIMESTAMP
);

-- Recyclable material definitions
CREATE TABLE IF NOT EXISTS recyclable_materials (
    material_id INTEGER PRIMARY KEY AUTOINCREMENT,
    material_name TEXT NOT NULL,
    material_item_id INTEGER NOT NULL,
    can_be_extracted BOOLEAN DEFAULT 1,
    extraction_difficulty INTEGER DEFAULT 1,
    market_value INTEGER DEFAULT 0,
    FOREIGN KEY (material_item_id) REFERENCES item_definitions(item_id) ON DELETE CASCADE
);

-- Recycling station definitions
CREATE TABLE IF NOT EXISTS recycling_stations (
    station_id INTEGER PRIMARY KEY AUTOINCREMENT,
    station_name TEXT NOT NULL,
    map_id INTEGER NOT NULL,
    x_coordinate REAL NOT NULL,
    y_coordinate REAL NOT NULL,
    z_coordinate REAL NOT NULL,
    efficiency_bonus REAL DEFAULT 1.0,
    is_active BOOLEAN DEFAULT 1,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_recycling_recipes_input ON recycling_recipes(input_item_id);
CREATE INDEX IF NOT EXISTS idx_recycling_outputs_recipe ON recycling_outputs(recipe_id);
CREATE INDEX IF NOT EXISTS idx_character_recycling_log_character ON character_recycling_log(character_id);
CREATE INDEX IF NOT EXISTS idx_recycling_statistics_character ON recycling_statistics(character_id);
