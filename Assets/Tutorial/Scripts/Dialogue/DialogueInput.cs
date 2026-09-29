using UnityEngine;
using UnityEngine.InputSystem;

namespace GardenGrowers.Dialogue
{
    /// <summary>
    /// Reads the "next line" button in conversations. Tap to skip ahead, hold to type faster.
    /// No mouse click, so it can never click the bag by accident.
    /// </summary>
    [DisallowMultipleComponent]
    public class DialogueInput : MonoBehaviour
    {
        private static DialogueInput instance;

        [SerializeField, Tooltip("Seconds the key must be held before it counts as 'hold to speed up'.")]
        private float holdThreshold = 0.2f;

        private InputAction advance;
        private float pressedAt = -1f;

        public static DialogueInput Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<DialogueInput>();
                    if (instance == null) instance = new GameObject("DialogueInput").AddComponent<DialogueInput>();
                }
                return instance;
            }
        }

        /// <summary>Pressed this frame. Ignored while the game is paused.</summary>
        public bool PressedThisFrame => Ready && advance.WasPressedThisFrame();

        /// <summary>Released this frame after a short tap.</summary>
        public bool TappedThisFrame => Ready && advance.WasReleasedThisFrame() && pressedAt >= 0f && Time.unscaledTime - pressedAt < holdThreshold;

        /// <summary>Unscaled time the current press started, or -1 if not pressed.</summary>
        public float PressedAt => pressedAt;

        /// <summary>Held long enough to count as "hold to speed up".</summary>
        public bool IsHeld => Ready && advance.IsPressed() && pressedAt >= 0f && Time.unscaledTime - pressedAt >= holdThreshold;

        private bool Ready => advance != null && Time.timeScale > 0f;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }

            instance = this;
            advance = new InputAction("DialogueAdvance", InputActionType.Button);
            advance.AddBinding("<Keyboard>/e");
            advance.AddBinding("<Keyboard>/space");
            advance.AddBinding("<Keyboard>/enter");
            advance.AddBinding("<Gamepad>/buttonSouth");
        }

        private void OnEnable() => advance?.Enable();

        private void OnDisable() => advance?.Disable();

        private void OnDestroy()
        {
            advance?.Dispose();
            if (instance == this) instance = null;
        }

        private void Update()
        {
            if (advance == null) return;
            if (advance.WasPressedThisFrame()) pressedAt = Time.unscaledTime;
        }

        private void LateUpdate()
        {
            if (advance != null && !advance.IsPressed()) pressedAt = -1f;
        }
    }
}
