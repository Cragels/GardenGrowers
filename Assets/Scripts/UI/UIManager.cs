using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

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
    public InventorySlot[] itemSlots;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescriptionText;


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
    }

    public void CloseSettings()
    {
        SceneManager.UnloadSceneAsync(Settings);
    }

    public void PauseMenuControle()
    {
        if (isPaused == false)
        {
            pauseMenu.SetActive(true);
            Hud.SetActive(false);
            isPaused = true;
            FreezeGame();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        else if (isPaused == true)
        {
            pauseMenu.SetActive(false);
            Hud.SetActive(true);
            isPaused = false;
            UnFreezeGame();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
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
