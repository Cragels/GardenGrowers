using UnityEngine;
using UnityEngine.EventSystems;

public class HandInventorySlot : InventorySlot
{
    public override void OnPointerClick(PointerEventData eventData)
    {
        //move item from hand to inventory
        InventoryManager.Instance.HandToInventory(inventoryType);
    }
}
