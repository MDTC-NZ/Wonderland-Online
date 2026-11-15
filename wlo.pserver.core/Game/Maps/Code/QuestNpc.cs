using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Maps
{
    /// <summary>
    /// Class for Quest related Npcs or Npcs that respond in game to interactions
    /// 
    /// Uses EvaluateQuestData to handle packets related to position and visibility per player
    /// </summary>
    public abstract class QuestNpc : InteractableObjects
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region Properties
        
        public virtual string Name { get { return ""; } }
        public virtual int NpcID { get; protected set; }
        public List<int> OfferedQuests { get; protected set; }
        public List<int> ReceivesQuests { get; protected set; }

        #endregion

        protected QuestNpc()
        {
            OfferedQuests = new List<int>();
            ReceivesQuests = new List<int>();
        }

        /// <summary>
        /// Evaluate quest data for player visibility and indicators
        /// </summary>
        public virtual void EvaluateQuestData(Player src)
        {
            if (src == null)
                return;

            try
            {
                // Check if this NPC has quests this player can take
                foreach (int questID in OfferedQuests)
                {
                    Quest quest = QuestManager.GetQuestTemplate(questID);
                    if (quest != null)
                    {
                        // Show quest indicator if player hasn't done it and meets requirements
                        if (!src.Quests.IsQuestCompleted(questID))
                        {
                            if (src.Level >= quest.RequiredLevel)
                            {
                                // Send quest available indicator to player
                                SendQuestIndicator(src, questID, QuestIndicatorType.Available);
                            }
                        }
                    }
                }

                // Check if this NPC can receive quests from player
                foreach (int questID in ReceivesQuests)
                {
                    Quest active = src.Quests.GetActiveQuest(questID);
                    if (active != null && active.IsCompleted)
                    {
                        // Send quest completion indicator to player
                        SendQuestIndicator(src, questID, QuestIndicatorType.ReadyForTurnIn);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error evaluating quest data for NPC {NpcID} and player {src.CharID}: {ex.Message}");
            }
        }

        /// <summary>
        /// Get quests offered by this NPC
        /// </summary>
        public virtual List<Quest> GetOfferedQuests()
        {
            List<Quest> quests = new List<Quest>();
            foreach (int questID in OfferedQuests)
            {
                Quest q = QuestManager.GetQuestTemplate(questID);
                if (q != null)
                    quests.Add(q);
            }
            return quests;
        }

        /// <summary>
        /// Get quests this NPC can receive/complete
        /// </summary>
        public virtual List<Quest> GetReceivableQuests()
        {
            List<Quest> quests = new List<Quest>();
            foreach (int questID in ReceivesQuests)
            {
                Quest q = QuestManager.GetQuestTemplate(questID);
                if (q != null)
                    quests.Add(q);
            }
            return quests;
        }

        /// <summary>
        /// Handles a player accepting a quest from this NPC
        /// </summary>
        public virtual bool AcceptQuestFromPlayer(Player src, int questID)
        {
            if (!OfferedQuests.Contains(questID))
            {
                return false;
            }

            if (src.Quests.AcceptQuest(questID))
            {
                log.Info($"Player {src.CharID} accepted quest {questID} from NPC {NpcID}");
                ShowQuestDialog(src, questID, QuestDialogType.Accepted);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Handles a player completing a quest with this NPC
        /// </summary>
        public virtual bool CompleteQuestWithPlayer(Player src, int questID)
        {
            if (!ReceivesQuests.Contains(questID))
            {
                return false;
            }

            if (src.Quests.CompleteQuest(questID))
            {
                log.Info($"Player {src.CharID} completed quest {questID} with NPC {NpcID}");
                ShowQuestDialog(src, questID, QuestDialogType.Completed);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Send quest indicator to player (override to implement client protocol)
        /// </summary>
        protected virtual void SendQuestIndicator(Player src, int questID, QuestIndicatorType type)
        {
            // Implementation should send packet to client with quest indicator
            // This is a placeholder for protocol-specific implementation
        }

        /// <summary>
        /// Show quest dialog to player (override to implement client protocol)
        /// </summary>
        protected virtual void ShowQuestDialog(Player src, int questID, QuestDialogType type)
        {
            // Implementation should send packet to client with quest dialog
            // This is a placeholder for protocol-specific implementation
        }

        /// <summary>
        /// Default interact implementation - should be overridden
        /// </summary>
        public virtual void Interact(Player src)
        {
            throw new NotImplementedException("Interact must be implemented by derived class");
        }

        public virtual void Interact(Player src, byte? answer = null)
        {
            throw new NotImplementedException("Interact must be implemented by derived class");
        }

        public virtual void Interact(Player src, byte? answer = null, params Code.ShoppingCart[] items)
        {
            throw new NotImplementedException("Interact must be implemented by derived class");
        }
    }

    /// <summary>
    /// Quest indicator types shown to players
    /// </summary>
    public enum QuestIndicatorType
    {
        Available = 0,
        ReadyForTurnIn = 1,
        Completed = 2,
        InProgress = 3
    }

    /// <summary>
    /// Quest dialog types
    /// </summary>
    public enum QuestDialogType
    {
        Accepted = 0,
        Completed = 1,
        Failed = 2,
        Progress = 3
    }
}
