using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class InventoryDragController : MonoBehaviour
{
    public static InventoryDragController Instance { get; private set; }

    [Header("Drag Layer")]
    [SerializeField] private RectTransform dragLayer;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private InventoryItem heldItem;
    private Vector2 originalLocalPos;
    private Vector2Int originalGridPos;
    private int originalRotation;
    private InventoryInstance sourceInventory;
    private InventoryGrid currentPreviewGrid;
    private bool dragging;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeDragLayer();
            return;
        }

        Debug.LogWarning($"Multiple {nameof(InventoryDragController)} instances found. Using the first one.", this);
    }

    private void InitializeDragLayer()
    {
        if (dragLayer == null)
        {
            dragLayer = GetComponent<RectTransform>();
            if (dragLayer == null)
            {
                DebugLogWarning("InventoryDragController needs a RectTransform or a dragLayer assigned.");
                return;
            }

            Canvas canvas = dragLayer.GetComponentInParent<Canvas>();
            if (canvas != null)
                dragLayer.SetParent(canvas.transform, false);
        }

        dragLayer.SetAsLastSibling();
    }

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
        currentPreviewGrid = null;

        if (sourceInventory == null)
        {
            DebugLogWarning($"Cannot begin drag for {item.name}: missing owner inventory.");
            dragging = false;
            return;
        }

        item.transform.SetAsLastSibling();
        originalLocalPos = item.RectTransform.localPosition;
        originalGridPos = item.GridPosition;
        originalRotation = item.Rotation;

        sourceInventory.Grid.RemoveItem(item);
        item.RectTransform.SetParent(dragLayer, true);
        item.SetBackgroundVisible(false);

        DebugLog($"BeginDrag {item.name} from {sourceInventory.InventoryId}");
    }

    private void HandleDragMovement()
    {
        heldItem.RectTransform.position = Input.mousePosition;
    }

    private void HandleRotationInput()
    {
        if (Input.GetKeyDown(KeyCode.R))
            heldItem.RotateClockwise();
    }

    private void HandlePreview()
    {
        var hoveredTile = GetHoveredTile();
        var hoveredGrid = hoveredTile?.Grid;

        if (hoveredGrid != currentPreviewGrid)
        {
            currentPreviewGrid?.ClearPreview();
            currentPreviewGrid = hoveredGrid;
        }

        if (currentPreviewGrid == null || hoveredTile == null)
        {
            return;
        }

        currentPreviewGrid.ShowPreview(hoveredTile.gridPosition, heldItem);
        DebugLog($"Preview on {currentPreviewGrid.Owner.InventoryId} at {hoveredTile.gridPosition}");
    }

    public void EndDrag()
    {
        dragging = false;
        currentPreviewGrid?.ClearPreview();

        if (heldItem == null)
            return;

        var hoveredTile = GetHoveredTile();
        var targetGrid = hoveredTile?.Grid;

        if (targetGrid == null)
        {
            DebugLog($"EndDrag: no valid target tile, returning {heldItem.name}");
            ReturnItem();
            return;
        }

        Vector2Int target = hoveredTile.gridPosition;
        bool success = targetGrid.PlaceItem(target, heldItem);

        if (!success)
        {
            DebugLog($"EndDrag: target placement rejected on {targetGrid.Owner.InventoryId} at {target}");
            ReturnItem();
            return;
        }

        heldItem.RectTransform.SetParent(targetGrid.Owner.ItemLayer, false);
        SnapToGrid(target, targetGrid, targetGrid.Owner.ItemLayer);
        heldItem.SetBackgroundVisible(true);

        DebugLog($"Dropped {heldItem.name} into {targetGrid.Owner.InventoryId} at {target}");

        heldItem = null;
        currentPreviewGrid = null;
    }

    private InventoryTile GetHoveredTile()
    {
        if (EventSystem.current == null)
        {
            DebugLogWarning("Missing EventSystem in scene.");
            return null;
        }

        var eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            if (result.gameObject.TryGetComponent<InventoryTile>(out var tile))
                return tile;
        }

        return null;
    }

    private void SnapToGrid(Vector2Int pos, InventoryGrid targetGrid, RectTransform targetItemLayer)
    {
        Vector2 finalPos = targetGrid.GetItemWorldPosition(pos, heldItem, targetItemLayer);
        heldItem.RectTransform.localPosition = finalPos;
    }

    private void ReturnItem()
    {
        if (heldItem == null)
            return;

        heldItem.RotateTo(originalRotation);

        if (sourceInventory != null)
        {
            sourceInventory.Grid.PlaceItem(originalGridPos, heldItem);
            heldItem.RectTransform.SetParent(sourceInventory.ItemLayer, false);
            heldItem.RectTransform.localPosition = originalLocalPos;
            heldItem.SetBackgroundVisible(true);
            DebugLog($"Returned {heldItem.name} to {sourceInventory.InventoryId}");
        }
        else
        {
            DebugLogWarning($"Cannot return {heldItem.name}: missing source inventory.");
        }

        heldItem = null;
    }

    private void DebugLog(string message)
    {
        if (debugLogs)
            Debug.Log(message, this);
    }

    private void DebugLogWarning(string message)
    {
        if (debugLogs)
            Debug.LogWarning(message, this);
    }

    [ContextMenu("Debug Drag Controller")]
    public void DebugControllerState()
    {
        Debug.Log($"Dragging={dragging}, HeldItem={(heldItem == null ? "none" : heldItem.name)}, CurrentPreviewGrid={(currentPreviewGrid == null ? "none" : currentPreviewGrid.Owner.InventoryId)}", this);
    }
}