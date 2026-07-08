using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Manages the inventory grid, tile mapping, previews, and placement logic.
/// Each InventoryGrid belongs to a single InventoryInstance.
/// </summary>
public class InventoryGrid : MonoBehaviour
{
    [Header("Grid Size")]
    [SerializeField]
    private int width = 8; // Number of tiles horizontally.

    [SerializeField]
    private int height = 6; // Number of tiles vertically.

    [Header("References")]
    [SerializeField]
    private GridLayoutGroup gridLayout; // Optional reference to the layout for cell sizing.

    [SerializeField]
    private RectTransform gridRoot; // Transform containing all tile UI elements.

    [Header("Preview Layer")]
    [SerializeField]
    private RectTransform previewLayer; // Parent for preview objects when dragging.

    [Header("Preview Colors")]
    [SerializeField]
    private Color validPreviewColor = new Color32(0, 255, 0, 89); // Color used when item placement is valid.

    [SerializeField]
    private Color invalidPreviewColor = new Color32(255, 0, 0, 89); // Color used when placement is invalid.

    [Header("Instance")]
    [SerializeField]
    private InventoryInstance owner; // Owning inventory instance.

    private InventoryTile[,] tiles; // Tile metadata for each grid coordinate.
    private InventoryItem[,] occupancy; // Tracks which item occupies each grid cell.
    private int batchUpdateCount;
    private readonly List<GameObject> previewObjects = new(); // Runtime preview objects.

    public int Width => width;
    public int Height => height;
    public Vector2 CellSize => gridLayout.cellSize;
    public Vector2 Spacing => gridLayout.spacing;
    public InventoryInstance Owner => owner;

    /// <summary>
    /// Editor-time auto-wire for common references when the component is reset.
    /// This helps reduce manual setup in the inspector.
    /// </summary>
    private void Reset()
    {
        // Editor reset path: auto-wire references if possible.
        if (gridLayout == null)
            gridLayout = GetComponent<GridLayoutGroup>();

        if (gridRoot == null)
            gridRoot = transform as RectTransform;

        if (owner == null)
            owner = GetComponentInParent<InventoryInstance>();

        if (owner != null)
            owner.SetGrid(this);
    }

    /// <summary>
    /// Runtime initialization for the inventory grid.
    /// Ensures references are valid and builds the tile lookup table.
    /// </summary>
    private void Awake()
    {
        // Runtime initialization and fallback wiring.
        if (gridLayout == null)
            gridLayout = GetComponent<GridLayoutGroup>();

        if (gridRoot == null)
            gridRoot = transform as RectTransform;

        if (owner == null)
            owner = GetComponentInParent<InventoryInstance>();

        if (owner != null)
            owner.SetGrid(this);

        if (previewLayer == null)
            previewLayer = owner?.PreviewLayer;

        EnsureInitialized();
    }

    /// <summary>
    /// Editor validation hook that keeps references synchronized when values change.
    /// </summary>
    private void OnValidate()
    {
        // Keep references consistent while editing in the Unity inspector.
        if (gridLayout == null)
            gridLayout = GetComponent<GridLayoutGroup>();

        if (gridRoot == null)
            gridRoot = transform as RectTransform;

        if (owner == null)
            owner = GetComponentInParent<InventoryInstance>();

        if (owner != null)
            owner.SetGrid(this);
    }

    private void EnsureInitialized()
    {
        if (width <= 0 || height <= 0)
            return;

        if (occupancy == null)
            occupancy = new InventoryItem[width, height];

        if (tiles == null && gridRoot != null)
            BuildTileMap();
    }

    /// <summary>
    /// Prints a debug summary of the current grid occupancy to the console.
    /// Includes a line-by-line grid map and anchor positions for placed items.
    /// </summary>
    [ContextMenu("Debug Grid State")]
    public void DebugGridState()
    {
        if (width <= 0 || height <= 0)
        {
            Debug.LogWarning($"InventoryGrid '{owner?.InventoryId ?? name}' has invalid size {width}x{height}.", this);
            return;
        }

        EnsureInitialized();

        int occupiedCount = 0;
        HashSet<InventoryItem> itemSet = new HashSet<InventoryItem>();

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            InventoryItem item = occupancy[x, y];
            if (item != null)
            {
                occupiedCount++;
                itemSet.Add(item);
            }
        }

        string header = $"InventoryGrid '{owner?.InventoryId ?? name}' {width}x{height} occupied={occupiedCount}";
        string gridText = GetDebugGridText();
        string itemText = BuildItemAnchorText(itemSet);

