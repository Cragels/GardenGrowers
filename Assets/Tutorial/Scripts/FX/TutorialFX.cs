using UnityEngine;

namespace GardenGrowers.FX
{
    /// <summary>Plays the tutorial's few sound effects.</summary>
    [DisallowMultipleComponent]
    public class TutorialFX : MonoBehaviour
    {
        public static TutorialFX Instance { get; private set; }

        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.6f;

        private AudioSource uiAudio;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            uiAudio = gameObject.AddComponent<AudioSource>();
            uiAudio.playOnAwake = false;
            uiAudio.spatialBlend = 0f;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Play(Sfx sound, float volume = 1f)
        {
            if (uiAudio == null) return;
            uiAudio.PlayOneShot(ProceduralSfx.Get(sound), sfxVolume * volume);
        }

        /// <summary>The "you did it" sound.</summary>
        public void Celebrate() => Play(Sfx.Chime);
    }
}
