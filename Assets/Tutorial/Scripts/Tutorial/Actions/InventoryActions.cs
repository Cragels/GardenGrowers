using System;
using System.Collections;
using GardenGrowers.Core;
using GardenGrowers.FX;
using UnityEngine;

namespace GardenGrowers.Tutorial.Actions
{
    /// <summary>Puts an item in the player's bag and shows a "Gogo gave you" pop-up.</summary>
    [Serializable, MenuPath("Inventory/Give Item")]
    public class GiveItemAction : TutorialAction
    {
        public ItemData item;
        [Min(1)] public int amount = 1;
        public bool showPopup = true;
        public string popupHeading = "Gogo gave you";

        public override IEnumerator Execute(TutorialContext context)
        {
            if (item == null || InventoryManager.Instance == null) yield break;

            // Seeds, fertiliser and tools live in the tools bag, crops in the items bag.
            InventorySlot.InventoryType bag = item is EquipmentData ? InventorySlot.InventoryType.Tool : InventorySlot.InventoryType.Item;
            bool added = InventoryManager.Instance.AddItem(item, amount, bag);
            if (!added) Debug.LogWarning("[Tutorial] Bag full, could not give " + item.itemName);

            if (showPopup && context.Hud != null)
            {
                string label = amount > 1 ? item.itemName + " x" + amount : item.itemName;
                context.Hud.ShowToast(popupHeading, label, item.thumbnail);
            }

            if (context.Fx != null) context.Fx.Play(Sfx.Reward, 0.8f);
        }
    }

    /// <summary>Makes sure the player has at least this many of an item, so they can't get stuck.</summary>
    [Serializable, MenuPath("Inventory/Top Up Item")]
    public class TopUpItemAction : TutorialAction
    {
        public ItemData item;
        [Min(1)] public int atLeast = 1;
        public bool showPopup = true;

        public override IEnumerator Execute(TutorialContext context)
        {
            if (item == null) yield break;
            int missing = atLeast - InventoryQuery.Count(item);
            if (missing <= 0) yield break;

            var give = new GiveItemAction { item = item, amount = missing, showPopup = showPopup };
            yield return give.Execute(context);
        }
    }
}
