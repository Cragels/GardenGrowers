using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    ItemData itemToDisplay;

    public Image itemDisplayImage;

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

    public void OnPointerEnter(PointerEventData eventData)
    {
        UIManager.Instance.DisplayItemInfo(itemToDisplay);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UIManager.Instance.DisplayItemInfo(null);
    }
}