        Debug.Log(header, this);
        Debug.Log(gridText, this);
        Debug.Log(itemText, this);
    }

    public string GetDebugGridText()
    {
        return BuildDebugGridText();
    }

    private string BuildDebugGridText()
    {
        var lineBuilder = new System.Text.StringBuilder();
        lineBuilder.AppendLine("Grid:");

        for (int y = 0; y < height; y++)
        {
            lineBuilder.Append("[");
            for (int x = 0; x < width; x++)
            {
                InventoryItem item = occupancy[x, y];
                char symbol = item == null ? '.' : (item.GridPosition == new Vector2Int(x, y) ? 'A' : 'X');
                lineBuilder.Append('[').Append(symbol).Append(']');
                if (x < width - 1)
                    lineBuilder.Append(' ');
            }
            lineBuilder.AppendLine("]");
        }

        return lineBuilder.ToString();
    }

    private string BuildItemAnchorText(HashSet<InventoryItem> itemSet)
    {
        if (itemSet.Count == 0)
            return "No items placed.\n";

        var itemBuilder = new System.Text.StringBuilder();
        itemBuilder.AppendLine("Placed Items:");

        foreach (InventoryItem item in itemSet)
        {
            itemBuilder.AppendLine($" - {item.name} at {item.GridPosition} anchor={item.Anchor}");
        }

        return itemBuilder.ToString();
    }

    /// <summary>
    /// Assigns the owning InventoryInstance, allowing the grid to resolve UI layers.
    /// </summary>
    public void SetOwner(InventoryInstance inventory)
    {
        owner = inventory;
    }

    /// <summary>
    /// Begins a batch update that suppresses intermediate inventory persistence.
    /// </summary>
    public void BeginBatchUpdate()
    {
        batchUpdateCount++;
    }

    /// <summary>
    /// Ends a batch update. If this is the last active batch and commitSave is true,
    /// the grid will persist its current state back into the bound inventory data.
    /// </summary>
    public void EndBatchUpdate(bool commitSave = true)
    {
        if (batchUpdateCount <= 0)
            return;

        batchUpdateCount--;
        if (batchUpdateCount == 0 && commitSave)
            SaveInventory();
    }

    /// <summary>
    /// Logs a hover event for the given tile when global hover debug is enabled.
    /// </summary>
    public void LogHoverTile(InventoryTile tile)
    {
        if (tile == null || tile.Grid == null || tile.Grid.Owner == null)
            return;

        InventoryDragController controller = InventoryDragController.Instance;
        if (controller == null || !controller.DebugHover)
            return;

        InventoryInstance inv = tile.Grid.Owner;
        Debug.Log($"Hover tile {tile.gridPosition} on inventory '{inv.name}' id='{inv.InventoryId}' frame={Time.frameCount}", tile);
    }

    /// <summary>
    /// Creates the internal lookup table for every grid cell by scanning child tiles.
    /// Each tile receives a grid coordinate and a cached RectTransform.
    /// </summary>
    private void BuildTileMap()
    {
        // Build the 2D tile array from the child objects in the grid root.
        tiles = new InventoryTile[width, height];
        int index = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Transform child = gridRoot.GetChild(index);
                InventoryTile tile = child.GetComponent<InventoryTile>() ?? child.gameObject.AddComponent<InventoryTile>();

                tile.gridPosition = new Vector2Int(x, y);
                tile.rect = child.GetComponent<RectTransform>();
                tile.grid = this;
                tiles[x, y] = tile;

                index++;
            }
        }
    }

    /// <summary>
    /// Renders a placement preview for the dragged item at the target origin.
    /// Uses valid/invalid colors depending on whether the item can be placed.
    /// </summary>
    public void ShowPreview(Vector2Int origin, InventoryItem item)
    {
        // Display a preview of where the item will land at the hovered grid position.
        ClearPreview();
        Color previewColor = CanPlaceItem(origin, item) ? validPreviewColor : invalidPreviewColor;
        bool[,] previewMap = new bool[width, height];

        for (int x = 0; x < item.Width; x++)
        for (int y = 0; y < item.Height; y++)
        {
            if (!item.Shape[x, y])
                continue;

            int gx = origin.x + x - item.Anchor.x;
            int gy = origin.y + y - item.Anchor.y;

            if (!IsValid(gx, gy))
                continue;

            previewMap[gx, gy] = true;
            CreateCellPreview(gx, gy, previewColor);
        }

        // Add spacing previews between adjacent preview cells for a connected appearance.
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width - 1; x++)
            if (previewMap[x, y] && previewMap[x + 1, y])
                CreateSpacingPreview(tiles[x, y].rect, tiles[x + 1, y].rect, true, previewColor);

        for (int y = 0; y < height - 1; y++)
        for (int x = 0; x < width; x++)
            if (previewMap[x, y] && previewMap[x, y + 1])
                CreateSpacingPreview(tiles[x, y].rect, tiles[x, y + 1].rect, false, previewColor);
    }

    /// <summary>
    /// Creates a preview tile image at the specified grid coordinate.
    /// </summary>
    private void CreateCellPreview(int x, int y, Color color)
    {
        GameObject go = new GameObject($"Preview_{x}_{y}", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(previewLayer, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.position = tiles[x, y].rect.position;
        rt.sizeDelta = tiles[x, y].rect.sizeDelta;

        go.GetComponent<Image>().color = color;
        previewObjects.Add(go);
    }

    /// <summary>
    /// Creates a visual preview spacer between adjacent preview tiles.
    /// This smooths the preview representation for multi-cell items.
    /// </summary>
    private void CreateSpacingPreview(RectTransform first, RectTransform second, bool horizontal, Color color)
    {
        GameObject go = new GameObject("PreviewSpacing", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(previewLayer, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        Vector2 midpoint = ((Vector2)first.position + (Vector2)second.position) * 0.5f;
        rt.position = midpoint;
        rt.sizeDelta = horizontal ? new Vector2(Spacing.x, CellSize.y) : new Vector2(CellSize.x, Spacing.y);

        go.GetComponent<Image>().color = color;
        previewObjects.Add(go);
    }

    /// <summary>
    /// Removes all preview visualization objects from the preview layer.
    /// </summary>
    public void ClearPreview()
    {
        foreach (GameObject obj in previewObjects)
            if (obj != null)
                Destroy(obj);

        previewObjects.Clear();
    }

    /// <summary>
    /// Checks whether a coordinate is within the bounds of the grid.
    /// </summary>
    private bool IsValid(int x, int y)
    {
        return x >= 0 && y >= 0 && x < width && y < height;
    }

    /// <summary>
    /// Determines whether the item can be placed at the target grid position.
    /// Validates bounds and absence of existing occupancy.
    /// </summary>
    public bool CanPlaceItem(Vector2Int position, InventoryItem item)
    {
        return CanPlaceItem(position, item, 0);
    }

    /// <summary>
    /// Debug-aware overload of CanPlaceItem. When debugLevel &gt; 0, logs the exact failure reason and cell.
    /// </summary>
    public bool CanPlaceItem(Vector2Int position, InventoryItem item, int debugLevel)
    {
        if (item == null)
        {
            if (debugLevel > 0)
                Debug.LogWarning($"[Grid] CanPlaceItem: item is null at position {position}.", this);
            return false;
        }

        if (item.Width <= 0 || item.Height <= 0)
        {
            if (debugLevel > 0)
                Debug.LogWarning($"[Grid] CanPlaceItem: item '{item.name}' has invalid size {item.Width}x{item.Height}.", this);
            return false;
        }

        if (debugLevel > 2)
            Debug.Log($"[Grid] CanPlaceItem start for '{item.name}' at {position}, item size={item.Width}x{item.Height} anchor={item.Anchor}.", this);

        bool sawTile = false;
        for (int x = 0; x < item.Width; x++)
        for (int y = 0; y < item.Height; y++)
        {
            if (!item.Shape[x, y])
                continue;

            sawTile = true;
            int gx = position.x + x - item.Anchor.x;
            int gy = position.y + y - item.Anchor.y;

            if (!IsValid(gx, gy))
            {
                if (debugLevel > 0)
                    Debug.LogWarning($"[Grid] CanPlaceItem: Cell out of bounds for item '{item.name}' at local ({x},{y}) -> grid ({gx},{gy}) anchor={item.Anchor} position={position}", this);
                return false;
            }

            if (occupancy[gx, gy] != null)
            {
                if (debugLevel > 0)
                    Debug.LogWarning($"[Grid] CanPlaceItem: Cell occupied at ({gx},{gy}) by '{occupancy[gx,gy].name}' blocking item '{item.name}' (local {x},{y})", this);
                return false;
            }

            if (debugLevel > 2)
                Debug.Log($"[Grid] CanPlaceItem: Candidate cell ({gx},{gy}) is free for item '{item.name}' (local {x},{y}).", this);
        }

        if (!sawTile)
        {
            if (debugLevel > 0)
                Debug.LogWarning($"[Grid] CanPlaceItem: item '{item.name}' has no occupied shape cells.", this);
            return false;
        }

        if (debugLevel > 2)
            Debug.Log($"[Grid] CanPlaceItem: item '{item.name}' can be placed at {position}.", this);

        return true;
    }

    /// <summary>
    /// Attempts to place the item in the grid and updates occupancy if successful.
    /// Removes any previous occupancy that belonged to the same item first.
    /// </summary>
    public bool PlaceItem(Vector2Int position, InventoryItem item)
    {
        return PlaceItem(position, item, 0);
    }

    /// <summary>
    /// Debug-aware overload of PlaceItem. When debugLevel &gt; 0, logs placement progress and failures.
    /// </summary>
    public bool PlaceItem(Vector2Int position, InventoryItem item, int debugLevel)
    {
        if (!CanPlaceItem(position, item, debugLevel))
        {
            if (debugLevel > 0)
                Debug.LogWarning($"[Grid] PlaceItem: Cannot place item '{item?.name}' at {position} in inventory '{owner?.InventoryId ?? name}'", this);
            return false;
        }

        RemoveItem(item);

        for (int x = 0; x < item.Width; x++)
        for (int y = 0; y < item.Height; y++)
        {
            if (!item.Shape[x, y])
                continue;

            int gx = position.x + x - item.Anchor.x;
            int gy = position.y + y - item.Anchor.y;
            occupancy[gx, gy] = item;
            if (debugLevel > 1)
                Debug.Log($"[Grid] Occupying cell ({gx},{gy}) for item '{item.name}'", this);
        }

        item.SetGridPosition(position);
        item.SetOwnerInventory(owner);
        item.RectTransform.localPosition = GetItemWorldPosition(position, item, owner.ItemLayer);

        if (debugLevel > 0)
            Debug.Log($"[Grid] Placed item '{item.name}' at {position} (anchor={item.Anchor}) localPos={item.RectTransform.localPosition}", this);

        SaveInventory();
        return true;
    }

    /// <summary>
    /// Clears any occupancy entries for the specified item.
    /// This is used before re-placing the item elsewhere.
    /// </summary>
    public void RemoveItem(InventoryItem item)
    {
        bool itemWasOccupying = false;
        
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
            if (occupancy[x, y] == item)
            {
                occupancy[x, y] = null;
                itemWasOccupying = true;
            }

        if (itemWasOccupying)
            SaveInventory();
    }

    /// <summary>
    /// Returns every unique item currently occupying this grid.
    /// </summary>
    public List<InventoryItem> GetAllItems()
    {
        EnsureInitialized();

        List<InventoryItem> items = new List<InventoryItem>();

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            InventoryItem item = occupancy[x, y];
            if (item != null && !items.Contains(item))
                items.Add(item);
        }

        return items;
    }

    /// <summary>
    /// Returns the serialized item data for every item in the grid.
    /// </summary>
    public List<InventoryItemData> GetAllItemData()
    {
        List<InventoryItemData> itemData = new List<InventoryItemData>();
        foreach (InventoryItem item in GetAllItems())
        {
            itemData.Add(item.ToItemData());
        }
        return itemData;
    }

    /// <summary>
    /// Removes all items from this grid and destroys their GameObjects.
    /// </summary>
    public void ClearAllItems()
    {
        EnsureInitialized();

        foreach (InventoryItem item in GetAllItems())
        {
            if (item == null)
                continue;

            RemoveItem(item);
            if (item != null)
                GameObject.Destroy(item.gameObject);
        }
    }

    /// <summary>
    /// Saves the current inventory state back to the owner's inventory data.
    /// </summary>
    private void SaveInventory()
    {
        if (batchUpdateCount > 0)
            return;

        if (owner == null || owner.InventoryData == null)
            return;

        if (!owner.gameObject.activeInHierarchy || !owner.enabled)
            return;

        owner.SaveInventoryData();
    }

    /// <summary>
    /// Returns the RectTransform of the tile at the given grid position.
    /// </summary>
    public RectTransform GetTileRect(Vector2Int pos)
    {
        if (!IsValid(pos.x, pos.y))
            return null;

        return tiles[pos.x, pos.y].rect;
    }

    /// <summary>
    /// Computes the local position for an item based on the anchor cell and the tile position.
    /// </summary>
    public Vector2 GetItemWorldPosition(Vector2Int gridPosition, InventoryItem item, RectTransform itemLayer)
    {
        RectTransform tileRect = tiles[gridPosition.x, gridPosition.y].rect;
        Vector2 local = itemLayer.InverseTransformPoint(tileRect.position);
        Vector2 offset = CalculateVisualOffset(item);
        return local - offset;
    }

    /// <summary>
    /// Calculates the pixel offset needed to align the item visuals to the grid tile.
    /// Uses the item anchor and the cell spacing to determine the correct origin.
    /// </summary>
    private Vector2 CalculateVisualOffset(InventoryItem item)
    {
        Vector2 cell = CellSize;
        Vector2 spacing = Spacing;
        float stepX = cell.x + spacing.x;
        float stepY = cell.y + spacing.y;

        float totalWidth = item.Width * cell.x + (item.Width - 1) * spacing.x;
        float totalHeight = item.Height * cell.y + (item.Height - 1) * spacing.y;

        float anchorX = -totalWidth * 0.5f + item.Anchor.x * stepX + cell.x * 0.5f;
        float anchorY = totalHeight * 0.5f - item.Anchor.y * stepY - cell.y * 0.5f;

        return new Vector2(anchorX, anchorY);
    }
}