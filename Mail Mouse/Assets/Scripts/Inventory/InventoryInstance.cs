using UnityEngine;

/// <summary>
/// Represents a single inventory instance.
/// Holds references to the grid and the UI layers used for items and preview visuals.
/// </summary>
public class InventoryInstance : MonoBehaviour
{
    [Header("Grid Data")]
    [SerializeField]
    private InventoryGrid grid; // The inventory grid component under this inventory instance.

    [Header("UI Layers")]
    [SerializeField]
    private RectTransform itemLayer; // The parent RectTransform where inventory item UI elements should be placed.

    [SerializeField]
    private RectTransform previewLayer; // The parent RectTransform for preview visuals while dragging.

    [Header("Optional ID")]
    [SerializeField]
    private string inventoryId; // Optional identifier for debugging and display purposes.

    public InventoryGrid Grid => grid;
    public RectTransform ItemLayer => itemLayer;
    public RectTransform PreviewLayer => previewLayer;

    public string InventoryId => string.IsNullOrWhiteSpace(inventoryId) ? name : inventoryId;

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
    /// Prints the inventory instance state for debugging.
    /// </summary>
    [ContextMenu("Debug Inventory State")]
    public void DebugInventoryState()
    {
        Debug.Log($"Inventory '{InventoryId}' - Grid: {(grid == null ? "missing" : $"{grid.Width}x{grid.Height}")} - ItemLayer: {(itemLayer == null ? "missing" : "ok")} - PreviewLayer: {(previewLayer == null ? "missing" : "ok")}", this);
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