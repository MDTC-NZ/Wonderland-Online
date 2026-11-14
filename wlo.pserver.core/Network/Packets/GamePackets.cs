using System;
using System.Collections.Generic;

namespace wlo.pserver.core.Network.Packets
{
    /// <summary>
    /// Network packet definitions for all game systems
    /// Handles serialization/deserialization of game events
    /// </summary>

    // Quest Packets
    public class QuestStartPacket
    {
        public int QuestId { get; set; }
        public string QuestName { get; set; }
        public string Description { get; set; }
        public int[] ObjectiveIds { get; set; }
    }

    public class QuestProgressPacket
    {
        public int QuestId { get; set; }
        public int ObjectiveId { get; set; }
        public int Progress { get; set; }
        public int TargetAmount { get; set; }
    }

    public class QuestCompletePacket
    {
        public int QuestId { get; set; }
        public int ExperienceReward { get; set; }
        public int MoneyReward { get; set; }
        public int[] ItemRewards { get; set; }
    }

    // Shop Packets
    public class ShopOpenPacket
    {
        public int ShopId { get; set; }
        public string ShopName { get; set; }
        public ShopItem[] Items { get; set; }
    }

    public class ShopItem
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public int Price { get; set; }
        public int Quantity { get; set; }
    }

    public class BuyItemPacket
    {
        public int ShopId { get; set; }
        public int ItemId { get; set; }
        public int Quantity { get; set; }
    }

    public class SellItemPacket
    {
        public int ShopId { get; set; }
        public int ItemId { get; set; }
        public int Quantity { get; set; }
    }

    // Inventory Packets
    public class InventorySyncPacket
    {
        public int[] ItemIds { get; set; }
        public int[] Quantities { get; set; }
        public int UsedSlots { get; set; }
        public int TotalSlots { get; set; }
    }

    public class ItemPickupPacket
    {
        public int ItemId { get; set; }
        public int Quantity { get; set; }
        public int SlotIndex { get; set; }
    }

    public class ItemDropPacket
    {
        public int ItemId { get; set; }
        public int Quantity { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }

    public class EquipItemPacket
    {
        public int ItemId { get; set; }
        public string EquipmentSlot { get; set; }
    }

    // Combat Packets
    public class AttackPacket
    {
        public int TargetId { get; set; }
        public int TargetType { get; set; }
        public int Damage { get; set; }
        public bool CriticalHit { get; set; }
    }

    public class DamagePacket
    {
        public int DamageAmount { get; set; }
        public int CurrentHealth { get; set; }
        public int SourceId { get; set; }
    }

    public class DeathPacket
    {
        public int CharacterId { get; set; }
        public int KillerId { get; set; }
        public int ExperienceLost { get; set; }
    }

    // Crafting Packets
    public class CraftRecipePacket
    {
        public int RecipeId { get; set; }
        public int Quantity { get; set; }
    }

    public class CraftingQueueStatusPacket
    {
        public int QueueId { get; set; }
        public string Status { get; set; }
        public int Progress { get; set; }
        public DateTime EstimatedCompletion { get; set; }
    }

    public class CraftingResultPacket
    {
        public int RecipeId { get; set; }
        public int ItemId { get; set; }
        public int Quantity { get; set; }
        public bool Success { get; set; }
    }

    /// <summary>
    /// Packet router - directs packets to appropriate handlers
    /// </summary>
    public class PacketRouter
    {
        private readonly Dictionary<Type, object> _handlers = new();
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public void Register<T>(object handler) where T : class
        {
            _handlers[typeof(T)] = handler;
            Logger.Debug($"Registered handler for {typeof(T).Name}");
        }

        public void Route(object packet, int connectionId)
        {
            var packetType = packet.GetType();
            if (_handlers.TryGetValue(packetType, out var handler))
            {
                try
                {
                    Logger.Debug($"Routing packet {packetType.Name} to handler");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Error handling packet type {packetType.Name}");
                }
            }
            else
            {
                Logger.Warn($"No handler registered for packet type {packetType.Name}");
            }
        }
    }
}
