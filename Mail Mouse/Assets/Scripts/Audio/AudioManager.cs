using UnityEngine;

[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Volumes")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Instance = Instance;
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();

        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();

        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();

        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        musicSource.loop = true;
        musicSource.playOnAwake = false;
        sfxSource.playOnAwake = false;

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

    public static void PlaySFX(AudioClip clip, float volumeScale = 1f, float pitch = 1f)
    {
        if (clip == null)
            return;

        AudioManager manager = EnsureInstance();
        manager.PlaySFXInternal(clip, volumeScale, pitch);
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
            return;

        AudioManager manager = EnsureInstance();
        manager.PlayMusicInternal(clip, loop);
    }

    public static void StopMusic()
    {
        if (Instance == null)
            return;

        Instance.musicSource?.Stop();
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

    private void PlaySFXInternal(AudioClip clip, float volumeScale, float pitch)
    {
        if (sfxSource == null || clip == null)
            return;

        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, GetEffectiveSfxVolume(volumeScale));
    }

    private void PlayMusicInternal(AudioClip clip, bool loop)
    {
        if (musicSource == null || clip == null)
            return;

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.volume = GetEffectiveMusicVolume();
        musicSource.Play();
    }

    private void ApplyVolumes()
    {
        if (musicSource != null)
            musicSource.volume = GetEffectiveMusicVolume();

        if (sfxSource != null)
            sfxSource.volume = GetEffectiveSfxVolume();
    }
}
