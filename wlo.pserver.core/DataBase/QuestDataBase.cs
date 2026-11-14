using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;
using Game;

namespace DataBase
{
    /// <summary>
    /// Database operations for quest data
    /// </summary>
    public class QuestDataBase : RCLibrary.Core.DataBase
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        const string DBServer = "GameDataBase";

        public QuestDataBase()
        {
        }

        /// <summary>
        /// Load all active quests for a player
        /// </summary>
        public void LoadPlayerActiveQuests(Player player)
        {
            try
            {
                string sql = $@"
                    SELECT caq.charQuestID, caq.questID, caq.progress, caq.state, 
                           caq.acceptedTime, caq.completedTime, caq.timeExpired
                    FROM character_active_quests caq
                    WHERE caq.charID = '{player.CharID}'";

                DataTable dt = GetDataTable(sql);

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        int questID = int.Parse(row["questID"].ToString());
                        Quest template = QuestManager.GetQuestTemplate(questID);

                        if (template != null)
                        {
                            Quest activeQuest = CloneQuest(template);
                            activeQuest.CurrentProgress = int.Parse(row["progress"].ToString());
                            activeQuest.State = (QuestState)int.Parse(row["state"].ToString());
                            activeQuest.AcceptedTime = DateTime.Parse(row["acceptedTime"].ToString());

                            if (row["completedTime"] != DBNull.Value)
                                activeQuest.CompletedTime = DateTime.Parse(row["completedTime"].ToString());

                            player.QuestMgr.AcceptQuest(questID);
                            player.QuestMgr.GetActiveQuest(questID).CurrentProgress = activeQuest.CurrentProgress;
                            player.QuestMgr.GetActiveQuest(questID).State = activeQuest.State;
                        }
                    }
                }

                log.Info($"Loaded {dt?.Rows.Count ?? 0} active quests for player {player.CharID}");
            }
            catch (Exception ex)
            {
                log.Error($"Error loading active quests for player {player.CharID}: {ex.Message}");
            }
        }

        /// <summary>
        /// Load all completed quests for a player
        /// </summary>
        public void LoadPlayerCompletedQuests(Player player)
        {
            try
            {
                string sql = $@"
                    SELECT ccq.questID, ccq.completedTime, ccq.rewardsClaimed, ccq.timesCompleted
                    FROM character_completed_quests ccq
                    WHERE ccq.charID = '{player.CharID}'";

                DataTable dt = GetDataTable(sql);

                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        int questID = int.Parse(row["questID"].ToString());
                        // Track completed quests internally
                    }
                }

                log.Info($"Loaded {dt?.Rows.Count ?? 0} completed quests for player {player.CharID}");
            }
            catch (Exception ex)
            {
                log.Error($"Error loading completed quests for player {player.CharID}: {ex.Message}");
            }
        }

        /// <summary>
        /// Save an active quest for a player
        /// </summary>
        public bool SaveActiveQuest(int charID, Quest quest)
        {
            try
            {
                string sql = $@"
                    INSERT INTO character_active_quests 
                    (charID, questID, progress, state, acceptedTime, completedTime, timeExpired)
                    VALUES ('{charID}', '{quest.QuestID}', '{quest.CurrentProgress}', 
                            '{(int)quest.State}', '{quest.AcceptedTime:yyyy-MM-dd HH:mm:ss}', 
                            {(quest.CompletedTime != DateTime.MinValue ? $"'{quest.CompletedTime:yyyy-MM-dd HH:mm:ss}'" : "NULL")}, 
                            '0')
                    ON DUPLICATE KEY UPDATE
                    progress = '{quest.CurrentProgress}',
                    state = '{(int)quest.State}',
                    completedTime = {(quest.CompletedTime != DateTime.MinValue ? $"'{quest.CompletedTime:yyyy-MM-dd HH:mm:ss}'" : "NULL")}";

                ExecuteNonQuery(sql);
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error saving active quest {quest.QuestID} for player {charID}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Save a completed quest for a player
        /// </summary>
        public bool SaveCompletedQuest(int charID, Quest quest)
        {
            try
            {
                string sql = $@"
                    INSERT INTO character_completed_quests 
                    (charID, questID, completedTime, rewardsClaimed, timesCompleted)
                    VALUES ('{charID}', '{quest.QuestID}', '{quest.CompletedTime:yyyy-MM-dd HH:mm:ss}', '1', '1')
                    ON DUPLICATE KEY UPDATE
                    timesCompleted = timesCompleted + 1,
                    completedTime = '{quest.CompletedTime:yyyy-MM-dd HH:mm:ss}'";

                ExecuteNonQuery(sql);
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error saving completed quest {quest.QuestID} for player {charID}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Remove an active quest from the database
        /// </summary>
        public bool RemoveActiveQuest(int charID, int questID)
        {
            try
            {
                string sql = $@"DELETE FROM character_active_quests 
                              WHERE charID = '{charID}' AND questID = '{questID}'";

                ExecuteNonQuery(sql);
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error removing active quest {questID} for player {charID}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if a player has completed a quest
        /// </summary>
        public bool HasCompletedQuest(int charID, int questID)
        {
            try
            {
                string sql = $@"SELECT COUNT(*) as cnt FROM character_completed_quests 
                              WHERE charID = '{charID}' AND questID = '{questID}'";

                DataTable dt = GetDataTable(sql);
                if (dt != null && dt.Rows.Count > 0)
                {
                    int count = int.Parse(dt.Rows[0]["cnt"].ToString());
                    return count > 0;
                }
                return false;
            }
            catch (Exception ex)
            {
                log.Error($"Error checking completed quest {questID} for player {charID}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get quest completion count for a repeatable quest
        /// </summary>
        public int GetQuestCompletionCount(int charID, int questID)
        {
            try
            {
                string sql = $@"SELECT COALESCE(timesCompleted, 0) as cnt FROM character_completed_quests 
                              WHERE charID = '{charID}' AND questID = '{questID}'";

                DataTable dt = GetDataTable(sql);
                if (dt != null && dt.Rows.Count > 0)
                {
                    return int.Parse(dt.Rows[0]["cnt"].ToString());
                }
                return 0;
            }
            catch (Exception ex)
            {
                log.Error($"Error getting completion count for quest {questID} for player {charID}: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Save all player quests (for logout/periodic save)
        /// </summary>
        public bool SaveAllPlayerQuests(int charID, Dictionary<int, Quest> activeQuests, 
                                        Dictionary<int, Quest> completedQuests)
        {
            try
            {
                // Save active quests
                foreach (var kvp in activeQuests)
                {
                    SaveActiveQuest(charID, kvp.Value);
                }

                // Save completed quests
                foreach (var kvp in completedQuests)
                {
                    SaveCompletedQuest(charID, kvp.Value);
                }

                log.Info($"Saved {activeQuests.Count} active and {completedQuests.Count} completed quests for player {charID}");
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error saving all player quests for {charID}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Clone a quest template
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
    }
}
