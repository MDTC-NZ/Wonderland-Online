using System;
using System.Collections.Generic;

namespace wlo.pserver.core.Game.CombatSystem.Models
{
    public class CombatSession
    {
        public int CombatId { get; set; }
        public int InitiatorCharacterId { get; set; }
        public int? DefenderCharacterId { get; set; }
        public int? DefenderMonsterId { get; set; }
        public string CombatStatus { get; set; } = "active";
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public List<CombatLogEntry> LogEntries { get; set; } = new();
    }

    public class CombatLogEntry
    {
        public int LogEntryId { get; set; }
        public int CombatId { get; set; }
        public string ActorType { get; set; } // character, monster
        public int ActorId { get; set; }
        public string ActionType { get; set; } // attack, skill, defend, dodge, death
        public int DamageDealt { get; set; }
        public bool CriticalHit { get; set; }
        public DateTime ActionTimestamp { get; set; }
    }

    public class StatusEffect
    {
        public int EffectId { get; set; }
        public int? CharacterId { get; set; }
        public int? MonsterId { get; set; }
        public string EffectType { get; set; } // stun, slow, poison, burn, etc.
        public int EffectLevel { get; set; } = 1;
        public DateTime AppliedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsPermanent { get; set; }
    }
}

namespace wlo.pserver.core.Game.CraftingSystem.Models
{
    public class Recipe
    {
        public int RecipeId { get; set; }
        public string RecipeName { get; set; }
        public string RecipeCategory { get; set; }
        public int RequiredLevel { get; set; } = 1;
        public int ResultItemId { get; set; }
        public int ResultQuantity { get; set; } = 1;
        public int CraftingTimeSeconds { get; set; } = 10;
        public int SuccessRate { get; set; } = 100;
        public List<RecipeIngredient> Ingredients { get; set; } = new();
    }

    public class RecipeIngredient
    {
        public int IngredientId { get; set; }
        public int RecipeId { get; set; }
        public int MaterialItemId { get; set; }
        public int RequiredQuantity { get; set; }
    }

    public class CraftingQueueItem
    {
        public int QueueId { get; set; }
        public int CharacterId { get; set; }
        public int RecipeId { get; set; }
        public int Quantity { get; set; }
        public int CompletedQuantity { get; set; }
        public string Status { get; set; } = "pending"; // pending, in_progress, completed, failed
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}

namespace wlo.pserver.core.Game.AlchemySystem.Models
{
    public class AlchemyFormula
    {
        public int FormulaId { get; set; }
        public string FormulaName { get; set; }
        public int ResultItemId { get; set; }
        public int ResultQuantity { get; set; } = 1;
        public int RequiredAlchemyRank { get; set; } = 1;
        public int SuccessRate { get; set; } = 80;
        public int MixingTimeSeconds { get; set; } = 15;
        public List<AlchemyIngredient> Ingredients { get; set; } = new();
    }

    public class AlchemyIngredient
    {
        public int IngredientId { get; set; }
        public int FormulaId { get; set; }
        public int ItemId { get; set; }
        public int RequiredQuantity { get; set; }
        public bool IsCatalyst { get; set; }
    }

    public class CharacterAlchemyRank
    {
        public int RankId { get; set; }
        public int CharacterId { get; set; }
        public int CurrentRank { get; set; } = 1;
        public long RankExperience { get; set; }
        public int RankMastery { get; set; }
    }
}

namespace wlo.pserver.core.Game.GatheringSystem.Models
{
    public class ResourceNode
    {
        public int NodeId { get; set; }
        public string NodeName { get; set; }
        public string ResourceType { get; set; } // ore, herb, wood, stone
        public int ResourceItemId { get; set; }
        public int HarvestLevelRequired { get; set; } = 1;
        public int HarvestTimeSeconds { get; set; } = 10;
        public int NodeHealth { get; set; } = 100;
        public int ResourceYieldMin { get; set; } = 1;
        public int ResourceYieldMax { get; set; } = 3;
        public int RespawnTimeMinutes { get; set; } = 30;
    }

    public class ActiveResourceNode
    {
        public int InstanceId { get; set; }
        public int NodeId { get; set; }
        public int MapId { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public int CurrentHealth { get; set; }
        public int RemainingResources { get; set; }
        public string Status { get; set; } = "available"; // available, harvesting, depleted
        public DateTime? RespawnAt { get; set; }
    }
}

namespace wlo.pserver.core.Game.RecyclingSystem.Models
{
    public class RecyclingRecipe
    {
        public int RecipeId { get; set; }
        public string RecipeName { get; set; }
        public int InputItemId { get; set; }
        public int SuccessRate { get; set; } = 80;
        public List<RecyclingOutput> Outputs { get; set; } = new();
    }

