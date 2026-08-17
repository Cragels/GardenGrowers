using UnityEngine;

public class PickUpItem : MonoBehaviour
{
    [Header("Item to Give")]
    [SerializeField] private ItemData itemToPickup;
    [SerializeField] private InventorySlot.InventoryType inventoryType;

    [Header("Behaviour")]
    [SerializeField] private bool destroyOnPickup = true;

    public void Pickup()
    {
        bool added = InventoryManager.Instance.AddItem(itemToPickup, inventoryType);

        if (!added)
        {
            Debug.Log("Inventory full, can't pick up ");
            return; // leave it in the world so the player can try again later
        }

        if (destroyOnPickup)
            Destroy(gameObject);
        
    }
}
