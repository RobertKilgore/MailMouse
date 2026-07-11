using UnityEngine;

[CreateAssetMenu(fileName = "AudioClips", menuName = "Audio/Audio Clips Database")]
public class AudioClips : ScriptableObject
{
    [Header("UI")]
    public AudioClip uiClick;
    public AudioClip uiOpen;
    public AudioClip uiClose;

    [Header("Inventory")]
    public AudioClip inventoryOpen;
    public AudioClip inventoryClose;
    public AudioClip packageSound;

    [Header("Music")]
    public AudioClip mainTheme;
}
