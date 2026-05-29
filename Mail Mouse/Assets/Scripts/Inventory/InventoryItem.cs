using UnityEngine;

public class InventoryItem : MonoBehaviour
{
    public bool[,] shape;

    public Vector2Int gridPosition;

    [SerializeField] private int rotation = 0;

    public RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        // TEMP TEST SHAPE (remove later when you build real items)
            shape = new bool[1, 4]
            {
                { true, true, true, true }
            };
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
        rotation += 90;

        if (rotation >= 360)
            rotation = 0;

        shape = RotateShape(shape);
        DebugPrintShape();

        // VISUAL ROTATION (THIS IS WHAT YOU WERE MISSING)
        if (rectTransform != null)
            rectTransform.rotation = Quaternion.Euler(0, 0, -rotation);
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
                rotated[y, w - 1 - x] = original[x, y];
            }
        }

        return rotated;
    }

    public bool[,] GetShape()
    {
        return shape;
    }

    [System.Serializable]
    public class ItemShape
    {
        public bool[,] cells;
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
                row += shape[x, y] ? "[X]" : "[ ]";
            }

            Debug.Log(row);
        }

        Debug.Log($"Width: {w} Height: {h}");
    }

}
