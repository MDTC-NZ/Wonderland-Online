using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

namespace wlo.pserver.core.Game.QuestSystem.Managers
{
    public interface IQuestManager
    {
        Task<Quest> GetQuestAsync(int questId);
        Task<List<Quest>> GetAvailableQuestsAsync(int characterLevel);
        Task<bool> StartQuestAsync(int characterId, int questId);
        Task<bool> UpdateObjectiveProgressAsync(int characterId, int questId, int objectiveId, int progress);
        Task<bool> CompleteQuestAsync(int characterId, int questId);
        Task<CharacterQuestProgress> GetCharacterQuestProgressAsync(int characterId, int questId);
        Task<List<CharacterQuestProgress>> GetCharacterActiveQuestsAsync(int characterId);
        Task<List<CharacterQuestProgress>> GetCharacterCompletedQuestsAsync(int characterId);
        Task InitializeQuestsAsync();
        Task<int> GetQuestCountAsync();
    }

    /// <summary>
    /// Manages quest lifecycle, progression, and reward distribution
    /// </summary>
    public class QuestManager : IQuestManager
    {
        private readonly QuestDatabase _questDatabase;
        private readonly Dictionary<int, Quest> _questCache = new();
        private readonly Dictionary<string, CharacterQuestProgress> _progressCache = new();
        private readonly object _cacheLock = new();
        private const int MAX_ACTIVE_QUESTS = 20;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public QuestManager(QuestDatabase questDatabase)
        {
            _questDatabase = questDatabase ?? throw new ArgumentNullException(nameof(questDatabase));
        }

        /// <summary>
        /// Initializes quest system by loading all quest definitions into memory cache
        /// </summary>
        public async Task InitializeQuestsAsync()
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                Logger.Info("Initializing quest system...");

                var allQuests = await _questDatabase.GetAllQuestsAsync();
                
                lock (_cacheLock)
                {
                    _questCache.Clear();
                    foreach (var quest in allQuests)
                    {
                        _questCache[quest.QuestId] = quest;
                    }
                }

