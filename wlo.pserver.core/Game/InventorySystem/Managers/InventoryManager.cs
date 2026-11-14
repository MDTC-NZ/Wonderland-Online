using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace wlo.pserver.core.Game.InventorySystem.Managers
{
    public interface IInventoryManager
    {
        Task<Inventory> GetInventoryAsync(int characterId);
        Task<bool> AddItemAsync(int characterId, int itemId, int quantity);
        Task<bool> RemoveItemAsync(int characterId, int itemId, int quantity);
        Task<bool> EquipItemAsync(int characterId, string slot, int itemId);
        Task<bool> UnequipItemAsync(int characterId, string slot);
        Task InitializeAsync();
    }

    /// <summary>
    /// Manages character inventory operations
    /// </summary>
    public class InventoryManager : IInventoryManager
    {
        private readonly InventoryDatabase _database;
        private readonly Dictionary<int, Inventory> _inventoryCache = new();
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public InventoryManager(InventoryDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public async Task InitializeAsync()
        {
            try
            {
                Logger.Info("Initializing inventory system...");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error initializing inventory system");
                throw;
            }
        }

        public async Task<Inventory> GetInventoryAsync(int characterId)
        {
            try
            {
                if (_inventoryCache.TryGetValue(characterId, out var cachedInventory))
                {
                    return cachedInventory;
                }

                var inventory = await _database.GetInventoryAsync(characterId);
                if (inventory != null)
                {
                    _inventoryCache[characterId] = inventory;
                }

                return inventory;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error retrieving inventory for character {characterId}");
                throw;
            }
        }

        public async Task<bool> AddItemAsync(int characterId, int itemId, int quantity)
        {
            try
            {
                var inventory = await GetInventoryAsync(characterId);
                if (inventory == null) return false;

                await _database.AddItemToInventoryAsync(characterId, itemId, quantity);
                inventory.ClearCache();
                
                Logger.Info($"Added {quantity}x item {itemId} to character {characterId}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error adding item to inventory");
                throw;
            }
        }

        public async Task<bool> RemoveItemAsync(int characterId, int itemId, int quantity)
        {
            try
            {
                var inventory = await GetInventoryAsync(characterId);
                if (inventory == null) return false;

                await _database.RemoveItemFromInventoryAsync(characterId, itemId, quantity);
                inventory.ClearCache();
                
                Logger.Info($"Removed {quantity}x item {itemId} from character {characterId}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error removing item from inventory");
                throw;
            }
        }

        public async Task<bool> EquipItemAsync(int characterId, string slot, int itemId)
        {
            try
            {
                await _database.EquipItemAsync(characterId, slot, itemId);
                
                if (_inventoryCache.TryGetValue(characterId, out var inventory))
                {
                    inventory.ClearCache();
                }
                
                Logger.Info($"Character {characterId} equipped item {itemId} to slot {slot}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error equipping item");
                throw;
            }
        }

        public async Task<bool> UnequipItemAsync(int characterId, string slot)
        {
            try
            {
                await _database.UnequipItemAsync(characterId, slot);
                
                if (_inventoryCache.TryGetValue(characterId, out var inventory))
                {
                    inventory.ClearCache();
                }
                
                Logger.Info($"Character {characterId} unequipped slot {slot}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error unequipping item");
                throw;
            }
        }
    }
}
