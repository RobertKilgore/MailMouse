using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles nearby interactable objects and routes interaction requests to the active object.
/// </summary>
public class InteractableObjectController : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private Collider detectionCollider;
    [SerializeField] private LayerMask interactableLayerMask = ~0;

    [Header("Highlight")]
    [SerializeField] private bool allowHighlight = true;

    private InteractableObject currentInteractable;
    private InputSystem_Actions inputActions;

    private void OnEnable()
    {
        if (inputActions == null)
            inputActions = new InputSystem_Actions();
        inputActions.Enable();
    }

    private void OnDisable()
    {
        if (inputActions != null)
            inputActions.Disable();
    }

    private void Update()
    {
        if (inputActions?.Player.Interact2.WasPressedThisFrame() == true)
        {
            if (TryCloseOpenInventoryMenu())
                return;

            TryInteractWithCurrentObject();
            return;
        }

        UpdateFocusedInteractable();
    }

    private void UpdateFocusedInteractable()
    {
        if (!allowHighlight)
        {
            ClearCurrentHighlight();
            return;
        }

        InteractableObject newInteractable = FindNearestInteractable();
        if (newInteractable == currentInteractable)
            return;

        ClearCurrentHighlight();
        currentInteractable = newInteractable;

        if (currentInteractable != null && currentInteractable.CanHighlight)
            currentInteractable.OnFocused();
    }

    private InteractableObject FindNearestInteractable()
    {
        if (detectionCollider == null)
            return null;

        InteractableObject nearest = null;
        float closestDistance = float.MaxValue;
        Vector3 origin = transform.position;

        InteractableObject[] interactables = FindObjectsByType<InteractableObject>(FindObjectsSortMode.None);
        foreach (InteractableObject interactable in interactables)
        {
            if (interactable == null || !interactable.CanInteract || interactable.gameObject == gameObject)
                continue;

            Collider interactableCollider = interactable.GetComponent<Collider>();
            if (interactableCollider == null || !interactableCollider.enabled)
                continue;

            if (!detectionCollider.bounds.Intersects(interactableCollider.bounds))
                continue;

            float distance = Vector3.Distance(origin, interactable.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                nearest = interactable;
            }
        }

        return nearest;
    }

    private void TryInteractWithCurrentObject()
    {
        if (currentInteractable == null || !currentInteractable.CanInteract)
            return;

        currentInteractable.Interact();
    }

    private bool TryCloseOpenInventoryMenu()
    {
        if (MenuManager.Instance == null)
            return false;

        foreach (MenuController menu in MenuManager.Instance.GetActiveMenus())
        {
            if (menu == null || !menu.IsOpen)
                continue;

            if (menu.gameObject.name.ToLower().Contains("inventory"))
            {
                menu.Close();
                return true;
            }
        }

        return false;
    }

    private void ClearCurrentHighlight()
    {
        if (currentInteractable != null)
        {
            currentInteractable.OnUnfocused();
            currentInteractable = null;
        }
    }
}
