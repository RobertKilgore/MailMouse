using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryDebugTester : MonoBehaviour
{
    public InventoryGrid grid;
    public InventoryItem testItem;

    public RectTransform gridRect;

    public Vector2Int testPosition;

    private void Update()
    {
        // Press M → get mouse grid position
        if (Input.GetKeyDown(KeyCode.M))
        {
            MoveToMousePosition();
        }

        // Press 1 → try place item
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TryPlace();
        }

        // Press 2 → remove item
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            grid.RemoveItem(testItem);
            Debug.Log("Removed item");
        }

        // Press R → rotate item
        if (Input.GetKeyDown(KeyCode.R))
        {
            testItem.RotateClockwise();
            Debug.Log("Rotated item");
        }
    }

    private void TryPlace()
    {
        bool success = grid.PlaceItem(testPosition, testItem);

        if (!success)
        {
            Debug.Log("FAILED placement");
            return;
        }

        Vector2 pos = grid.GetTilePosition(testPosition);

        testItem.rectTransform.anchoredPosition = pos;

        testItem.gameObject.SetActive(true);
    }



    private void MoveToMousePosition()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            InventoryTile tile = result.gameObject.GetComponent<InventoryTile>();

            if (tile != null)
            {
                testPosition = tile.gridPosition;

                Debug.Log("Mouse over tile: " + testPosition);
                return;
            }
        }

        Debug.Log("Mouse not over any tile");
    }
}