-- Wonderland Online Shop System Schema
-- NPC shop management with inventory and transactions

-- Shop definitions
CREATE TABLE IF NOT EXISTS shops (
    shop_id INTEGER PRIMARY KEY,
    shop_name TEXT NOT NULL,
    shop_description TEXT,
    npc_id INTEGER NOT NULL UNIQUE,
    shop_type TEXT CHECK(shop_type IN ('general', 'weapon', 'armor', 'potion', 'scroll', 'quest', 'special', 'pet', 'equipment')),
    map_id INTEGER NOT NULL,
    x_coordinate REAL NOT NULL,
    y_coordinate REAL NOT NULL,
    z_coordinate REAL NOT NULL,
    buy_multiplier REAL DEFAULT 1.0,
    sell_multiplier REAL DEFAULT 0.8,
    max_inventory_slots INTEGER DEFAULT 50,
    is_active BOOLEAN DEFAULT 1,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Shop inventory
CREATE TABLE IF NOT EXISTS shop_inventory (
    inventory_id INTEGER PRIMARY KEY AUTOINCREMENT,
    shop_id INTEGER NOT NULL,
    item_id INTEGER NOT NULL,
    item_quantity INTEGER DEFAULT 1,
    item_price INTEGER NOT NULL,
    restock_interval_hours INTEGER DEFAULT 24,
    last_restocked TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    max_stock INTEGER DEFAULT 100,
    min_stock INTEGER DEFAULT 10,
    is_limited_stock BOOLEAN DEFAULT 0,
    is_purchasable BOOLEAN DEFAULT 1,
    is_sellable BOOLEAN DEFAULT 1,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(shop_id, item_id),
    FOREIGN KEY (shop_id) REFERENCES shops(shop_id) ON DELETE CASCADE
);

-- Shop transaction log
CREATE TABLE IF NOT EXISTS shop_transactions (
    transaction_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL,
    shop_id INTEGER NOT NULL,
    item_id INTEGER NOT NULL,
    transaction_type TEXT CHECK(transaction_type IN ('buy', 'sell')),
    quantity INTEGER NOT NULL,
    unit_price INTEGER NOT NULL,
    total_price INTEGER NOT NULL,
    character_level INTEGER,
    transaction_timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    map_id INTEGER
);

-- NPC definitions (extended)
CREATE TABLE IF NOT EXISTS npc_definitions (
    npc_id INTEGER PRIMARY KEY,
    npc_name TEXT NOT NULL,
    npc_type TEXT CHECK(npc_type IN ('shopkeeper', 'quest_giver', 'trainer', 'healer', 'guard', 'special')),
    map_id INTEGER NOT NULL,
    x_coordinate REAL NOT NULL,
    y_coordinate REAL NOT NULL,
    z_coordinate REAL NOT NULL,
    direction REAL DEFAULT 0.0,
    npc_model_id INTEGER,
    npc_level INTEGER DEFAULT 1,
    npc_description TEXT,
    npc_dialogue TEXT,
    is_attackable BOOLEAN DEFAULT 0,
    quest_giver_id INTEGER UNIQUE,
    shop_keeper_id INTEGER UNIQUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- NPC dialogue options
CREATE TABLE IF NOT EXISTS npc_dialogue_options (
    dialogue_option_id INTEGER PRIMARY KEY AUTOINCREMENT,
    npc_id INTEGER NOT NULL,
    dialogue_text TEXT NOT NULL,
    dialogue_action TEXT CHECK(dialogue_action IN ('open_shop', 'start_quest', 'complete_quest', 'trade', 'nothing')),
    action_param INTEGER,
    dialogue_index INTEGER,
    requirement_type TEXT CHECK(requirement_type IN ('none', 'quest_completed', 'item_owned', 'level_requirement')),
    requirement_value INTEGER,
    FOREIGN KEY (npc_id) REFERENCES npc_definitions(npc_id) ON DELETE CASCADE
);

-- Character shop favorites/bookmarks
CREATE TABLE IF NOT EXISTS character_shop_favorites (
    favorite_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL,
    shop_id INTEGER NOT NULL,
    favorited_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_visited TIMESTAMP,
    visit_count INTEGER DEFAULT 0,
    UNIQUE(character_id, shop_id),
    FOREIGN KEY (shop_id) REFERENCES shops(shop_id) ON DELETE CASCADE
);

-- Shop pricing history (for analytics)
CREATE TABLE IF NOT EXISTS shop_pricing_history (
    history_id INTEGER PRIMARY KEY AUTOINCREMENT,
    shop_id INTEGER NOT NULL,
    item_id INTEGER NOT NULL,
    old_price INTEGER,
    new_price INTEGER,
    changed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    reason TEXT,
    FOREIGN KEY (shop_id) REFERENCES shops(shop_id) ON DELETE CASCADE
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_shops_npc_id ON shops(npc_id);
CREATE INDEX IF NOT EXISTS idx_shops_type ON shops(shop_type);
CREATE INDEX IF NOT EXISTS idx_shops_map_id ON shops(map_id);
CREATE INDEX IF NOT EXISTS idx_shop_inventory_shop_id ON shop_inventory(shop_id);
CREATE INDEX IF NOT EXISTS idx_shop_inventory_item_id ON shop_inventory(item_id);
CREATE INDEX IF NOT EXISTS idx_shop_inventory_stock ON shop_inventory(item_quantity);
CREATE INDEX IF NOT EXISTS idx_shop_transactions_character_id ON shop_transactions(character_id);
CREATE INDEX IF NOT EXISTS idx_shop_transactions_shop_id ON shop_transactions(shop_id);
CREATE INDEX IF NOT EXISTS idx_shop_transactions_timestamp ON shop_transactions(transaction_timestamp);
CREATE INDEX IF NOT EXISTS idx_npc_definitions_type ON npc_definitions(npc_type);
CREATE INDEX IF NOT EXISTS idx_npc_definitions_map ON npc_definitions(map_id);
CREATE INDEX IF NOT EXISTS idx_character_shop_favorites ON character_shop_favorites(character_id);
CREATE INDEX IF NOT EXISTS idx_shop_pricing_history ON shop_pricing_history(shop_id, item_id);
