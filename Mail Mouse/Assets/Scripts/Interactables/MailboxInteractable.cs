using UnityEngine;

/// <summary>
/// Interactable implementation for mailbox objects.
/// </summary>
[RequireComponent(typeof(InventoryDataHolder))]
public class MailboxInteractable : InteractableObject
{
    [Header("Mailbox Interaction")]
    [SerializeField] private PlayerMenuInputController playerMenuInputController;

    private InventoryDataHolder inventoryDataHolder;

    private void Awake()
    {
        inventoryDataHolder = GetComponent<InventoryDataHolder>();
        if (playerMenuInputController == null)
            playerMenuInputController = FindFirstObjectByType<PlayerMenuInputController>(FindObjectsInactive.Include);
    }

    public override void OnFocused()
    {
        if (!CanHighlight)
            return;

        OutlineManager.EnableOutline(gameObject);
    }

    public override void OnUnfocused()
    {
        OutlineManager.DisableOutline(gameObject);
    }

    public override void Interact()
    {
        if (inventoryDataHolder == null || inventoryDataHolder.inventoryData == null)
            return;

        if (playerMenuInputController != null)
            playerMenuInputController.RequestInventoryToggle(inventoryDataHolder.inventoryData.inventoryType, inventoryDataHolder.inventoryData);
    }
}
