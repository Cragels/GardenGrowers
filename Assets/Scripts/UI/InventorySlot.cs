using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    ItemData itemToDisplay;

    public Image itemDisplayImage;

    public enum InventoryType
    {
        Item, Tool
    }

    public InventoryType inventoryType;

    int slotIndex;

    public void Display(ItemData itemToDisplay)
    {
        if (itemToDisplay != null) 
        {
            itemDisplayImage.sprite = itemToDisplay.thumbnail;
            this.itemToDisplay = itemToDisplay;

            itemDisplayImage.gameObject.SetActive(true);

            return;
            
        }
        itemDisplayImage.gameObject.SetActive(false);

    }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        //Move item from inventroy to hand
        InventoryManager.Instance.InventoryToHand(slotIndex, inventoryType);

    }

    public void AssignIndex(int slotIndex)
    {
        this.slotIndex = slotIndex;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        UIManager.Instance.DisplayItemInfo(itemToDisplay);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UIManager.Instance.DisplayItemInfo(null);
    }
}
