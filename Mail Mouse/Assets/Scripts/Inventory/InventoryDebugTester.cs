using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class InventoryDebugTester : MonoBehaviour
{
    public InventoryGrid grid;
    public InventoryItem testItem;
    public RectTransform itemLayer;
    public Vector2Int testPosition;

    private InventoryTile currentHoveredTile;

    private void Update()
    {
        UpdateHoveredTile();

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            grid.RemoveItem(testItem);
            TryPlace();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            grid.RemoveItem(testItem);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            grid.RemoveItem(testItem);
            testItem.RotateClockwise();
            TryPlace();
        }
    }

    // -----------------------------
    // HOVER SYSTEM
    // -----------------------------
    private void UpdateHoveredTile()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        InventoryTile foundTile = null;

        foreach (var result in results)
        {
            InventoryTile tile = result.gameObject.GetComponent<InventoryTile>();
            if (tile != null)
            {
                foundTile = tile;
                break;
            }
        }

        // ENTER
        if (foundTile != null && foundTile != currentHoveredTile)
        {
            currentHoveredTile = foundTile;
            testPosition = foundTile.gridPosition;

            Debug.Log($"ENTER TILE: {testPosition}");
        }

        // EXIT
        if (foundTile == null && currentHoveredTile != null)
        {
            Debug.Log($"EXIT TILE: {currentHoveredTile.gridPosition}");
            currentHoveredTile = null;
        }
    }

    // -----------------------------
    // CORE OFFSET LOGIC
    // -----------------------------
    private Vector2 CalculateVisualOffset(InventoryItem item)
    {
        Vector2 cellSize = grid.cellSize;
        Debug.Log(cellSize);

        int w = item.GetWidth();
        int h = item.GetHeight();

        // ITEM center in GRID SPACE
        Vector2 itemCenter = new Vector2(w, h) * 0.5f;

        // anchor center in GRID SPACE
        Vector2 anchorCenter = new Vector2(
            item.anchor.x + 0.5f,
            item.anchor.y + 0.5f
        );

        Vector2 gridOffset = itemCenter - anchorCenter;

        Vector2 pixelOffset = Vector2.Scale(gridOffset, cellSize);

        Debug.Log($"ItemCenter(grid): {itemCenter}");
        Debug.Log($"AnchorCenter(grid): {anchorCenter}");
        Debug.Log($"Offset(pixel): {pixelOffset}");

        return pixelOffset;
    }

    // -----------------------------
    // PLACEMENT
    // -----------------------------
    private void TryPlace()
    {
        Debug.Log("===== TRY PLACE =====");
        Debug.Log($"Target Position: {testPosition}");
        Debug.Log($"Item Anchor: {testItem.anchor}");
        Debug.Log($"Item Size: {testItem.GetWidth()}x{testItem.GetHeight()}");

        if (!grid.PlaceItem(testPosition, testItem))
        {
            Debug.Log("PLACEMENT FAILED");
            return;
        }

        RectTransform tileRect =
            grid.tiles[testPosition.x, testPosition.y].rect;

        // ALWAYS use UI-local space
        testItem.rectTransform.SetParent(itemLayer, true);

        Vector2 tileLocalPos =
            itemLayer.InverseTransformPoint(tileRect.position);

        Vector2 offset =
            CalculateVisualOffset(testItem);

        testItem.rectTransform.localPosition =
            tileLocalPos - offset;

        Debug.Log("PLACEMENT SUCCESS");

        Debug.Log($"Tile Local Pos: {tileLocalPos}");
        Debug.Log($"Final Local Pos: {testItem.rectTransform.localPosition}");
    }
}