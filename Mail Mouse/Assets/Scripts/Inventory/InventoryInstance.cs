using UnityEngine;

public class InventoryInstance : MonoBehaviour
{
    [Header("Grid Data")]
    [SerializeField] private InventoryGrid grid;

    [Header("UI Layers")]
    [SerializeField] private RectTransform itemLayer;
    [SerializeField] private RectTransform previewLayer;

    [Header("Optional ID")]
    [SerializeField] private string inventoryId;

    public InventoryGrid Grid => grid;
    public RectTransform ItemLayer => itemLayer;
    public RectTransform PreviewLayer => previewLayer;
    public string InventoryId => string.IsNullOrWhiteSpace(inventoryId) ? name : inventoryId;

    private void Reset()
    {
        if (grid == null)
            grid = GetComponentInChildren<InventoryGrid>();

        if (itemLayer == null)
            itemLayer = transform.Find("ItemLayer") as RectTransform;

        if (previewLayer == null)
            previewLayer = transform.Find("PreviewLayer") as RectTransform;

        if (grid != null)
            grid.SetOwner(this);
    }

    private void Awake()
    {
        if (grid == null)
            grid = GetComponentInChildren<InventoryGrid>();

        if (grid != null)
            grid.SetOwner(this);
    }

    private void OnValidate()
    {
        if (grid != null)
            grid.SetOwner(this);
    }

    [ContextMenu("Debug Inventory State")]
    public void DebugInventoryState()
    {
        Debug.Log($"Inventory '{InventoryId}' - Grid: {(grid == null ? "missing" : $"{grid.Width}x{grid.Height}")} - ItemLayer: {(itemLayer == null ? "missing" : "ok")} - PreviewLayer: {(previewLayer == null ? "missing" : "ok")}", this);
    }

    public void SetGrid(InventoryGrid inventoryGrid)
    {
        grid = inventoryGrid;
    }
}