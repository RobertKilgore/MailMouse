using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

/// <summary>
/// Main inventory controller for the player.
/// Manages opening/closing the player inventory and mailbox inventories.
/// </summary>
public class PlayerInventoryController : MonoBehaviour
{
    [Header("Inventory Sets")]
    [SerializeField]
    [Tooltip("The inventory set to open when pressing E (player inventory).")]
    private InventorySetDefinition playerInventorySet;

    [Header("Nearby Inventory UI Set")]
    [SerializeField]
    [Tooltip("The inventory set template to use for nearby inventories. This is a UI layout (player + nearby inventory slots).")]
    private InventorySetDefinition nearbyInventorySet;

    [Header("Player Data")]
    [SerializeField]
    [Tooltip("Optional InventoryData to populate the player UI slot when opening inventories.")]
    private InventoryDataHolder playerInventoryHolder;

    [Header("Menu Control")]
    [SerializeField]
    [Tooltip("The MenuController component on the inventory UI GameObject.")]
    private MenuController inventoryMenuController;

    [Header("Interaction")]
    [SerializeField]
    [Tooltip("The detection collider (trigger) for finding nearby inventories.")]
    private Collider detectionCollider;

    public InventoryData PlayerInventoryData => playerInventoryHolder != null ? playerInventoryHolder.inventoryData : null;

    private InventorySetManager setManager;
    private InputSystem_Actions inputActions;
    private InventoryDataHolder currentClosestInventory;

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

    private void OnDestroy()
    {
        // Clean up outline when controller is destroyed
        if (currentClosestInventory != null)
            OutlineManager.DisableOutline(currentClosestInventory.gameObject);
    }

    private void Start()
    {
        setManager = InventorySetManager.Instance ?? FindFirstObjectByType<InventorySetManager>(FindObjectsInactive.Include);
        if (setManager == null)
            Debug.LogWarning("No InventorySetManager found in scene. Inventory management may not work.", this);
    }

    private void Update()
    {
        HandleToggleInventory();
        HandleInteractWithClosestInventory();
        UpdateOutlineVisualization();
    }

    /// <summary>
    /// Toggles the player inventory set (or closes it if already open).
    /// </summary>
    private void HandleToggleInventory()
    {
        if (!inputActions.Player.Interact.WasPressedThisFrame())
            return;

        // Ensure we have a reference to the InventorySetManager. If it's not present yet,
        // try to find it (including inactive objects). If the inventory menu creates
        // or activates the manager when opened, open the menu first and try again.
        if (setManager == null)
            setManager = InventorySetManager.Instance ?? FindFirstObjectByType<InventorySetManager>(FindObjectsInactive.Include);

        if (setManager == null && inventoryMenuController != null)
        {
            Debug.Log("PlayerInventoryController: InventorySetManager missing — opening inventory menu to ensure manager exists.");
            inventoryMenuController.Open();
            // Try again after opening the menu in case the manager is part of that UI hierarchy
            setManager = InventorySetManager.Instance ?? FindFirstObjectByType<InventorySetManager>(FindObjectsInactive.Include);
            Debug.Log($"PlayerInventoryController: InventorySetManager after menu open: {(setManager != null ? "found" : "still null")}"
            );
        }

        if (setManager == null)
        {
            Debug.LogWarning("No InventorySetManager available when trying to toggle inventory.", this);
            return;
        }

        Debug.Log("PlayerInventoryController: Interact pressed - toggling player inventory (E)");

        // If any set is open, close it
        if (setManager.IsSetOpen)
        {
            Debug.Log("[PlayerInventoryController] Closing open inventory set");
            setManager.CloseInventorySet();
            if (inventoryMenuController != null)
                inventoryMenuController.Close();
        }
        else if (playerInventorySet != null)
        {
            Debug.Log("[PlayerInventoryController] Opening player inventory");
            // Open menu first to activate UI, then populate with InventorySetManager
            if (inventoryMenuController != null)
            {
                Debug.Log($"[PlayerInventoryController] inventoryMenuController: {inventoryMenuController.gameObject.name}, active: {inventoryMenuController.gameObject.activeSelf}");
                inventoryMenuController.Open();
                Debug.Log($"[PlayerInventoryController] After Open(): active: {inventoryMenuController.gameObject.activeSelf}, activeInHierarchy: {inventoryMenuController.gameObject.activeInHierarchy}");
                inventoryMenuController.transform.SetAsLastSibling();
            }
            else
            {
                Debug.LogError("[PlayerInventoryController] inventoryMenuController is NULL!");
            }

            // Otherwise open the player inventory set and bind the player data
            List<InventoryData> ordered = null;
            if (PlayerInventoryData != null)
                ordered = new List<InventoryData> { PlayerInventoryData };
            else
                Debug.LogWarning("PlayerInventoryController: playerInventoryData is null when opening player inventory.", this);

            Debug.Log("[PlayerInventoryController] Calling setManager.OpenInventorySet()");
            setManager.OpenInventorySet(playerInventorySet, ordered);
        }
        else
        {
            Debug.LogWarning("Player inventory set not assigned.", this);
        }
    }

