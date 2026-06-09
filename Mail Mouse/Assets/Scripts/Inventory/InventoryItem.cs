using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private MailData mailData;

    [TextArea(3, 10)]
    [SerializeField] private string shapeDefinition = "X";

    [Header("Tile Visual")]
    [SerializeField] private Color tileColor = new Color32(88, 88, 88, 179);

    private bool[,] shape;
    private Vector2Int anchor;
    private Vector2Int gridPosition;
    private int rotation;
    private RectTransform rectTransform;
    private InventoryDragController dragController;
    private InventoryInstance ownerInventory;
    private RectTransform backgroundRoot;

    public MailData MailData => mailData;
    public Vector2Int GridPosition => gridPosition;
    public int Rotation => rotation;
    public RectTransform RectTransform => rectTransform;
    public bool[,] Shape => shape;
    public Vector2Int Anchor => anchor;
    public int Width => shape.GetLength(0);
    public int Height => shape.GetLength(1);
    public InventoryInstance OwnerInventory => ownerInventory;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        dragController = InventoryDragController.Instance ?? FindFirstObjectByType<InventoryDragController>();
        ownerInventory = GetComponentInParent<InventoryInstance>();

        if (ownerInventory == null)
        {
            Debug.LogWarning($"{name} is not parented under an InventoryInstance.", this);
            return;
        }

        if (dragController == null)
            Debug.LogWarning($"No InventoryDragController found in scene for {name}.", this);

        AlignRoot();
        BuildShapeFromDefinition();
        CalculateAnchor();
        UpdateRectSize();
        BuildBackgroundVisual();
    }

    private void OnValidate()
    {
        rectTransform = GetComponent<RectTransform>();
        ownerInventory ??= GetComponentInParent<InventoryInstance>();
    }

    private void AlignRoot()
    {
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
    }

    private void UpdateRectSize()
    {
        Vector2 cell = ownerInventory.Grid.CellSize;
        Vector2 spacing = ownerInventory.Grid.Spacing;

        float totalWidth = Width * cell.x + (Width - 1) * spacing.x;
        float totalHeight = Height * cell.y + (Height - 1) * spacing.y;

        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, totalWidth);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalHeight);
    }

    private void BuildShapeFromDefinition()
    {
        string[] rows = shapeDefinition.Replace("\r", "").Split('\n');
        int height = rows.Length;
        int width = rows[0].Length;

        shape = new bool[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                shape[x, y] = rows[y][x] == 'X';
            }
        }
    }

    private void BuildBackgroundVisual()
    {
        if (backgroundRoot != null)
            Destroy(backgroundRoot.gameObject);

        backgroundRoot = new GameObject("BackgroundRoot", typeof(RectTransform)).GetComponent<RectTransform>();
        backgroundRoot.SetParent(rectTransform, false);
        backgroundRoot.anchorMin = Vector2.zero;
        backgroundRoot.anchorMax = Vector2.one;
        backgroundRoot.offsetMin = Vector2.zero;
        backgroundRoot.offsetMax = Vector2.zero;
        backgroundRoot.SetAsFirstSibling();

        Vector2 cell = ownerInventory.Grid.CellSize;
        Vector2 spacing = ownerInventory.Grid.Spacing;

        float totalWidth = Width * cell.x + (Width - 1) * spacing.x;
        float totalHeight = Height * cell.y + (Height - 1) * spacing.y;
        float startX = -totalWidth * 0.5f;
        float startY = totalHeight * 0.5f;

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (!shape[x, y])
                    continue;

                CreateTile(x, y, startX, startY, cell, spacing);
            }
        }

        BuildSpacingFill(startX, startY, cell, spacing, Width, Height);
    }

    private void CreateTile(int x, int y, float startX, float startY, Vector2 cell, Vector2 spacing)
    {
        GameObject tile = new GameObject($"Tile_{x}_{y}", typeof(RectTransform), typeof(Image));
        RectTransform rt = tile.GetComponent<RectTransform>();
        rt.SetParent(backgroundRoot, false);
        rt.sizeDelta = cell;
        rt.anchoredPosition = new Vector2(
            startX + x * (cell.x + spacing.x) + cell.x * 0.5f,
            startY - y * (cell.y + spacing.y) - cell.y * 0.5f
        );
        tile.GetComponent<Image>().color = tileColor;
    }

    private void BuildSpacingFill(float startX, float startY, Vector2 cell, Vector2 spacing, int w, int h)
    {
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w - 1; x++)
            {
                if (!shape[x, y] || !shape[x + 1, y])
                    continue;

                CreateSpacingTile(
                    startX + x * (cell.x + spacing.x) + cell.x + spacing.x * 0.5f,
                    startY - y * (cell.y + spacing.y) - cell.y * 0.5f,
                    new Vector2(spacing.x, cell.y)
                );
            }
        }

        for (int y = 0; y < h - 1; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (!shape[x, y] || !shape[x, y + 1])
                    continue;

                CreateSpacingTile(
                    startX + x * (cell.x + spacing.x) + cell.x * 0.5f,
                    startY - y * (cell.y + spacing.y) - cell.y - spacing.y * 0.5f,
                    new Vector2(cell.x, spacing.y)
                );
            }
        }
    }

    private void CreateSpacingTile(float x, float y, Vector2 size)
    {
        GameObject tile = new GameObject("Spacing", typeof(RectTransform), typeof(Image));
        RectTransform rt = tile.GetComponent<RectTransform>();
        rt.SetParent(backgroundRoot, false);
        rt.sizeDelta = size;
        rt.anchoredPosition = new Vector2(x, y);
        tile.GetComponent<Image>().color = tileColor;
    }

    public void SetGridPosition(Vector2Int newPos)
    {
        gridPosition = newPos;
    }

    public void SetOwnerInventory(InventoryInstance inventory)
    {
        ownerInventory = inventory;
    }

    public void SetBackgroundVisible(bool visible)
    {
        if (backgroundRoot == null)
            return;

        backgroundRoot.gameObject.SetActive(visible);
    }

    public void OnPointerDown(PointerEventData eventData) { }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragController?.BeginDrag(this);
    }

    public void OnDrag(PointerEventData eventData) { }

    public void OnEndDrag(PointerEventData eventData)
    {
        dragController?.EndDrag();
    }

    public void RotateTo(int targetRotation)
    {
        targetRotation %= 360;
        while (rotation != targetRotation)
            RotateClockwise();
    }

    public void RotateClockwise()
    {
        int oldHeight = shape.GetLength(1);
        rotation = (rotation + 90) % 360;
        shape = RotateShape(shape);
        anchor = new Vector2Int(oldHeight - 1 - anchor.y, anchor.x);
        rectTransform.rotation = Quaternion.Euler(0, 0, -rotation);
    }

    private bool[,] RotateShape(bool[,] original)
    {
        int w = original.GetLength(0);
        int h = original.GetLength(1);
        bool[,] rotated = new bool[h, w];

        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                rotated[h - 1 - y, x] = original[x, y];

        return rotated;
    }

    private void CalculateAnchor()
    {
        int w = shape.GetLength(0);
        int h = shape.GetLength(1);
        Vector2 center = new Vector2((w - 1) / 2f, (h - 1) / 2f);

        float best = float.MaxValue;
        Vector2Int bestCell = Vector2Int.zero;

        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                if (shape[x, y])
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d < best)
                    {
                        best = d;
                        bestCell = new Vector2Int(x, y);
                    }
                }

        anchor = bestCell;
    }

    [ContextMenu("Debug Item State")]
    public void DebugItemState()
    {
        Debug.Log($"Item '{name}' owner={(ownerInventory == null ? "none" : ownerInventory.InventoryId)} pos={gridPosition} rot={rotation}", this);
    }
}