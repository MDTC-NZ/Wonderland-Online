using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace wlo.pserver.core.Game.ShopSystem.Managers
{
    public interface IShopManager
    {
        Task<Shop> GetShopAsync(int shopId);
        Task<bool> BuyItemAsync(int characterId, int shopId, int itemId, int quantity);
        Task<bool> SellItemAsync(int characterId, int shopId, int itemId, int quantity);
        Task InitializeShopsAsync();
    }

    /// <summary>
    /// Manages shop operations, buying, selling, and inventory
    /// </summary>
    public class ShopManager : IShopManager
    {
        private readonly ShopDatabase _shopDatabase;
        private readonly Dictionary<int, Shop> _shopCache = new();
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public ShopManager(ShopDatabase shopDatabase)
        {
            _shopDatabase = shopDatabase ?? throw new ArgumentNullException(nameof(shopDatabase));
        }

        public async Task InitializeShopsAsync()
        {
            try
            {
                Logger.Info("Initializing shop system...");
                var shops = await _shopDatabase.GetAllShopsAsync();
                
                foreach (var shop in shops)
                {
                    _shopCache[shop.ShopId] = shop;
                }

                Logger.Info($"Shop system initialized: {_shopCache.Count} shops loaded");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error initializing shop system");
                throw;
            }
        }

        public async Task<Shop> GetShopAsync(int shopId)
        {
            try
            {
                if (_shopCache.TryGetValue(shopId, out var cachedShop))
                {
                    return cachedShop;
                }

                var shop = await _shopDatabase.GetShopByIdAsync(shopId);
                if (shop != null)
                {
                    _shopCache[shopId] = shop;
                }

                return shop;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error retrieving shop {shopId}");
                throw;
            }
        }

        public async Task<bool> BuyItemAsync(int characterId, int shopId, int itemId, int quantity)
        {
            try
            {
                var shop = await GetShopAsync(shopId);
                if (shop == null) return false;

                var shopItem = shop.InventoryItems.FirstOrDefault(i => i.ItemId == itemId);
                if (shopItem == null || !shopItem.IsPurchasable || shopItem.ItemQuantity < quantity)
                {
                    Logger.Warn($"Item {itemId} not available for purchase at shop {shopId}");
                    return false;
                }

                var totalPrice = shopItem.ItemPrice * quantity;
                
                // Record transaction
                var transaction = new ShopTransaction
                {
                    CharacterId = characterId,
                    ShopId = shopId,
                    ItemId = itemId,
                    TransactionType = "buy",
                    Quantity = quantity,
                    UnitPrice = shopItem.ItemPrice,
                    TotalPrice = totalPrice,
                    TransactionTimestamp = DateTime.UtcNow
                };

                await _shopDatabase.SaveTransactionAsync(transaction);
                shopItem.ItemQuantity -= quantity;

                Logger.Info($"Character {characterId} bought {quantity}x item {itemId} from shop {shopId}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error buying item from shop");
                throw;
            }
        }

        public async Task<bool> SellItemAsync(int characterId, int shopId, int itemId, int quantity)
        {
            try
            {
                var shop = await GetShopAsync(shopId);
                if (shop == null) return false;

                var shopItem = shop.InventoryItems.FirstOrDefault(i => i.ItemId == itemId);
                if (shopItem == null || !shopItem.IsSellable)
                {
                    Logger.Warn($"Shop {shopId} does not accept item {itemId}");
                    return false;
                }

                var unitPrice = (int)(shopItem.ItemPrice * shop.SellMultiplier);
                var totalPrice = unitPrice * quantity;

                var transaction = new ShopTransaction
                {
                    CharacterId = characterId,
                    ShopId = shopId,
                    ItemId = itemId,
                    TransactionType = "sell",
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    TotalPrice = totalPrice,
                    TransactionTimestamp = DateTime.UtcNow
                };

                await _shopDatabase.SaveTransactionAsync(transaction);
                shopItem.ItemQuantity += quantity;

                Logger.Info($"Character {characterId} sold {quantity}x item {itemId} to shop {shopId}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error selling item to shop");
                throw;
            }
        }
    }
}
