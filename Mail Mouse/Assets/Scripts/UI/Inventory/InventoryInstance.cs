using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Represents a single inventory instance.
/// Holds references to the grid and the UI layers used for items and preview visuals.
/// </summary>
public class InventoryInstance : MonoBehaviour
{

    // Global toggle to enable logging when inventories are saved. Set to true during testing.
    public static bool SaveLogging = false;

    [Header("Address Display")]
    [SerializeField]
    [Tooltip("Optional TMP text object to display the currently bound inventory address.")]
    private TMP_Text addressTextTMP;

    [Header("Optional ID")]
    [SerializeField]
    private string inventoryId; // Optional identifier for debugging and display purposes.

    
    [Header("Grid Data")]
    [SerializeField]
    private InventoryGrid grid; // The inventory grid component under this inventory instance.

    [Header("UI Layers")]
    [SerializeField]
    private RectTransform itemLayer; // The parent RectTransform where inventory item UI elements should be placed.

    [SerializeField]
    private RectTransform previewLayer; // The parent RectTransform for preview visuals while dragging.


    [Header("Inventory Data")]
    [SerializeField]
    private InventoryData inventoryData; // The current data object used to populate this inventory view.

    [Header("Debug")]
    [SerializeField]
    [Tooltip("Enable high debug output when spawning a random item from the editor context menu.")]
    private bool highDebugOutput = false;

    public InventoryGrid Grid => grid;
    public RectTransform ItemLayer => itemLayer;
    public RectTransform PreviewLayer => previewLayer;
    public InventoryData InventoryData => inventoryData;

