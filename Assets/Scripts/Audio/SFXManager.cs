using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Audio;

//https://www.youtube.com/watch?v=oKXP1HZ8xIs

public class SFXManager : MonoBehaviour
{
   
    public static SFXManager Instance { get; private set; }

    public AudioMixerGroup sfxGroup;


    [System.Serializable]
    public struct SFXEntry // Got to use scruct again ;)
    {
        public string key;
        public AudioClip clip;
    }

   

    [Header("Sound Effects")]
    public SFXEntry[] sfxEntries;

    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("Settings")]
    [Range(0f, 1f)]
    public float volume = 1f;

    
    
    private HashMap<string, AudioClip> _sfxMap;

   
    private void Awake()
    {
        
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Auto-add AudioSource if not assigned
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.outputAudioMixerGroup = sfxGroup;

        // Initialise the HashMap and load all sounds into it
        InitialiseSFXMap();
    }

   
    private void InitialiseSFXMap()
    {
        _sfxMap = new HashMap<string, AudioClip>();

        if (sfxEntries == null || sfxEntries.Length == 0)
        {
            Debug.LogWarning("[SFXManager] No SFX entries assigned in the Inspector.");
            return;
        }

        foreach (SFXEntry entry in sfxEntries)
        {
            if (string.IsNullOrEmpty(entry.key))
            {
                Debug.LogWarning("[SFXManager] Skipping entry with empty key.");
                continue;
            }

            if (entry.clip == null)
            {
                Debug.LogWarning($"[SFXManager] AudioClip for key '{entry.key}' is null. Skipping.");
                continue;
            }

            _sfxMap.Add(entry.key, entry.clip);
            Debug.Log($"[SFXManager] Loaded sound: '{entry.key}'");
        }

        Debug.Log($"[SFXManager] Initialised with {_sfxMap.Count} sounds.");
    }

    
    public void PlaySound(string soundKey)
    {
        if (string.IsNullOrEmpty(soundKey))
        {
            Debug.LogWarning("[SFXManager] PlaySound called with an empty or null key.");
            return;
        }

        AudioClip clip;

       
        if (_sfxMap.TryGetValue(soundKey, out clip))
        {
            audioSource.PlayOneShot(clip, volume);
        }
        else
        {
            Debug.LogWarning($"[SFXManager] Sound key '{soundKey}' not found in HashMap.");
        }
    }

    public void PlaySoundAtPosition(string soundKey, Vector3 position)
    {
        if (string.IsNullOrEmpty(soundKey))
        {
            Debug.LogWarning("[SFXManager] PlaySoundAtPosition called with an empty key.");
            return;
        }

        AudioClip clip;

        if (_sfxMap.TryGetValue(soundKey, out clip))
        {
            AudioSource.PlayClipAtPoint(clip, position, volume);
        }
        else
        {
            Debug.LogWarning($"[SFXManager] Sound key '{soundKey}' not found in HashMap.");
        }
    }

  
    public void RegisterSound(string soundKey, AudioClip clip)
    {
        if (string.IsNullOrEmpty(soundKey) || clip == null)
        {
            Debug.LogWarning("[SFXManager] RegisterSound: invalid key or null clip.");
            return;
        }

        _sfxMap.Add(soundKey, clip);
        Debug.Log($"[SFXManager] Registered new sound at runtime: '{soundKey}'");
    }

  
    public bool HasSound(string soundKey)
    {
        return _sfxMap.ContainsKey(soundKey);
    }

 
    public void UnregisterSound(string soundKey)
    {
        if (_sfxMap.Remove(soundKey))
            Debug.Log($"[SFXManager] Removed sound: '{soundKey}'");
        else
            Debug.LogWarning($"[SFXManager] Could not remove '{soundKey}' — key not found.");
    }

    
    [ContextMenu("Debug: Print HashMap Contents")]
    public void PrintHashMapContents()
    {
        Debug.Log(_sfxMap.ToString());
    }
}
