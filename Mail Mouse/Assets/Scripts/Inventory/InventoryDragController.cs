using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class InventoryDragController : MonoBehaviour
{
    [Header("References")]
    public InventoryGrid grid;
    public RectTransform itemLayer;

    private InventoryItem heldItem;
    private InventoryTile hoveredTile;

    private Vector2 originalLocalPos;
    private Vector2Int originalGridPos;

    private bool dragging;

    private void Update()
    {
        HandleHover();

        if (!dragging || heldItem == null)
            return;

        HandleDragMovement();
        HandleRotationInput();
    }

    // =====================================================
    // HOVER SYSTEM
    // =====================================================

    private void HandleHover()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        InventoryTile found = null;

        foreach (var r in results)
        {
            found = r.gameObject.GetComponent<InventoryTile>();
            if (found != null)
                break;
        }

        hoveredTile = found;
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
    // ROTATION
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
    // SNAP
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