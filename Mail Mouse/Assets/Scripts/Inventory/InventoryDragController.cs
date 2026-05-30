using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryDragController : MonoBehaviour
{
    [Header("References")]
    public InventoryGrid grid;
    public RectTransform itemLayer;

    private InventoryItem heldItem;

    private Vector2 originalLocalPos;
    private Vector2Int originalGridPos;

    private bool dragging;

    private void Update()
    {
        if (!dragging || heldItem == null)
            return;

        HandleDragMovement();
        HandleRotationInput();
    }

    // =====================================================
    // DRAG START
    // =====================================================

    public void BeginDrag(InventoryItem item)
    {
        heldItem = item;
        dragging = true;

        originalLocalPos = item.RectTransform.localPosition;
        originalGridPos = item.GridPosition;

        grid.RemoveItem(item);

        item.RectTransform.SetParent(itemLayer, true);
    }

    // =====================================================
    // DRAG MOVE
    // =====================================================

    private void HandleDragMovement()
    {
        heldItem.RectTransform.position = Input.mousePosition;
    }

    // =====================================================
    // ROTATION INPUT
    // =====================================================

    private void HandleRotationInput()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            heldItem.RotateClockwise();
        }
    }

    // =====================================================
    // DROP
    // =====================================================

    public void EndDrag()
    {
        dragging = false;

        if (heldItem == null)
            return;

        InventoryTile hoveredTile = GetHoveredTile();

        if (hoveredTile == null)
        {
            ReturnItem();
            return;
        }

        Vector2Int target = hoveredTile.gridPosition;

        bool success = grid.PlaceItem(target, heldItem);

        if (!success)
        {
            ReturnItem();
            return;
        }

        SnapToGrid(target);

        heldItem = null;
    }

    // =====================================================
    // HOVER DETECTION (SIMPLE SAFE VERSION)
    // =====================================================

    private InventoryTile GetHoveredTile()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var r in results)
        {
            InventoryTile tile = r.gameObject.GetComponent<InventoryTile>();
            if (tile != null)
                return tile;
        }

        return null;
    }

    // =====================================================
    // SNAP TO GRID
    // =====================================================

    private void SnapToGrid(Vector2Int pos)
    {
        Vector2 finalPos =
            grid.GetItemWorldPosition(
                pos,
                heldItem,
                itemLayer
            );

        heldItem.RectTransform.localPosition = finalPos;
    }

    // =====================================================
    // RETURN ITEM
    // =====================================================

    private void ReturnItem()
    {
        grid.PlaceItem(originalGridPos, heldItem);

        heldItem.RectTransform.localPosition = originalLocalPos;

        heldItem = null;
    }
}