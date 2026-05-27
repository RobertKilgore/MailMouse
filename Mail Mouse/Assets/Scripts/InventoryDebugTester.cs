using UnityEngine;

public class InventoryDebugTester : MonoBehaviour
{
    public InventoryGrid grid;

    public InventoryItem testItem;

    public Vector2Int testPosition;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TryPlace();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            grid.RemoveItem(testItem);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            testItem.RotateClockwise();
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            MoveToMousePosition();
        }
    }

    private void TryPlace()
    {
        if (grid.PlaceItem(testPosition, testItem))
        {
            Debug.Log("Placed item at " + testPosition);
        }
        else
        {
            Debug.Log("FAILED to place item at " + testPosition);
        }
    }

    private void MoveToMousePosition()
    {
        Vector2 mouse = Input.mousePosition;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            grid.GetComponent<RectTransform>(),
            mouse,
            null,
            out localPoint
        );

        int x = Mathf.FloorToInt(localPoint.x / 64f);
        int y = Mathf.FloorToInt(-localPoint.y / 64f);

        testPosition = new Vector2Int(x, y);

        Debug.Log("Mouse grid position: " + testPosition);
    }
}