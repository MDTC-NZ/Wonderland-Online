using System;
using System.Collections.Generic;

namespace wlo.pserver.core.Game.ShopSystem.Models
{
    public class Shop
    {
        public int ShopId { get; set; }
        public string ShopName { get; set; }
        public string ShopDescription { get; set; }
        public int NpcId { get; set; }
        public string ShopType { get; set; }
        public int MapId { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float BuyMultiplier { get; set; } = 1.0f;
        public float SellMultiplier { get; set; } = 0.8f;
        public int MaxInventorySlots { get; set; } = 50;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public List<ShopInventoryItem> InventoryItems { get; set; } = new();
    }

    public class ShopInventoryItem
    {
        public int InventoryId { get; set; }
        public int ShopId { get; set; }
        public int ItemId { get; set; }
        public int ItemQuantity { get; set; } = 1;
        public int ItemPrice { get; set; }
        public int RestockIntervalHours { get; set; } = 24;
        public DateTime LastRestocked { get; set; }
        public int MaxStock { get; set; } = 100;
        public bool IsPurchasable { get; set; } = true;
        public bool IsSellable { get; set; } = true;
    }

    public class ShopTransaction
    {
        public int TransactionId { get; set; }
        public int CharacterId { get; set; }
        public int ShopId { get; set; }
        public int ItemId { get; set; }
        public string TransactionType { get; set; } // buy, sell
        public int Quantity { get; set; }
        public int UnitPrice { get; set; }
        public int TotalPrice { get; set; }
        public DateTime TransactionTimestamp { get; set; }
    }

    public class NPC
    {
        public int NpcId { get; set; }
        public string NpcName { get; set; }
        public string NpcType { get; set; }
        public int MapId { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public string NpcDescription { get; set; }
        public bool IsAttackable { get; set; } = false;
        public int Level { get; set; } = 1;
    }
}
