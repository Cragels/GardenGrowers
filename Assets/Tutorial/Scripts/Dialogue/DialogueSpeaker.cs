using System;
using System.Collections;
using UnityEngine;

namespace GardenGrowers.Dialogue
{
    /// <summary>Put this on any NPC that talks. Plays lines through its speech bubble and voice.</summary>
    [DisallowMultipleComponent]
    public class DialogueSpeaker : MonoBehaviour
    {
        [SerializeField] private SpeakerProfile profile;
        [SerializeField] private SpeechBubble bubble;
        [SerializeField] private BabbleVoice voice;
        [Tooltip("Where the player's camera looks during a conversation (usually the face).")]
        [SerializeField] private Transform lookPoint = null;

        private ISpeechReactor[] reactors = Array.Empty<ISpeechReactor>();
        private Coroutine running;
        private int speechId;
        private Mood currentMood;
        private bool cameraFocused;

        public SpeakerProfile Profile => profile;
        public Transform LookPoint => lookPoint != null ? lookPoint : transform;
        public bool IsSpeaking { get; private set; }

        /// <summary>The line being said right now (already formatted), or null.</summary>
        public string CurrentText { get; private set; }

        public Transform BubbleTransform => bubble != null ? bubble.transform : transform;

        public SpeechBubble Bubble => bubble;

        /// <summary>Optional text filter, like swapping {earned} for a number.</summary>
        public Func<string, string> TextFormatter { get; set; }

        public event Action<DialogueLine> LineStarted;
        public event Action SpeechEnded;

        private void Awake()
        {
            if (bubble == null) bubble = GetComponentInChildren<SpeechBubble>(true);
            if (voice == null) voice = GetComponent<BabbleVoice>();
            reactors = GetComponentsInChildren<ISpeechReactor>(true);
        }

        private void OnEnable()
        {
            if (bubble != null && bubble.Typewriter != null) bubble.Typewriter.CharacterRevealed += HandleCharacter;
        }

        private void OnDisable()
        {
            if (bubble != null && bubble.Typewriter != null) bubble.Typewriter.CharacterRevealed -= HandleCharacter;
            running = null;
            if (IsSpeaking) EndSpeech();
            ReleaseCamera();
        }

        public void SetProfile(SpeakerProfile newProfile) => profile = newProfile;

        /// <summary>Starts talking. Yield the returned coroutine to wait for it.</summary>
        public Coroutine Say(DialogueSequence sequence)
        {
            Stop();
            running = StartCoroutine(Speak(sequence));
            return running;
        }

        public void Stop()
        {
            speechId++; // any conversation still running notices and quits
            if (running != null)
            {
                StopCoroutine(running);
                running = null;
            }

            if (IsSpeaking) EndSpeech();
        }

        /// <summary>The conversation itself. Yield it straight from another coroutine.</summary>
        public IEnumerator Speak(DialogueSequence sequence)
        {
            if (sequence == null || sequence.IsEmpty || bubble == null) yield break;
            if (IsSpeaking) Stop();

            int id = ++speechId;
            IsSpeaking = true;
            foreach (ISpeechReactor reactor in reactors) reactor.OnSpeechStarted();

            if (sequence.focusCamera && GardenGrowers.CameraSystems.CameraFocus.Instance != null)
            {
                GardenGrowers.CameraSystems.CameraFocus.Instance.BeginFocus(this, LookPoint);
                cameraFocused = true;
            }

            bubble.Show(profile);
            DialogueInput input = DialogueInput.Instance;

            foreach (DialogueLine line in sequence.lines)
            {
                if (line == null || string.IsNullOrWhiteSpace(line.text)) continue;

                currentMood = line.mood;
                string text = TextFormatter != null ? TextFormatter(line.text) : line.text;
                CurrentText = text;
                float moodSpeed = profile != null ? profile.MoodSpeed(line.mood) : 1f;

                LineStarted?.Invoke(line);
                foreach (ISpeechReactor reactor in reactors) reactor.OnLineStarted(line);

                bubble.Present(text, profile, moodSpeed);
                float lineStart = Time.unscaledTime;

                while (bubble.Typewriter.IsTyping)
                {
                    if (id != speechId) yield break;
                    bool fast = sequence.waitForInput && input.IsHeld;
                    // Only a tap that started on this line counts, so the key that moved us here can't skip it.
                    bool freshTap = input.TappedThisFrame && input.PressedAt >= lineStart;
                    if (sequence.waitForInput && freshTap) bubble.Typewriter.Complete();
                    float multiplier = fast && profile != null ? profile.fastForwardMultiplier : 1f;
                    bubble.Typewriter.Tick(Time.deltaTime, multiplier);
                    yield return null;
                }

                foreach (ISpeechReactor reactor in reactors) reactor.OnLineFinished(line);

                if (sequence.waitForInput)
                {
                    bubble.SetContinueVisible(true);
                    float heldTimer = 0f;
                    while (true)
                    {
                        if (id != speechId) yield break;
                        if (input.PressedThisFrame) break;
                        if (input.IsHeld)
                        {
                            heldTimer += Time.deltaTime;
                            if (heldTimer > 0.4f) break; // holding skips through quickly
                        }
                        yield return null;
                    }
                    bubble.SetContinueVisible(false);
                    yield return null; // wait a frame so one press can't skip two lines
                }
                else
                {
                    float wait = line.holdSeconds;
                    if (profile != null) wait += profile.autoAdvanceBaseDelay + text.Length * profile.autoAdvancePerCharacter;
                    while (wait > 0f)
                    {
                        if (id != speechId) yield break;
                        wait -= Time.deltaTime;
                        yield return null;
                    }
                }
            }

            if (id != speechId) yield break;
            EndSpeech();
            running = null;
        }

        private void EndSpeech()
        {
            CurrentText = null;
            if (bubble != null) bubble.Hide();
            ReleaseCamera();
            IsSpeaking = false;
            foreach (ISpeechReactor reactor in reactors) reactor.OnSpeechEnded();
            SpeechEnded?.Invoke();
        }

        private void ReleaseCamera()
        {
            if (!cameraFocused) return;
            cameraFocused = false;
            if (GardenGrowers.CameraSystems.CameraFocus.Instance != null) GardenGrowers.CameraSystems.CameraFocus.Instance.EndFocus(this);
        }

        private void HandleCharacter(char letter)
        {
            bool spoke = voice != null && voice.Speak(letter, profile, currentMood);
            if (!spoke) return;
            foreach (ISpeechReactor reactor in reactors) reactor.OnSyllable(letter, currentMood);
        }
    }
}
