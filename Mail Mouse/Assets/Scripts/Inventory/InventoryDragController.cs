using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class InventoryDragController : MonoBehaviour
{
    [Header("References")]
    public InventoryGrid grid;
    public RectTransform itemLayer;

    private InventoryItem heldItem;

    private Vector2 originalLocalPos;
    private Vector2Int originalGridPos;
    private int originalRotation;

    private InventoryInstance sourceInventory;
    private InventoryInstance currentInventory;

    private bool dragging;

    private void Update()
    {
        if (!dragging || heldItem == null)
            return;

        HandleDragMovement();
        HandleRotationInput();
        HandlePreview();
    }

    public void BeginDrag(InventoryItem item)
    {
        heldItem = item;
        dragging = true;

        sourceInventory = item.OwnerInventory;
        currentInventory = sourceInventory;

        item.transform.SetAsLastSibling();

        originalLocalPos = item.RectTransform.localPosition;
        originalGridPos = item.GridPosition;
        originalRotation = item.Rotation;

        sourceInventory.grid.RemoveItem(item);

        item.RectTransform.SetParent(item.OwnerInventory.itemLayer, true);
        item.SetBackgroundVisible(false);
    }

    private void HandleDragMovement()
    {
        heldItem.RectTransform.position = Input.mousePosition;
    }

    private void HandleRotationInput()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            heldItem.RotateClockwise();
        }
    }

    private void HandlePreview()
    {
        InventoryTile hoveredTile = GetHoveredTile();

        if (hoveredTile == null)
        {
            currentInventory.grid.ClearPreview();
            return;
        }

        Vector2Int target = hoveredTile.gridPosition;

        currentInventory = sourceInventory;

        currentInventory.grid.ShowPreview(target, heldItem);
    }

    public void EndDrag()
    {
        dragging = false;

        currentInventory.grid.ClearPreview();

        if (heldItem == null)
            return;

        InventoryTile hoveredTile = GetHoveredTile();

        if (hoveredTile == null)
        {
            ReturnItem();
            return;
        }

        Vector2Int target = hoveredTile.gridPosition;

        bool success = currentInventory.grid.PlaceItem(target, heldItem);

        if (!success)
        {
            ReturnItem();
            return;
        }

        SnapToGrid(target);

        heldItem.SetBackgroundVisible(true);
        heldItem = null;
    }

    private InventoryTile GetHoveredTile()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var r in results)
        {
            InventoryTile tile = r.gameObject.GetComponent<InventoryTile>();
            if (tile != null)
                return tile;
        }

        return null;
    }

    private void SnapToGrid(Vector2Int pos)
    {
        Vector2 finalPos =
            grid.GetItemWorldPosition(pos, heldItem, itemLayer);

        heldItem.RectTransform.localPosition = finalPos;
    }

    private void ReturnItem()
    {
        heldItem.RotateTo(originalRotation);

        sourceInventory.grid.PlaceItem(originalGridPos, heldItem);

        heldItem.RectTransform.localPosition = originalLocalPos;

        heldItem.SetBackgroundVisible(true);

        heldItem = null;
    }
}