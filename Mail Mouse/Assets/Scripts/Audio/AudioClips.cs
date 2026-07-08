using UnityEngine;

[CreateAssetMenu(fileName = "AudioClips", menuName = "Audio/Audio Clips Database")]
public class AudioClips : ScriptableObject
{
    [Header("UI")]
    public AudioClip uiClick;
    public AudioClip uiOpen;
    public AudioClip uiClose;

    [Header("Interaction")]
    public AudioClip interact;
    public AudioClip pickup;
    public AudioClip drop;

    [Header("Inventory")]
    public AudioClip inventoryOpen;
    public AudioClip inventoryClose;
    public AudioClip itemPlace;
    public AudioClip itemRemove;

    [Header("Music")]
    public AudioClip mainTheme;
}
