using System.Collections.Generic;
using GardenGrowers.Core;
using GardenGrowers.Dialogue;
using UnityEngine;

namespace GardenGrowers.NPC
{
    /// <summary>
    /// Animates Gogo in code: idle, walk and talk states, small gestures on top,
    /// and her head turning to follow the player.
    /// </summary>
    [RequireComponent(typeof(GrandmaRig))]
    [DisallowMultipleComponent]
    public class GrandmaAnimator : MonoBehaviour, ISpeechReactor
    {
        [SerializeField] private GrandmaMover mover;
        [SerializeField] private GrandmaAnimationSettings settings = new GrandmaAnimationSettings();
        [Tooltip("Speed (m/s) above which she counts as walking.")]
        [SerializeField, Min(0f)] private float walkThreshold = 0.15f;

        private GrandmaIdleState idleState;
        private GrandmaWalkState walkState;
        private GrandmaTalkState talkState;
        private GrandmaState current;
        private GrandmaState previous;
        private float blend = 1f;

        private readonly List<GestureClip> gestures = new List<GestureClip>();

        private readonly SpringFloat squashSpring = new SpringFloat();
        private readonly SpringFloat nodSpring = new SpringFloat();
        private readonly SpringVector3 lookSpring = new SpringVector3();
        private readonly SpringVector3 freeHandSpring = new SpringVector3();
        private readonly SpringVector3 stickHandSpring = new SpringVector3();

        private Vector3 stickAimPoint;
        private float stickAimWeight;
        private bool springsReady;

        public GrandmaRig Rig { get; private set; }
        public GrandmaAnimationSettings Settings => settings;
        public float Height => Rig != null ? Rig.height : 2f;
        public float LocalTime { get; private set; }
        public float LocomotionSpeed { get; private set; }
        public float TalkEnergy { get; private set; }
        public RigPose MoodPose { get; private set; }
        public Mood CurrentMood { get; private set; }
        public bool IsTalking { get; private set; }
        public bool IsGesturing => gestures.Count > 0;
        public string CurrentStateName => current != null ? current.Name : "None";

        /// <summary>What her head follows (usually the player's camera).</summary>
        public Transform LookTarget { get; set; }

        private void Awake()
        {
            Rig = GetComponent<GrandmaRig>();
            if (mover == null) mover = GetComponentInParent<GrandmaMover>();

            idleState = new GrandmaIdleState(this);
            walkState = new GrandmaWalkState(this);
            talkState = new GrandmaTalkState(this, idleState);
            current = idleState;
            current.Enter();
        }

        private void Start()
        {
            ResetSprings();
        }

        /// <summary>Puts the animation numbers back to their defaults. Tool 2 uses this on the prefab.</summary>
        public void UseDefaultSettings() => settings = new GrandmaAnimationSettings();

        private void ResetSprings()
        {
            if (Rig == null) return;
            if (Rig.FreeHandRest.valid) freeHandSpring.Reset(Rig.FreeHandRest.position);
            if (Rig.StickHandRest.valid) stickHandSpring.Reset(Rig.StickHandRest.position);
            squashSpring.Reset(0f);
            springsReady = true;
        }

        //Gestures

        /// <summary>Play a one-shot gesture. Point needs a target.</summary>
        public void Play(GestureType type, Transform target = null)
        {
            GestureClip clip = GestureClip.Create(type, target);
            if (clip == null) return;

            // Never stack two of the same gesture.
            for (int i = gestures.Count - 1; i >= 0; i--)
            {
                if (gestures[i].GetType() != clip.GetType()) continue;
                gestures[i].End(this);
                gestures.RemoveAt(i);
            }

            clip.Begin(this);
            gestures.Add(clip);
        }

        /// <summary>A small happy hop.</summary>
        public void Celebrate() => Play(GestureType.Hop);

        public void StopGestures()
        {
            foreach (GestureClip clip in gestures) clip.End(this);
            gestures.Clear();
        }

        /// <summary>Points the walking stick at a world position (weight 0 = off).</summary>
        public void SetStickAim(Vector3 worldPoint, float weight)
        {
            stickAimPoint = worldPoint;
            stickAimWeight = Mathf.Clamp01(weight);
        }

        //Frame update

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || Rig == null || Rig.body == null || !Rig.BodyRest.valid) return;
            if (!springsReady) ResetSprings();

            LocalTime += dt;

            float moverSpeed = mover != null ? mover.Speed : 0f;
            bool turning = mover != null && mover.IsTurningInPlace;
            LocomotionSpeed = Mathf.Max(moverSpeed, turning ? 0.9f : 0f);

            GrandmaState desired = LocomotionSpeed > walkThreshold ? walkState : IsTalking ? talkState : (GrandmaState)idleState;
            if (desired != current) SwitchState(desired);

            // State, crossfaded when it changes.
            RigPose pose = current.Evaluate(dt);
            if (blend < 1f && previous != null)
            {
                blend = Mathf.Min(1f, blend + dt / Mathf.Max(0.01f, settings.stateBlendTime));
                pose = RigPose.Lerp(previous.Evaluate(dt), pose, Ease.Smooth(blend));
            }

            // Mood body language while talking.
            RigPose moodTarget = IsTalking ? GrandmaMoodPoses.For(CurrentMood, this) : default;
            MoodPose = RigPose.Lerp(MoodPose, moodTarget, Ease.Damp(5f, dt));
            TalkEnergy = Mathf.MoveTowards(TalkEnergy, 0f, dt * 1.5f);

