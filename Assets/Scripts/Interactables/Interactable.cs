using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;


public class Interactable : MonoBehaviour
{
    //https://www.youtube.com/watch?v=b7Yf6BFx6js
    public static Interactable Instance { get; private set; }

    Outline outline;
    [Header("Base Message")]
    public string message;

    [Header("Tool Requirement")]
    [Tooltip("Leave empty if this interactable doesn't need a specific tool")]
    [SerializeField] private ItemData requiredTool;

    [Header("Case Messages")]
    [SerializeField, TextArea] private string correctToolMessage;
    [SerializeField, TextArea] private string wrongToolMessage;
    [SerializeField, TextArea] private string missingToolMessage;

    public UnityEvent onInteraction;
    [Tooltip("Fires when the player interacts with the wrong (or no) tool equipped")]
    public UnityEvent onWrongTool;

    [Header("Prompt UI")]
    [SerializeField] private GameObject promptCanvas;
    [SerializeField] private TextMeshProUGUI promptText;

    public System.Func<string> messageProvider;

    void Start()
    {
        outline = GetComponent<Outline>();
        DisableOutline();

       RefreshPrompt();

        DisablePrompt();
    }
    public void RefreshPrompt()
    {
        if (promptText == null) return;
        promptText.text = messageProvider != null ? messageProvider() : GetCurrentMessage();
    }

    private string GetCurrentMessage()
    {
        if (requiredTool == null)
            return message;

        ItemData equipped = InventoryManager.Instance.equippedTool;

        if (equipped == requiredTool)
            return string.IsNullOrEmpty(correctToolMessage) ? message : correctToolMessage;

        if (equipped == null)
            return string.IsNullOrEmpty(missingToolMessage) ? message : missingToolMessage;

        return string.IsNullOrEmpty(wrongToolMessage) ? message : wrongToolMessage;
    }

    public void Interact()
    {
        onInteraction.Invoke();
    }

    public void DisableOutline()
    {
        outline.enabled = false;
        DisablePrompt();
    }

    public void EnableOutline()
    {
        outline.enabled = true;
        RefreshPrompt();
        EnablePrompt();
    }

    private void EnablePrompt() => promptCanvas.SetActive(true);
    private void DisablePrompt() => promptCanvas.SetActive(false);

    //void OnDestroy()
    //{
    //    Debug.LogWarning($"{name} (Interactable) destroyed at {Time.frameCount}", this);
   // }
}
