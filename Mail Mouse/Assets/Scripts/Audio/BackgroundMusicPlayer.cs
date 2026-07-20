using UnityEngine;

public class BackgroundMusicPlayer : MonoBehaviour
{
    [Header("Music")]
    [SerializeField] private AudioClip musicClip;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool loop = true;
    [SerializeField] private bool stopOnDisable = true;

    private void Start()
    {
        if (!playOnStart)
            return;

        if (musicClip != null)
            AudioManager.PlayMusic(musicClip, loop);
        else
            Debug.LogWarning("[BackgroundMusicPlayer] No music clip assigned.");
    }

    private void OnDisable()
    {
        if (stopOnDisable)
            AudioManager.StopMusic();
    }
}
