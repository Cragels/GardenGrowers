using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;


public class Interactable : MonoBehaviour
{
    //https://www.youtube.com/watch?v=b7Yf6BFx6js
    Outline outline;
    public string message;

    public UnityEvent onInteraction;

    [Header("Prompt UI")]
    [SerializeField] private GameObject promptCanvas;
    [SerializeField] private TextMeshProUGUI promptText;


    void Start()
    {
        outline = GetComponent<Outline>();
        DisableOutline();

        if (promptText != null)
            promptText.text = message;

        DisablePrompt();
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
        EnablePrompt();
    }

    private void EnablePrompt() => promptCanvas.SetActive(true);
    private void DisablePrompt() => promptCanvas.SetActive(false);

    //void OnDestroy()
    //{
    //    Debug.LogWarning($"{name} (Interactable) destroyed at {Time.frameCount}", this);
   // }
}