            // Gestures on top.
            Transform lookOverride = null;
            for (int i = gestures.Count - 1; i >= 0; i--)
            {
                GestureClip clip = gestures[i];
                clip.Advance(dt);
                pose += clip.Evaluate(this);
                if (clip.LookOverride != null) lookOverride = clip.LookOverride;
                if (!clip.IsDone) continue;
                clip.End(this);
                gestures.RemoveAt(i);
            }

            // Lean into turns, bob on syllables, turn the head to the look target.
            if (mover != null)
            {
                pose.bodyEuler.z += Mathf.Clamp(-mover.AngularVelocity * settings.turnLean, -settings.maxTurnLean, settings.maxTurnLean);
            }
            pose.headEuler.x += nodSpring.Step(0f, settings.nodSpring, dt);
            pose.headEuler += ComputeLook(lookOverride != null ? lookOverride : LookTarget, dt);

            Apply(pose, dt);
        }

        private void SwitchState(GrandmaState next)
        {
            current.Exit();
            previous = current;
            current = next;
            current.Enter();
            blend = 0f;
        }

        private Vector3 ComputeLook(Transform target, float dt)
        {
            Vector3 desired = Vector3.zero;
            if (target != null && Rig.head != null && Rig.HeadRest.valid)
            {
                Vector3 local = Rig.body.InverseTransformPoint(target.position);
                Vector3 eyes = Rig.HeadRest.position + Vector3.up * (0.12f * Height);
                Vector3 d = local - eyes;
                float flat = new Vector2(d.x, d.z).magnitude;
                if (flat > 0.05f)
                {
                    float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                    float pitch = -Mathf.Atan2(d.y, flat) * Mathf.Rad2Deg;
                    desired = new Vector3(
                        Mathf.Clamp(pitch, -settings.lookUpLimit, settings.lookDownLimit),
                        Mathf.Clamp(yaw, -settings.lookYawLimit, settings.lookYawLimit),
                        0f);
                }
            }

            return lookSpring.Step(desired, settings.lookSpring, dt);
        }

        private void Apply(RigPose pose, float dt)
        {
            GrandmaRig r = Rig;

            r.body.localPosition = r.BodyRest.position + pose.bodyOffset;
            r.body.localRotation = r.BodyRest.rotation * Quaternion.Euler(pose.bodyEuler);

            // Taller means thinner, so she keeps her volume.
            float stretch = Mathf.Clamp(1f + squashSpring.Step(pose.squash, settings.squashSpring, dt), 0.8f, 1.2f);
            float thin = 1f / Mathf.Sqrt(stretch);
            Vector3 squashScale = new Vector3(thin, stretch, thin);

            PartRest scaleRest = r.squash != null ? r.SquashRest : r.BodyRest;
            if (r.ScaleTarget != null && scaleRest.valid) r.ScaleTarget.localScale = Vector3.Scale(scaleRest.scale, squashScale);

            if (r.head != null && r.HeadRest.valid)
            {
                r.head.localPosition = Vector3.Scale(r.HeadRest.position, squashScale) + pose.headOffset;
                r.head.localRotation = r.HeadRest.rotation * Quaternion.Euler(pose.headEuler);
            }

            if (r.freeHand != null && r.FreeHandRest.valid)
            {
                Vector3 target = Vector3.Scale(r.FreeHandRest.position, squashScale) + pose.freeHandOffset;
                r.freeHand.localPosition = freeHandSpring.Step(target, settings.handSpring, dt);
                r.freeHand.localRotation = r.FreeHandRest.rotation * Quaternion.Euler(pose.freeHandEuler);
            }

            if (r.stickHand != null && r.StickHandRest.valid)
            {
                Vector3 target = Vector3.Scale(r.StickHandRest.position, squashScale) + pose.stickHandOffset;
                r.stickHand.localPosition = stickHandSpring.Step(target, settings.handSpring, dt);
                r.stickHand.localRotation = r.StickHandRest.rotation * Quaternion.Euler(pose.stickHandEuler);
            }

            if (r.stick != null && r.StickRest.valid)
            {
                r.stick.localRotation = r.StickRest.rotation * Quaternion.Euler(pose.stickEuler);
                if (stickAimWeight > 0.001f)
                {
                    Vector3 tipDirection = r.stick.TransformDirection(r.stickTipLocal.normalized);
                    Vector3 wanted = stickAimPoint - r.stick.position;
                    if (wanted.sqrMagnitude > 0.0001f)
                    {
                        Quaternion aimed = Quaternion.FromToRotation(tipDirection, wanted.normalized) * r.stick.rotation;
                        r.stick.rotation = Quaternion.Slerp(r.stick.rotation, aimed, stickAimWeight);
                    }
                }
            }
        }

        //Reacting to talking

        public void OnSpeechStarted()
        {
            IsTalking = true;
        }

        public void OnLineStarted(DialogueLine line)
        {
            CurrentMood = line.mood;
            TalkEnergy = 1f;
            // Point needs a target, so GrandmaController handles it.
            if (line.gesture != GestureType.None && line.gesture != GestureType.Point) Play(line.gesture);
        }

        public void OnSyllable(char letter, Mood mood)
        {
            nodSpring.AddImpulse(settings.syllableNod * Random.Range(0.6f, 1.1f));
            TalkEnergy = 1f;
        }

        public void OnLineFinished(DialogueLine line)
        {
        }

        public void OnSpeechEnded()
        {
            IsTalking = false;
            CurrentMood = Mood.Neutral;
        }
    }
}
