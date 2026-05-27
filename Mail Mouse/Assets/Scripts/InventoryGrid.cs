using UnityEngine;

public class InventoryGrid : MonoBehaviour
{
    [Header("Grid Size")]
    public int width = 8;
    public int height = 6;

    private InventoryItem[,] grid;
    
    private void Awake()
    {
        grid = new InventoryItem[width, height];
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

                int gridX = position.x + x;
                int gridY = position.y + y;

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

                int gridX = position.x + x;
                int gridY = position.y + y;

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
