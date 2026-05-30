using UnityEngine;
using UnityEngine.UI;

public class InventoryPlacementPreview : MonoBehaviour
{
    public static InventoryPlacementPreview Instance;

    private RectTransform root;

    [SerializeField] private Color validColor = new Color(0f, 1f, 0f, 0.25f);
    [SerializeField] private Color invalidColor = new Color(1f, 0f, 0f, 0.25f);

    private void Awake()
    {
        Instance = this;

        GameObject go = new GameObject("PlacementPreview", typeof(RectTransform));
        root = go.GetComponent<RectTransform>();

        root.SetParent(transform, false);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        go.SetActive(false);
    }

    public void Hide()
    {
        if (root == null) return;

        root.gameObject.SetActive(false);

        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }

    public void ShowPreview(
        InventoryItem item,
        Vector2Int origin,
        bool canPlace,
        InventoryGrid grid)
    {
        if (item == null || grid == null) return;

        Hide();
        root.gameObject.SetActive(true);

        Color c = canPlace ? validColor : invalidColor;

        Vector2 cell = grid.CellSize;
        Vector2 spacing = grid.Spacing;

        bool[,] shape = item.Shape;

        int w = shape.GetLength(0);
        int h = shape.GetLength(1);

        // IMPORTANT: origin is the hovered grid cell
        Vector2 startWorld =
            GetCellLocalPosition(origin, grid, item.RectTransform);

        // MAIN CELLS
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (!shape[x, y]) continue;

                Vector2 localPos = GetCellOffset(x, y, cell, spacing);

                CreateBlock(
                    startWorld + localPos,
                    cell,
                    c
                );
            }
        }

        BuildBridges(shape, grid, origin, cell, spacing, c);
    }

    // =====================================================
    // GRID ORIGIN -> LOCAL POSITION
    // =====================================================

    private Vector2 GetCellLocalPosition(Vector2Int gridPos, InventoryGrid grid, RectTransform itemLayer)
    {
        RectTransform tile =
            grid.GetTileRect(gridPos); // we add this helper below

        return itemLayer.InverseTransformPoint(tile.position);
    }

    // =====================================================
    // OFFSET INSIDE ITEM SHAPE
    // =====================================================

    private Vector2 GetCellOffset(int x, int y, Vector2 cell, Vector2 spacing)
    {
        return new Vector2(
            x * (cell.x + spacing.x) + cell.x * 0.5f,
            -(y * (cell.y + spacing.y) + cell.y * 0.5f)
        );
    }

    // =====================================================
    // BRIDGES (spacing fill)
    // =====================================================

    private void BuildBridges(
        bool[,] shape,
        InventoryGrid grid,
        Vector2Int origin,
        Vector2 cell,
        Vector2 spacing,
        Color c)
    {
        int w = shape.GetLength(0);
        int h = shape.GetLength(1);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w - 1; x++)
            {
                if (!shape[x, y] || !shape[x + 1, y]) continue;

                CreateBlock(
                    GetCellLocalPosition(origin, grid, root)
                    + GetCellOffset(x, y, cell, spacing)
                    + new Vector2(cell.x + spacing.x * 0.5f, 0),
                    new Vector2(spacing.x, cell.y),
                    c
                );
            }
        }

        for (int y = 0; y < h - 1; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (!shape[x, y] || !shape[x, y + 1]) continue;

                CreateBlock(
                    GetCellLocalPosition(origin, grid, root)
                    + GetCellOffset(x, y, cell, spacing)
                    + new Vector2(0, -(cell.y + spacing.y * 0.5f)),
                    new Vector2(cell.x, spacing.y),
                    c
                );
            }
        }
    }

    // =====================================================
    // CREATE BLOCK
    // =====================================================

    private void CreateBlock(Vector2 pos, Vector2 size, Color c)
    {
        GameObject go = new GameObject("Preview", typeof(RectTransform), typeof(Image));

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(root, false);

        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        go.GetComponent<Image>().color = c;
    }
}