using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Sound Library")]
    [SerializeField] private AudioClips audioClips;

    [Header("Playback Rules")]
    [Tooltip("If true, sounds will be prevented from overlapping when the same SFX channel is already busy.")]
    [SerializeField] private bool preventSfxOverlap = true;

    [Tooltip("How long a sound remains considered active before another same-category sound can play again.")]
    [SerializeField] private float overlapCooldown = 0.1f;

    [Header("Volumes")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    private readonly Dictionary<string, float> lastPlayedTimes = new Dictionary<string, float>();
    private bool musicPausedByTimeScale;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[AudioManager] Another AudioManager already exists; destroying duplicate instance.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        AudioSource[] existingSources = GetComponents<AudioSource>();

        if (musicSource == null)
            musicSource = existingSources.Length > 0 ? existingSources[0] : gameObject.AddComponent<AudioSource>();

        if (sfxSource == null)
        {
            if (existingSources.Length > 1)
                sfxSource = existingSources[1];
            else
                sfxSource = gameObject.AddComponent<AudioSource>();
        }

        if (musicSource == sfxSource)
            sfxSource = gameObject.AddComponent<AudioSource>();

        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;

        ApplyVolumes();
    }

    public static AudioManager EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        AudioManager existing = FindFirstObjectByType<AudioManager>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject audioManagerObject = new GameObject("AudioManager");
        return audioManagerObject.AddComponent<AudioManager>();
    }

    public static void PlaySFX(AudioClip clip, float volumeScale = 1f, float pitch = 1f, string channel = "default")
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] PlaySFX called with a null clip.");
            return;
        }

        AudioManager manager = EnsureInstance();
        Debug.Log($"[AudioManager] Playing SFX '{clip.name}' via {manager.name}");
        manager.PlaySFXInternal(clip, volumeScale, pitch, channel);
    }

    public static void PlayInventoryOpenSound()
    {
        AudioManager manager = EnsureInstance();
        manager.PlayInventoryOpenSoundInternal();
    }

    public static void PlayInventoryCloseSound()
    {
        AudioManager manager = EnsureInstance();
        manager.PlayInventoryCloseSoundInternal();
    }

    public static void PlayPauseOpenSound()
    {
        AudioManager manager = EnsureInstance();
        manager.PlayPauseOpenSoundInternal();
    }

    public static void PlayPauseCloseSound()
    {
        AudioManager manager = EnsureInstance();
        manager.PlayPauseCloseSoundInternal();
    }

    public static void PlayUIButtonClickSound()
    {
        AudioManager manager = EnsureInstance();
        manager.PlayUIButtonClickSoundInternal();
    }

    public static void PlayPackageSound()
    {
        AudioManager manager = EnsureInstance();
        manager.PlayPackageSoundInternal();
    }

    public static void PlaySFXAtPoint(AudioClip clip, Vector3 position, float volumeScale = 1f)
    {
        if (clip == null)
            return;

        AudioSource.PlayClipAtPoint(clip, position, Mathf.Clamp01(Instance != null ? Instance.GetEffectiveSfxVolume(volumeScale) : volumeScale));
    }

    public static void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] PlayMusic called with a null clip.");
            return;
        }

        AudioManager manager = EnsureInstance();
        Debug.Log($"[AudioManager] Playing music '{clip.name}' via {manager.name}");
        manager.PlayMusicInternal(clip, loop);
    }

    public static void StopMusic()
    {
        if (Instance == null)
            return;

        Instance.musicSource?.Stop();
    }

    private void Update()
    {
        if (musicSource == null)
            return;

        if (Time.timeScale <= 0f)
        {
            if (musicSource.isPlaying)
            {
                musicSource.Pause();
                musicPausedByTimeScale = true;
            }
            return;
        }

        if (musicPausedByTimeScale && musicSource.clip != null && !musicSource.isPlaying)
        {
            musicSource.UnPause();
            musicPausedByTimeScale = false;
        }
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        ApplyVolumes();
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        ApplyVolumes();
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        ApplyVolumes();
    }

    public float GetEffectiveMusicVolume()
    {
        return masterVolume * musicVolume;
    }

    public float GetEffectiveSfxVolume(float volumeScale = 1f)
    {
        return Mathf.Clamp01(masterVolume * sfxVolume * volumeScale);
    }

    private void PlayInventoryOpenSoundInternal()
    {
        if (audioClips != null && audioClips.inventoryOpen != null)
        {
            Debug.Log("[AudioManager] Playing inventory open sound.");
            PlaySFX(audioClips.inventoryOpen, channel: "inventory_ui");
            return;
        }

        Debug.LogWarning("[AudioManager] Inventory open sound clip is not assigned.");
    }

    private void PlayInventoryCloseSoundInternal()
    {
        if (audioClips != null && audioClips.inventoryClose != null)
        {
            Debug.Log("[AudioManager] Playing inventory close sound.");
            PlaySFX(audioClips.inventoryClose, channel: "inventory_ui");
            return;
        }

        Debug.LogWarning("[AudioManager] Inventory close sound clip is not assigned.");
    }

    private void PlayPauseOpenSoundInternal()
    {
        if (audioClips != null && audioClips.uiOpen != null)
        {
            Debug.Log("[AudioManager] Playing pause open sound.");
            PlaySFX(audioClips.uiOpen, channel: "menu_ui");
            return;
        }

        Debug.LogWarning("[AudioManager] Pause open sound clip is not assigned.");
    }

    private void PlayPauseCloseSoundInternal()
    {
        if (audioClips != null && audioClips.uiClose != null)
        {
            Debug.Log("[AudioManager] Playing pause close sound.");
            PlaySFX(audioClips.uiClose, channel: "menu_ui");
            return;
        }

        Debug.LogWarning("[AudioManager] Pause close sound clip is not assigned.");
    }

    private void PlayUIButtonClickSoundInternal()
    {
        if (audioClips != null && audioClips.uiClick != null)
        {
            Debug.Log("[AudioManager] Playing button click sound.");
            PlaySFX(audioClips.uiClick, channel: "menu_ui");
            return;
        }

        Debug.LogWarning("[AudioManager] UI click sound clip is not assigned.");
    }

    private void PlayPackageSoundInternal()
    {
        if (audioClips != null && audioClips.packageSound != null)
        {
            Debug.Log("[AudioManager] Playing package sound.");
            PlaySFX(audioClips.packageSound, channel: "inventory_ui");
            return;
        }

        Debug.LogWarning("[AudioManager] Package sound clip is not assigned.");
    }

    private void PlaySFXInternal(AudioClip clip, float volumeScale, float pitch, string channel)
    {
        if (sfxSource == null || clip == null)
        {
            Debug.LogWarning($"[AudioManager] Cannot play SFX '{clip?.name ?? "null"}' because the SFX AudioSource is missing.");
            return;
        }

        if (preventSfxOverlap && ShouldBlockPlayback(channel, clip.name))
        {
            Debug.Log($"[AudioManager] Blocked overlapping SFX '{clip.name}' on channel '{channel}'.");
            return;
        }

        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, GetEffectiveSfxVolume(volumeScale));
        lastPlayedTimes[channel] = Time.unscaledTime;
        Debug.Log($"[AudioManager] SFX playback started: '{clip.name}', channel='{channel}', volume={GetEffectiveSfxVolume(volumeScale):0.00}, pitch={pitch:0.00}");
    }

    private void PlayMusicInternal(AudioClip clip, bool loop)
    {
        if (musicSource == null || clip == null)
        {
            Debug.LogWarning($"[AudioManager] Cannot play music '{clip?.name ?? "null"}' because the music AudioSource is missing.");
            return;
        }

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.volume = GetEffectiveMusicVolume();
        musicSource.Play();

        if (Time.timeScale <= 0f)
        {
            musicSource.Pause();
            musicPausedByTimeScale = true;
        }
        else
        {
            musicPausedByTimeScale = false;
        }

        Debug.Log($"[AudioManager] Music playback started: '{clip.name}', loop={loop}");
    }

    private bool ShouldBlockPlayback(string channel, string clipName)
    {
        if (string.IsNullOrEmpty(channel))
            return false;

        if (!lastPlayedTimes.TryGetValue(channel, out float lastTime))
            return false;

        return Time.unscaledTime - lastTime < overlapCooldown;
    }

    private void ApplyVolumes()
    {
        if (musicSource != null)
            musicSource.volume = GetEffectiveMusicVolume();

        if (sfxSource != null)
            sfxSource.volume = GetEffectiveSfxVolume();
    }
}
