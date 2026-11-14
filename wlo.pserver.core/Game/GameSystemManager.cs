using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using wlo.pserver.core.Game.QuestSystem.Managers;
using wlo.pserver.core.Game.ShopSystem.Managers;
using wlo.pserver.core.Game.InventorySystem.Managers;
using wlo.pserver.core.Game.MonsterSystem.Managers;

namespace wlo.pserver.core.Game
{
    /// <summary>
    /// Central coordinator for all game systems
    /// Manages initialization, integration, and cross-system communication
    /// </summary>
    public class GameSystemManager
    {
        private readonly string _databasePath;
        private readonly string _dataDirectory;

        // System managers
        public IQuestManager QuestManager { get; private set; }
        public IShopManager ShopManager { get; private set; }
        public IInventoryManager InventoryManager { get; private set; }
        public MonsterManager MonsterManager { get; private set; }

        // Event system for cross-system communication
        public EventBus EventBus { get; private set; } = new();

        // State
        private bool _initialized = false;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public GameSystemManager(string databasePath, string dataDirectory)
        {
            _databasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));
            _dataDirectory = dataDirectory ?? throw new ArgumentNullException(nameof(dataDirectory));
        }

        /// <summary>
        /// Initializes all game systems in proper order
        /// </summary>
        public async Task InitializeAllSystemsAsync()
        {
            if (_initialized)
            {
                Logger.Warn("Game systems already initialized");
                return;
            }

            try
            {
                Logger.Info("=== INITIALIZING GAME SYSTEMS ===");

                // Initialize databases
                Logger.Info("Initializing databases...");
                var questDb = new QuestSystem.Database.QuestDatabase(_databasePath);
                var shopDb = new ShopSystem.Database.ShopDatabase(_databasePath);
                var inventoryDb = new InventorySystem.Database.InventoryDatabase(_databasePath);
                var monsterDb = new MonsterSystem.Database.MonsterDatabase(_databasePath);

                // Initialize managers in dependency order
                Logger.Info("Initializing Quest System...");
                QuestManager = new QuestManager(questDb);
                await QuestManager.InitializeQuestsAsync();

                Logger.Info("Initializing Shop System...");
                ShopManager = new ShopManager(shopDb);
                await ShopManager.InitializeShopsAsync();

                Logger.Info("Initializing Inventory System...");
                InventoryManager = new InventoryManager(inventoryDb);
                await InventoryManager.InitializeAsync();

                Logger.Info("Initializing Monster System...");
                MonsterManager = new MonsterManager(monsterDb);
                await MonsterManager.InitializeAsync();

                // Hook up event handlers for cross-system communication
                SetupEventHandlers();

                _initialized = true;
                Logger.Info("=== ALL GAME SYSTEMS INITIALIZED SUCCESSFULLY ===");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "CRITICAL: Error initializing game systems");
                throw;
            }
        }

        /// <summary>
        /// Sets up event handlers for cross-system communication
        /// </summary>
        private void SetupEventHandlers()
        {
            // Quest completion -> reward player with items/money
            EventBus.Subscribe("QuestCompleted", async (questId, characterId) =>
            {
                Logger.Info($"Quest {questId} completed, processing rewards for character {characterId}");
                // Reward distribution would happen here
            });

            // Monster death -> drop loot to inventory
            EventBus.Subscribe("MonsterDeath", async (monsterId, characterId) =>
            {
                Logger.Info($"Monster {monsterId} killed by character {characterId}, dropping loot");
                // Loot distribution would happen here
            });

            // Item purchased -> remove money, add to inventory
            EventBus.Subscribe("ItemPurchased", async (itemId, quantity, characterId) =>
            {
                Logger.Info($"Character {characterId} purchased {quantity}x item {itemId}");
            });
        }

        /// <summary>
        /// Gracefully shuts down all systems
        /// </summary>
        public async Task ShutdownAsync()
        {
            try
            {
                Logger.Info("Shutting down game systems...");
                
                if (QuestManager is QuestManager qm)
                {
                    qm.ClearCache();
                }

                _initialized = false;
                Logger.Info("Game systems shut down complete");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error shutting down game systems");
                throw;
            }
        }

        /// <summary>
        /// Gets system status for monitoring
        /// </summary>
        public Dictionary<string, object> GetSystemStatus()
        {
            return new Dictionary<string, object>
            {
                { "IsInitialized", _initialized },
                { "QuestSystemAvailable", QuestManager != null },
                { "ShopSystemAvailable", ShopManager != null },
                { "InventorySystemAvailable", InventoryManager != null },
                { "MonsterSystemAvailable", MonsterManager != null },
                { "Timestamp", DateTime.UtcNow }
            };
        }
    }

    /// <summary>
    /// Simple event bus for cross-system communication
    /// </summary>
    public class EventBus
    {
        private readonly Dictionary<string, List<Delegate>> _subscribers = new();
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public void Subscribe<T1>(string eventName, Func<T1, Task> handler)
        {
            if (!_subscribers.ContainsKey(eventName))
            {
                _subscribers[eventName] = new List<Delegate>();
            }
            _subscribers[eventName].Add(handler);
            Logger.Debug($"Subscribed to event: {eventName}");
        }

        public async Task PublishAsync<T1>(string eventName, T1 arg1)
        {
            if (_subscribers.TryGetValue(eventName, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    if (handler is Func<T1, Task> func)
                    {
                        try
                        {
                            await func(arg1);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, $"Error in event handler for {eventName}");
                        }
                    }
                }
            }
        }

        public void Subscribe<T1, T2>(string eventName, Func<T1, T2, Task> handler)
        {
            if (!_subscribers.ContainsKey(eventName))
            {
                _subscribers[eventName] = new List<Delegate>();
            }
            _subscribers[eventName].Add(handler);
        }

        public async Task PublishAsync<T1, T2>(string eventName, T1 arg1, T2 arg2)
        {
            if (_subscribers.TryGetValue(eventName, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    if (handler is Func<T1, T2, Task> func)
                    {
                        try
                        {
                            await func(arg1, arg2);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, $"Error in event handler for {eventName}");
                        }
                    }
                }
            }
        }
    }
}
