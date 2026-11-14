using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
    /// <summary>
    /// Represents a quest in the game
    /// </summary>
    public class Quest
    {
        #region Properties
        
        public int QuestID { get; set; }
        public string QuestName { get; set; }
        public string Description { get; set; }
        public byte Level { get; set; }
        
        #region Quest Requirements
        public int RequiredLevel { get; set; }
        public List<int> PrerequisiteQuests { get; set; }
        #endregion

        #region Quest Objectives
        public int ObjectiveCount { get; set; }
        public int CurrentProgress { get; set; }
        public string ObjectiveType { get; set; }
        public int ObjectiveTargetID { get; set; }
        #endregion

        #region Quest NPCs
        public int StartNpcID { get; set; }
        public int EndNpcID { get; set; }
        #endregion

        #region Quest Rewards
        public int RewardEXP { get; set; }
        public int RewardMoney { get; set; }
        public List<QuestReward> ItemRewards { get; set; }
        public List<int> RewardSkills { get; set; }
        #endregion

        #region Quest Status
        public QuestState State { get; set; }
        public DateTime AcceptedTime { get; set; }
        public DateTime CompletedTime { get; set; }
        public bool IsRepeatable { get; set; }
        public byte TimeLimit { get; set; }
        #endregion

        #region Internal Tracking
        public bool IsActive { get { return State == QuestState.InProgress; } }
        public bool IsCompleted { get { return State == QuestState.Completed; } }
        public int ProgressPercentage
        {
            get
            {
                return ObjectiveCount > 0 ? (CurrentProgress * 100) / ObjectiveCount : 0;
            }
        }
        #endregion

        #endregion

        public Quest()
        {
            QuestID = 0;
            QuestName = string.Empty;
            Description = string.Empty;
            Level = 1;
            RequiredLevel = 1;
            PrerequisiteQuests = new List<int>();
            ObjectiveCount = 1;
            CurrentProgress = 0;
            State = QuestState.NotStarted;
            ItemRewards = new List<QuestReward>();
            RewardSkills = new List<int>();
            IsRepeatable = false;
            TimeLimit = 0;
        }

        public void AcceptQuest()
        {
            State = QuestState.InProgress;
            AcceptedTime = DateTime.Now;
            CurrentProgress = 0;
        }

        public void CompleteQuest()
        {
            if (CurrentProgress >= ObjectiveCount)
            {
                State = QuestState.Completed;
                CompletedTime = DateTime.Now;
            }
        }

        public void UpdateProgress(int amount = 1)
        {
            CurrentProgress = Math.Min(CurrentProgress + amount, ObjectiveCount);
        }

        public void ResetProgress()
        {
            CurrentProgress = 0;
            State = QuestState.NotStarted;
            AcceptedTime = DateTime.MinValue;
        }

        public override string ToString()
        {
            return $"[{QuestID}] {QuestName} - Progress: {CurrentProgress}/{ObjectiveCount}";
        }
    }

    /// <summary>
    /// Represents a quest reward item
    /// </summary>
    public class QuestReward
    {
        public int ItemID { get; set; }
        public int Quantity { get; set; }
        public bool IsChoice { get; set; }

        public QuestReward()
        {
            ItemID = 0;
            Quantity = 1;
            IsChoice = false;
        }

        public QuestReward(int itemId, int qty, bool isChoice = false)
        {
            ItemID = itemId;
            Quantity = qty;
            IsChoice = isChoice;
        }
    }

    /// <summary>
    /// Quest state enumeration
    /// </summary>
    public enum QuestState
    {
        NotStarted = 0,
        InProgress = 1,
        Completed = 2,
        Rewarded = 3,
        Failed = 4,
        Abandoned = 5
    }

    /// <summary>
    /// Quest objective type enumeration
    /// </summary>
    public enum QuestObjectiveType
    {
        KillMob = 0,
        CollectItem = 1,
        TalkToNpc = 2,
        ReachLocation = 3,
        UseItem = 4,
        EquipItem = 5,
        DeliverItem = 6
    }
}
