using UnityEngine;

public class InventoryItem : MonoBehaviour
{
    public bool[,] shape;

    public Vector2Int gridPosition;

    public Vector2Int anchor;

    [SerializeField] private int rotation = 0;

    public RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        // TEMP TEST SHAPE
        shape = new bool[3, 3]
        {
            { false, true, false},
            {false, true, true},
            {true, true, false}
        };
        

        CalculateAnchor();
        DebugPrintShape();
    }

    public int GetWidth()
    {
        return shape.GetLength(0);
    }

    public int GetHeight()
    {
        return shape.GetLength(1);
    }

    public int GetRotation()
    {
        return rotation;
    }

    public void RotateClockwise()
    {
        int oldHeight = shape.GetLength(1);

        rotation += 90;

        if (rotation >= 360)
            rotation = 0;

        shape = RotateShape(shape);

        anchor = RotateAnchor(anchor, oldHeight);

        DebugPrintShape();

        if (rectTransform != null)
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
                // CLOCKWISE ROTATION
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

    private void CalculateAnchor()
    {
        int w = shape.GetLength(0);
        int h = shape.GetLength(1);

        Vector2 center = new Vector2(
            (w - 1) / 2f,
            (h - 1) / 2f
        );

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

        Debug.Log($"Anchor: {anchor}");
    }

    public bool[,] GetShape()
    {
        return shape;
    }

    public void DebugPrintShape()
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
                    row += shape[x, y] ? "[X]" : "[ ]";
            }

            Debug.Log(row);
        }

        Debug.Log($"Width: {w} Height: {h}");
    }
}