    public string InventoryId
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(inventoryData?.inventoryId))
                return inventoryData.inventoryId;

            if (!string.IsNullOrWhiteSpace(inventoryId))
                return inventoryId;

            return name;
        }
    }

    /// <summary>
    /// Editor hook used to auto-wire the grid and UI layers when this component is added or reset.
    /// </summary>
    private void Reset()
    {
        // Called in the editor when the component is first added or reset.
        // Use this to auto-wire obvious child references and reduce manual setup.
        if (grid == null)
            grid = GetComponentInChildren<InventoryGrid>();

        if (itemLayer == null)
            itemLayer = transform.Find("ItemLayer") as RectTransform;

        if (previewLayer == null)
            previewLayer = transform.Find("PreviewLayer") as RectTransform;

        if (grid != null)
            grid.SetOwner(this);
    }

    /// <summary>
    /// Runtime initialization to ensure the grid has a valid owner reference.
    /// </summary>
    private void Awake()
    {
        // Runtime fallback wiring if the inspector did not assign the grid reference.
        if (grid == null)
            grid = GetComponentInChildren<InventoryGrid>();

        if (grid != null)
            grid.SetOwner(this);
    }

    /// <summary>
    /// Keeps the grid owner assignment up to date when changes occur in the editor.
    /// </summary>
    private void OnValidate()
    {
        // Editor-phase validation to keep owner wiring consistent when the hierarchy changes.
        if (grid != null)
            grid.SetOwner(this);
    }

    /// <summary>
    /// Clears all runtime items from this inventory.
    /// </summary>
    public void ClearInventory()
    {
        if (grid == null)
            return;

        grid.BeginBatchUpdate();
        grid.ClearAllItems();
        grid.EndBatchUpdate(false);
    }

    /// <summary>
    /// Writes the current grid state back into the bound inventory data object.
    /// </summary>
    public void SaveInventoryData()
    {
        if (grid == null || inventoryData == null)
            return;

        if (!gameObject.activeInHierarchy || !enabled)
            return;

        inventoryData.items = new List<InventoryItemData>(grid.GetAllItemData());

        if (SaveLogging)
        {
            int count = inventoryData.items != null ? inventoryData.items.Count : 0;
            Debug.Log($"[Inventory Save] Saved inventory '{InventoryId}' items={count}", this);
        }
    }

    /// <summary>
    /// Assigns the inventory data object used to drive this view.
    /// Also updates the address display to match the new data.
    /// </summary>
    public void SetInventoryData(InventoryData data)
    {
        RebindInventoryData(data);
    }

    /// <summary>
    /// Rebinds this inventory instance to the given data object and clears any existing visuals.
    /// This ensures the UI does not keep stale runtime state when the backing data changes.
    /// </summary>
    public void RebindInventoryData(InventoryData data)
    {
        if (inventoryData != data)
        {
            if (inventoryData != null && grid != null && gameObject.activeInHierarchy && enabled)
            {
                SaveInventoryData();
            }

            if (grid != null)
            {
                grid.BeginBatchUpdate();
                try
                {
                    grid.ClearAllItems();
                }
                finally
                {
                    grid.EndBatchUpdate(false);
                }
            }

            inventoryData = data;
        }

        SetAddressText(data?.address);
    }

    /// <summary>
    /// Updates the bound address text object from the current inventory data.
    /// </summary>
    public void SetAddressText(string address)
    {
        if (addressTextTMP != null)
        {
            addressTextTMP.text = address ?? string.Empty;
        }
    }

    /// <summary>
    /// Updates the address display to match the currently bound inventory data.
    /// </summary>
    public void RefreshAddressDisplay()
    {
        SetAddressText(inventoryData?.address);
    }

    /// <summary>
    /// Prints the inventory instance state for debugging.
    /// Includes the current grid map output when available.
    /// </summary>
    [ContextMenu("Debug Inventory State")]
    public void DebugInventoryState()
    {
        string gridInfo = grid == null ? "missing" : $"{grid.Width}x{grid.Height}";
        string gridText = grid == null ? "Grid unavailable." : grid.GetDebugGridText();
        string dataId = inventoryData == null ? "none" : inventoryData.inventoryId;

        Debug.Log($"Inventory '{InventoryId}' - Grid: {gridInfo} - ItemLayer: {(itemLayer == null ? "missing" : "ok")} - PreviewLayer: {(previewLayer == null ? "missing" : "ok")} - DataId: {dataId}", this);
        Debug.Log(gridText, this);
    }

    /// <summary>
    /// Editor context menu action to spawn a random mail item into this inventory using the
    /// project's `InventorySpawner`. Tries random positions until a valid placement is found.
    /// </summary>
    [ContextMenu("Spawn Random Item")]
    public void SpawnRandomItemViaSpawner()
    {
        InventorySpawner spawner = FindFirstObjectByType<InventorySpawner>();
        if (spawner == null)
        {
            Debug.LogWarning($"No InventorySpawner found in scene to spawn item for inventory '{InventoryId}'.", this);
            return;
        }

        if (grid == null)
        {
            Debug.LogWarning($"Inventory '{InventoryId}' has no grid assigned.", this);
            return;
        }

        if (!spawner.HasPrefabs)
        {
            Debug.LogWarning($"InventorySpawner has no item prefabs assigned. Assign prefabs in the InventorySpawner component before spawning items.", this);
            return;
        }

        int debugLevel = highDebugOutput ? 3 : 0;
        Debug.Log($"Context menu spawn initiated for inventory '{InventoryId}' ({grid.Width}x{grid.Height} grid) debugLevel={debugLevel}.", this);

        if (highDebugOutput)
            grid.DebugGridState();

        InventoryItem spawned = spawner.SpawnItemInInventory(this, null, null, null, null, debugLevel);
        if (spawned != null)
        {
            Debug.Log($"Spawned random item '{spawned.name}' in inventory '{InventoryId}'{(highDebugOutput ? " with high debug output" : string.Empty)}.", this);
            return;
        }

        Debug.LogWarning($"Failed to spawn a random item in inventory '{InventoryId}' - no valid placement or item shape is too large.", this);
    }

    /// <summary>
    /// Assigns the grid owner reference.
    /// Called by InventoryGrid during initialization.
    /// </summary>
    public void SetGrid(InventoryGrid inventoryGrid)
    {
        grid = inventoryGrid;
    }
}