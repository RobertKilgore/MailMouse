using UnityEngine;
using UnityEngine.UI;

public class InventoryGrid : MonoBehaviour
{
    [Header("Grid Size")]
    public int width = 8;
    public int height = 6;

    public GridLayoutGroup gridLayout;
    public RectTransform gridRoot;

    public InventoryTile[,] tiles;

    private InventoryItem[,] grid;

    public Vector2 cellSize;

    private void Awake()
    {
        grid = new InventoryItem[width, height];

        BuildTileMap();

        //cellSize = tiles[0,0].rect.rect.size;
    }

    private void Start()
    {
        cellSize = gridLayout.cellSize;
        Debug.Log($"CELL SIZE: {cellSize}");
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

                InventoryTile tile = child.GetComponent<InventoryTile>();

                if (tile == null)
                    tile = child.gameObject.AddComponent<InventoryTile>();

                tile.gridPosition = new Vector2Int(x, y);
                tile.rect = child.GetComponent<RectTransform>();

                tiles[x, y] = tile;

                index++;
            }
        }
    }

    public bool CanPlaceItem(Vector2Int position, InventoryItem item)
    {
        bool[,] shape = item.shape;

        int w = shape.GetLength(0);
        int h = shape.GetLength(1);

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                if (!shape[x, y])
                    continue;

                int gridX = position.x + x - item.anchor.x;
                int gridY = position.y + y - item.anchor.y;

                if (gridX < 0 || gridY < 0 ||
                    gridX >= width || gridY >= height)
                    return false;

                if (grid[gridX, gridY] != null)
                    return false;
            }
        }

        return true;
    }

    public bool PlaceItem(Vector2Int position, InventoryItem item)
    {
        if (!CanPlaceItem(position, item))
            return false;

        bool[,] shape = item.shape;

        int w = shape.GetLength(0);
        int h = shape.GetLength(1);

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                if (!shape[x, y])
                    continue;

                int gridX = position.x + x - item.anchor.x;
                int gridY = position.y + y - item.anchor.y;

                grid[gridX, gridY] = item;
            }
        }

        return true;
    }

    public void RemoveItem(InventoryItem item)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == item)
                {
                    grid[x, y] = null;
                }
            }
        }
    }
}