    /// <summary>
    /// Handles interaction with the closest nearby inventory when F is pressed.
    /// </summary>
    private void HandleInteractWithClosestInventory()
    {
        if (!inputActions.Player.Interact2.WasPressedThisFrame())
            return;

        if (detectionCollider == null)
        {
            Debug.LogWarning("Detection collider not assigned!", this);
            return;
        }

        // Ensure the InventorySetManager is resolved; if it's created when opening the menu,
        // open the menu first and retry.
        if (setManager == null)
            setManager = InventorySetManager.Instance ?? FindFirstObjectByType<InventorySetManager>(FindObjectsInactive.Include);

        if (setManager == null && inventoryMenuController != null)
        {
            inventoryMenuController.Open();
            setManager = InventorySetManager.Instance ?? FindFirstObjectByType<InventorySetManager>(FindObjectsInactive.Include);
        }

        if (setManager == null)
        {
            Debug.LogWarning("Cannot interact with nearby inventory: InventorySetManager not found.", this);
            return;
        }

        // Close inventory if open
        if (setManager.IsSetOpen)
        {
            setManager.CloseInventorySet();
            if (inventoryMenuController != null)
                inventoryMenuController.Close();
            return;
        }

        // Find closest nearby inventory
        InventoryDataHolder closestHolder = GetClosestNearbyInventory();
        if (closestHolder == null)
        {
            Debug.Log("No nearby inventory found within detection range.");
            return;
        }

        OpenNearbyInventory(closestHolder);
    }

    /// <summary>
    /// Finds the closest InventoryDataHolder within the detection collider's range.
    /// </summary>
    private InventoryDataHolder GetClosestNearbyInventory()
    {
        if (detectionCollider == null)
        {
            Debug.LogWarning("Detection collider not assigned!", this);
            return null;
        }

        // Get the bounds of the detection collider and use its radius/size
        Bounds bounds = detectionCollider.bounds;
        float detectionRadius = bounds.extents.magnitude; // Approximate radius for sphere checking

        // Use Physics.OverlapSphere to find all colliders in range
        Collider[] collidersInRange = Physics.OverlapSphere(transform.position, detectionRadius);

        InventoryDataHolder closest = null;
        float closestDistance = float.MaxValue;
        Vector3 playerPos = transform.position;

        foreach (Collider col in collidersInRange)
        {
            InventoryDataHolder holder = col.GetComponent<InventoryDataHolder>();
            if (holder == null || holder == playerInventoryHolder)
                continue;

            float distance = Vector3.Distance(playerPos, holder.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = holder;
            }
        }

        int foundCount = closest != null ? 1 : 0;
        Debug.Log($"Found {foundCount} inventory in range at distance {closestDistance}");

        return closest;
    }

