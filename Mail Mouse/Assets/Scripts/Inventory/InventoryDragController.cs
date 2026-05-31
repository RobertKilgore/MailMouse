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
    private int originalRotation;

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

        item.transform.SetAsLastSibling();

        originalLocalPos = item.RectTransform.localPosition;
        originalGridPos = item.GridPosition;
        originalRotation = item.Rotation;

        grid.RemoveItem(item);

        item.RectTransform.SetParent(itemLayer, true);
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

    // =========================
    // PREVIEW (SAFE VERSION)
    // =========================
    private void HandlePreview()
    {
        InventoryTile hoveredTile = GetHoveredTile();

        if (hoveredTile == null)
        {
            grid.ClearPreview();
            return;
        }

        Vector2Int target = hoveredTile.gridPosition;
        grid.ShowPreview(target, heldItem);
    }

    public void EndDrag()
    {
        dragging = false;

        grid.ClearPreview();

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

        heldItem.SetBackgroundVisible(true);
        heldItem = null;
    }

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

    private void SnapToGrid(Vector2Int pos)
    {
        Vector2 finalPos =
            grid.GetItemWorldPosition(pos, heldItem, itemLayer);

        heldItem.RectTransform.localPosition = finalPos;
    }

    private void ReturnItem()
    {
        heldItem.RotateTo(originalRotation);

        grid.PlaceItem(originalGridPos, heldItem);

        heldItem.RectTransform.localPosition = originalLocalPos;

        heldItem.SetBackgroundVisible(true);

        heldItem = null;
    }
}