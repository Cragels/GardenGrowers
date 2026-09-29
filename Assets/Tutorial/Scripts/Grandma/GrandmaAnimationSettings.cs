using System;
using GardenGrowers.Core;
using UnityEngine;

namespace GardenGrowers.NPC
{
    /// <summary>The numbers behind Gogo's animation. Distances are fractions of her height.</summary>
    [Serializable]
    public class GrandmaAnimationSettings
    {
        [Header("Walk")]
        [Tooltip("Length of one step, as a fraction of her height.")]
        [Range(0.05f, 0.6f)] public float stepLength = 0.2f;
        [Range(0f, 0.2f)] public float hopHeight = 0.012f;
        [Range(0f, 0.3f)] public float hurryHopHeight = 0.025f;
        [Tooltip("Side-to-side roll per step, in degrees.")]
        [Range(0f, 25f)] public float waddle = 4f;
        [Range(0f, 20f)] public float twist = 2f;
        [Tooltip("Forward lean per m/s of speed, in degrees.")]
        [Range(0f, 10f)] public float leanPerSpeed = 2f;
        [Range(0f, 30f)] public float maxLean = 8f;
        [Tooltip("Speed that counts as a normal walk and as a hurry (m/s).")]
        public float walkSpeedReference = 1.8f;
        public float hurrySpeedReference = 4.2f;

        [Header("Turning")]
        [Tooltip("Degrees of lean per degree/second of turning.")]
        [Range(0f, 0.2f)] public float turnLean = 0.02f;
        [Range(0f, 30f)] public float maxTurnLean = 6f;

        [Header("Blending")]
        [Tooltip("Crossfade time between Idle, Walk and Talk.")]
        [Range(0.05f, 1f)] public float stateBlendTime = 0.25f;

        [Header("Springs")]
        public SpringSettings squashSpring = new SpringSettings(4f, 0.9f);
        public SpringSettings handSpring = new SpringSettings(3.2f, 0.85f);
        public SpringSettings lookSpring = new SpringSettings(2.4f, 0.9f);
        public SpringSettings nodSpring = new SpringSettings(5f, 0.7f);

        [Header("Head tracking")]
        [Range(0f, 90f)] public float lookYawLimit = 65f;
        [Range(0f, 45f)] public float lookUpLimit = 20f;
        [Range(0f, 45f)] public float lookDownLimit = 28f;

        [Header("Talking")]
        [Tooltip("How much her head bobs on each voice blip.")]
        [Range(0f, 300f)] public float syllableNod = 45f;
    }
}
