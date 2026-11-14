using System;
using System.Collections.Generic;
using System.Linq;

namespace wlo.pserver.core.Game.InventorySystem.Models
{
    public class Inventory
    {
        public int CharacterId { get; set; }
        public Dictionary<int, InventorySlot> MainSlots { get; set; } = new();
        public Dictionary<string, InventorySlot> EquipmentSlots { get; set; } = new();
        public Dictionary<int, InventorySlot> QuickSlots { get; set; } = new();
        public int MaxMainSlots { get; set; } = 50;
        public int UsedMainSlots => MainSlots.Values.Count(s => s.ItemId.HasValue);

        public bool CanAddItem(int itemId, int quantity, int maxStackSize) =>
            UsedMainSlots < MaxMainSlots || MainSlots.Values.Any(s => s.ItemId == itemId && s.Quantity < maxStackSize);

        public void ClearCache()
        {
            MainSlots.Clear();
            EquipmentSlots.Clear();
            QuickSlots.Clear();
        }
    }

    public class InventorySlot
    {
        public int SlotIndex { get; set; }
        public int? ItemId { get; set; }
        public int? StackId { get; set; }
        public int Quantity { get; set; }
        public bool IsLocked { get; set; }
        public DateTime LastModified { get; set; }
    }

    public class Item
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public string ItemDescription { get; set; }
        public string ItemType { get; set; }
        public string ItemGrade { get; set; }
        public int ItemLevel { get; set; } = 1;
        public int MaxStack { get; set; } = 1;
        public float Weight { get; set; }
        public bool IsTradeable { get; set; } = true;
        public bool IsDroppable { get; set; } = true;
        public bool IsSellable { get; set; } = true;
        public int BasePrice { get; set; }
        public int DurabilityMax { get; set; } = 100;
        public List<ItemAttribute> Attributes { get; set; } = new();
    }

    public class ItemStack
    {
        public int StackId { get; set; }
        public int ItemId { get; set; }
        public int? CharacterId { get; set; }
        public int Quantity { get; set; } = 1;
        public DateTime CreatedAt { get; set; }
        public DateTime LastModified { get; set; }
        public List<ItemEnchantment> Enchantments { get; set; } = new();
    }

    public class ItemAttribute
    {
        public int AttributeId { get; set; }
        public int ItemId { get; set; }
        public string AttributeType { get; set; }
        public int AttributeValue { get; set; }
        public bool IsRandomRoll { get; set; }
        public int? MinRoll { get; set; }
        public int? MaxRoll { get; set; }
    }

    public class ItemEnchantment
    {
        public int EnchantmentId { get; set; }
        public int StackId { get; set; }
        public string EnchantmentType { get; set; }
        public int EnchantmentLevel { get; set; } = 1;
        public int BonusValue { get; set; }
        public DateTime EnchantedAt { get; set; }
    }
}
