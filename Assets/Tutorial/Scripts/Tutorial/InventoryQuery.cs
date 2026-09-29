using System;

namespace GardenGrowers.Tutorial
{
    /// <summary>Read-only questions about the player's inventory. Counts include what they're holding.</summary>
    public static class InventoryQuery
    {
        private static InventoryManager Inventory => InventoryManager.Instance;

        public static int Money => Inventory != null ? Inventory.money : 0;

        public static int Count(ItemData item)
        {
            if (item == null) return 0;
            return CountWhere(candidate => candidate == item);
        }

        public static int CountWhere(Func<ItemData, bool> match)
        {
            InventoryManager inventory = Inventory;
            if (inventory == null || match == null) return 0;

            int total = 0;
            total += Sum(inventory.tools, inventory.toolQuantities, match);
            total += Sum(inventory.items, inventory.itemQuantities, match);
            if (inventory.equippedTool != null && match(inventory.equippedTool)) total += Math.Max(1, inventory.equippedToolQuantity);
            if (inventory.equippedItem != null && match(inventory.equippedItem)) total += Math.Max(1, inventory.equippedItemQuantity);
            return total;
        }

        public static int CountSeeds() => CountWhere(item => item is SeedData);

        public static bool IsHolding(ItemData item)
        {
            InventoryManager inventory = Inventory;
            return inventory != null && item != null && (inventory.equippedTool == item || inventory.equippedItem == item);
        }

        public static bool IsHoldingToolType(EquipmentData.ToolType type)
        {
            InventoryManager inventory = Inventory;
            return inventory != null && inventory.equippedTool is EquipmentData tool && tool.toolType == type;
        }

        private static int Sum(ItemData[] slots, int[] quantities, Func<ItemData, bool> match)
        {
            if (slots == null) return 0;
            int total = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null || !match(slots[i])) continue;
                int quantity = quantities != null && i < quantities.Length ? quantities[i] : 1;
                total += Math.Max(1, quantity);
            }
            return total;
        }
    }
}
