using System.Reflection;
using UnityEngine;

namespace GardenGrowers.Tutorial
{
    /// <summary>
    /// Makes Gogo's sell counter sell the item in your hand too, and show it in the price.
    /// The group's SellAll only counts the bag, so this moves the held item in first.
    /// </summary>
    [DefaultExecutionOrder(-100)] // before PlayerInteraction, so the item is in the bag when SellAll counts
    [DisallowMultipleComponent]
    public class SellIncludesHeldItem : MonoBehaviour
    {
        [SerializeField] private Interactable interactable;

        // PlayerInteraction keeps what you are looking at in a private field.
        private static readonly FieldInfo HoveredField =
            typeof(PlayerInteraction).GetField("currentInteractable", BindingFlags.Instance | BindingFlags.NonPublic);

        private PlayerInteraction playerInteraction;
        private System.Func<string> priceMessage;

        private void Awake()
        {
            if (interactable == null) interactable = GetComponent<Interactable>();
            priceMessage = PriceMessage;
        }

        private void Update()
        {
            if (interactable == null) return;

            // SellAllInteractable sets its own price text on Start and OnEnable. Keep ours.
            if (interactable.messageProvider != priceMessage) interactable.messageProvider = priceMessage;

            InventoryManager inventory = InventoryManager.Instance;
            PlayerInputHandler input = PlayerInputHandler.Instance;
            if (inventory == null || input == null || inventory.equippedItem == null) return;
            if (!input.InteractTriggered || !IsLookedAt()) return;

            inventory.HandToInventory(InventorySlot.InventoryType.Item);
        }

        private bool IsLookedAt()
        {
            if (playerInteraction == null) playerInteraction = FindFirstObjectByType<PlayerInteraction>();
            if (playerInteraction == null || !playerInteraction.enabled || HoveredField == null) return false;
            return ReferenceEquals(HoveredField.GetValue(playerInteraction), interactable);
        }

        /// <summary>Same text as the group's prompt, but the price includes what you are holding.</summary>
        private static string PriceMessage()
        {
            int total = BagValue() + (HasFreeBagSlot() ? HandValue() : 0);
            return total > 0 ? "Sell All Items - R" + total : "No items to sell";
        }

        private static int BagValue()
        {
            InventoryManager inventory = InventoryManager.Instance;
            if (inventory == null) return 0;

            int total = 0;
            for (int i = 0; i < inventory.items.Length; i++)
            {
                if (inventory.items[i] != null) total += inventory.items[i].sellPrice * inventory.itemQuantities[i];
            }
            return total;
        }

        private static int HandValue()
        {
            InventoryManager inventory = InventoryManager.Instance;
            if (inventory == null || inventory.equippedItem == null) return 0;
            return inventory.equippedItem.sellPrice * inventory.equippedItemQuantity;
        }

        /// <summary>The held item can only go back in the bag if there is an empty slot.</summary>
        private static bool HasFreeBagSlot()
        {
            InventoryManager inventory = InventoryManager.Instance;
            if (inventory == null) return false;

            foreach (ItemData item in inventory.items)
            {
                if (item == null) return true;
            }
            return false;
        }
    }
}
