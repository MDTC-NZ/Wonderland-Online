using System;
using System.Collections.Generic;

namespace wlo.pserver.core.Game.QuestSystem.Models
{
    /// <summary>
    /// Tracks a character's progress on a quest
    /// </summary>
    public class CharacterQuestProgress
    {
        public int CharacterQuestId { get; set; }
        public int CharacterId { get; set; }
        public int QuestId { get; set; }
        public string QuestStatus { get; set; } = "not_started"; // not_started, in_progress, completed, abandoned, failed
        public int ProgressPercentage { get; set; } = 0;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public int TimesCompleted { get; set; } = 0;
        public DateTime? LastCompletedAt { get; set; }

        // Related collections
        public Quest QuestDefinition { get; set; }
        public List<ObjectiveProgress> ObjectiveProgress { get; set; } = new();

        public bool IsActive => QuestStatus == "in_progress";
        public bool IsCompleted => QuestStatus == "completed";
        public bool CanRepeat => IsCompleted && QuestDefinition?.IsRepeatable == true;

        public void Start()
        {
            QuestStatus = "in_progress";
            StartedAt = DateTime.UtcNow;
            LastUpdated = DateTime.UtcNow;
        }

        public void Complete()
        {
            QuestStatus = "completed";
            CompletedAt = DateTime.UtcNow;
            LastCompletedAt = DateTime.UtcNow;
            ProgressPercentage = 100;
            TimesCompleted++;
            LastUpdated = DateTime.UtcNow;
        }

        public void Fail()
        {
            QuestStatus = "failed";
            LastUpdated = DateTime.UtcNow;
        }

        public void Abandon()
        {
            QuestStatus = "abandoned";
            LastUpdated = DateTime.UtcNow;
        }

        public decimal GetElapsedTimeMinutes()
        {
            if (!StartedAt.HasValue) return 0;
            return (decimal)(DateTime.UtcNow - StartedAt.Value).TotalMinutes;
        }
    }

    /// <summary>
    /// Tracks progress on an individual quest objective
    /// </summary>
    public class ObjectiveProgress
    {
        public int ObjectiveProgressId { get; set; }
        public int CharacterQuestId { get; set; }
        public int ObjectiveId { get; set; }
        public int ProgressAmount { get; set; } = 0;
        public bool IsCompleted { get; set; } = false;
        public DateTime? CompletedAt { get; set; }

        // Related objective
        public QuestObjective Objective { get; set; }

        public void UpdateProgress(int amount)
        {
            ProgressAmount = amount;
        }

        public void AddProgress(int amount)
        {
            ProgressAmount += amount;
        }

        public void Complete()
        {
            IsCompleted = true;
            CompletedAt = DateTime.UtcNow;
        }

        public decimal GetProgressPercentage(int targetAmount)
        {
            if (targetAmount <= 0) return 0;
            return Math.Min(100, (ProgressAmount * 100) / targetAmount);
        }
    }

    /// <summary>
    /// Records historical completion of a quest
    /// </summary>
    public class QuestCompletionRecord
    {
        public int HistoryId { get; set; }
        public int CharacterId { get; set; }
        public int QuestId { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        public int CompletionTimeMinutes { get; set; }
        public int ExperienceEarned { get; set; }
        public int MoneyEarned { get; set; }
        public List<int> ItemsEarned { get; set; } = new();

        public QuestCompletionRecord()
        {
        }

        public QuestCompletionRecord(int characterId, int questId)
        {
            CharacterId = characterId;
            QuestId = questId;
        }
    }
}
