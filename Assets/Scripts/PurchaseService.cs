using UnityEngine;

public class PurchaseService : MonoBehaviour
{
    [Header("What's being sold")]
    [SerializeField] private ItemData itemToSell;
    [SerializeField] private InventorySlot.InventoryType inventoryType;
    [SerializeField] private int quantity = 1;

    [Header("References")]
    [SerializeField] private Interactable interactable;

    private void Start()
    {
        interactable.messageProvider = GetCurrentMessage;
    }

    public void Purchase()
    {
        int cost = itemToSell.buyPrice * quantity;

        if (!InventoryManager.Instance.SpendMoney(cost))
        {
            Debug.Log("Not enough money for " + itemToSell);
            interactable.RefreshPrompt();
            return;
        }

        bool added = InventoryManager.Instance.AddItem(itemToSell, quantity, inventoryType);

        if (!added)
        {
            InventoryManager.Instance.AddMoney(cost); // refund, inventory full
            interactable.RefreshPrompt();
            return;
        }

        
    }

    private string GetCurrentMessage()
    {
        return $"Buy {itemToSell} x{quantity} - R{itemToSell.buyPrice * quantity}";
    }
}
