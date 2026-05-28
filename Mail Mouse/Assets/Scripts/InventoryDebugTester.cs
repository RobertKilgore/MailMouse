using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.UI;

public class InventoryDebugTester : MonoBehaviour
{
    public InventoryGrid grid;
    public InventoryItem testItem;

    public RectTransform itemLayer;

    public Vector2Int testPosition;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
            MoveToMousePosition(); 

        if (Input.GetKeyDown(KeyCode.Alpha1)) {
            grid.RemoveItem(testItem);
            TryPlace();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
            grid.RemoveItem(testItem);

        if (Input.GetKeyDown(KeyCode.R)) {
            testItem.RotateClockwise();
            grid.RemoveItem(testItem);
            TryPlace();
        }
    }

    private void TryPlace()
    {
        if (!grid.PlaceItem(testPosition, testItem))
            return;

        RectTransform tileRect =
            grid.tiles[testPosition.x, testPosition.y].rect;

        // IMPORTANT
        // Parent to item layer first
        testItem.rectTransform.SetParent(itemLayer, false);

        // Match the tile's world position
        testItem.rectTransform.position = tileRect.position;
        Vector2 offset = new Vector2(0,0);
        // Because pivot is centered,
        // offset by half item size
        if(testItem.GetRotation() % 180 == 0) {
            offset = new Vector2(
                testItem.rectTransform.rect.width * (.25f * (testItem.GetWidth() - 1)),
                -testItem.rectTransform.rect.height * (.25f * (testItem.GetHeight() - 1))
            );
        } else  {
            offset = new Vector2(
                testItem.rectTransform.rect.height * (.25f * (testItem.GetWidth() - 1)),
                -testItem.rectTransform.rect.width * (.25f * (testItem.GetHeight() - 1))
            );
        }

        Debug.Log(testItem.rectTransform.rect.width);
        Debug.Log(testItem.rectTransform.rect.height);
        Debug.Log(offset);

        testItem.rectTransform.anchoredPosition += offset;
    }

    private void MoveToMousePosition()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            InventoryTile tile = result.gameObject.GetComponent<InventoryTile>();

            if (tile != null)
            {
                testPosition = tile.gridPosition;
                return;
            }
        }
    }
}