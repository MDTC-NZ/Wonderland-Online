using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace wlo.pserver.core.Game.InventorySystem.Database
{
    public class InventoryDatabase
    {
        private readonly string _connectionString;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public InventoryDatabase(string dbPath)
        {
            _connectionString = $"Data Source={dbPath};Version=3;";
        }

        public async Task<Inventory> GetInventoryAsync(int characterId)
        {
            var inventory = new Inventory { CharacterId = characterId };
            
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"SELECT * FROM inventory_slots 
                            WHERE character_id = @charId AND inventory_type = 'main'
                            ORDER BY slot_index";
                        command.Parameters.AddWithValue("@charId", characterId);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            int index = 0;
                            while (await reader.ReadAsync())
                            {
                                inventory.MainSlots[index++] = new InventorySlot
                                {
                                    SlotIndex = (int)(long)reader["slot_index"],
                                    ItemId = reader["item_id"] != System.DBNull.Value ? (int)(long)reader["item_id"] : null,
                                    Quantity = (int)(long)reader["stack_quantity"],
                                    IsLocked = (int)(long)reader["is_locked"] == 1
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error loading inventory for character {characterId}");
            }

            return inventory;
        }

        public async Task AddItemToInventoryAsync(int characterId, int itemId, int quantity)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"INSERT INTO inventory_slots
                            (character_id, slot_index, inventory_type, item_id, stack_quantity)
                            VALUES (@charId, (SELECT COUNT(*) FROM inventory_slots WHERE character_id = @charId),
                                   'main', @itemId, @qty)";
                        
                        command.Parameters.AddWithValue("@charId", characterId);
                        command.Parameters.AddWithValue("@itemId", itemId);
                        command.Parameters.AddWithValue("@qty", quantity);
                        
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error adding item to inventory");
            }
        }

        public async Task RemoveItemFromInventoryAsync(int characterId, int itemId, int quantity)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"UPDATE inventory_slots SET stack_quantity = stack_quantity - @qty
                            WHERE character_id = @charId AND item_id = @itemId AND stack_quantity >= @qty";
                        
                        command.Parameters.AddWithValue("@charId", characterId);
                        command.Parameters.AddWithValue("@itemId", itemId);
                        command.Parameters.AddWithValue("@qty", quantity);
                        
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error removing item from inventory");
            }
        }

        public async Task EquipItemAsync(int characterId, string slot, int itemId)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"INSERT OR REPLACE INTO equipment_slots
                            (character_id, equipment_slot, item_id, durability_current, durability_max)
                            VALUES (@charId, @slot, @itemId, 100, 100)";
                        
                        command.Parameters.AddWithValue("@charId", characterId);
                        command.Parameters.AddWithValue("@slot", slot);
                        command.Parameters.AddWithValue("@itemId", itemId);
                        
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error equipping item");
            }
        }

        public async Task UnequipItemAsync(int characterId, string slot)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"DELETE FROM equipment_slots
                            WHERE character_id = @charId AND equipment_slot = @slot";
                        
                        command.Parameters.AddWithValue("@charId", characterId);
                        command.Parameters.AddWithValue("@slot", slot);
                        
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error unequipping item");
            }
        }
    }
}
