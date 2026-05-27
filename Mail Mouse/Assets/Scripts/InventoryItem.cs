using UnityEngine;

public class InventoryItem : MonoBehaviour
{
    
    [SerializeField] private int rotation;
    
    public bool[,] shape;

    public void RotateClockwise()
    {
        rotation += 90;

        if (rotation >= 360)
            rotation = 0;

        shape = RotateShape(shape);
    }

    private bool[,] RotateShape(bool[,] original)
    {
        int width = original.GetLength(0);
        int height = original.GetLength(1);

        bool[,] rotated = new bool[height, width];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                rotated[y, width - 1 - x] = original[x, y];
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
