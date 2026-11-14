-- Wonderland Online Inventory System Schema
-- Character inventory management with slots, stacks, and equipment

-- Inventory slots
CREATE TABLE IF NOT EXISTS inventory_slots (
    slot_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL,
    slot_index INTEGER NOT NULL,
    inventory_type TEXT CHECK(inventory_type IN ('main', 'equipment', 'quickslot', 'bank', 'pet')),
    item_id INTEGER,
    item_stack_id INTEGER,
    stack_quantity INTEGER DEFAULT 0,
    is_locked BOOLEAN DEFAULT 0,
    last_modified TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(character_id, inventory_type, slot_index)
);

-- Item definitions (master list)
CREATE TABLE IF NOT EXISTS item_definitions (
    item_id INTEGER PRIMARY KEY,
    item_name TEXT NOT NULL,
    item_description TEXT NOT NULL,
    item_type TEXT CHECK(item_type IN ('weapon', 'armor', 'consumable', 'quest', 'crafting', 'misc', 'pet_item', 'skill_book')),
    item_grade TEXT CHECK(item_grade IN ('common', 'uncommon', 'rare', 'epic', 'legendary', 'unique')),
    item_level INTEGER DEFAULT 1,
    item_icon_id INTEGER,
    max_stack INTEGER DEFAULT 1,
    weight REAL DEFAULT 0.0,
    is_tradeable BOOLEAN DEFAULT 1,
    is_droppable BOOLEAN DEFAULT 1,
    is_sellable BOOLEAN DEFAULT 1,
    base_price INTEGER DEFAULT 0,
    durability_max INTEGER DEFAULT 100,
    item_version INTEGER DEFAULT 1,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Item stacks (grouped items)
CREATE TABLE IF NOT EXISTS item_stacks (
    stack_id INTEGER PRIMARY KEY AUTOINCREMENT,
    item_id INTEGER NOT NULL,
    character_id INTEGER,
    quantity INTEGER NOT NULL DEFAULT 1,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_modified TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (item_id) REFERENCES item_definitions(item_id) ON DELETE CASCADE
);

-- Equipment slots
CREATE TABLE IF NOT EXISTS equipment_slots (
    equipment_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL,
    equipment_slot TEXT CHECK(equipment_slot IN ('head', 'chest', 'legs', 'feet', 'hands', 'mainhand', 'offhand', 'neck', 'ring1', 'ring2', 'waist', 'back')),
    item_id INTEGER NOT NULL,
    item_level INTEGER,
    durability_current INTEGER,
    durability_max INTEGER,
    equipped_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(character_id, equipment_slot),
    FOREIGN KEY (item_id) REFERENCES item_definitions(item_id) ON DELETE CASCADE
);

-- Item attributes (stats on items)
CREATE TABLE IF NOT EXISTS item_attributes (
    attribute_id INTEGER PRIMARY KEY AUTOINCREMENT,
    item_id INTEGER NOT NULL,
    attribute_type TEXT CHECK(attribute_type IN ('strength', 'intelligence', 'dexterity', 'constitution', 'wisdom', 'charisma', 'armor', 'damage', 'critical_rate', 'accuracy')),
    attribute_value INTEGER NOT NULL,
    is_random_roll BOOLEAN DEFAULT 0,
    min_roll INTEGER,
    max_roll INTEGER,
    FOREIGN KEY (item_id) REFERENCES item_definitions(item_id) ON DELETE CASCADE
);

-- Item enchantments
CREATE TABLE IF NOT EXISTS item_enchantments (
    enchantment_id INTEGER PRIMARY KEY AUTOINCREMENT,
    stack_id INTEGER NOT NULL,
    enchantment_type TEXT NOT NULL,
    enchantment_level INTEGER DEFAULT 1,
    enchantment_bonus_value INTEGER,
    enchanted_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (stack_id) REFERENCES item_stacks(stack_id) ON DELETE CASCADE
);

-- Durability history
CREATE TABLE IF NOT EXISTS item_durability_log (
    durability_log_id INTEGER PRIMARY KEY AUTOINCREMENT,
    stack_id INTEGER NOT NULL,
    durability_before INTEGER,
    durability_after INTEGER,
    damage_type TEXT,
    log_timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (stack_id) REFERENCES item_stacks(stack_id) ON DELETE CASCADE
);

-- Inventory usage log
CREATE TABLE IF NOT EXISTS inventory_log (
    log_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL,
    action_type TEXT CHECK(action_type IN ('pickup', 'drop', 'equip', 'unequip', 'use', 'consume', 'combine', 'discard')),
    item_id INTEGER NOT NULL,
    quantity_changed INTEGER,
    source TEXT,
    destination TEXT,
    log_timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Quick slots (hotbar)
CREATE TABLE IF NOT EXISTS quickslot_bar (
    quickslot_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL,
    slot_position INTEGER NOT NULL,
    item_id INTEGER,
    action_type TEXT CHECK(action_type IN ('item', 'skill', 'macro')),
    action_param INTEGER,
    UNIQUE(character_id, slot_position)
);

-- Bank storage
CREATE TABLE IF NOT EXISTS bank_storage (
    bank_id INTEGER PRIMARY KEY AUTOINCREMENT,
    character_id INTEGER NOT NULL,
    slot_index INTEGER NOT NULL,
    item_id INTEGER,
    stack_id INTEGER,
    quantity INTEGER DEFAULT 0,
    stored_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(character_id, slot_index)
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_inventory_slots_character ON inventory_slots(character_id);
CREATE INDEX IF NOT EXISTS idx_inventory_slots_type ON inventory_slots(inventory_type);
CREATE INDEX IF NOT EXISTS idx_item_definitions_type ON item_definitions(item_type);
CREATE INDEX IF NOT EXISTS idx_item_definitions_grade ON item_definitions(item_grade);
CREATE INDEX IF NOT EXISTS idx_item_stacks_character ON item_stacks(character_id);
CREATE INDEX IF NOT EXISTS idx_item_stacks_item_id ON item_stacks(item_id);
CREATE INDEX IF NOT EXISTS idx_equipment_slots_character ON equipment_slots(character_id);
CREATE INDEX IF NOT EXISTS idx_equipment_slots_slot ON equipment_slots(equipment_slot);
CREATE INDEX IF NOT EXISTS idx_item_attributes_item_id ON item_attributes(item_id);
CREATE INDEX IF NOT EXISTS idx_item_enchantments_stack_id ON item_enchantments(stack_id);
CREATE INDEX IF NOT EXISTS idx_inventory_log_character ON inventory_log(character_id);
CREATE INDEX IF NOT EXISTS idx_quickslot_bar_character ON quickslot_bar(character_id);
CREATE INDEX IF NOT EXISTS idx_bank_storage_character ON bank_storage(character_id);
