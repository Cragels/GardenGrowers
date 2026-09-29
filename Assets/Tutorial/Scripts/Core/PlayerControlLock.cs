using System;
using System.Collections.Generic;
using UnityEngine;

namespace GardenGrowers.Core
{
    /// <summary>Pauses player control until every owner has unlocked it.</summary>
    [DisallowMultipleComponent]
    public class PlayerControlLock : MonoBehaviour
    {
        public static PlayerControlLock Instance { get; private set; }

        [SerializeField] private FirstPersonController firstPersonController;
        [SerializeField] private PlayerInteraction playerInteraction;
        [Tooltip("Stop the camera spinning while the player moves the mouse to click bag slots.")]
        [SerializeField] private bool freezeWhileBagOpen = true;

        private readonly HashSet<object> owners = new HashSet<object>();
        private readonly object bagOwner = new object();

        // PlayerInteraction keeps what you're looking at in a private field.
        private static readonly System.Reflection.FieldInfo HoveredField =
            typeof(PlayerInteraction).GetField("currentInteractable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        public bool IsLocked => owners.Count > 0;

        /// <summary>True when control is taken away, false when it comes back.</summary>
        public event Action<bool> LockChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            FindPlayerIfMissing();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void FindPlayerIfMissing()
        {
            if (firstPersonController == null) firstPersonController = FindFirstObjectByType<FirstPersonController>();
            if (playerInteraction == null) playerInteraction = FindFirstObjectByType<PlayerInteraction>();
        }

        public void Lock(object owner)
        {
            if (owner == null) return;
            if (owners.Add(owner) && owners.Count == 1) Apply(true);
        }

        public void Unlock(object owner)
        {
            if (owner == null) return;
            if (owners.Remove(owner) && owners.Count == 0) Apply(false);
        }

        public void UnlockAll()
        {
            if (owners.Count == 0) return;
            owners.Clear();
            Apply(false);
        }

        private void Apply(bool locked)
        {
            FindPlayerIfMissing();
            if (locked) HideHoverPrompt();
            if (firstPersonController != null) firstPersonController.enabled = !locked;
            if (playerInteraction != null) playerInteraction.enabled = !locked;

            // The E that closed the conversation must not also trigger what the player looks at.
            if (!locked && PlayerInputHandler.Instance != null) PlayerInputHandler.Instance.ConsumeInteract();

            LockChanged?.Invoke(locked);
        }

        /// <summary>Turning PlayerInteraction off left its "E TO ..." prompt stuck on screen, so clear it.</summary>
        private void HideHoverPrompt()
        {
            if (playerInteraction == null || HoveredField == null) return;
            if (!(HoveredField.GetValue(playerInteraction) is Interactable hovered) || hovered == null) return;
            if (hovered.GetComponent<global::Outline>() != null) hovered.DisableOutline();
            HoveredField.SetValue(playerInteraction, null);
        }

        private void Update()
        {
            // The group's bag leaves mouse-look on, so hold the player still while it's open.
            if (!freezeWhileBagOpen) return;
            bool bagOpen = GardenGrowers.UI.BagSlotHighlighter.IsBagOpen;
            bool holding = owners.Contains(bagOwner);
            if (bagOpen && !holding) Lock(bagOwner);
            else if (!bagOpen && holding) Unlock(bagOwner);
        }

        private void LateUpdate()
        {
            // The pause menu turns the controller back on when it closes, so keep it off while locked.
            if (IsLocked && firstPersonController != null && firstPersonController.enabled && Time.timeScale > 0f)
            {
                firstPersonController.enabled = false;
            }
        }
    }
}
