using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
    /// <summary>
    /// Manages all quests for a player
    /// Handles quest acceptance, progress tracking, completion, and rewards
    /// </summary>
    public class QuestManager
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Player m_player;
        private Dictionary<int, Quest> m_activeQuests;
        private Dictionary<int, Quest> m_completedQuests;
        private static Dictionary<int, Quest> m_questDatabase = new Dictionary<int, Quest>();
        private readonly object m_lockObj = new object();

        public QuestManager(Player player)
        {
            m_player = player;
            m_activeQuests = new Dictionary<int, Quest>();
            m_completedQuests = new Dictionary<int, Quest>();
        }

        #region Quest Management

        /// <summary>
        /// Registers a quest template in the global database
        /// </summary>
        public static void RegisterQuestTemplate(Quest quest)
        {
            lock (m_questDatabase)
            {
                if (!m_questDatabase.ContainsKey(quest.QuestID))
                {
                    m_questDatabase[quest.QuestID] = quest;
                }
            }
        }

        /// <summary>
        /// Gets a quest template by ID
        /// </summary>
        public static Quest GetQuestTemplate(int questID)
        {
            lock (m_questDatabase)
            {
                if (m_questDatabase.ContainsKey(questID))
                {
                    return m_questDatabase[questID];
                }
            }
            return null;
        }

        /// <summary>
        /// Accepts a quest for the player
        /// </summary>
        public bool AcceptQuest(int questID)
        {
            lock (m_lockObj)
            {
                if (m_activeQuests.ContainsKey(questID))
                {
                    log.Warn($"Player {m_player.CharID} already has quest {questID} active");
                    return false;
                }

                if (m_completedQuests.ContainsKey(questID))
                {
                    var quest = m_completedQuests[questID];
                    if (!quest.IsRepeatable)
                    {
                        log.Warn($"Player {m_player.CharID} cannot repeat non-repeatable quest {questID}");
                        return false;
                    }
                }

                Quest template = GetQuestTemplate(questID);
                if (template == null)
                {
                    log.Error($"Quest template {questID} not found");
                    return false;
                }

                if (m_player.Level < template.RequiredLevel)
                {
                    log.Warn($"Player {m_player.CharID} is too low level for quest {questID}");
                    return false;
                }

                if (!CheckPrerequisites(questID))
                {
                    log.Warn($"Player {m_player.CharID} has not completed prerequisites for quest {questID}");
                    return false;
                }

                Quest newQuest = CloneQuest(template);
                newQuest.AcceptQuest();
                m_activeQuests[questID] = newQuest;

                log.Info($"Player {m_player.CharID} accepted quest {questID} - {newQuest.QuestName}");
                return true;
            }
        }

        /// <summary>
        /// Abandons an active quest
        /// </summary>
        public bool AbandonQuest(int questID)
        {
            lock (m_lockObj)
            {
                if (m_activeQuests.ContainsKey(questID))
                {
                    m_activeQuests[questID].State = QuestState.Abandoned;
                    m_activeQuests.Remove(questID);
                    log.Info($"Player {m_player.CharID} abandoned quest {questID}");
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Updates quest progress
        /// </summary>
        public void UpdateQuestProgress(int questID, int amount = 1)
        {
            lock (m_lockObj)
            {
                if (m_activeQuests.ContainsKey(questID))
                {
                    Quest quest = m_activeQuests[questID];
                    quest.UpdateProgress(amount);

                    if (quest.CurrentProgress >= quest.ObjectiveCount)
                    {
                        quest.State = QuestState.Completed;
                        log.Info($"Player {m_player.CharID} completed objective for quest {questID}");
                    }
                }
            }
        }

        /// <summary>
        /// Completes a quest and grants rewards
        /// </summary>
        public bool CompleteQuest(int questID)
        {
            lock (m_lockObj)
            {
                if (!m_activeQuests.ContainsKey(questID))
                {
                    log.Warn($"Player {m_player.CharID} does not have active quest {questID}");
                    return false;
                }

                Quest quest = m_activeQuests[questID];
                if (quest.CurrentProgress < quest.ObjectiveCount)
                {
                    log.Warn($"Player {m_player.CharID} quest {questID} not yet complete (progress: {quest.CurrentProgress}/{quest.ObjectiveCount})");
                    return false;
                }

                quest.CompleteQuest();
                quest.State = QuestState.Rewarded;

                GrantQuestRewards(quest);

                m_activeQuests.Remove(questID);
                m_completedQuests[questID] = quest;

                log.Info($"Player {m_player.CharID} completed quest {questID} - {quest.QuestName}");
                return true;
            }
        }

        /// <summary>
        /// Fails a quest
        /// </summary>
        public bool FailQuest(int questID)
        {
            lock (m_lockObj)
            {
                if (!m_activeQuests.ContainsKey(questID))
                {
                    return false;
                }

                m_activeQuests[questID].State = QuestState.Failed;
                m_activeQuests.Remove(questID);
                log.Info($"Player {m_player.CharID} failed quest {questID}");
                return true;
            }
        }

        #endregion

        #region Quest Queries

        /// <summary>
        /// Gets all active quests
        /// </summary>
        public Dictionary<int, Quest> GetActiveQuests()
        {
            lock (m_lockObj)
            {
                return new Dictionary<int, Quest>(m_activeQuests);
            }
        }

        /// <summary>
        /// Gets a specific active quest
        /// </summary>
        public Quest GetActiveQuest(int questID)
        {
            lock (m_lockObj)
            {
                if (m_activeQuests.ContainsKey(questID))
                {
                    return m_activeQuests[questID];
                }
                return null;
            }
        }

        /// <summary>
        /// Gets all completed quests
        /// </summary>
        public Dictionary<int, Quest> GetCompletedQuests()
        {
            lock (m_lockObj)
            {
                return new Dictionary<int, Quest>(m_completedQuests);
            }
        }

        /// <summary>
        /// Checks if a quest is completed
        /// </summary>
        public bool IsQuestCompleted(int questID)
        {
            lock (m_lockObj)
            {
                return m_completedQuests.ContainsKey(questID);
            }
        }

        /// <summary>
        /// Gets the count of active quests
        /// </summary>
        public int GetActiveQuestCount()
        {
            lock (m_lockObj)
            {
                return m_activeQuests.Count;
            }
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Checks if all prerequisites for a quest are met
        /// </summary>
        private bool CheckPrerequisites(int questID)
        {
            Quest template = GetQuestTemplate(questID);
            if (template == null || template.PrerequisiteQuests.Count == 0)
            {
                return true;
            }

            foreach (int prereq in template.PrerequisiteQuests)
            {
                if (!m_completedQuests.ContainsKey(prereq))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Creates a copy of a quest template for the player
        /// </summary>
        private Quest CloneQuest(Quest template)
        {
            return new Quest
            {
                QuestID = template.QuestID,
                QuestName = template.QuestName,
                Description = template.Description,
                Level = template.Level,
                RequiredLevel = template.RequiredLevel,
                PrerequisiteQuests = new List<int>(template.PrerequisiteQuests),
                ObjectiveCount = template.ObjectiveCount,
                CurrentProgress = 0,
                ObjectiveType = template.ObjectiveType,
                ObjectiveTargetID = template.ObjectiveTargetID,
                StartNpcID = template.StartNpcID,
                EndNpcID = template.EndNpcID,
                RewardEXP = template.RewardEXP,
                RewardMoney = template.RewardMoney,
                ItemRewards = new List<QuestReward>(template.ItemRewards),
                RewardSkills = new List<int>(template.RewardSkills),
                IsRepeatable = template.IsRepeatable,
                TimeLimit = template.TimeLimit,
                State = QuestState.NotStarted
            };
        }

        /// <summary>
        /// Grants rewards for completing a quest
        /// </summary>
        private void GrantQuestRewards(Quest quest)
        {
            try
            {
                if (quest.RewardEXP > 0)
                {
                    m_player.AddExp(quest.RewardEXP);
                    log.Info($"Player {m_player.CharID} gained {quest.RewardEXP} EXP from quest {quest.QuestID}");
                }

                if (quest.RewardMoney > 0)
                {
                    m_player.AddMoney(quest.RewardMoney);
                    log.Info($"Player {m_player.CharID} gained {quest.RewardMoney} money from quest {quest.QuestID}");
                }

                foreach (var reward in quest.ItemRewards)
                {
                    if (reward.ItemID > 0)
                    {
                        m_player.AddItemToInventory(reward.ItemID, reward.Quantity);
                        log.Info($"Player {m_player.CharID} gained {reward.Quantity}x item {reward.ItemID} from quest {quest.QuestID}");
                    }
                }

                foreach (var skillID in quest.RewardSkills)
                {
                    m_player.LearnSkill(skillID);
                    log.Info($"Player {m_player.CharID} learned skill {skillID} from quest {quest.QuestID}");
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error granting quest rewards for quest {quest.QuestID} to player {m_player.CharID}: {ex.Message}");
            }
        }

        #endregion

        /// <summary>
        /// Loads all player's quest data from database
        /// </summary>
        public Task LoadQuestsFromDatabase()
        {
            return Task.Factory.StartNew(() =>
            {
                try
                {
                    lock (m_lockObj)
                    {
                        m_activeQuests.Clear();
                        m_completedQuests.Clear();

                        // TODO: Implement database loading
                        log.Info($"Loaded quests for player {m_player.CharID}");
                    }
                }
                catch (Exception ex)
                {
                    log.Error($"Error loading quests for player {m_player.CharID}: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Saves all player's quest data to database
        /// </summary>
        public Task SaveQuestsToDatabse()
        {
            return Task.Factory.StartNew(() =>
            {
                try
                {
                    lock (m_lockObj)
                    {
                        // TODO: Implement database saving
                        log.Info($"Saved quests for player {m_player.CharID}");
                    }
                }
                catch (Exception ex)
                {
                    log.Error($"Error saving quests for player {m_player.CharID}: {ex.Message}");
                }
            });
        }
    }
}
