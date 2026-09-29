using System;
using GardenGrowers.Core;
using UnityEngine;

namespace GardenGrowers.Tutorial.Conditions
{
    /// <summary>Done when the player has at least this many of an item (bag + hand).</summary>
    [Serializable, MenuPath("Inventory/Has Item")]
    public class HasItemCondition : TutorialCondition
    {
        public ItemData item;
        [Min(1)] public int amount = 1;

        public override bool IsMet(TutorialContext context) => InventoryQuery.Count(item) >= amount;
    }

    /// <summary>Done when the item is in the player's hand (not just in the bag).</summary>
    [Serializable, MenuPath("Inventory/Holding Item")]
    public class HoldingItemCondition : TutorialCondition
    {
        public ItemData item;

        public override bool IsMet(TutorialContext context) => InventoryQuery.IsHolding(item);
    }

    /// <summary>Done when the player holds any tool of this type (any seed counts for Seeds).</summary>
    [Serializable, MenuPath("Inventory/Holding Tool Type")]
    public class HoldingToolTypeCondition : TutorialCondition
    {
        public EquipmentData.ToolType toolType = EquipmentData.ToolType.Shovel;

        public override bool IsMet(TutorialContext context) => InventoryQuery.IsHoldingToolType(toolType);
    }

    /// <summary>Done when the player gains this many of an item, or any seed, since the step started.</summary>
    [Serializable, MenuPath("Inventory/Gain Item")]
    public class GainItemCondition : TutorialCondition
    {
        public ItemData item;
        public bool anySeed;
        [Min(1)] public int amount = 1;

        private int startCount;

        public override void Begin(TutorialContext context) => startCount = Current();

        public override bool IsMet(TutorialContext context) => Current() - startCount >= amount;

        public override string Progress(TutorialContext context)
        {
            if (amount <= 1) return null;
            return Mathf.Clamp(Current() - startCount, 0, amount) + " / " + amount;
        }

        private int Current() => anySeed ? InventoryQuery.CountSeeds() : InventoryQuery.Count(item);
    }

    /// <summary>Done when the player earns this much since the step started. Saves it as {earned} for Gogo.</summary>
    [Serializable, MenuPath("Inventory/Earn Money")]
    public class EarnMoneyCondition : TutorialCondition
    {
        [Min(1)] public int amount = 1;

        private int startMoney;

        public override void Begin(TutorialContext context) => startMoney = InventoryQuery.Money;

        public override bool IsMet(TutorialContext context)
        {
            int earned = InventoryQuery.Money - startMoney;
            if (earned < amount) return false;
            context.SetVariable("earned", earned.ToString());
            context.SetVariable("money", InventoryQuery.Money.ToString());
            return true;
        }
    }
}