    /// <summary>
    /// Updates the outline visualization based on the current closest inventory.
    /// </summary>
    private void UpdateOutlineVisualization()
    {
        if (detectionCollider == null)
        {
            Debug.LogWarning("Detection collider not assigned!", this);
            return;
        }

        // Get the new closest inventory
        Bounds bounds = detectionCollider.bounds;
        float detectionRadius = bounds.extents.magnitude;
        Collider[] collidersInRange = Physics.OverlapSphere(transform.position, detectionRadius);

        InventoryDataHolder newClosest = null;
        float closestDistance = float.MaxValue;
        Vector3 playerPos = transform.position;

        foreach (Collider col in collidersInRange)
        {
            InventoryDataHolder holder = col.GetComponent<InventoryDataHolder>();
            if (holder == null || holder == playerInventoryHolder)
                continue;

            float distance = Vector3.Distance(playerPos, holder.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                newClosest = holder;
            }
        }

        // If the closest changed, update the outline
        if (newClosest != currentClosestInventory)
        {
            Debug.Log($"Closest inventory changed from {(currentClosestInventory != null ? currentClosestInventory.gameObject.name : "none")} to {(newClosest != null ? newClosest.gameObject.name : "none")}", this);

            // Remove outline from old closest
            if (currentClosestInventory != null)
                OutlineManager.DisableOutline(currentClosestInventory.gameObject);

            // Add outline to new closest
            currentClosestInventory = newClosest;
            if (currentClosestInventory != null)
                OutlineManager.EnableOutline(currentClosestInventory.gameObject);
        }
    }

    /// <summary>
    /// Opens a nearby inventory.
    /// </summary>
    private void OpenNearbyInventory(InventoryDataHolder inventoryHolder)
    {
        if (setManager == null || inventoryHolder == null || nearbyInventorySet == null)
        {
            Debug.LogWarning("Cannot open nearby inventory: missing setManager, holder, or inventory set template.", this);
            return;
        }

        InventoryData nearbyData = inventoryHolder.inventoryData;
        if (nearbyData == null)
        {
            Debug.LogWarning($"Inventory holder {inventoryHolder.gameObject.name} has no inventory data.", this);
            return;
        }

        // Build ordered data list: player data then nearby inventory data
        List<InventoryData> orderedData = new List<InventoryData>();
        if (PlayerInventoryData != null)
            orderedData.Add(PlayerInventoryData);
        orderedData.Add(nearbyData);

        // Open menu first to activate UI, then populate with InventorySetManager
        if (inventoryMenuController != null)
            inventoryMenuController.Open();

        setManager.OpenInventorySet(nearbyInventorySet, orderedData);
        Debug.Log($"Opened inventory at {inventoryHolder.gameObject.name}", this);
    }

    /// <summary>
    /// Closes the currently active inventory set.
    /// </summary>
    public void CloseCurrentSet()
    {
        if (setManager != null && setManager.IsSetOpen)
            setManager.CloseInventorySet();
    }

    [ContextMenu("Debug Nearby Inventories")]
    public void DebugNearbyInventories()
    {
        if (detectionCollider == null)
        {
            Debug.Log("Detection collider not assigned!", this);
            return;
        }

        Bounds bounds = detectionCollider.bounds;
        float detectionRadius = bounds.extents.magnitude;
        Collider[] collidersInRange = Physics.OverlapSphere(transform.position, detectionRadius);

        Debug.Log($"Found {collidersInRange.Length} colliders in detection range.", this);
        foreach (Collider col in collidersInRange)
        {
            InventoryDataHolder holder = col.GetComponent<InventoryDataHolder>();
            if (holder != null && holder != playerInventoryHolder)
                Debug.Log($"  - {holder.gameObject.name}: {holder.inventoryData?.inventoryId ?? "no data"}", this);
        }
        if (playerInventoryHolder != null)
            Debug.Log($"Player inventory holder: {playerInventoryHolder.inventoryData?.inventoryId ?? "no data"}", this);
        else
            Debug.Log("Player inventory holder: not assigned", this);
    }
}
