using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    ItemData itemToDisplay;

    public Image itemDisplayImage;
    [SerializeField] private TextMeshProUGUI quantityText;
    public enum InventoryType
    {
        Item, Tool
    }

    public InventoryType inventoryType;

    int slotIndex;

    public void Display(ItemData itemToDisplay, int quantity = 1)
    {
        if (itemToDisplay != null) 
        {
            itemDisplayImage.sprite = itemToDisplay.thumbnail;
            this.itemToDisplay = itemToDisplay;

            itemDisplayImage.gameObject.SetActive(true);

            if (quantityText != null)
                quantityText.text = quantity > 1 ? quantity.ToString() : "";

            return;
            
        }

        itemDisplayImage.gameObject.SetActive(false);
        if (quantityText != null) quantityText.text = "";

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
