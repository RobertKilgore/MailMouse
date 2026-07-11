using UnityEngine;

/// <summary>
/// Base class for any object that can be interacted with by the player.
/// </summary>
public abstract class InteractableObject : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private bool canInteract = true;
    [SerializeField] private bool canHighlight = true;

    public bool CanInteract => canInteract;
    public bool CanHighlight => canHighlight;

    public virtual void OnFocused()
    {
    }

    public virtual void OnUnfocused()
    {
    }

    public virtual void Interact()
    {
    }
}
