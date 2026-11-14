-- Wonderland Online World System Schema
-- Maps, locations, and world management

-- World maps
CREATE TABLE IF NOT EXISTS world_maps (
    map_id INTEGER PRIMARY KEY,
    map_name TEXT NOT NULL,
    map_description TEXT,
    map_width REAL NOT NULL,
    map_height REAL NOT NULL,
    map_region TEXT CHECK(map_region IN ('mainland', 'dungeon', 'tower', 'island', 'cave', 'castle', 'underground')),
    recommended_level INTEGER DEFAULT 1,
    maximum_level INTEGER DEFAULT 200,
    pvp_enabled BOOLEAN DEFAULT 0,
    weather_system TEXT,
    day_night_cycle BOOLEAN DEFAULT 1,
    music_track_id INTEGER,
    ambient_sound_id INTEGER,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Map zones
CREATE TABLE IF NOT EXISTS map_zones (
    zone_id INTEGER PRIMARY KEY AUTOINCREMENT,
    map_id INTEGER NOT NULL,
    zone_name TEXT NOT NULL,
    zone_description TEXT,
    zone_type TEXT CHECK(zone_type IN ('safe', 'combat', 'dungeon', 'boss', 'neutral')),
    zone_x_min REAL NOT NULL,
    zone_x_max REAL NOT NULL,
    zone_y_min REAL NOT NULL,
    zone_y_max REAL NOT NULL,
    zone_z_min REAL DEFAULT 0.0,
    zone_z_max REAL DEFAULT 100.0,
    pvp_enabled BOOLEAN DEFAULT 0,
    is_instanced BOOLEAN DEFAULT 0,
    max_players_per_instance INTEGER DEFAULT 0,
    FOREIGN KEY (map_id) REFERENCES world_maps(map_id) ON DELETE CASCADE
);

-- Locations (POIs - Points of Interest)
CREATE TABLE IF NOT EXISTS world_locations (
    location_id INTEGER PRIMARY KEY AUTOINCREMENT,
    map_id INTEGER NOT NULL,
    location_name TEXT NOT NULL,
    location_type TEXT CHECK(location_type IN ('town', 'village', 'dungeon_entrance', 'landmark', 'quest_hub', 'shop', 'boss_lair', 'resource_area')),
    x_coordinate REAL NOT NULL,
    y_coordinate REAL NOT NULL,
    z_coordinate REAL NOT NULL,
    location_description TEXT,
    recommended_level INTEGER DEFAULT 1,
    discovery_reward_experience INTEGER DEFAULT 0,
    is_discovered BOOLEAN DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (map_id) REFERENCES world_maps(map_id) ON DELETE CASCADE
);

-- Map transitions (portals/doors)
CREATE TABLE IF NOT EXISTS map_transitions (
    transition_id INTEGER PRIMARY KEY AUTOINCREMENT,
    from_map_id INTEGER NOT NULL,
    to_map_id INTEGER NOT NULL,
    transition_type TEXT CHECK(transition_type IN ('portal', 'door', 'gate', 'stairway', 'warp', 'natural_passage')),
    from_x REAL NOT NULL,
    from_y REAL NOT NULL,
    from_z REAL NOT NULL,
    to_x REAL NOT NULL,
    to_y REAL NOT NULL,
    to_z REAL NOT NULL,
    transition_name TEXT,
    requires_quest_id INTEGER,
    requires_item_id INTEGER,
    is_bidirectional BOOLEAN DEFAULT 1,
    cooldown_seconds INTEGER DEFAULT 0,
    is_active BOOLEAN DEFAULT 1,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (from_map_id) REFERENCES world_maps(map_id) ON DELETE CASCADE,
    FOREIGN KEY (to_map_id) REFERENCES world_maps(map_id) ON DELETE CASCADE
);

-- Spawn points
CREATE TABLE IF NOT EXISTS spawn_points (
    spawn_id INTEGER PRIMARY KEY AUTOINCREMENT,
    map_id INTEGER NOT NULL,
    spawn_type TEXT CHECK(spawn_type IN ('character_spawn', 'resurrection', 'instance_spawn')),
    x_coordinate REAL NOT NULL,
    y_coordinate REAL NOT NULL,
    z_coordinate REAL NOT NULL,
    direction REAL DEFAULT 0.0,
    spawn_radius INTEGER DEFAULT 5,
    is_active BOOLEAN DEFAULT 1,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (map_id) REFERENCES world_maps(map_id) ON DELETE CASCADE
);

-- World events
CREATE TABLE IF NOT EXISTS world_events (
    event_id INTEGER PRIMARY KEY AUTOINCREMENT,
    event_name TEXT NOT NULL,
    event_type TEXT CHECK(event_type IN ('boss_spawn', 'invasion', 'weather', 'festival', 'dynamic_event', 'dungeon_opening')),
    map_id INTEGER NOT NULL,
    start_time TIMESTAMP,
    end_time TIMESTAMP,
    event_status TEXT CHECK(event_status IN ('scheduled', 'active', 'completed', 'cancelled')),
    event_description TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (map_id) REFERENCES world_maps(map_id) ON DELETE CASCADE
);

-- Character discoveries
CREATE TABLE IF NOT EXISTS character_discoveries (
    discovery_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL,
    location_id INTEGER NOT NULL,
    discovered_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_marked BOOLEAN DEFAULT 0,
    is_waypoint BOOLEAN DEFAULT 0,
    custom_name TEXT,
    UNIQUE(character_id, location_id),
    FOREIGN KEY (location_id) REFERENCES world_locations(location_id) ON DELETE CASCADE
);

-- Map weather conditions
CREATE TABLE IF NOT EXISTS map_weather (
    weather_id INTEGER PRIMARY KEY AUTOINCREMENT,
    map_id INTEGER NOT NULL,
    weather_type TEXT CHECK(weather_type IN ('clear', 'rain', 'snow', 'storm', 'fog', 'sandstorm', 'blizzard')),
    intensity_level INTEGER DEFAULT 1,
    start_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    end_time TIMESTAMP,
    weather_effects TEXT, -- JSON
    FOREIGN KEY (map_id) REFERENCES world_maps(map_id) ON DELETE CASCADE
);

-- World time/day cycle
CREATE TABLE IF NOT EXISTS world_time_settings (
    setting_id INTEGER PRIMARY KEY,
    current_game_hour INTEGER DEFAULT 12,
    current_game_day INTEGER DEFAULT 1,
    current_game_month TEXT DEFAULT 'January',
    current_game_year INTEGER DEFAULT 1,
    day_length_minutes REAL DEFAULT 60.0,
    last_updated TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Dungeon instances
CREATE TABLE IF NOT EXISTS dungeon_instances (
    instance_id INTEGER PRIMARY KEY AUTOINCREMENT,
    dungeon_map_id INTEGER NOT NULL,
    party_leader_character_id INTEGER NOT NULL,
    instance_created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    instance_expires_at TIMESTAMP,
    max_players INTEGER DEFAULT 4,
    current_players INTEGER DEFAULT 0,
    is_active BOOLEAN DEFAULT 1,
    difficulty_level INTEGER DEFAULT 1,
    FOREIGN KEY (dungeon_map_id) REFERENCES world_maps(map_id) ON DELETE CASCADE
);

-- Dungeon instance players
CREATE TABLE IF NOT EXISTS dungeon_instance_players (
    instance_player_id INTEGER PRIMARY KEY AUTOINCREMENT,
    instance_id INTEGER NOT NULL,
    character_id INTEGER NOT NULL,
    joined_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    left_at TIMESTAMP,
    is_active BOOLEAN DEFAULT 1,
    FOREIGN KEY (instance_id) REFERENCES dungeon_instances(instance_id) ON DELETE CASCADE
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_world_maps_region ON world_maps(map_region);
CREATE INDEX IF NOT EXISTS idx_world_maps_level ON world_maps(recommended_level);
CREATE INDEX IF NOT EXISTS idx_map_zones_map_id ON map_zones(map_id);
CREATE INDEX IF NOT EXISTS idx_map_zones_type ON map_zones(zone_type);
CREATE INDEX IF NOT EXISTS idx_world_locations_map_id ON world_locations(map_id);
CREATE INDEX IF NOT EXISTS idx_world_locations_type ON world_locations(location_type);
CREATE INDEX IF NOT EXISTS idx_map_transitions_from ON map_transitions(from_map_id);
CREATE INDEX IF NOT EXISTS idx_map_transitions_to ON map_transitions(to_map_id);
CREATE INDEX IF NOT EXISTS idx_spawn_points_map_id ON spawn_points(map_id);
CREATE INDEX IF NOT EXISTS idx_spawn_points_type ON spawn_points(spawn_type);
CREATE INDEX IF NOT EXISTS idx_world_events_map_id ON world_events(map_id);
CREATE INDEX IF NOT EXISTS idx_world_events_status ON world_events(event_status);
CREATE INDEX IF NOT EXISTS idx_character_discoveries_character ON character_discoveries(character_id);
CREATE INDEX IF NOT EXISTS idx_map_weather_map_id ON map_weather(map_id);
CREATE INDEX IF NOT EXISTS idx_dungeon_instances_leader ON dungeon_instances(party_leader_character_id);
CREATE INDEX IF NOT EXISTS idx_dungeon_instance_players_instance ON dungeon_instance_players(instance_id);
