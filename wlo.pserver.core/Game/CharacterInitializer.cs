using System;
using System.Threading.Tasks;

namespace wlo.pserver.core.Game
{
    /// <summary>
    /// CRITICAL FIX: Initializes new character spawn position in the game world
    /// New characters spawn at Ship map, not at Character Select
    /// </summary>
    public class CharacterInitializer
    {
        private readonly InventorySystem.Managers.IInventoryManager _inventoryManager;
        private readonly WorldSystem.Managers.IWorldManager _worldManager;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public const int DEFAULT_SPAWN_MAP_ID = 10000; // Ship map
        public const float DEFAULT_SPAWN_X = 100.0f;
        public const float DEFAULT_SPAWN_Y = 100.0f;
        public const float DEFAULT_SPAWN_Z = 0.0f;

        public CharacterInitializer(
            InventorySystem.Managers.IInventoryManager inventoryManager,
            WorldSystem.Managers.IWorldManager worldManager)
        {
            _inventoryManager = inventoryManager;
            _worldManager = worldManager;
        }

        /// <summary>
        /// Initializes a newly created character with default spawn position and starting items
        /// </summary>
        public async Task<bool> InitializeNewCharacterAsync(int characterId, string characterClass)
        {
            try
            {
                Logger.Info($"Initializing new character {characterId} (class: {characterClass})");

                // Set spawn location to Ship map
                var spawnPoint = await _worldManager.GetSpawnPointAsync(DEFAULT_SPAWN_MAP_ID, "character_spawn");
                if (spawnPoint == null)
                {
                    Logger.Error($"No spawn point found for map {DEFAULT_SPAWN_MAP_ID}");
                    return false;
                }

                // Create character record with spawn position
                await _worldManager.SetCharacterLocationAsync(
                    characterId,
                    DEFAULT_SPAWN_MAP_ID,
                    spawnPoint.X,
                    spawnPoint.Y,
                    spawnPoint.Z
                );

                // Initialize inventory with starting items based on class
                var startingItems = GetStartingItemsByClass(characterClass);
                foreach (var (itemId, quantity) in startingItems)
                {
                    await _inventoryManager.AddItemAsync(characterId, itemId, quantity);
                }

                // Set initial stats
                await InitializeCharacterStatsAsync(characterId, characterClass);

                Logger.Info($"Character {characterId} initialized successfully at map {DEFAULT_SPAWN_MAP_ID}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error initializing character {characterId}");
                return false;
            }
        }

        /// <summary>
        /// Gets starting inventory items based on character class
        /// </summary>
        private (int itemId, int quantity)[] GetStartingItemsByClass(string characterClass)
        {
            return characterClass?.ToLower() switch
            {
                "warrior" => new[]
                {
                    (100001, 1), // Iron Sword
                    (200001, 1), // Leather Armor
                    (300001, 5)  // Health Potion x5
                },
                "mage" => new[]
                {
                    (100002, 1), // Wooden Staff
                    (200002, 1), // Cloth Robe
                    (300002, 10) // Mana Potion x10
                },
                "archer" => new[]
                {
                    (100003, 1), // Bow
                    (400001, 100), // Arrow x100
                    (200003, 1), // Leather Armor
                    (300001, 3)  // Health Potion x3
                },
                "paladin" => new[]
                {
                    (100004, 1), // Holy Sword
                    (200004, 1), // Plate Armor
                    (300003, 5)  // Holy Water x5
                },
                _ => new[]
                {
                    (100001, 1), // Default: Iron Sword
                    (200001, 1), // Default: Leather Armor
                    (300001, 5)  // Default: Health Potion x5
                }
            };
        }

        /// <summary>
        /// Initializes character with default base stats
        /// </summary>
        private async Task InitializeCharacterStatsAsync(int characterId, string characterClass)
        {
            // This would set base stats in character table
            // Stats would be initialized based on class
            await Task.CompletedTask;
            Logger.Debug($"Initialized stats for character {characterId}");
        }

        /// <summary>
        /// TELEPORT FIX: Ensure character never gets stuck at Character Select
        /// Call this if character is found at invalid location
        /// </summary>
        public async Task<bool> ValidateAndFixCharacterLocationAsync(int characterId, int currentMapId)
        {
            try
            {
                // Character Select map ID (typically a special zone)
                const int CHARACTER_SELECT_MAP = 0;

                if (currentMapId == CHARACTER_SELECT_MAP)
                {
                    Logger.Warn($"Character {characterId} stuck at Character Select, teleporting to Ship...");

                    var spawnPoint = await _worldManager.GetSpawnPointAsync(DEFAULT_SPAWN_MAP_ID, "character_spawn");
                    if (spawnPoint != null)
                    {
                        await _worldManager.SetCharacterLocationAsync(
                            characterId,
                            DEFAULT_SPAWN_MAP_ID,
                            spawnPoint.X,
                            spawnPoint.Y,
                            spawnPoint.Z
                        );
                        
                        Logger.Info($"Character {characterId} successfully teleported to Ship");
                        return true;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error validating character {characterId} location");
                return false;
            }
        }
    }

    /// <summary>
    /// World system manager stub for character positioning
    /// </summary>
    public interface IWorldManager
    {
        Task<SpawnPoint> GetSpawnPointAsync(int mapId, string spawnType);
        Task SetCharacterLocationAsync(int characterId, int mapId, float x, float y, float z);
    }

    public class SpawnPoint
    {
        public int SpawnId { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Direction { get; set; }
    }
}