    public class RecyclingOutput
    {
        public int OutputId { get; set; }
        public int RecipeId { get; set; }
        public int MaterialItemId { get; set; }
        public int MinQuantity { get; set; } = 1;
        public int MaxQuantity { get; set; } = 3;
        public float OutputChance { get; set; } = 1.0f;
    }
}

namespace wlo.pserver.core.Game.PetSystem.Models
{
    public class PetTemplate
    {
        public int PetId { get; set; }
        public string PetName { get; set; }
        public string PetType { get; set; } // attack, defense, support, healer
        public int BaseLevel { get; set; } = 1;
        public int MaxLevel { get; set; } = 100;
        public int BaseAttack { get; set; }
        public int BaseDefense { get; set; }
        public int BaseHealth { get; set; }
        public float GrowthAttack { get; set; } = 1.0f;
        public float GrowthDefense { get; set; } = 1.0f;
        public float GrowthHealth { get; set; } = 1.0f;
    }

    public class OwnedPet
    {
        public int OwnedPetId { get; set; }
        public int CharacterId { get; set; }
        public int PetId { get; set; }
        public string PetName { get; set; }
        public int CurrentLevel { get; set; } = 1;
        public long CurrentExperience { get; set; }
        public int CurrentHealth { get; set; }
        public int MaxHealth { get; set; }
        public bool IsActive { get; set; }
        public int Happiness { get; set; } = 100;
        public int Loyalty { get; set; } = 50;
        public DateTime CapturedAt { get; set; }
    }
}

namespace wlo.pserver.core.Game.SkillSystem.Models
{
    public class SkillDefinition
    {
        public int SkillId { get; set; }
        public string SkillName { get; set; }
        public string SkillType { get; set; } // physical, magic, support, passive
        public string SkillCategory { get; set; }
        public int RequiredLevel { get; set; } = 1;
        public int MaxLevel { get; set; } = 20;
        public int CastTimeMs { get; set; }
        public int CooldownMs { get; set; } = 1000;
        public int ManaCost { get; set; }
        public int StaminaCost { get; set; }
        public float RangeMeters { get; set; }
    }

    public class SkillTree
    {
        public int TreeId { get; set; }
        public string TreeName { get; set; }
        public string TreeType { get; set; } // class_specific, universal, hybrid
        public int MaxTier { get; set; } = 5;
        public List<SkillTreeNode> Nodes { get; set; } = new();
    }

    public class SkillTreeNode
    {
        public int NodeId { get; set; }
        public int TreeId { get; set; }
        public int SkillId { get; set; }
        public int NodeTier { get; set; }
        public int? PrerequisiteNodeId { get; set; }
        public int NodeCostPoints { get; set; } = 1;
        public bool IsPassive { get; set; }
    }

    public class CharacterSkill
    {
        public int CharacterSkillId { get; set; }
        public int CharacterId { get; set; }
        public int SkillId { get; set; }
        public int SkillLevel { get; set; } = 1;
        public long SkillExperience { get; set; }
        public DateTime LearnedAt { get; set; }
        public DateTime? LastUsed { get; set; }
        public DateTime? CooldownUntil { get; set; }
    }
}

namespace wlo.pserver.core.Game.WorldSystem.Models
{
    public class WorldMap
    {
        public int MapId { get; set; }
        public string MapName { get; set; }
        public string MapDescription { get; set; }
        public float MapWidth { get; set; }
        public float MapHeight { get; set; }
        public string MapRegion { get; set; } // mainland, dungeon, tower, island
        public int RecommendedLevel { get; set; } = 1;
        public bool PvpEnabled { get; set; }
        public List<MapZone> Zones { get; set; } = new();
        public List<WorldLocation> Locations { get; set; } = new();
        public List<SpawnPoint> SpawnPoints { get; set; } = new();
    }

    public class MapZone
    {
        public int ZoneId { get; set; }
        public int MapId { get; set; }
        public string ZoneName { get; set; }
        public string ZoneType { get; set; } // safe, combat, dungeon
        public float MinX { get; set; }
        public float MaxX { get; set; }
        public float MinY { get; set; }
        public float MaxY { get; set; }
        public bool PvpEnabled { get; set; }
    }

    public class WorldLocation
    {
        public int LocationId { get; set; }
        public int MapId { get; set; }
        public string LocationName { get; set; }
        public string LocationType { get; set; } // town, dungeon_entrance, landmark
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public int RecommendedLevel { get; set; }
    }

    public class SpawnPoint
    {
        public int SpawnId { get; set; }
        public int MapId { get; set; }
        public string SpawnType { get; set; } // character_spawn, resurrection, instance_spawn
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Direction { get; set; }
        public int SpawnRadius { get; set; } = 5;
    }

    public class MapTransition
    {
        public int TransitionId { get; set; }
        public int FromMapId { get; set; }
        public int ToMapId { get; set; }
        public string TransitionType { get; set; } // portal, door, gate, stairway
        public float FromX { get; set; }
        public float FromY { get; set; }
        public float FromZ { get; set; }
        public float ToX { get; set; }
        public float ToY { get; set; }
        public float ToZ { get; set; }
        public bool IsBidirectional { get; set; } = true;
    }
}
