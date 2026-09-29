using System.Collections;
using GardenGrowers.Dialogue;
using UnityEngine;

namespace GardenGrowers.NPC
{
    /// <summary>Keeps Gogo at her stall in the main level and has her react when the player sells.</summary>
    [DisallowMultipleComponent]
    public class GogoStall : MonoBehaviour
    {
        [SerializeField] private GrandmaController gogo;
        [Tooltip("The sell counter's Interactable. The group's SellAllInteractable is on the same object.")]
        [SerializeField] private Interactable counter;
        [Tooltip("{earned} becomes the amount made.")]
        [SerializeField] private DialogueLine soldLine = new DialogueLine("R{earned}! Siyabonga!", Mood.Excited);
        [SerializeField] private DialogueLine nothingToSellLine = new DialogueLine("Bring me some vegetables first, my child.", Mood.Neutral);

        private int moneyLastFrame;
        private Coroutine reacting;

        private void Awake()
        {
            if (gogo == null) gogo = GetComponent<GrandmaController>();
            if (counter == null) counter = GetComponentInChildren<Interactable>(true);
        }

        private void OnEnable()
        {
            if (counter != null) counter.onInteraction.AddListener(OnSell);
        }

        private void OnDisable()
        {
            if (counter != null) counter.onInteraction.RemoveListener(OnSell);
        }

        private void Start()
        {
            if (gogo != null) gogo.StandStill();
        }

        // Money at the end of each frame, so OnSell can work out what the sale made.
        private void LateUpdate()
        {
            if (InventoryManager.Instance != null) moneyLastFrame = InventoryManager.Instance.money;
        }

        // Added at runtime, so it runs after the group's SellAll on the same event.
        private void OnSell()
        {
            if (gogo == null || gogo.Speaker == null || InventoryManager.Instance == null) return;

            int earned = InventoryManager.Instance.money - moneyLastFrame;
            if (reacting != null) StopCoroutine(reacting);
            reacting = StartCoroutine(React(earned));
        }

        private IEnumerator React(int earned)
        {
            DialogueLine template = earned > 0 ? soldLine : nothingToSellLine;
            var line = new DialogueLine(template.text.Replace("{earned}", earned.ToString()), template.mood, template.gesture);
            yield return gogo.Speaker.Say(DialogueSequence.Comment(line));

            if (earned > 0) gogo.Celebrate();
            reacting = null;
        }
    }
}
