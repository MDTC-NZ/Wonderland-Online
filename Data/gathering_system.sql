-- Wonderland Online Gathering System Schema
-- Resource nodes, harvesting, and material collection

-- Resource node templates
CREATE TABLE IF NOT EXISTS resource_nodes (
    node_id INTEGER PRIMARY KEY,
    node_name TEXT NOT NULL,
    resource_type TEXT CHECK(resource_type IN ('ore', 'herb', 'wood', 'stone', 'water', 'crystal', 'rare_material')),
    resource_item_id INTEGER NOT NULL,
    harvest_level_required INTEGER DEFAULT 1,
    harvest_skill_id INTEGER,
    harvest_time_seconds INTEGER DEFAULT 10,
    node_health INTEGER DEFAULT 100,
    node_model_id INTEGER,
    resource_yield_min INTEGER DEFAULT 1,
    resource_yield_max INTEGER DEFAULT 3,
    respawn_time_minutes INTEGER DEFAULT 30,
    is_infinite BOOLEAN DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Active resource nodes in world
CREATE TABLE IF NOT EXISTS active_resource_nodes (
    instance_id INTEGER PRIMARY KEY AUTOINCREMENT,
    node_id INTEGER NOT NULL,
    map_id INTEGER NOT NULL,
    x_coordinate REAL NOT NULL,
    y_coordinate REAL NOT NULL,
    z_coordinate REAL NOT NULL,
    current_health INTEGER NOT NULL,
    max_health INTEGER NOT NULL,
    remaining_resources INTEGER,
    node_status TEXT CHECK(node_status IN ('available', 'harvesting', 'depleted', 'respawning')),
    spawned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    depleted_at TIMESTAMP,
    respawn_at TIMESTAMP,
    last_harvested_by INTEGER,
    harvesting_character_id INTEGER,
    harvesting_start_time TIMESTAMP,
    FOREIGN KEY (node_id) REFERENCES resource_nodes(node_id) ON DELETE CASCADE
);

-- Gathering node spawn points
CREATE TABLE IF NOT EXISTS gathering_spawn_points (
    spawn_id INTEGER PRIMARY KEY AUTOINCREMENT,
    node_id INTEGER NOT NULL,
    map_id INTEGER NOT NULL,
    x_coordinate REAL NOT NULL,
    y_coordinate REAL NOT NULL,
    z_coordinate REAL NOT NULL,
    spawn_radius INTEGER DEFAULT 5,
    spawn_interval_seconds INTEGER DEFAULT 1800,
    max_instances INTEGER DEFAULT 3,
    is_active BOOLEAN DEFAULT 1,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (node_id) REFERENCES resource_nodes(node_id) ON DELETE CASCADE
);

-- Character gathering skills
CREATE TABLE IF NOT EXISTS character_gathering_skills (
    gathering_skill_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL,
    skill_type TEXT CHECK(skill_type IN ('mining', 'herbalism', 'lumberjacking', 'stone_cutting', 'crystal_cutting')),
    skill_level INTEGER DEFAULT 1,
    skill_experience BIGINT DEFAULT 0,
    resources_gathered INTEGER DEFAULT 0,
    UNIQUE(character_id, skill_type)
);

-- Harvesting log
CREATE TABLE IF NOT EXISTS harvesting_log (
    harvest_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL,
    node_id INTEGER NOT NULL,
    instance_id INTEGER,
    resource_item_id INTEGER NOT NULL,
    quantity_harvested INTEGER DEFAULT 1,
    harvest_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    harvest_success BOOLEAN DEFAULT 1,
    skill_level_at_harvest INTEGER,
    experience_gained INTEGER DEFAULT 0,
    FOREIGN KEY (node_id) REFERENCES resource_nodes(node_id) ON DELETE CASCADE,
    FOREIGN KEY (instance_id) REFERENCES active_resource_nodes(instance_id) ON DELETE SET NULL
);

-- Resource availability calendar
CREATE TABLE IF NOT EXISTS resource_availability (
    availability_id INTEGER PRIMARY KEY AUTOINCREMENT,
    node_id INTEGER NOT NULL,
    available_start_hour INTEGER DEFAULT 0,
    available_end_hour INTEGER DEFAULT 24,
    available_start_day INTEGER DEFAULT 0,
    available_end_day INTEGER DEFAULT 6,
    is_always_available BOOLEAN DEFAULT 1,
    season_restriction TEXT CHECK(season_restriction IN ('none', 'spring', 'summer', 'fall', 'winter')),
    FOREIGN KEY (node_id) REFERENCES resource_nodes(node_id) ON DELETE CASCADE
);

-- Gathering statistics
CREATE TABLE IF NOT EXISTS gathering_statistics (
    stat_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL UNIQUE,
    total_resources_gathered INTEGER DEFAULT 0,
    total_gathering_time_hours REAL DEFAULT 0.0,
    total_skill_experience BIGINT DEFAULT 0,
    last_harvest_time TIMESTAMP,
    most_gathered_resource_id INTEGER
);

-- Resource quality variants
CREATE TABLE IF NOT EXISTS resource_quality_levels (
    quality_id INTEGER PRIMARY KEY AUTOINCREMENT,
    resource_item_id INTEGER NOT NULL,
    quality_tier TEXT CHECK(quality_tier IN ('poor', 'common', 'uncommon', 'rare', 'epic', 'legendary')),
    quality_multiplier REAL DEFAULT 1.0,
    drop_chance_percentage REAL DEFAULT 0.0,
    FOREIGN KEY (resource_item_id) REFERENCES item_definitions(item_id) ON DELETE CASCADE
);

-- Gathering quest log
CREATE TABLE IF NOT EXISTS gathering_quest_log (
    quest_log_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL,
    resource_type TEXT,
    target_quantity INTEGER,
    current_quantity INTEGER DEFAULT 0,
    quest_start TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    quest_complete_time TIMESTAMP
);

-- Resource node maintenance
CREATE TABLE IF NOT EXISTS node_maintenance_log (
    maintenance_id INTEGER PRIMARY KEY AUTOINCREMENT,
    instance_id INTEGER NOT NULL,
    maintenance_type TEXT CHECK(maintenance_type IN ('respawn', 'depletion', 'damage', 'reset')),
    performed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    performer_type TEXT,
    performer_id INTEGER,
    FOREIGN KEY (instance_id) REFERENCES active_resource_nodes(instance_id) ON DELETE CASCADE
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_resource_nodes_type ON resource_nodes(resource_type);
CREATE INDEX IF NOT EXISTS idx_resource_nodes_level ON resource_nodes(harvest_level_required);
CREATE INDEX IF NOT EXISTS idx_active_resource_nodes_map ON active_resource_nodes(map_id);
CREATE INDEX IF NOT EXISTS idx_active_resource_nodes_status ON active_resource_nodes(node_status);
CREATE INDEX IF NOT EXISTS idx_active_resource_nodes_respawn ON active_resource_nodes(respawn_at);
CREATE INDEX IF NOT EXISTS idx_gathering_spawn_points_node ON gathering_spawn_points(node_id);
CREATE INDEX IF NOT EXISTS idx_gathering_spawn_points_map ON gathering_spawn_points(map_id);
CREATE INDEX IF NOT EXISTS idx_character_gathering_skills_character ON character_gathering_skills(character_id);
CREATE INDEX IF NOT EXISTS idx_harvesting_log_character ON harvesting_log(character_id);
CREATE INDEX IF NOT EXISTS idx_harvesting_log_node ON harvesting_log(node_id);
CREATE INDEX IF NOT EXISTS idx_harvesting_log_timestamp ON harvesting_log(harvest_time);
CREATE INDEX IF NOT EXISTS idx_gathering_statistics_character ON gathering_statistics(character_id);
CREATE INDEX IF NOT EXISTS idx_resource_quality_levels_item ON resource_quality_levels(resource_item_id);
