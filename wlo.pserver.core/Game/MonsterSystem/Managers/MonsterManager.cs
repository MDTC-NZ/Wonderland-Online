using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace wlo.pserver.core.Game.MonsterSystem.Managers
{
    public class MonsterManager
    {
        private readonly MonsterDatabase _database;
        private readonly Dictionary<int, MonsterTemplate> _templateCache = new();
        private readonly Dictionary<int, ActiveMonster> _activeMonsters = new();
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public MonsterManager(MonsterDatabase database)
        {
            _database = database;
        }

        public async Task InitializeAsync()
        {
            try
            {
                Logger.Info("Initializing monster system...");
                var templates = await _database.GetAllMonstersAsync();
                foreach (var template in templates)
                {
                    _templateCache[template.MonsterId] = template;
                }
                Logger.Info($"Loaded {_templateCache.Count} monster templates");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error initializing monster system");
                throw;
            }
        }

        public async Task<MonsterTemplate> GetMonsterTemplateAsync(int monsterId)
        {
            if (_templateCache.TryGetValue(monsterId, out var template))
            {
                return await Task.FromResult(template);
            }
            return null;
        }

        public async Task<ActiveMonster> SpawnMonsterAsync(int monsterId, int mapId, float x, float y, float z)
        {
            try
            {
                var template = await GetMonsterTemplateAsync(monsterId);
                if (template == null) return null;

                var activeMonster = new ActiveMonster
                {
                    MonsterId = monsterId,
                    MapId = mapId,
                    X = x,
                    Y = y,
                    Z = z,
                    CurrentHealth = template.MaxHealth,
                    MaxHealth = template.MaxHealth,
                    Status = "alive",
                    SpawnedAt = DateTime.UtcNow
                };

                await _database.SaveActiveMonsterAsync(activeMonster);
                _activeMonsters[activeMonster.InstanceId] = activeMonster;
                
                Logger.Info($"Monster {monsterId} spawned at map {mapId}");
                return activeMonster;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error spawning monster");
                throw;
            }
        }

        public async Task<bool> DamageMonsterAsync(int instanceId, int damage)
        {
            try
            {
                if (!_activeMonsters.TryGetValue(instanceId, out var monster))
                {
                    return false;
                }

                monster.CurrentHealth = Math.Max(0, monster.CurrentHealth - damage);
                
                if (monster.CurrentHealth <= 0)
                {
                    monster.Status = "dead";
                }

                await _database.UpdateMonsterHealthAsync(instanceId, monster.CurrentHealth);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error damaging monster");
                throw;
            }
        }

        public async Task<bool> RemoveMonsterAsync(int instanceId)
        {
            try
            {
                _activeMonsters.Remove(instanceId);
                await _database.RemoveActiveMonsterAsync(instanceId);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error removing monster");
                throw;
            }
        }
    }
}
