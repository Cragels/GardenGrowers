using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    

    [Header("Status Bar")]
    public Image toolEquipSlot;


    [Header("UI Text Elements")]
    //public TextMeshProUGUI livesText;
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI timeText;

    private string Settings = "SettingsMenu";
    private string mainMenu = "MainMenu";

    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject Hud;
    [SerializeField] private GameObject gameOverMenu;
    [SerializeField] private GameObject gameWinMenu;
    [SerializeField] private GameObject Inventory;

    [Header("Inventory System")]
    private bool openInventory = true;
    public InventorySlot[] toolSlots;

    //tool equip slot UI on the Inventory Panel
    public HandInventorySlot toolHandSlot;

    public InventorySlot[] itemSlots;

    //item equip slot UI on the Inventory Panel
    public HandInventorySlot itemHandSlot;

    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescriptionText;

    public FirstPersonController firstPersonController;

    public bool inSettings;


    private bool isPaused = false;



    [SerializeField] private InputActionAsset playerControls;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        RenderInventory();
        AssignSlotIndexes();

        inSettings = false;
    }

    public void AssignSlotIndexes()
    {
        for (int i = 0; i < toolSlots.Length; i++) 
        {
            toolSlots[i].AssignIndex(i);
            itemSlots[i].AssignIndex(i);
        }
    }

    public void UpdateTime(int hour, int minute)
    {
        timeText.text = hour + ":" + minute;
    }

    public void UpdateMoney(int money)
    {
        moneyText.text = "R " + money;
    }

    public void FreezeGame()
    {
        Time.timeScale = 0f;
    }

    public void UnFreezeGame()
    {
        Time.timeScale = 1f;
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Time.timeScale = 1;

        Debug.Log("Restart");
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenu);
    }

    public void InventoryUIControl()
    {
        if (openInventory == true) 
        {
            Inventory.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            openInventory = false;

            
        }
        else if (openInventory == false) 
        {
            Inventory.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            openInventory = true;
        }
        RenderInventory();
    }

    public void SettingsLoad()
    {
        SceneManager.LoadScene(Settings, LoadSceneMode.Additive);
        inSettings = true;
    }

    public void CloseSettings()
    {
        SceneManager.UnloadSceneAsync(Settings);
        inSettings = false;
    }

    public void PauseMenuControle()
    {
        if (inSettings == true)
        {
            CloseSettings();
        }
        else
        {
            if (isPaused == false)
            {
                pauseMenu.SetActive(true);
                Hud.SetActive(false);
                isPaused = true;
                FreezeGame();

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                firstPersonController.enabled = false;
            }

            else if (isPaused == true)
            {
                pauseMenu.SetActive(false);
                Hud.SetActive(true);
                isPaused = false;
                UnFreezeGame();

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                firstPersonController.enabled = true;
            }
        }
    }

    public void GameOverMenu()
    {

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        gameOverMenu.SetActive(true);

    }

    public void GameWin()
    {

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        gameWinMenu.SetActive(true);

    }

    public void RenderInventory()
    {
        ItemData[] inventoryToolSlots = InventoryManager.Instance.tools;

        ItemData[] inventoryItemSlots = InventoryManager.Instance.items;

        RenderInventoryPanel(inventoryToolSlots, toolSlots);

        RenderInventoryPanel(inventoryItemSlots, itemSlots);

        toolHandSlot.Display(InventoryManager.Instance.equippedTool);
        itemHandSlot.Display(InventoryManager.Instance.equippedItem);

        FirstPersonToolDisplay.Instance.EquipTool(InventoryManager.Instance.equippedTool);

        //get tool from inventory manager
        ItemData equippedTool = InventoryManager.Instance.equippedTool;

        if (equippedTool != null)
        {
            toolEquipSlot.sprite = equippedTool.thumbnail;
            

            toolEquipSlot.gameObject.SetActive(true);



            return;

        }
        toolEquipSlot.gameObject.SetActive(false);
    }

    void RenderInventoryPanel(ItemData[] Slots, InventorySlot[] uiSlots)
    {
        ItemData[] inventoryToolSlot = InventoryManager.Instance.tools;
        for (int i = 0; i < uiSlots.Length; i++)
        {
            uiSlots[i].Display(Slots[i]);
        }
    }

    public void DisplayItemInfo(ItemData data)
    {
        if (data == null) 
        {
            itemNameText.text = "";
            itemDescriptionText.text = "";

            return;
        }
        itemNameText.text = data.name;
        itemDescriptionText.text = data.description;
    }
}
