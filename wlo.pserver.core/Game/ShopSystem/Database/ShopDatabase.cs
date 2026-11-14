using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace wlo.pserver.core.Game.ShopSystem.Database
{
    public class ShopDatabase
    {
        private readonly string _connectionString;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public ShopDatabase(string dbPath)
        {
            _connectionString = $"Data Source={dbPath};Version=3;";
        }

        public async Task<List<Shop>> GetAllShopsAsync()
        {
            var shops = new List<Shop>();
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM shops WHERE is_active = 1";
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                shops.Add(new Shop
                                {
                                    ShopId = (int)(long)reader["shop_id"],
                                    ShopName = reader["shop_name"].ToString(),
                                    NpcId = (int)(long)reader["npc_id"],
                                    ShopType = reader["shop_type"].ToString(),
                                    MapId = (int)(long)reader["map_id"],
                                    X = (float)(double)reader["x_coordinate"],
                                    Y = (float)(double)reader["y_coordinate"],
                                    Z = (float)(double)reader["z_coordinate"]
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error loading shops");
            }
            return shops;
        }

        public async Task<Shop> GetShopByIdAsync(int shopId)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM shops WHERE shop_id = @shopId";
                        command.Parameters.AddWithValue("@shopId", shopId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new Shop
                                {
                                    ShopId = (int)(long)reader["shop_id"],
                                    ShopName = reader["shop_name"].ToString(),
                                    NpcId = (int)(long)reader["npc_id"],
                                    ShopType = reader["shop_type"].ToString(),
                                    MapId = (int)(long)reader["map_id"]
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error loading shop {shopId}");
            }
            return null;
        }

        public async Task SaveTransactionAsync(ShopTransaction transaction)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"INSERT INTO shop_transactions
                            (character_id, shop_id, item_id, transaction_type, quantity, unit_price, total_price, transaction_timestamp)
                            VALUES (@charId, @shopId, @itemId, @type, @qty, @unitPrice, @totalPrice, @timestamp)";
                        
                        command.Parameters.AddWithValue("@charId", transaction.CharacterId);
                        command.Parameters.AddWithValue("@shopId", transaction.ShopId);
                        command.Parameters.AddWithValue("@itemId", transaction.ItemId);
                        command.Parameters.AddWithValue("@type", transaction.TransactionType);
                        command.Parameters.AddWithValue("@qty", transaction.Quantity);
                        command.Parameters.AddWithValue("@unitPrice", transaction.UnitPrice);
                        command.Parameters.AddWithValue("@totalPrice", transaction.TotalPrice);
                        command.Parameters.AddWithValue("@timestamp", transaction.TransactionTimestamp);
                        
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error saving transaction");
            }
        }
    }
}
