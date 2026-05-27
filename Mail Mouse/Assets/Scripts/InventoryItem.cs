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
        shape = new bool[1, 2]
        {
            { true, true },
        };
    }

    public void RotateClockwise()
    {
        rotation += 90;

        if (rotation >= 360)
            rotation = 0;

        shape = RotateShape(shape);

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

}
