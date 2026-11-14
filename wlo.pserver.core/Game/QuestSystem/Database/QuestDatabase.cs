using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.Linq;

namespace wlo.pserver.core.Game.QuestSystem.Database
{
    public interface IQuestDatabase
    {
        Task<List<Quest>> GetAllQuestsAsync();
        Task<Quest> GetQuestByIdAsync(int questId);
        Task<CharacterQuestProgress> GetCharacterQuestProgressAsync(int characterId, int questId);
        Task<List<CharacterQuestProgress>> GetCharacterQuestsByStatusAsync(int characterId, string status);
        Task SaveCharacterQuestProgressAsync(CharacterQuestProgress progress);
        Task SaveQuestCompletionAsync(QuestCompletionRecord record);
        Task<int> GetQuestCountAsync();
    }

    /// <summary>
    /// Handles all quest-related database operations with efficient SQL queries
    /// </summary>
    public class QuestDatabase : IQuestDatabase
    {
        private readonly string _connectionString;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public QuestDatabase(string dbPath)
        {
            _connectionString = $"Data Source={dbPath};Version=3;";
        }

        /// <summary>
        /// Loads all quests with related data - optimized for batch loading
        /// </summary>
        public async Task<List<Quest>> GetAllQuestsAsync()
        {
            var quests = new List<Quest>();
            
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Load quest definitions
                    var questDict = new Dictionary<int, Quest>();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM quest_definitions ORDER BY quest_id";
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var quest = MapQuest(reader);
                                questDict[quest.QuestId] = quest;
                                quests.Add(quest);
                            }
                        }
                    }

                    // Load objectives
                    if (questDict.Count > 0)
                    {
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = "SELECT * FROM quest_objectives ORDER BY quest_id, objective_index";
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    var objective = MapObjective(reader);
                                    if (questDict.TryGetValue(objective.QuestId, out var quest))
                                    {
                                        quest.Objectives.Add(objective);
                                    }
                                }
                            }
                        }

                        // Load rewards
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = "SELECT * FROM quest_rewards ORDER BY quest_id";
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    var reward = MapReward(reader);
                                    if (questDict.TryGetValue(reward.QuestId, out var quest))
                                    {
                                        quest.Rewards.Add(reward);
                                    }
                                }
                            }
                        }

                        // Load dialogue
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = "SELECT * FROM quest_dialogue ORDER BY quest_id, dialogue_index";
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    var dialogue = MapDialogue(reader);
                                    if (questDict.TryGetValue(dialogue.QuestId, out var quest))
                                    {
                                        quest.Dialogue.Add(dialogue);
                                    }
                                }
                            }
                        }

                        // Load markers
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = "SELECT * FROM quest_markers ORDER BY quest_id";
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    var marker = MapMarker(reader);
                                    if (questDict.TryGetValue(marker.QuestId, out var quest))
                                    {
                                        quest.Markers.Add(marker);
                                    }
                                }
                            }
                        }
                    }
                }

                Logger.Info($"Loaded {quests.Count} quests from database");
                return quests;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error loading all quests");
                throw;
            }
        }

        /// <summary>
        /// Gets a single quest by ID
        /// </summary>
        public async Task<Quest> GetQuestByIdAsync(int questId)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    Quest quest = null;
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM quest_definitions WHERE quest_id = @questId";
                        command.Parameters.AddWithValue("@questId", questId);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                quest = MapQuest(reader);
                            }
                        }
                    }

                    if (quest == null) return null;

                    // Load related data
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM quest_objectives WHERE quest_id = @questId ORDER BY objective_index";
                        command.Parameters.AddWithValue("@questId", questId);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                quest.Objectives.Add(MapObjective(reader));
                            }
                        }
                    }

                    return quest;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error retrieving quest {questId}");
                throw;
            }
        }

        /// <summary>
        /// Gets character's progress on a quest
        /// </summary>
        public async Task<CharacterQuestProgress> GetCharacterQuestProgressAsync(int characterId, int questId)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    CharacterQuestProgress progress = null;
                    
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"SELECT * FROM character_quests 
                            WHERE character_id = @characterId AND quest_id = @questId";
                        command.Parameters.AddWithValue("@characterId", characterId);
                        command.Parameters.AddWithValue("@questId", questId);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                progress = MapCharacterQuestProgress(reader);
                            }
                        }
                    }

                    if (progress == null) return null;

                    // Load objective progress
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"SELECT cqo.*, qo.target_amount 
                            FROM character_quest_objectives cqo
                            JOIN quest_objectives qo ON cqo.objective_id = qo.objective_id
                            WHERE cqo.character_quest_id = @charQuestId";
                        command.Parameters.AddWithValue("@charQuestId", progress.CharacterQuestId);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                progress.ObjectiveProgress.Add(MapObjectiveProgress(reader));
                            }
                        }
                    }

                    return progress;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error retrieving quest progress for character {characterId}");
                throw;
            }
        }

        /// <summary>
        /// Gets all quests for a character with specific status
        /// </summary>
        public async Task<List<CharacterQuestProgress>> GetCharacterQuestsByStatusAsync(int characterId, string status)
        {
            var quests = new List<CharacterQuestProgress>();
            
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"SELECT * FROM character_quests 
                            WHERE character_id = @characterId AND quest_status = @status
                            ORDER BY last_updated DESC";
                        command.Parameters.AddWithValue("@characterId", characterId);
                        command.Parameters.AddWithValue("@status", status);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                quests.Add(MapCharacterQuestProgress(reader));
                            }
                        }
                    }
                }

                return quests;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error retrieving quests for character {characterId}");
                throw;
            }
        }

        /// <summary>
        /// Saves character quest progress to database
        /// </summary>
        public async Task SaveCharacterQuestProgressAsync(CharacterQuestProgress progress)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Insert or update quest progress
                            using (var command = connection.CreateCommand())
                            {
                                command.CommandText = @"INSERT OR REPLACE INTO character_quests 
                                    (character_id, quest_id, quest_status, progress_percentage, started_at, 
                                     completed_at, times_completed, last_completed_at, last_updated)
                                    VALUES (@characterId, @questId, @status, @progress, @started, 
                                           @completed, @timesCompleted, @lastCompleted, CURRENT_TIMESTAMP)";
                                
                                command.Parameters.AddWithValue("@characterId", progress.CharacterId);
                                command.Parameters.AddWithValue("@questId", progress.QuestId);
                                command.Parameters.AddWithValue("@status", progress.QuestStatus);
                                command.Parameters.AddWithValue("@progress", progress.ProgressPercentage);
                                command.Parameters.AddWithValue("@started", progress.StartedAt ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("@completed", progress.CompletedAt ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("@timesCompleted", progress.TimesCompleted);
                                command.Parameters.AddWithValue("@lastCompleted", progress.LastCompletedAt ?? (object)DBNull.Value);
                                
                                await command.ExecuteNonQueryAsync();
                            }

                            // Update objective progress
                            foreach (var objProgress in progress.ObjectiveProgress)
                            {
                                using (var command = connection.CreateCommand())
                                {
                                    command.CommandText = @"INSERT OR REPLACE INTO character_quest_objectives
                                        (character_quest_id, objective_id, progress_amount, is_completed, completed_at)
                                        VALUES (@charQuestId, @objId, @progress, @completed, @completedAt)";
                                    
                                    command.Parameters.AddWithValue("@charQuestId", progress.CharacterQuestId);
                                    command.Parameters.AddWithValue("@objId", objProgress.ObjectiveId);
                                    command.Parameters.AddWithValue("@progress", objProgress.ProgressAmount);
                                    command.Parameters.AddWithValue("@completed", objProgress.IsCompleted ? 1 : 0);
                                    command.Parameters.AddWithValue("@completedAt", objProgress.CompletedAt ?? (object)DBNull.Value);
                                    
                                    await command.ExecuteNonQueryAsync();
                                }
                            }

                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error saving quest progress");
                throw;
            }
        }

        /// <summary>
        /// Records quest completion in history
        /// </summary>
        public async Task SaveQuestCompletionAsync(QuestCompletionRecord record)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"INSERT INTO quest_completion_history
                            (character_id, quest_id, completed_at, completion_time_minutes, 
                             experience_earned, money_earned, items_earned)
                            VALUES (@characterId, @questId, @completedAt, @timeMinutes,
                                   @experience, @money, @items)";
                        
                        command.Parameters.AddWithValue("@characterId", record.CharacterId);
                        command.Parameters.AddWithValue("@questId", record.QuestId);
                        command.Parameters.AddWithValue("@completedAt", record.CompletedAt);
                        command.Parameters.AddWithValue("@timeMinutes", record.CompletionTimeMinutes);
                        command.Parameters.AddWithValue("@experience", record.ExperienceEarned);
                        command.Parameters.AddWithValue("@money", record.MoneyEarned);
                        command.Parameters.AddWithValue("@items", string.Join(",", record.ItemsEarned));
                        
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error saving quest completion");
                throw;
            }
        }

        /// <summary>
        /// Gets total quest count
        /// </summary>
        public async Task<int> GetQuestCountAsync()
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT COUNT(*) FROM quest_definitions";
                        return (int)(long)await command.ExecuteScalarAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error getting quest count");
                throw;
            }
        }

        // Mapping helper methods
        private Quest MapQuest(SQLiteDataReader reader)
        {
            return new Quest
            {
                QuestId = (int)(long)reader["quest_id"],
                QuestName = reader["quest_name"].ToString(),
                QuestDescription = reader["quest_description"].ToString(),
                Category = reader["category"].ToString(),
                LevelRequirement = (int)(long)reader["level_requirement"],
                LevelMaximum = (int)(long)reader["level_maximum"],
                QuestType = reader["quest_type"].ToString(),
                QuestGiverNpcId = (int)(long)reader["quest_giver_npc_id"],
                QuestCompleterNpcId = (int)(long)reader["quest_completer_npc_id"],
                PrerequisiteQuestId = reader["prerequisite_quest_id"] != DBNull.Value ? (int)(long)reader["prerequisite_quest_id"] : null,
                StartLocationId = (int)(long)reader["start_location_id"],
                CompleteLocationId = (int)(long)reader["complete_location_id"],
                TimeLimitMinutes = (int)(long)reader["time_limit_minutes"],
                IsRepeatable = reader["is_repeatable"] != DBNull.Value && (int)(long)reader["is_repeatable"] == 1,
                RepeatCooldownHours = (int)(long)reader["repeat_cooldown_hours"],
                QuestVersion = (int)(long)reader["quest_version"],
                CreatedAt = DateTime.Parse(reader["created_at"].ToString())
            };
        }

        private QuestObjective MapObjective(SQLiteDataReader reader)
        {
            return new QuestObjective
            {
                ObjectiveId = (int)(long)reader["objective_id"],
                QuestId = (int)(long)reader["quest_id"],
                ObjectiveIndex = (int)(long)reader["objective_index"],
                ObjectiveType = reader["objective_type"].ToString(),
                TargetId = reader["target_id"] != DBNull.Value ? (int)(long)reader["target_id"] : null,
                TargetAmount = (int)(long)reader["target_amount"],
                TargetDescription = reader["target_description"]?.ToString() ?? "",
                IsOptional = reader["is_optional"] != DBNull.Value && (int)(long)reader["is_optional"] == 1
            };
        }

        private QuestReward MapReward(SQLiteDataReader reader)
        {
            return new QuestReward
            {
                RewardId = (int)(long)reader["reward_id"],
                QuestId = (int)(long)reader["quest_id"],
                RewardType = reader["reward_type"].ToString(),
                RewardValueId = reader["reward_value_id"] != DBNull.Value ? (int)(long)reader["reward_value_id"] : null,
                RewardAmount = (int)(long)reader["reward_amount"],
                RewardDescription = reader["reward_description"]?.ToString() ?? "",
                IsChoiceReward = reader["is_choice_reward"] != DBNull.Value && (int)(long)reader["is_choice_reward"] == 1,
                ChoiceGroup = (int)(long)reader["choice_group"]
            };
        }

        private QuestDialogue MapDialogue(SQLiteDataReader reader)
        {
            return new QuestDialogue
            {
                DialogueId = (int)(long)reader["dialogue_id"],
                QuestId = (int)(long)reader["quest_id"],
                NpcId = (int)(long)reader["npc_id"],
                DialogueStage = reader["dialogue_stage"].ToString(),
                DialogueText = reader["dialogue_text"].ToString(),
                DialogueIndex = (int)(long)reader["dialogue_index"]
            };
        }

        private QuestMarker MapMarker(SQLiteDataReader reader)
        {
            return new QuestMarker
            {
                MarkerId = (int)(long)reader["marker_id"],
                QuestId = (int)(long)reader["quest_id"],
                MarkerType = reader["marker_type"].ToString(),
                MapId = (int)(long)reader["map_id"],
                XCoordinate = (float)(double)reader["x_coordinate"],
                YCoordinate = (float)(double)reader["y_coordinate"],
                ZCoordinate = (float)(double)reader["z_coordinate"],
                MarkerDescription = reader["marker_description"]?.ToString() ?? ""
            };
        }

        private CharacterQuestProgress MapCharacterQuestProgress(SQLiteDataReader reader)
        {
            return new CharacterQuestProgress
            {
                CharacterQuestId = (int)(long)reader["character_quest_id"],
                CharacterId = (int)(long)reader["character_id"],
                QuestId = (int)(long)reader["quest_id"],
                QuestStatus = reader["quest_status"].ToString(),
                ProgressPercentage = (int)(long)reader["progress_percentage"],
                StartedAt = reader["started_at"] != DBNull.Value ? DateTime.Parse(reader["started_at"].ToString()) : null,
                CompletedAt = reader["completed_at"] != DBNull.Value ? DateTime.Parse(reader["completed_at"].ToString()) : null,
                LastUpdated = DateTime.Parse(reader["last_updated"].ToString()),
                TimesCompleted = (int)(long)reader["times_completed"],
                LastCompletedAt = reader["last_completed_at"] != DBNull.Value ? DateTime.Parse(reader["last_completed_at"].ToString()) : null
            };
        }

        private ObjectiveProgress MapObjectiveProgress(SQLiteDataReader reader)
        {
            return new ObjectiveProgress
            {
                ObjectiveProgressId = (int)(long)reader["objective_progress_id"],
                ObjectiveId = (int)(long)reader["objective_id"],
                ProgressAmount = (int)(long)reader["progress_amount"],
                IsCompleted = reader["is_completed"] != DBNull.Value && (int)(long)reader["is_completed"] == 1,
                CompletedAt = reader["completed_at"] != DBNull.Value ? DateTime.Parse(reader["completed_at"].ToString()) : null
            };
        }
    }
}
