using UnityEngine;

/// <summary>
/// Handles player input for opening the player inventory and interacting with nearby external inventories.
/// Attach this component to the player GameObject and assign the player inventory and camera references.
/// </summary>
public class PlayerInventoryController : MonoBehaviour
{
    [Header("Player Inventory")]
    [SerializeField]
    private InventoryInstance playerInventory;

    [Header("Interaction")]
    [SerializeField]
    private Camera playerCamera;

    [SerializeField]
    private float interactDistance = 3f;

    [SerializeField]
    private LayerMask interactLayerMask = ~0;

    [Header("Input")]
    [SerializeField]
    private KeyCode toggleInventoryKey = KeyCode.I;

    [SerializeField]
    private KeyCode interactKey = KeyCode.E;

    private InventoryInstance activeExternalInventory;

    private void Reset()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleInventoryKey))
            TogglePlayerInventory();

        if (Input.GetKeyDown(interactKey))
            TryInteractWithExternalInventory();
    }

    /// <summary>
    /// Toggles the player inventory GameObject active state.
    /// </summary>
    public void TogglePlayerInventory()
    {
        if (playerInventory == null)
        {
            Debug.LogWarning("PlayerInventoryController has no playerInventory assigned.", this);
            return;
        }

        bool isOpen = playerInventory.gameObject.activeSelf;
        playerInventory.gameObject.SetActive(!isOpen);
        Debug.Log($"Player inventory {(isOpen ? "closed" : "opened")}", this);
    }

    /// <summary>
    /// Attempts to interact with an external inventory in front of the player.
    /// If the same inventory is already open, it closes it instead.
    /// </summary>
    public void TryInteractWithExternalInventory()
    {
        if (playerCamera == null)
        {
            Debug.LogWarning("PlayerInventoryController requires a Camera reference.", this);
            return;
        }

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayerMask))
        {
            InventoryInstance inventory = hit.collider.GetComponentInParent<InventoryInstance>();
            if (inventory != null)
            {
                ToggleExternalInventory(inventory);
                return;
            }
        }

        Debug.Log("No external inventory found in range.", this);
    }

    /// <summary>
    /// Opens or closes the targeted external inventory.
    /// </summary>
    private void ToggleExternalInventory(InventoryInstance inventory)
    {
        if (inventory == activeExternalInventory)
        {
            CloseExternalInventory();
            return;
        }

        OpenExternalInventory(inventory);
    }

    /// <summary>
    /// Opens the specified external inventory and closes any previously opened external inventory.
    /// </summary>
    private void OpenExternalInventory(InventoryInstance inventory)
    {
        CloseExternalInventory();

        if (inventory == null)
            return;

        activeExternalInventory = inventory;
        activeExternalInventory.gameObject.SetActive(true);
        Debug.Log($"Opened external inventory '{inventory.InventoryId}'", this);
    }

    /// <summary>
    /// Closes the currently active external inventory, if one is open.
    /// </summary>
    public void CloseExternalInventory()
    {
        if (activeExternalInventory == null)
            return;

        activeExternalInventory.gameObject.SetActive(false);
        Debug.Log($"Closed external inventory '{activeExternalInventory.InventoryId}'", this);
        activeExternalInventory = null;
    }

    /// <summary>
    /// Returns true when the player inventory is currently open.
    /// </summary>
    public bool IsPlayerInventoryOpen => playerInventory != null && playerInventory.gameObject.activeSelf;

    /// <summary>
    /// Returns true when an external inventory is currently open.
    /// </summary>
    public bool IsExternalInventoryOpen => activeExternalInventory != null;
}
