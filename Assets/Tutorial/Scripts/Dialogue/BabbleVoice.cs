using UnityEngine;

namespace GardenGrowers.Dialogue
{
    /// <summary>
    /// Plays gibberish voice blips while text types out.
    /// Cycles through a few AudioSources so a new pitch never bends a blip still playing.
    /// </summary>
    public class BabbleVoice : MonoBehaviour, IDialogueVoice
    {
        [SerializeField, Range(0f, 1f)] private float spatialBlend = 0.55f;
        [SerializeField, Min(0.1f)] private float minDistance = 4f;
        [SerializeField, Min(1f)] private float maxDistance = 40f;
        [SerializeField, Range(1, 6)] private int voices = 3;

        private AudioSource[] sources;
        private int nextSource;
        private float lastBlipTime = -10f;

        private void Awake()
        {
            BuildSources();
        }

        private void BuildSources()
        {
            if (sources != null) return;

            sources = new AudioSource[voices];
            for (int i = 0; i < voices; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = spatialBlend;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = minDistance;
                source.maxDistance = maxDistance;
                source.dopplerLevel = 0f;
                sources[i] = source;
            }
        }

        public bool Speak(char letter, SpeakerProfile speaker, Mood mood)
        {
            if (speaker == null || !char.IsLetterOrDigit(letter)) return false;
            if (Time.unscaledTime - lastBlipTime < speaker.blipInterval) return false;

            BuildSources();
            AudioClip clip = PickClip(letter, speaker);
            if (clip == null) return false;

            lastBlipTime = Time.unscaledTime;

            // Mostly random pitch, plus a small fixed offset per letter so words feel consistent.
            float letterOffset = ((char.ToLowerInvariant(letter) % 7) - 3) * 0.015f;
            float variation = Random.Range(-speaker.pitchVariation, speaker.pitchVariation);

            AudioSource source = sources[nextSource];
            nextSource = (nextSource + 1) % sources.Length;
            source.pitch = Mathf.Clamp(speaker.voicePitch * speaker.MoodPitch(mood) * (1f + variation + letterOffset), 0.3f, 3f);
            source.PlayOneShot(clip, speaker.voiceVolume);
            return true;
        }

        private static AudioClip PickClip(char letter, SpeakerProfile speaker)
        {
            if (speaker.voiceClips != null && speaker.voiceClips.Length > 0)
            {
                int index = Mathf.Abs(char.ToLowerInvariant(letter)) % speaker.voiceClips.Length;
                return speaker.voiceClips[index];
            }

            AudioClip[] vowels = BabbleSynth.VowelClips;
            return vowels[BabbleSynth.VowelIndexFor(letter)];
        }
    }
}
