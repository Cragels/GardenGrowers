using System.Collections;
using GardenGrowers.Core;
using UnityEngine;

namespace GardenGrowers.CameraSystems
{
    /// <summary>Turns the camera to whoever is talking, then gives control back.</summary>
    [DisallowMultipleComponent]
    public class CameraFocus : MonoBehaviour
    {
        public static CameraFocus Instance { get; private set; }

        [Header("References (found automatically if empty)")]
        [SerializeField] private Transform playerBody;
        [SerializeField] private Transform playerCamera;

        [Header("Feel")]
        [SerializeField, Min(0.1f)] private float turnSharpness = 7f;
        [SerializeField, Min(0.05f)] private float returnDuration = 0.35f;
        [SerializeField] private float maxLookUp = 35f;
        [SerializeField] private float maxLookDown = 45f;
        [Tooltip("Aim a little above the face so the speech bubble is in view.")]
        [SerializeField] private float aimHeightOffset = 0.35f;

        private enum FocusState { Idle, Focusing, Returning }

        private FocusState state = FocusState.Idle;
        private object owner;
        private Transform target;
        private Quaternion savedCameraRotation;
        private Quaternion returnFrom;
        private float returnTimer;

        public bool IsFocusing => state == FocusState.Focusing;

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
            if (playerBody == null)
            {
                var controller = FindFirstObjectByType<FirstPersonController>();
                if (controller != null) playerBody = controller.transform;
            }

            if (playerCamera == null && Camera.main != null) playerCamera = Camera.main.transform;
        }

        /// <summary>Start looking at a speaker. Pauses player control.</summary>
        public void BeginFocus(object who, Transform lookAt)
        {
            FindPlayerIfMissing();
            if (playerBody == null || playerCamera == null || lookAt == null) return;

            // Only remember the original pitch when we were not already focused.
            if (state == FocusState.Idle) savedCameraRotation = playerCamera.localRotation;

            owner = who;
            target = lookAt;
            state = FocusState.Focusing;
            if (PlayerControlLock.Instance != null) PlayerControlLock.Instance.Lock(this);
        }

        /// <summary>Ease back to the player's own view and give control back.</summary>
        public void EndFocus(object who)
        {
            if (state != FocusState.Focusing || owner != who) return;
            owner = null;

            // The camera can already be gone if Play mode stops mid-conversation.
            if (playerCamera == null)
            {
                state = FocusState.Idle;
                target = null;
                if (PlayerControlLock.Instance != null) PlayerControlLock.Instance.Unlock(this);
                return;
            }

            state = FocusState.Returning;
            returnFrom = playerCamera.localRotation;
            returnTimer = 0f;
        }

        /// <summary>A quick look at something outside a conversation. Can be yielded.</summary>
        public IEnumerator LookAtFor(Transform somewhere, float seconds)
        {
            object token = new object();
            BeginFocus(token, somewhere);
            yield return new WaitForSeconds(seconds);
            EndFocus(token);
            while (state == FocusState.Returning) yield return null;
        }

        private void LateUpdate()
        {
            if (playerBody == null || playerCamera == null) return;

            switch (state)
            {
                case FocusState.Focusing:
                    UpdateFocus();
                    break;
                case FocusState.Returning:
                    UpdateReturn();
                    break;
            }
        }

        private void UpdateFocus()
        {
            if (target == null) return;

            Vector3 aimPoint = target.position + Vector3.up * aimHeightOffset;
            Vector3 toTarget = aimPoint - playerCamera.position;
            Vector3 flat = new Vector3(toTarget.x, 0f, toTarget.z);
            if (flat.sqrMagnitude < 0.0001f) return;

            float blend = Ease.Damp(turnSharpness, Time.deltaTime);

            float targetYaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
            float yaw = Mathf.LerpAngle(playerBody.eulerAngles.y, targetYaw, blend);
            playerBody.rotation = Quaternion.Euler(0f, yaw, 0f);

            float targetPitch = -Mathf.Atan2(toTarget.y, flat.magnitude) * Mathf.Rad2Deg;
            targetPitch = Mathf.Clamp(targetPitch, -maxLookUp, maxLookDown);
            float currentPitch = Mathf.DeltaAngle(0f, playerCamera.localEulerAngles.x);
            playerCamera.localRotation = Quaternion.Euler(Mathf.Lerp(currentPitch, targetPitch, blend), 0f, 0f);
        }

        private void UpdateReturn()
        {
            returnTimer += Time.deltaTime;
            float t = Ease.InOutSine(returnTimer / returnDuration);
            playerCamera.localRotation = Quaternion.Slerp(returnFrom, savedCameraRotation, t);

            if (returnTimer >= returnDuration)
            {
                playerCamera.localRotation = savedCameraRotation;
                state = FocusState.Idle;
                target = null;
                if (PlayerControlLock.Instance != null) PlayerControlLock.Instance.Unlock(this);
            }
        }
    }
}
