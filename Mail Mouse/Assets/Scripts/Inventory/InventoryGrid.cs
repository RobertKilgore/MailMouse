using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryGrid : MonoBehaviour
{
    [Header("Grid Size (Authoritative Source)")]
    [SerializeField] private int width = 8;
    [SerializeField] private int height = 6;

    [Header("References")]
    [SerializeField] private GridLayoutGroup gridLayout;
    [SerializeField] private RectTransform gridRoot;

    [Header("Preview Layer")]
    [SerializeField] private RectTransform previewLayer;

    [Header("Preview Colors")]
    [SerializeField] private Color validPreviewColor = new Color(0f, 1f, 0f, 0.35f);
    [SerializeField] private Color invalidPreviewColor = new Color(1f, 0f, 0f, 0.35f);

    [Header("Instance")]
    [SerializeField] private InventoryInstance owner;

    private InventoryTile[,] tiles;
    private InventoryItem[,] occupancy;
    private readonly List<GameObject> previewObjects = new();

    public int Width => width;
    public int Height => height;
    public Vector2 CellSize => gridLayout.cellSize;
    public Vector2 Spacing => gridLayout.spacing;
    public InventoryInstance Owner => owner;

    private void Reset()
    {
        if (gridLayout == null)
            gridLayout = GetComponent<GridLayoutGroup>();

        if (gridRoot == null)
            gridRoot = transform as RectTransform;

        if (owner == null)
            owner = GetComponentInParent<InventoryInstance>();

        if (owner != null)
            owner.SetGrid(this);
    }

    private void Awake()
    {
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

        occupancy = new InventoryItem[width, height];
        BuildTileMap();
    }

    private void OnValidate()
    {
        if (gridLayout == null)
            gridLayout = GetComponent<GridLayoutGroup>();

        if (gridRoot == null)
            gridRoot = transform as RectTransform;

        if (owner == null)
            owner = GetComponentInParent<InventoryInstance>();

        if (owner != null)
            owner.SetGrid(this);
    }

    [ContextMenu("Debug Grid State")]
    public void DebugGridState()
    {
        int occupiedCount = 0;
        if (occupancy != null)
        {
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (occupancy[x, y] != null)
                        occupiedCount++;
        }

        Debug.Log($"InventoryGrid '{owner?.InventoryId ?? name}' {width}x{height} occupied={occupiedCount}", this);
    }

    public void SetOwner(InventoryInstance inventory)
    {
        owner = inventory;
    }

    private void BuildTileMap()
    {
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

    public void ShowPreview(Vector2Int origin, InventoryItem item)
    {
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

        for (int y = 0; y < height; y++)
        for (int x = 0; x < width - 1; x++)
            if (previewMap[x, y] && previewMap[x + 1, y])
                CreateSpacingPreview(tiles[x, y].rect, tiles[x + 1, y].rect, true, previewColor);

        for (int y = 0; y < height - 1; y++)
        for (int x = 0; x < width; x++)
            if (previewMap[x, y] && previewMap[x, y + 1])
                CreateSpacingPreview(tiles[x, y].rect, tiles[x, y + 1].rect, false, previewColor);
    }

    private void CreateCellPreview(int x, int y, Color color)
    {
        RectTransform tile = tiles[x, y].rect;
        GameObject go = new GameObject($"Preview_{x}_{y}", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(previewLayer, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.position = tile.position;
        rt.sizeDelta = tile.sizeDelta;

        go.GetComponent<Image>().color = color;
        previewObjects.Add(go);
    }

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

    public void ClearPreview()
    {
        foreach (GameObject obj in previewObjects)
            if (obj != null)
                Destroy(obj);

        previewObjects.Clear();
    }

    private bool IsValid(int x, int y)
    {
        return x >= 0 && y >= 0 && x < width && y < height;
    }

    public bool CanPlaceItem(Vector2Int position, InventoryItem item)
    {
        for (int x = 0; x < item.Width; x++)
        for (int y = 0; y < item.Height; y++)
        {
            if (!item.Shape[x, y])
                continue;

            int gx = position.x + x - item.Anchor.x;
            int gy = position.y + y - item.Anchor.y;

            if (!IsValid(gx, gy) || occupancy[gx, gy] != null)
                return false;
        }

        return true;
    }

    public bool PlaceItem(Vector2Int position, InventoryItem item)
    {
        if (!CanPlaceItem(position, item))
            return false;

        RemoveItem(item);

        for (int x = 0; x < item.Width; x++)
        for (int y = 0; y < item.Height; y++)
        {
            if (!item.Shape[x, y])
                continue;

            int gx = position.x + x - item.Anchor.x;
            int gy = position.y + y - item.Anchor.y;
            occupancy[gx, gy] = item;
        }

        item.SetGridPosition(position);
        item.SetOwnerInventory(owner);
        return true;
    }

    public void RemoveItem(InventoryItem item)
    {
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
            if (occupancy[x, y] == item)
                occupancy[x, y] = null;
    }

    public RectTransform GetTileRect(Vector2Int pos)
    {
        if (!IsValid(pos.x, pos.y))
            return null;

        return tiles[pos.x, pos.y].rect;
    }

    public Vector2 GetItemWorldPosition(Vector2Int gridPosition, InventoryItem item, RectTransform itemLayer)
    {
        RectTransform tileRect = tiles[gridPosition.x, gridPosition.y].rect;
        Vector2 local = itemLayer.InverseTransformPoint(tileRect.position);
        Vector2 offset = CalculateVisualOffset(item);
        return local - offset;
    }

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