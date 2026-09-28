using UnityEngine;

public class SellAllInteractable : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Interactable interactable;

    private void Start()
    {
        interactable.messageProvider = GetCurrentMessage;
    }

    private void OnEnable()
    {
        // Keep the hover total accurate if inventory changes while not looking at this object
        if (interactable != null) interactable.messageProvider = GetCurrentMessage;
    }

    private int CalculateTotalValue()
    {
        int total = 0;
        ItemData[] items = InventoryManager.Instance.items;
        int[] quantities = InventoryManager.Instance.itemQuantities;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null)
                total += items[i].sellPrice * quantities[i];
        }

        return total;
    }

    private string GetCurrentMessage()
    {
        int total = CalculateTotalValue();
        return total > 0 ? $"Sell All Items - R{total}" : "No items to sell";
    }

    
    public void SellAll()
    {
        int total = CalculateTotalValue();
        if (total <= 0)
        {
            interactable.RefreshPrompt();
            
            return;
        }

        ItemData[] items = InventoryManager.Instance.items;
        int[] quantities = InventoryManager.Instance.itemQuantities;

        for (int i = 0; i < items.Length; i++)
        {
            items[i] = null;
            quantities[i] = 0;
        }

        InventoryManager.Instance.AddMoney(total);
        UIManager.Instance.RenderInventory();
        interactable.RefreshPrompt();
        SFXManager.Instance.PlaySound("Sell");
    }
}
