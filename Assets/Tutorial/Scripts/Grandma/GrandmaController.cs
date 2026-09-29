using System.Collections;
using GardenGrowers.Dialogue;
using UnityEngine;

namespace GardenGrowers.NPC
{
    /// <summary>One front door for everything Gogo can do. The tutorial only talks to this class.</summary>
    [DisallowMultipleComponent]
    public class GrandmaController : MonoBehaviour, ISpeechReactor
    {
        [SerializeField] private GrandmaMover mover;
        [SerializeField] private GrandmaAnimator animator;
        [SerializeField] private DialogueSpeaker speaker;
        [Tooltip("Before a conversation she steps back to at least this distance, so the camera isn't in her face.")]
        [SerializeField, Min(0.5f)] private float talkDistance = 2.6f;

        private Transform player;
        private Transform playerCamera;

        public GrandmaMover Mover => mover;
        public GrandmaAnimator Animator => animator;
        public DialogueSpeaker Speaker => speaker;
        public bool IsTalking => speaker != null && speaker.IsSpeaking;
        public Transform Player => player;

        /// <summary>What she points her stick at when a line has the Point gesture.</summary>
        public Transform PointTarget { get; set; }

        private void Awake()
        {
            if (mover == null) mover = GetComponent<GrandmaMover>();
            if (animator == null) animator = GetComponentInChildren<GrandmaAnimator>();
            if (speaker == null) speaker = GetComponent<DialogueSpeaker>();
        }

        private void Start()
        {
            FindPlayer();
        }

        private void FindPlayer()
        {
            if (player == null)
            {
                var controller = FindFirstObjectByType<FirstPersonController>();
                if (controller != null) player = controller.transform;
            }

            if (playerCamera == null && Camera.main != null) playerCamera = Camera.main.transform;
            if (animator != null && animator.LookTarget == null) animator.LookTarget = playerCamera != null ? playerCamera : player;
            if (mover != null && player != null) mover.FaceTowards(player);
        }

        //Movement

        public void FollowPlayer()
        {
            FindPlayer();
            if (player != null) mover.Follow(player);
            mover.FaceTowards(player);
        }

        public void StandStill()
        {
            mover.Stop();
            mover.FaceTowards(player);
        }

        public void GoTo(Vector3 point, float stopDistance = 0.35f)
        {
            mover.GoTo(point, stopDistance);
        }

        /// <summary>Like GoTo, but waits until she arrives or times out.</summary>
        public IEnumerator WalkTo(Vector3 point, float stopDistance = 0.35f, float timeout = 20f)
        {
            mover.GoTo(point, stopDistance);
            float timer = 0f;
            yield return null;
            while (!mover.HasArrived && timer < timeout)
            {
                timer += Time.deltaTime;
                yield return null;
            }
            StandStill();
        }

        public IEnumerator ApproachPlayer(float distance = 3f, float timeout = 20f)
        {
            FindPlayer();
            if (player == null) yield break;

            float timer = 0f;
            Vector3 lastSpot = new Vector3(float.MaxValue, 0f, 0f);
            while (timer < timeout)
            {
                Vector3 fromPlayer = transform.position - player.position;
                fromPlayer.y = 0f;
                if (fromPlayer.magnitude <= distance + 0.3f) break;

                // Only re-plan if the player really moved, so her path stays smooth.
                Vector3 spot = player.position + fromPlayer.normalized * distance;
                if ((spot - lastSpot).sqrMagnitude > 0.25f)
                {
                    mover.GoTo(spot, 0.3f);
                    lastSpot = spot;
                }

                timer += Time.deltaTime;
                yield return null;
            }
            StandStill();
        }

        /// <summary>If the player is too close, she steps back first.</summary>
        public IEnumerator MakeRoom(float minDistance, float timeout = 2.5f)
        {
            FindPlayer();
            if (player == null) yield break;

            Vector3 fromPlayer = transform.position - player.position;
            fromPlayer.y = 0f;
            float distance = fromPlayer.magnitude;
            if (distance >= minDistance) yield break;

            Vector3 away = distance > 0.05f ? fromPlayer / distance : -player.forward;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f) away = Vector3.back;

            // Straight back if there's room, else the nearest free direction.
            Vector3 spot = mover.FindSpotAround(player.position, away, minDistance + 0.3f);
            yield return WalkTo(spot, 0.2f, timeout);
        }

        public void Teleport(Vector3 point, Quaternion rotation)
        {
            mover.Warp(point);
            transform.rotation = rotation;
        }

        //Body language

        /// <summary>Turns to face the target, then points her stick at it.</summary>
        public IEnumerator PointAt(Transform target)
        {
            if (target == null) yield break;

            mover.FaceTowards(target.position);
            float timer = 0f;
            while (mover.AngleTo(target.position) > 25f && timer < 0.8f)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            animator.Play(GestureType.Point, target);
            yield return new WaitForSeconds(1.8f);
            mover.FaceTowards(player);
        }

        public void Celebrate() => animator.Celebrate();

        public void Gesture(GestureType gesture) => animator.Play(gesture, PointTarget);

        //Talking

        /// <summary>Yield this from a coroutine to wait until she finishes talking.</summary>
        public IEnumerator Say(DialogueSequence sequence)
        {
            if (speaker == null || sequence == null || sequence.IsEmpty) yield break;
            // The camera turns to her face, so give it room first.
            if (sequence.focusCamera) yield return MakeRoom(talkDistance);
            yield return speaker.Speak(sequence);
        }

        /// <summary>Talks without waiting, for quick comments while the player keeps playing.</summary>
        public void Comment(DialogueSequence sequence)
        {
            if (speaker == null || sequence == null || sequence.IsEmpty) return;
            speaker.Say(sequence);
        }

        public void StopTalking()
        {
            if (speaker != null) speaker.Stop();
        }

        //ISpeechReactor

        public void OnSpeechStarted()
        {
            FindPlayer();
            if (mover.Speed < 0.2f) mover.FaceTowards(player);
        }

        public void OnLineStarted(DialogueLine line)
        {
            if (line.gesture != GestureType.Point || PointTarget == null) return;

            StartCoroutine(PointAt(PointTarget));
        }

        public void OnSyllable(char letter, Mood mood)
        {
        }

        public void OnLineFinished(DialogueLine line)
        {
        }

        public void OnSpeechEnded()
        {
        }
    }
}
