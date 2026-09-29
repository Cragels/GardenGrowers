using UnityEngine;

namespace GardenGrowers.Dialogue
{
    /// <summary>A character's name, typing speed, voice and bubble colours. Make one per character.</summary>
    [CreateAssetMenu(menuName = "Garden Growers/Dialogue/Speaker Profile", fileName = "New Speaker")]
    public class SpeakerProfile : ScriptableObject
    {
        [Header("Name")]
        public string displayName = "Gogo Thandi";

        [Header("Typing")]
        [Min(1f)] public float charactersPerSecond = 34f;
        [Min(1f)] public float fastForwardMultiplier = 3.5f;
        [Tooltip("Pause after . , ! ? so sentences breathe.")]
        [Min(0f)] public float punctuationPause = 0.16f;

        [Header("Auto-advance (quick comments)")]
        [Min(0f)] public float autoAdvanceBaseDelay = 1.1f;
        [Min(0f)] public float autoAdvancePerCharacter = 0.035f;

        [Header("Voice (babble)")]
        [Tooltip("1 = normal. Higher = squeakier.")]
        [Range(0.5f, 2f)] public float voicePitch = 1.08f;
        [Range(0f, 0.5f)] public float pitchVariation = 0.12f;
        [Tooltip("Shortest gap between two voice blips, in seconds.")]
        [Range(0.03f, 0.2f)] public float blipInterval = 0.072f;
        [Range(0f, 1f)] public float voiceVolume = 0.45f;
        [Tooltip("Optional. Real voice clips here replace the built-in babble.")]
        public AudioClip[] voiceClips;

        [Header("Speech bubble")]
        public Color textColor = Color.white;
        public Color nameColor = new Color(1f, 0.61f, 0f);

        /// <summary>Voice pitch for a mood. Excited sounds higher, sad sounds lower.</summary>
        public float MoodPitch(Mood mood)
        {
            switch (mood)
            {
                case Mood.Excited: return 1.16f;
                case Mood.Happy: return 1.07f;
                case Mood.Proud: return 1.02f;
                case Mood.Thinking: return 0.97f;
                case Mood.Worried: return 1.05f;
                case Mood.Sad: return 0.9f;
                default: return 1f;
            }
        }

        /// <summary>Typing speed for a mood. Sad lines come out slower.</summary>
        public float MoodSpeed(Mood mood)
        {
            switch (mood)
            {
                case Mood.Excited: return 1.2f;
                case Mood.Sad: return 0.8f;
                case Mood.Thinking: return 0.9f;
                default: return 1f;
            }
        }
    }
}
