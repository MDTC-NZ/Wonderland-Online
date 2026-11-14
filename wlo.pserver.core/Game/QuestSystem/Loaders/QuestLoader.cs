using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace wlo.pserver.core.Game.QuestSystem.Loaders
{
    /// <summary>
    /// Loads 5000+ quests from JSON data files into the database
    /// </summary>
    public class QuestDataLoader
    {
        private readonly string _connectionString;
        private readonly string _dataDirectory;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public QuestDataLoader(string dbPath, string dataDirectory)
        {
            _connectionString = $"Data Source={dbPath};Version=3;";
            _dataDirectory = dataDirectory;
        }

        /// <summary>
        /// Loads all quest data from JSON files
        /// </summary>
        public async Task LoadQuestDataAsync()
        {
            try
            {
                Logger.Info("Starting quest data loader...");
                
                var questsFile = Path.Combine(_dataDirectory, "quests.json");
                if (!File.Exists(questsFile))
                {
                    Logger.Warn($"Quest data file not found: {questsFile}");
                    return;
                }

                var jsonContent = await File.ReadAllTextAsync(questsFile);
                using (var jsonDoc = JsonDocument.Parse(jsonContent))
                {
                    var quests = jsonDoc.RootElement.GetProperty("quests").EnumerateArray();
                    int count = 0;

                    using (var connection = new SQLiteConnection(_connectionString))
                    {
                        await connection.OpenAsync();
                        
                        foreach (var questJson in quests)
                        {
                            await InsertQuestFromJsonAsync(connection, questJson);
                            count++;
                            
                            if (count % 100 == 0)
                            {
                                Logger.Info($"Loaded {count} quests...");
                            }
                        }
                    }

                    Logger.Info($"Quest data loading complete: {count} quests loaded");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error loading quest data");
                throw;
            }
        }

        private async Task InsertQuestFromJsonAsync(SQLiteConnection connection, JsonElement questJson)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"INSERT OR IGNORE INTO quest_definitions
                    (quest_id, quest_name, quest_description, category, level_requirement, level_maximum,
                     quest_type, quest_giver_npc_id, quest_completer_npc_id, prerequisite_quest_id,
                     start_location_id, complete_location_id, time_limit_minutes, is_repeatable,
                     repeat_cooldown_hours, quest_version)
                    VALUES (@questId, @name, @description, @category, @levelReq, @levelMax,
                           @type, @giverNpc, @completerNpc, @prereqQuest,
                           @startLoc, @completeLoc, @timeLimit, @repeatable,
                           @cooldown, @version)";

                command.Parameters.AddWithValue("@questId", questJson.GetProperty("id").GetInt32());
                command.Parameters.AddWithValue("@name", questJson.GetProperty("name").GetString());
                command.Parameters.AddWithValue("@description", questJson.GetProperty("description").GetString() ?? "");
                command.Parameters.AddWithValue("@category", questJson.GetProperty("category").GetString() ?? "general");
                command.Parameters.AddWithValue("@levelReq", questJson.GetProperty("level_requirement").GetInt32());
                command.Parameters.AddWithValue("@levelMax", questJson.GetProperty("level_maximum").GetInt32());
                command.Parameters.AddWithValue("@type", questJson.GetProperty("quest_type").GetString() ?? "general");
                command.Parameters.AddWithValue("@giverNpc", questJson.GetProperty("quest_giver_npc_id").GetInt32());
                command.Parameters.AddWithValue("@completerNpc", questJson.GetProperty("quest_completer_npc_id").GetInt32());
                
                var prereqElement = questJson.GetProperty("prerequisite_quest_id");
                command.Parameters.AddWithValue("@prereqQuest", prereqElement.ValueKind == JsonValueKind.Null ? DBNull.Value : prereqElement.GetInt32());
                
                command.Parameters.AddWithValue("@startLoc", questJson.GetProperty("start_location_id").GetInt32());
                command.Parameters.AddWithValue("@completeLoc", questJson.GetProperty("complete_location_id").GetInt32());
                command.Parameters.AddWithValue("@timeLimit", questJson.GetProperty("time_limit_minutes").GetInt32());
                command.Parameters.AddWithValue("@repeatable", questJson.GetProperty("is_repeatable").GetBoolean() ? 1 : 0);
                command.Parameters.AddWithValue("@cooldown", questJson.GetProperty("repeat_cooldown_hours").GetInt32());
                command.Parameters.AddWithValue("@version", questJson.GetProperty("quest_version").GetInt32());

                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
