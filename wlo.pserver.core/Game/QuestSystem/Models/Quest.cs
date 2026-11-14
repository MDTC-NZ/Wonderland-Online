using System;
using System.Collections.Generic;

namespace wlo.pserver.core.Game.QuestSystem.Models
{
    /// <summary>
    /// Represents a complete quest definition with all metadata
    /// </summary>
    public class Quest
    {
        public int QuestId { get; set; }
        public string QuestName { get; set; }
        public string QuestDescription { get; set; }
        public string Category { get; set; }
        public int LevelRequirement { get; set; } = 1;
        public int LevelMaximum { get; set; } = 200;
        public string QuestType { get; set; } // kill, collect, delivery, escort, explore, dialog, combination
        public int QuestGiverNpcId { get; set; }
        public int QuestCompleterNpcId { get; set; }
        public int? PrerequisiteQuestId { get; set; }
        public int StartLocationId { get; set; }
        public int CompleteLocationId { get; set; }
        public int TimeLimitMinutes { get; set; } = 0;
        public bool IsRepeatable { get; set; } = false;
        public int RepeatCooldownHours { get; set; } = 24;
        public int QuestVersion { get; set; } = 1;
        public DateTime CreatedAt { get; set; }

        // Related collections
        public List<QuestObjective> Objectives { get; set; } = new();
        public List<QuestReward> Rewards { get; set; } = new();
        public List<QuestDialogue> Dialogue { get; set; } = new();
        public List<QuestMarker> Markers { get; set; } = new();

        public Quest Clone()
        {
            return new Quest
            {
                QuestId = this.QuestId,
                QuestName = this.QuestName,
                QuestDescription = this.QuestDescription,
                Category = this.Category,
                LevelRequirement = this.LevelRequirement,
                LevelMaximum = this.LevelMaximum,
                QuestType = this.QuestType,
                QuestGiverNpcId = this.QuestGiverNpcId,
                QuestCompleterNpcId = this.QuestCompleterNpcId,
                PrerequisiteQuestId = this.PrerequisiteQuestId,
                StartLocationId = this.StartLocationId,
                CompleteLocationId = this.CompleteLocationId,
                TimeLimitMinutes = this.TimeLimitMinutes,
                IsRepeatable = this.IsRepeatable,
                RepeatCooldownHours = this.RepeatCooldownHours,
                QuestVersion = this.QuestVersion,
                CreatedAt = this.CreatedAt
            };
        }
    }

    /// <summary>
    /// Represents a single objective within a quest
    /// </summary>
    public class QuestObjective
    {
        public int ObjectiveId { get; set; }
        public int QuestId { get; set; }
        public int ObjectiveIndex { get; set; }
        public string ObjectiveType { get; set; } // kill, collect, talk, reach_location, use_item, equip_item, combine_items
        public int? TargetId { get; set; }
        public int TargetAmount { get; set; } = 1;
        public string TargetDescription { get; set; }
        public bool IsOptional { get; set; } = false;
    }

    /// <summary>
    /// Represents a quest dialogue line
    /// </summary>
    public class QuestDialogue
    {
        public int DialogueId { get; set; }
        public int QuestId { get; set; }
        public int NpcId { get; set; }
        public string DialogueStage { get; set; } // start, progress, complete
        public string DialogueText { get; set; }
        public int DialogueIndex { get; set; }
    }

    /// <summary>
    /// Represents a reward for completing a quest
    /// </summary>
    public class QuestReward
    {
        public int RewardId { get; set; }
        public int QuestId { get; set; }
        public string RewardType { get; set; } // item, experience, money, skill, title, faction_reputation
        public int? RewardValueId { get; set; }
        public int RewardAmount { get; set; } = 1;
        public string RewardDescription { get; set; }
        public bool IsChoiceReward { get; set; } = false;
        public int ChoiceGroup { get; set; } = 0;
    }

    /// <summary>
    /// Represents a marker/checkpoint for a quest
    /// </summary>
    public class QuestMarker
    {
        public int MarkerId { get; set; }
        public int QuestId { get; set; }
        public string MarkerType { get; set; } // start, checkpoint, objective, end
        public int MapId { get; set; }
        public float XCoordinate { get; set; }
        public float YCoordinate { get; set; }
        public float ZCoordinate { get; set; }
        public string MarkerDescription { get; set; }
    }
}
