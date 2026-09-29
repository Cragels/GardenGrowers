using System;
using UnityEngine;

namespace GardenGrowers.NPC
{
    /// <summary>The resting local position, rotation and scale of one body part.</summary>
    [Serializable]
    public struct PartRest
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public bool valid;

        public static PartRest Of(Transform part)
        {
            if (part == null) return default;
            return new PartRest
            {
                position = part.localPosition,
                rotation = part.localRotation,
                scale = part.localScale,
                valid = true
            };
        }
    }

    /// <summary>
    /// Holds Gogo's body parts so the animator can move them. Only body is required.
    /// The Gogo baker (Tools > Garden Growers > Tutorial) fills this in.
    /// </summary>
    [DisallowMultipleComponent]
    public class GrandmaRig : MonoBehaviour
    {
        [Header("Parts")]
        [Tooltip("Moves and tilts. Pivot at her feet.")]
        public Transform body;
        [Tooltip("Only scaled (squash and stretch). Child of Body. Can be empty.")]
        public Transform squash;
        public Transform head;
        public Transform freeHand;
        public Transform stickHand;
        [Tooltip("Pivot at the handle, so it swings from her hand.")]
        public Transform stick;

        [Header("Points")]
        [Tooltip("Her face. The player's camera looks here while she talks.")]
        public Transform lookPoint;
        [Tooltip("Where the speech bubble floats.")]
        public Transform bubbleAnchor;

        [Header("Measurements")]
        [Tooltip("Her height in metres. All animation distances scale with this.")]
        [Min(0.1f)] public float height = 2.6f;
        [Tooltip("Direction from the stick's handle to its tip, in the stick's local space.")]
        public Vector3 stickTipLocal = Vector3.down;

        public PartRest BodyRest { get; private set; }
        public PartRest SquashRest { get; private set; }
        public PartRest HeadRest { get; private set; }
        public PartRest FreeHandRest { get; private set; }
        public PartRest StickHandRest { get; private set; }
        public PartRest StickRest { get; private set; }

        public Transform ScaleTarget => squash != null ? squash : body;

        private void Awake()
        {
            CaptureRestPose();
        }

        public void CaptureRestPose()
        {
            BodyRest = PartRest.Of(body);
            SquashRest = PartRest.Of(squash);
            HeadRest = PartRest.Of(head);
            FreeHandRest = PartRest.Of(freeHand);
            StickHandRest = PartRest.Of(stickHand);
            StickRest = PartRest.Of(stick);
        }

        /// <summary>The "outward" direction for a hand, away from her middle, in body space.</summary>
        public Vector3 OutwardOf(PartRest hand)
        {
            float side = Mathf.Sign(hand.position.x);
            if (Mathf.Approximately(side, 0f)) side = 1f;
            return new Vector3(side, 0f, 0f);
        }
    }
}
