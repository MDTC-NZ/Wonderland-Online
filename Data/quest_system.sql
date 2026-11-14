-- Wonderland Online Quest System Schema
-- Comprehensive quest management with progress tracking and rewards

-- Main quest definitions
CREATE TABLE IF NOT EXISTS quest_definitions (
    quest_id INTEGER PRIMARY KEY,
    quest_name TEXT NOT NULL,
    quest_description TEXT NOT NULL,
    category TEXT NOT NULL,
    level_requirement INTEGER DEFAULT 1,
    level_maximum INTEGER DEFAULT 200,
    quest_type TEXT CHECK(quest_type IN ('kill', 'collect', 'delivery', 'escort', 'explore', 'dialog', 'combination')),
    quest_giver_npc_id INTEGER NOT NULL,
    quest_completer_npc_id INTEGER NOT NULL,
    prerequisite_quest_id INTEGER,
    start_location_id INTEGER NOT NULL,
    complete_location_id INTEGER NOT NULL,
    time_limit_minutes INTEGER DEFAULT 0,
    is_repeatable BOOLEAN DEFAULT 0,
    repeat_cooldown_hours INTEGER DEFAULT 24,
    quest_version INTEGER DEFAULT 1,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (prerequisite_quest_id) REFERENCES quest_definitions(quest_id) ON DELETE SET NULL
);

-- Quest objectives (what player must do)
CREATE TABLE IF NOT EXISTS quest_objectives (
    objective_id INTEGER PRIMARY KEY AUTOINCREMENT,
    quest_id INTEGER NOT NULL,
    objective_index INTEGER NOT NULL,
    objective_type TEXT CHECK(objective_type IN ('kill', 'collect', 'talk', 'reach_location', 'use_item', 'equip_item', 'combine_items')),
    target_id INTEGER,
    target_amount INTEGER DEFAULT 1,
    target_description TEXT,
    is_optional BOOLEAN DEFAULT 0,
    UNIQUE(quest_id, objective_index),
    FOREIGN KEY (quest_id) REFERENCES quest_definitions(quest_id) ON DELETE CASCADE
);

-- Quest dialogue chains
CREATE TABLE IF NOT EXISTS quest_dialogue (
    dialogue_id INTEGER PRIMARY KEY AUTOINCREMENT,
    quest_id INTEGER NOT NULL,
    npc_id INTEGER NOT NULL,
    dialogue_stage TEXT CHECK(dialogue_stage IN ('start', 'progress', 'complete')),
    dialogue_text TEXT NOT NULL,
    dialogue_index INTEGER NOT NULL,
    FOREIGN KEY (quest_id) REFERENCES quest_definitions(quest_id) ON DELETE CASCADE
);

-- Quest rewards (items, experience, money)
CREATE TABLE IF NOT EXISTS quest_rewards (
    reward_id INTEGER PRIMARY KEY AUTOINCREMENT,
    quest_id INTEGER NOT NULL,
    reward_type TEXT CHECK(reward_type IN ('item', 'experience', 'money', 'skill', 'title', 'faction_reputation')),
    reward_value_id INTEGER,
    reward_amount INTEGER DEFAULT 1,
    reward_description TEXT,
    is_choice_reward BOOLEAN DEFAULT 0,
    choice_group INTEGER DEFAULT 0,
    FOREIGN KEY (quest_id) REFERENCES quest_definitions(quest_id) ON DELETE CASCADE
);

-- Player quest progress tracking
CREATE TABLE IF NOT EXISTS character_quests (
    character_quest_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL,
    quest_id INTEGER NOT NULL,
    quest_status TEXT CHECK(quest_status IN ('not_started', 'in_progress', 'completed', 'abandoned', 'failed')) DEFAULT 'not_started',
    progress_percentage INTEGER DEFAULT 0,
    started_at TIMESTAMP,
    completed_at TIMESTAMP,
    last_updated TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    times_completed INTEGER DEFAULT 0,
    last_completed_at TIMESTAMP,
    UNIQUE(character_id, quest_id),
    FOREIGN KEY (quest_id) REFERENCES quest_definitions(quest_id) ON DELETE CASCADE
);

-- Individual objective progress
CREATE TABLE IF NOT EXISTS character_quest_objectives (
    objective_progress_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_quest_id INTEGER NOT NULL,
    objective_id INTEGER NOT NULL,
    progress_amount INTEGER DEFAULT 0,
    is_completed BOOLEAN DEFAULT 0,
    completed_at TIMESTAMP,
    UNIQUE(character_quest_id, objective_id),
    FOREIGN KEY (character_quest_id) REFERENCES character_quests(character_quest_id) ON DELETE CASCADE,
    FOREIGN KEY (objective_id) REFERENCES quest_objectives(objective_id) ON DELETE CASCADE
);

-- Quest completion history
CREATE TABLE IF NOT EXISTS quest_completion_history (
    history_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL,
    quest_id INTEGER NOT NULL,
    completed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    completion_time_minutes INTEGER,
    experience_earned INTEGER,
    money_earned INTEGER,
    items_earned TEXT -- JSON array of items
);

-- Quest markers/checkpoints
CREATE TABLE IF NOT EXISTS quest_markers (
    marker_id INTEGER PRIMARY KEY AUTOINCREMENT,
    quest_id INTEGER NOT NULL,
    marker_type TEXT CHECK(marker_type IN ('start', 'checkpoint', 'objective', 'end')),
    map_id INTEGER NOT NULL,
    x_coordinate REAL NOT NULL,
    y_coordinate REAL NOT NULL,
    z_coordinate REAL NOT NULL,
    marker_description TEXT,
    FOREIGN KEY (quest_id) REFERENCES quest_definitions(quest_id) ON DELETE CASCADE
);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS idx_quest_definitions_giver ON quest_definitions(quest_giver_npc_id);
CREATE INDEX IF NOT EXISTS idx_quest_definitions_completer ON quest_definitions(quest_completer_npc_id);
CREATE INDEX IF NOT EXISTS idx_quest_definitions_level ON quest_definitions(level_requirement, level_maximum);
CREATE INDEX IF NOT EXISTS idx_quest_definitions_type ON quest_definitions(quest_type);
CREATE INDEX IF NOT EXISTS idx_character_quests_character_id ON character_quests(character_id);
CREATE INDEX IF NOT EXISTS idx_character_quests_quest_id ON character_quests(quest_id);
CREATE INDEX IF NOT EXISTS idx_character_quests_status ON character_quests(character_id, quest_status);
CREATE INDEX IF NOT EXISTS idx_quest_objectives_quest_id ON quest_objectives(quest_id);
CREATE INDEX IF NOT EXISTS idx_quest_rewards_quest_id ON quest_rewards(quest_id);
CREATE INDEX IF NOT EXISTS idx_quest_completion_history_character ON quest_completion_history(character_id);
CREATE INDEX IF NOT EXISTS idx_quest_markers_quest_id ON quest_markers(quest_id);

-- Triggers for auto-update timestamps
CREATE TRIGGER IF NOT EXISTS trg_character_quests_update
AFTER UPDATE ON character_quests
FOR EACH ROW
BEGIN
    UPDATE character_quests SET last_updated = CURRENT_TIMESTAMP 
    WHERE character_quest_id = NEW.character_quest_id;
END;
