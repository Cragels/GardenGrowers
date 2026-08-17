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
    //tool in player hand
    public ItemData equippedTool = null;

    [Header("Items")]
    //Item Slots
    public ItemData[] items = new ItemData[8];
    //item in player hand
    public ItemData equippedItem = null;

    public void InventoryToHand(int slotIndex, InventorySlot.InventoryType inventoryType)
    {
        if(inventoryType == InventorySlot.InventoryType.Item)
        {
            //cache the inventory slot ItemData from InventoryManager
            ItemData itemToEquip = items[slotIndex];

            //change the Inventory slot to the hand's
            items[slotIndex] = equippedItem;

            //change the Hand's Slot to the Inventory Slot's
            equippedItem = itemToEquip;
        }
        else
        {
            //cache the inventory slot ItemData from InventoryManager
            ItemData toolToEquip = tools[slotIndex];

            //change the Inventory slot to the hand's
            tools[slotIndex] = equippedTool;

            //change the Hand's Slot to the Inventory Slot's
            equippedTool = toolToEquip;

            
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
                    equippedItem = null;
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
                    equippedTool = null;
                    break;
                }
            }
            

        }

        UIManager.Instance.RenderInventory();
    }

    public bool AddItem(ItemData item, InventorySlot.InventoryType inventoryType)
    {
        ItemData[] slots = (inventoryType == InventorySlot.InventoryType.Item) ? items : tools;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = item;
                UIManager.Instance.RenderInventory();
                return true; // pickup succeeded
            }
        }

        return false; // inventory full
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

}
