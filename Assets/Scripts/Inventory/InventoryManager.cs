using TMPro;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }
    [Header("Tools")]
    //Tool Slots
    public ItemData[] tools = new ItemData[8];
    public int[] toolQuantities = new int[8];
    //tool in player hand
    public ItemData equippedTool = null;
    public int equippedToolQuantity = 0;

    [Header("Items")]
    //Item Slots
    public ItemData[] items = new ItemData[8];
    public int[] itemQuantities = new int[8];
    //item in player hand
    public ItemData equippedItem = null;
    public int equippedItemQuantity = 0;

    [Header("Economy")]
    //[SerializeField]public TextMeshProUGUI moneyText;
    public int money = 0;

    public void InventoryToHand(int slotIndex, InventorySlot.InventoryType inventoryType)
    {
        if(inventoryType == InventorySlot.InventoryType.Item)
        {
            //cache the inventory slot ItemData from InventoryManager
            ItemData itemToEquip = items[slotIndex];
            int qtyToEquip = itemQuantities[slotIndex];

            //change the Inventory slot to the hand's
            items[slotIndex] = equippedItem;
            itemQuantities[slotIndex] = equippedItemQuantity;

            //change the Hand's Slot to the Inventory Slot's
            equippedItem = itemToEquip;
            equippedItemQuantity = qtyToEquip;
        }
        else
        {
            //cache the inventory slot ItemData from InventoryManager
            ItemData toolToEquip = tools[slotIndex];
            int qtyToEquip = toolQuantities[slotIndex];

            //change the Inventory slot to the hand's
            tools[slotIndex] = equippedTool;
            toolQuantities[slotIndex] = equippedToolQuantity;

            //change the Hand's Slot to the Inventory Slot's
            equippedTool = toolToEquip;
            equippedToolQuantity = qtyToEquip;


        }
        
        UIManager.Instance.RenderInventory();

    }

    public void HandToInventory(InventorySlot.InventoryType inventoryType)
    {
        if (inventoryType == InventorySlot.InventoryType.Item)
        {
            //itterate and find first empty slot
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null)
                {
                    //send equipped item to nearset empty slot
                    items[i] = equippedItem;
                    itemQuantities[i] = equippedItemQuantity;
                    equippedItem = null;
                    equippedItemQuantity = 0;
                    break;
                }
            }

      
        }
        else
        {
            //itterate and find first empty slot
            for (int i = 0; i < tools.Length; i++)
            {
                if (tools[i] == null)
                {
                    //send equipped item to nearset empty slot
                    tools[i] = equippedTool;
                    toolQuantities[i] = equippedToolQuantity;
                    equippedTool = null;
                    equippedToolQuantity = 0;
                    break;
                }
            }
            

        }

        UIManager.Instance.RenderInventory();
    }

    public bool AddItem(ItemData item, int amount ,InventorySlot.InventoryType inventoryType)
    {
        ItemData[] slots = (inventoryType == InventorySlot.InventoryType.Item) ? items : tools;
        int[] quantities = (inventoryType == InventorySlot.InventoryType.Item) ? itemQuantities : toolQuantities;

        if (item.isStackable)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == item && quantities[i] < item.maxStack)
                {
                    int space = item.maxStack - quantities[i];
                    int toAdd = Mathf.Min(space, amount);
                    quantities[i] += toAdd;
                    amount -= toAdd;
                    if (amount <= 0) { UIManager.Instance.RenderInventory(); return true; }
                }
            }
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = item;
                quantities[i] = Mathf.Min(amount, item.maxStack);
                amount -= quantities[i];
                if (amount <= 0) { UIManager.Instance.RenderInventory(); return true; }
            }
        }

        UIManager.Instance.RenderInventory();
        return amount <= 0;
        /*
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = item;
                UIManager.Instance.RenderInventory();
                return true; // pickup succeeded
            }
        }
        */
        //return false; // inventory full
    }

    public bool ConsumeEquippedTool(int amount = 1)
    {
        if (equippedTool == null || equippedToolQuantity < amount) return false;
        equippedToolQuantity -= amount;
        if (equippedToolQuantity <= 0)
        {
            equippedTool = null;
            equippedToolQuantity = 0;
        }
        UIManager.Instance.RenderInventory();
        return true;
    }

    public void AddMoney(int amount)
    {
        money += amount;
        UIManager.Instance.UpdateMoney(money);
    }

    public bool SpendMoney(int amount)
    {
        if (money < amount) return false;
        money -= amount;
        UIManager.Instance.UpdateMoney(money);
        return true;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

}
