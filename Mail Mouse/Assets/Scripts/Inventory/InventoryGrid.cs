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

    // =====================================================
    // PUBLIC READONLY ACCESS (SOURCE OF TRUTH)
    // =====================================================

    public int Width => width;
    public int Height => height;
    public Vector2 CellSize => gridLayout.cellSize;

    // =====================================================
    // INIT
    // =====================================================

    private void Awake()
    {
        grid = new InventoryItem[width, height];
        BuildTileMap();
    }

    // =====================================================
    // TILE MAP
    // =====================================================

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
    // PLACEMENT CHECK
    // =====================================================

    public bool CanPlaceItem(Vector2Int position, InventoryItem item)
    {
        for (int x = 0; x < item.Width; x++)
        {
            for (int y = 0; y < item.Height; y++)
            {
                bool[,] shape = item.Shape;
                if (!shape[x, y])
                    continue;

                int gridX = position.x + x - item.Anchor.x;
                int gridY = position.y + y - item.Anchor.y;

                if (gridX < 0 || gridY < 0 ||
                    gridX >= width || gridY >= height)
                    return false;

                InventoryItem existing = grid[gridX, gridY];

                if (existing != null && existing != item)
                    return false;
            }
        }

        return true;
    }

    // =====================================================
    // PLACE ITEM
    // =====================================================

    public bool PlaceItem(Vector2Int position, InventoryItem item)
    {
        if (!CanPlaceItem(position, item))
            return false;

        RemoveItem(item);

        bool[,] shape = item.Shape;

        for (int x = 0; x < item.Width; x++)
        {
            for (int y = 0; y < item.Height; y++)
            {
                if (!shape[x, y])
                    continue;

                int gridX = position.x + x - item.Anchor.x;
                int gridY = position.y + y - item.Anchor.y;

                grid[gridX, gridY] = item;
            }
        }

        item.SetGridPosition(position);

        return true;
    }

    // =====================================================
    // REMOVE
    // =====================================================

    public void RemoveItem(InventoryItem item)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == item)
                    grid[x, y] = null;
            }
        }
    }

    // =====================================================
    // POSITIONING HELPERS
    // =====================================================

    public Vector2 GetItemWorldPosition(
        Vector2Int gridPosition,
        InventoryItem item,
        RectTransform itemLayer)
    {
        RectTransform tileRect =
            tiles[gridPosition.x, gridPosition.y].rect;

        Vector2 tileLocalPos =
            itemLayer.InverseTransformPoint(tileRect.position);

        Vector2 offset =
            CalculateVisualOffset(item);

        return tileLocalPos - offset;
    }

    private Vector2 CalculateVisualOffset(InventoryItem item)
    {
        Vector2 cellSize = CellSize;

        Vector2 itemCenter =
            new Vector2(item.Width, item.Height) * 0.5f;

        Vector2 anchorCenter =
            new Vector2(
                item.Anchor.x + 0.5f,
                item.Anchor.y + 0.5f
            );

        Vector2 gridOffset = itemCenter - anchorCenter;

        return Vector2.Scale(gridOffset, cellSize);
    }
}