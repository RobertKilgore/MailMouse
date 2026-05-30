using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryItem :
    MonoBehaviour,
    IPointerDownHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    // =====================================================
    // INSPECTOR DATA (DESIGNER-EDITABLE ONLY)
    // =====================================================

    [Header("Mail Data")]
    [SerializeField] private MailData mailData;

    [Header("Shape Definition")]
    [TextArea(3, 10)]
    [SerializeField] private string shapeDefinition = @"X";

    // =====================================================
    // RUNTIME STATE (NOT EDITOR ACCESSIBLE)
    // =====================================================

    private bool[,] shape;

    private Vector2Int anchor;
    private Vector2Int gridPosition;

    private int rotation = 0;

    private RectTransform rectTransform;
    private InventoryDragController dragController;

    // =====================================================
    // PUBLIC READ-ONLY ACCESS (SAFE FOR OTHER SYSTEMS)
    // =====================================================

    public MailData MailData => mailData;

    public Vector2Int GridPosition => gridPosition;

    public int Rotation => rotation;

    public RectTransform RectTransform => rectTransform;

    public bool[,] Shape => shape;

    public Vector2Int Anchor => anchor;

    public int Width => shape.GetLength(0);

    public int Height => shape.GetLength(1);

    // =====================================================
    // INITIALIZATION
    // =====================================================

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        dragController = FindFirstObjectByType<InventoryDragController>();

        BuildShapeFromDefinition();
        CalculateAnchor();

        DebugPrintShape();
    }

    // =====================================================
    // SHAPE PARSING
    // =====================================================

    private void BuildShapeFromDefinition()
    {
        string[] rows =
            shapeDefinition
            .Replace("\r", "")
            .Split('\n');

        int height = rows.Length;
        int width = rows[0].Length;

        shape = new bool[width, height];

        for (int y = 0; y < height; y++)
        {
            string row = rows[y];

            for (int x = 0; x < width; x++)
            {
                shape[x, y] = row[x] == 'X';
            }
        }
    }

    // =====================================================
    // GRID POSITION CONTROL (ONLY GRID CAN SET THIS)
    // =====================================================

    public void SetGridPosition(Vector2Int newPos)
    {
        gridPosition = newPos;
    }

    // =====================================================
    // DRAG EVENTS
    // =====================================================

    public void OnPointerDown(PointerEventData eventData)
    {
        // optional debug
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragController.BeginDrag(this);
    }

    public void OnDrag(PointerEventData eventData) { }

    public void OnEndDrag(PointerEventData eventData)
    {
        dragController.EndDrag();
    }

    // =====================================================
    // ROTATION (CONTROLLED INTERNAL STATE)
    // =====================================================

    public void RotateClockwise()
    {
        int oldHeight = shape.GetLength(1);

        rotation += 90;
        if (rotation >= 360)
            rotation = 0;

        shape = RotateShape(shape);
        anchor = RotateAnchor(anchor, oldHeight);

        rectTransform.rotation =
            Quaternion.Euler(0, 0, -rotation);
    }

    private bool[,] RotateShape(bool[,] original)
    {
        int w = original.GetLength(0);
        int h = original.GetLength(1);

        bool[,] rotated = new bool[h, w];

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                rotated[h - 1 - y, x] = original[x, y];
            }
        }

        return rotated;
    }

    private Vector2Int RotateAnchor(Vector2Int oldAnchor, int oldHeight)
    {
        return new Vector2Int(
            oldHeight - 1 - oldAnchor.y,
            oldAnchor.x
        );
    }

    // =====================================================
    // ANCHOR CALCULATION (INTERNAL ONLY)
    // =====================================================

    private void CalculateAnchor()
    {
        int w = shape.GetLength(0);
        int h = shape.GetLength(1);

        Vector2 center =
            new Vector2((w - 1) / 2f, (h - 1) / 2f);

        float bestDistance = float.MaxValue;
        Vector2Int bestCell = Vector2Int.zero;

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                if (!shape[x, y])
                    continue;

                float dist = Vector2.Distance(
                    new Vector2(x, y),
                    center
                );

                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    bestCell = new Vector2Int(x, y);
                }
            }
        }

        anchor = bestCell;
    }

    // =====================================================
    // DEBUG
    // =====================================================

    private void DebugPrintShape()
    {
        Debug.Log("===== ITEM SHAPE =====");

        int w = shape.GetLength(0);
        int h = shape.GetLength(1);

        for (int y = 0; y < h; y++)
        {
            string row = "";

            for (int x = 0; x < w; x++)
            {
                if (anchor.x == x && anchor.y == y)
                    row += "[A]";
                else
                    row += shape[x, y] ? "[X]" : "[_]";
            }

            Debug.Log(row);
        }

        Debug.Log($"Size: {w}x{h} | Rot: {rotation}");
    }

    // =====================================================
    // HELPER ACCESSORS (ONLY IF NEEDED)
    // =====================================================

    public string GetRecipient() => mailData?.recipient ?? "";
    public string GetAddress() => mailData?.address ?? "";
}