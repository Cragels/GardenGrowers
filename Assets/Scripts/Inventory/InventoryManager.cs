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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
