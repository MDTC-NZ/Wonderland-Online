using System;
using System.Collections.Generic;

namespace wlo.pserver.core.Game.MonsterSystem.Models
{
    public class MonsterTemplate
    {
        public int MonsterId { get; set; }
        public string MonsterName { get; set; }
        public string MonsterType { get; set; }
        public int Level { get; set; }
        public int ExperienceReward { get; set; }
        public int MoneyReward { get; set; }
        public int MaxHealth { get; set; }
        public int AttackPower { get; set; }
        public float AttackSpeed { get; set; } = 1.0f;
        public int Defense { get; set; }
        public bool IsAggressive { get; set; } = true;
        public List<MonsterLootDrop> LootDrops { get; set; } = new();
        public List<MonsterSkill> Skills { get; set; } = new();
    }

    public class ActiveMonster
    {
        public int InstanceId { get; set; }
        public int MonsterId { get; set; }
        public int MapId { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public int CurrentHealth { get; set; }
        public int MaxHealth { get; set; }
        public string Status { get; set; } // alive, dead, despawning
        public DateTime SpawnedAt { get; set; }
    }

    public class MonsterLootDrop
    {
        public int LootId { get; set; }
        public int MonsterId { get; set; }
        public int ItemId { get; set; }
        public float DropChance { get; set; }
        public int MinQuantity { get; set; } = 1;
        public int MaxQuantity { get; set; } = 1;
        public bool IsRareDrop { get; set; }
    }

    public class MonsterSkill
    {
        public int SkillId { get; set; }
        public int MonsterId { get; set; }
        public int SkillId_Actual { get; set; }
        public int SkillLevel { get; set; } = 1;
        public float CastProbability { get; set; } = 0.5f;
        public int CooldownSeconds { get; set; } = 30;
    }
}
