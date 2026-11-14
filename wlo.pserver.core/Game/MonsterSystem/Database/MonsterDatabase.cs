using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace wlo.pserver.core.Game.MonsterSystem.Database
{
    public class MonsterDatabase
    {
        private readonly string _connectionString;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public MonsterDatabase(string dbPath)
        {
            _connectionString = $"Data Source={dbPath};Version=3;";
        }

        public async Task<List<MonsterTemplate>> GetAllMonstersAsync()
        {
            var monsters = new List<MonsterTemplate>();
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM monster_templates";
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                monsters.Add(new MonsterTemplate
                                {
                                    MonsterId = (int)(long)reader["monster_id"],
                                    MonsterName = reader["monster_name"].ToString(),
                                    MonsterType = reader["monster_type"].ToString(),
                                    Level = (int)(long)reader["level"],
                                    ExperienceReward = (int)(long)reader["experience_reward"],
                                    MoneyReward = (int)(long)reader["money_reward"],
                                    MaxHealth = (int)(long)reader["max_health"],
                                    AttackPower = (int)(long)reader["attack_power"]
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error loading monsters");
            }
            return monsters;
        }

        public async Task SaveActiveMonsterAsync(ActiveMonster monster)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"INSERT INTO active_monsters
                            (monster_id, spawn_id, map_id, x_coordinate, y_coordinate, z_coordinate,
                             current_health, max_health, status, spawned_at, last_activity)
                            VALUES (@monsterId, 0, @mapId, @x, @y, @z, @health, @maxHealth, @status, @spawned, CURRENT_TIMESTAMP)";
                        
                        command.Parameters.AddWithValue("@monsterId", monster.MonsterId);
                        command.Parameters.AddWithValue("@mapId", monster.MapId);
                        command.Parameters.AddWithValue("@x", monster.X);
                        command.Parameters.AddWithValue("@y", monster.Y);
                        command.Parameters.AddWithValue("@z", monster.Z);
                        command.Parameters.AddWithValue("@health", monster.CurrentHealth);
                        command.Parameters.AddWithValue("@maxHealth", monster.MaxHealth);
                        command.Parameters.AddWithValue("@status", monster.Status);
                        command.Parameters.AddWithValue("@spawned", monster.SpawnedAt);
                        
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error saving active monster");
            }
        }

        public async Task UpdateMonsterHealthAsync(int instanceId, int health)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"UPDATE active_monsters 
                            SET current_health = @health, last_activity = CURRENT_TIMESTAMP
                            WHERE instance_id = @instanceId";
                        
                        command.Parameters.AddWithValue("@health", health);
                        command.Parameters.AddWithValue("@instanceId", instanceId);
                        
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error updating monster health");
            }
        }

        public async Task RemoveActiveMonsterAsync(int instanceId)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "DELETE FROM active_monsters WHERE instance_id = @instanceId";
                        command.Parameters.AddWithValue("@instanceId", instanceId);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error removing active monster");
            }
        }
    }
}