                stopwatch.Stop();
                Logger.Info($"Quest system initialized: {_questCache.Count} quests loaded in {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error initializing quest system");
                throw;
            }
        }

        /// <summary>
        /// Retrieves a quest by ID with caching
        /// </summary>
        public async Task<Quest> GetQuestAsync(int questId)
        {
            try
            {
                lock (_cacheLock)
                {
                    if (_questCache.TryGetValue(questId, out var cachedQuest))
                    {
                        return cachedQuest;
                    }
                }

                var quest = await _questDatabase.GetQuestByIdAsync(questId);
                if (quest != null)
                {
                    lock (_cacheLock)
                    {
                        _questCache[questId] = quest;
                    }
                }

                return quest;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error retrieving quest {questId}");
                throw;
            }
        }

        /// <summary>
        /// Gets available quests for a character based on level
        /// </summary>
        public async Task<List<Quest>> GetAvailableQuestsAsync(int characterLevel)
        {
            try
            {
                var availableQuests = new List<Quest>();

                lock (_cacheLock)
                {
                    availableQuests = _questCache.Values
                        .Where(q => characterLevel >= q.LevelRequirement && characterLevel <= q.LevelMaximum)
                        .ToList();
                }

                return await Task.FromResult(availableQuests);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error retrieving available quests for level {characterLevel}");
                throw;
            }
        }

        /// <summary>
        /// Starts a quest for a character
        /// </summary>
        public async Task<bool> StartQuestAsync(int characterId, int questId)
        {
            try
            {
                var quest = await GetQuestAsync(questId);
                if (quest == null)
                {
                    Logger.Warn($"Quest {questId} not found");
                    return false;
                }

                var existingProgress = await GetCharacterQuestProgressAsync(characterId, questId);
                
                // Check if quest is already in progress or completed
                if (existingProgress != null)
                {
                    if (existingProgress.IsActive)
                    {
                        Logger.Warn($"Character {characterId} already has quest {questId} in progress");
                        return false;
                    }
                    
                    if (existingProgress.IsCompleted && !quest.IsRepeatable)
                    {
                        Logger.Warn($"Character {characterId} has already completed non-repeatable quest {questId}");
                        return false;
                    }
                }

                // Check active quest limit
                var activeQuests = await GetCharacterActiveQuestsAsync(characterId);
                if (activeQuests.Count >= MAX_ACTIVE_QUESTS)
                {
                    Logger.Warn($"Character {characterId} has reached max active quests ({MAX_ACTIVE_QUESTS})");
                    return false;
                }

                // Create or update quest progress
                var progress = existingProgress ?? new CharacterQuestProgress
                {
                    CharacterId = characterId,
                    QuestId = questId,
                    QuestDefinition = quest
                };

                progress.Start();
                
                // Initialize objective progress
                foreach (var objective in quest.Objectives)
                {
                    progress.ObjectiveProgress.Add(new ObjectiveProgress
                    {
                        ObjectiveId = objective.ObjectiveId,
                        Objective = objective,
                        ProgressAmount = 0
                    });
                }

                // Save to database
                await _questDatabase.SaveCharacterQuestProgressAsync(progress);
                
                // Update cache
                var cacheKey = $"{characterId}_{questId}";
                lock (_cacheLock)
                {
                    _progressCache[cacheKey] = progress;
                }

                Logger.Info($"Quest {questId} started for character {characterId}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error starting quest {questId} for character {characterId}");
                throw;
            }
        }

        /// <summary>
        /// Updates progress on a quest objective
        /// </summary>
        public async Task<bool> UpdateObjectiveProgressAsync(int characterId, int questId, int objectiveId, int progress)
        {
            try
            {
                var questProgress = await GetCharacterQuestProgressAsync(characterId, questId);
                if (questProgress == null || !questProgress.IsActive)
                {
                    Logger.Warn($"Character {characterId} does not have active quest {questId}");
                    return false;
                }

                var objectiveProgress = questProgress.ObjectiveProgress
                    .FirstOrDefault(op => op.ObjectiveId == objectiveId);
                
                if (objectiveProgress == null)
                {
                    Logger.Warn($"Objective {objectiveId} not found for quest {questId}");
                    return false;
                }

                var targetAmount = objectiveProgress.Objective.TargetAmount;
                objectiveProgress.UpdateProgress(Math.Min(progress, targetAmount));

                if (!objectiveProgress.IsCompleted && progress >= targetAmount)
                {
                    objectiveProgress.Complete();
                    Logger.Info($"Objective {objectiveId} completed for character {characterId}");
                }

                // Check if all objectives are complete
                var allComplete = questProgress.ObjectiveProgress
                    .Where(op => !op.Objective.IsOptional)
                    .All(op => op.IsCompleted);

                if (allComplete)
                {
                    questProgress.ProgressPercentage = 100;
                }

                // Save progress
                await _questDatabase.SaveCharacterQuestProgressAsync(questProgress);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error updating objective progress for character {characterId}");
                throw;
            }
        }

        /// <summary>
        /// Completes a quest for a character
        /// </summary>
        public async Task<bool> CompleteQuestAsync(int characterId, int questId)
        {
            try
            {
                var questProgress = await GetCharacterQuestProgressAsync(characterId, questId);
                if (questProgress == null)
                {
                    Logger.Warn($"Character {characterId} does not have quest {questId}");
                    return false;
                }

                // Verify all required objectives are complete
                var requiredObjectives = questProgress.ObjectiveProgress
                    .Where(op => !op.Objective.IsOptional)
                    .ToList();

                if (!requiredObjectives.All(op => op.IsCompleted))
                {
                    Logger.Warn($"Not all required objectives complete for quest {questId}");
                    return false;
                }

                questProgress.Complete();

                // Save completion record
                var completionRecord = new QuestCompletionRecord(characterId, questId)
                {
                    CompletionTimeMinutes = (int)questProgress.GetElapsedTimeMinutes()
                };

                await _questDatabase.SaveCharacterQuestProgressAsync(questProgress);
                await _questDatabase.SaveQuestCompletionAsync(completionRecord);

                Logger.Info($"Quest {questId} completed by character {characterId}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error completing quest {questId} for character {characterId}");
                throw;
            }
        }

        /// <summary>
        /// Gets character's progress on a specific quest
        /// </summary>
        public async Task<CharacterQuestProgress> GetCharacterQuestProgressAsync(int characterId, int questId)
        {
            try
            {
                var cacheKey = $"{characterId}_{questId}";
                
                lock (_cacheLock)
                {
                    if (_progressCache.TryGetValue(cacheKey, out var cachedProgress))
                    {
                        return cachedProgress;
                    }
                }

                var progress = await _questDatabase.GetCharacterQuestProgressAsync(characterId, questId);
                if (progress != null)
                {
                    progress.QuestDefinition = await GetQuestAsync(questId);
                    lock (_cacheLock)
                    {
                        _progressCache[cacheKey] = progress;
                    }
                }

                return progress;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error retrieving quest progress for character {characterId}");
                throw;
            }
        }

        /// <summary>
        /// Gets all active quests for a character
        /// </summary>
        public async Task<List<CharacterQuestProgress>> GetCharacterActiveQuestsAsync(int characterId)
        {
            try
            {
                var activeQuests = await _questDatabase.GetCharacterQuestsByStatusAsync(characterId, "in_progress");
                
                foreach (var quest in activeQuests)
                {
                    quest.QuestDefinition = await GetQuestAsync(quest.QuestId);
                }

                return activeQuests;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error retrieving active quests for character {characterId}");
                throw;
            }
        }

        /// <summary>
        /// Gets all completed quests for a character
        /// </summary>
        public async Task<List<CharacterQuestProgress>> GetCharacterCompletedQuestsAsync(int characterId)
        {
            try
            {
                var completedQuests = await _questDatabase.GetCharacterQuestsByStatusAsync(characterId, "completed");
                
                foreach (var quest in completedQuests)
                {
                    quest.QuestDefinition = await GetQuestAsync(quest.QuestId);
                }

                return completedQuests;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error retrieving completed quests for character {characterId}");
                throw;
            }
        }

        /// <summary>
        /// Gets the total number of quests in the system
        /// </summary>
        public async Task<int> GetQuestCountAsync()
        {
            try
            {
                lock (_cacheLock)
                {
                    if (_questCache.Count > 0)
                    {
                        return _questCache.Count;
                    }
                }

                return await _questDatabase.GetQuestCountAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error getting quest count");
                throw;
            }
        }

        /// <summary>
        /// Clears the cache (useful for memory management or updates)
        /// </summary>
        public void ClearCache()
        {
            lock (_cacheLock)
            {
                _questCache.Clear();
                _progressCache.Clear();
            }
            Logger.Info("Quest cache cleared");
        }
    }
}
