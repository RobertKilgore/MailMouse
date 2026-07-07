using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    private InventoryDataHolder currentClosestInventory;

    private void OnDestroy()
    {
        if (inventoryMenuController != null)
        {
            inventoryMenuController.Opened -= HandleInventoryMenuOpened;
            inventoryMenuController.Closed -= HandleInventoryMenuClosed;
        }

        // Clean up outline when controller is destroyed
        if (currentClosestInventory != null)
            OutlineManager.DisableOutline(currentClosestInventory.gameObject);
    }

    private void Start()
    {
        ResolveSetManager();
        ResolveInventoryMenuController();
    }

    private void ResolveSetManager()
    {
        if (setManager != null)
            return;

        setManager = InventorySetManager.Instance ?? FindFirstObjectByType<InventorySetManager>(FindObjectsInactive.Include);
        if (setManager == null)
            Debug.LogWarning("No InventorySetManager found in scene. Inventory management may not work.", this);
    }

    private void ResolveInventoryMenuController()
    {
        if (inventoryMenuController != null)
            return;

        inventoryMenuController = GetComponentInChildren<MenuController>(true);
        if (inventoryMenuController == null)
            return;

        inventoryMenuController.Opened -= HandleInventoryMenuOpened;
        inventoryMenuController.Closed -= HandleInventoryMenuClosed;
        inventoryMenuController.Opened += HandleInventoryMenuOpened;
        inventoryMenuController.Closed += HandleInventoryMenuClosed;
    }

    private bool IsInventoryMenuOpen()
    {
        if (inventoryMenuController != null)
        {
            if (inventoryMenuController.IsOpen)
                return true;

            if (MenuManager.Instance != null && MenuManager.Instance.GetActiveMenus().Any(menu => menu == inventoryMenuController))
                return true;
        }

        return setManager != null && setManager.IsSetOpen;
    }

    private void OpenInventoryMenuAndSet(InventorySetDefinition setDefinition, List<InventoryData> orderedData)
    {
        ResolveSetManager();
        ResolveInventoryMenuController();

        if (setManager == null)
        {
            Debug.LogWarning("Cannot open inventory: InventorySetManager not found.", this);
            return;
        }

        bool menuIsOpen = inventoryMenuController != null &&
            (inventoryMenuController.IsOpen || (MenuManager.Instance != null && MenuManager.Instance.GetActiveMenus().Any(menu => menu == inventoryMenuController)));

        if (!menuIsOpen)
        {
            Debug.Log("[PlayerInventoryController] Inventory menu is not open; skipping inventory set open from controller.");
            return;
        }

        if (inventoryMenuController != null)
            inventoryMenuController.transform.SetAsLastSibling();

        if (setDefinition != null)
            setManager.OpenInventorySet(setDefinition, orderedData);
        else
            Debug.LogWarning("Inventory set definition not assigned.", this);
    }

    private void CloseInventoryMenuAndSet()
    {
        ResolveSetManager();

        if (setManager != null && setManager.IsSetOpen)
            setManager.CloseInventorySet();
    }

    private void HandleInventoryMenuOpened(MenuController menu)
    {
        if (menu != inventoryMenuController)
            return;

        if (inventoryMenuController != null)
            inventoryMenuController.transform.SetAsLastSibling();
    }

    private void HandleInventoryMenuClosed(MenuController menu)
    {
        if (menu != inventoryMenuController)
            return;

        ResolveSetManager();
        if (setManager != null && setManager.IsSetOpen)
        {
            Debug.Log("[PlayerInventoryController] Inventory menu closed; closing active inventory set.");
            setManager.CloseInventorySet();
        }
    }

    private void Update()
    {
        UpdateOutlineVisualization();
    }

    /// <summary>
    /// Handles interaction with the closest nearby inventory when F is pressed.
    /// </summary>
    private void HandleInteractWithClosestInventory()
    {
        if (detectionCollider == null)
        {
            Debug.LogWarning("Detection collider not assigned!", this);
            return;
        }

        ResolveSetManager();
        ResolveInventoryMenuController();

        if (setManager == null)
        {
            Debug.LogWarning("Cannot interact with nearby inventory: InventorySetManager not found.", this);
            return;
        }

        // Close inventory if open
        if (IsInventoryMenuOpen())
        {
            CloseInventoryMenuAndSet();
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

        OpenInventoryMenuAndSet(nearbyInventorySet, orderedData);
        Debug.Log($"Opened inventory at {inventoryHolder.gameObject.name}", this);
    }

    /// <summary>
    /// Closes the currently active inventory set.
    /// </summary>
    public void CloseCurrentSet()
    {
        CloseInventoryMenuAndSet();
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
