using UnityEngine;
using UnityEngine.UI;

public class InventoryGrid : MonoBehaviour
{
    [Header("Grid Size (Authoritative Source)")]
    [SerializeField] private int width = 8;
    [SerializeField] private int height = 6;

    [Header("References")]
    [SerializeField] private GridLayoutGroup gridLayout;
    [SerializeField] private RectTransform gridRoot;

    private InventoryTile[,] tiles;
    private InventoryItem[,] grid;

    public int Width => width;
    public int Height => height;
    public Vector2 CellSize => gridLayout.cellSize;
    public Vector2 Spacing => gridLayout.spacing;

    private void Awake()
    {
        grid = new InventoryItem[width, height];
        BuildTileMap();
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

                InventoryTile tile =
                    child.GetComponent<InventoryTile>();

                if (tile == null)
                    tile = child.gameObject.AddComponent<InventoryTile>();

                tile.gridPosition = new Vector2Int(x, y);
                tile.rect = child.GetComponent<RectTransform>();

                tiles[x, y] = tile;

                index++;
            }
        }
    }

    // =====================================================
    // REQUIRED FOR PREVIEW SYSTEM
    // =====================================================
    public RectTransform GetTileRect(Vector2Int pos)
    {
        if (pos.x < 0 || pos.y < 0 || pos.x >= width || pos.y >= height)
            return null;

        return tiles[pos.x, pos.y].rect;
    }

    public bool CanPlaceItem(Vector2Int position, InventoryItem item)
    {
        for (int x = 0; x < item.Width; x++)
        for (int y = 0; y < item.Height; y++)
        {
            if (!item.Shape[x, y]) continue;

            int gx = position.x + x - item.Anchor.x;
            int gy = position.y + y - item.Anchor.y;

            if (gx < 0 || gy < 0 || gx >= width || gy >= height)
                return false;

            if (grid[gx, gy] != null)
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
            if (!item.Shape[x, y]) continue;

            int gx = position.x + x - item.Anchor.x;
            int gy = position.y + y - item.Anchor.y;

            grid[gx, gy] = item;
        }

        item.SetGridPosition(position);
        return true;
    }

    public void RemoveItem(InventoryItem item)
    {
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
            if (grid[x, y] == item)
                grid[x, y] = null;
    }

    public Vector2 GetItemWorldPosition(Vector2Int gridPosition, InventoryItem item, RectTransform itemLayer)
    {
        RectTransform tileRect = tiles[gridPosition.x, gridPosition.y].rect;

        Vector2 local =
            itemLayer.InverseTransformPoint(tileRect.position);

        Vector2 offset =
            CalculateVisualOffset(item);

        return local - offset;
    }

    private Vector2 CalculateVisualOffset(InventoryItem item)
    {
        Vector2 cell = CellSize;

        Vector2 itemCenter =
            new Vector2(item.Width, item.Height) * 0.5f;

        Vector2 anchorCenter =
            new Vector2(item.Anchor.x + 0.5f, item.Anchor.y + 0.5f);

        Vector2 diff = itemCenter - anchorCenter;

        return Vector2.Scale(diff, cell);
    }
}