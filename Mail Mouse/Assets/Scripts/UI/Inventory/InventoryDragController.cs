using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Handles drag-and-drop interaction for inventory items across inventory grids.
/// Keeps a single shared controller for the scene and manages preview display.
/// </summary>
public class InventoryDragController : MonoBehaviour
{
    public static InventoryDragController Instance { get; private set; }

    [Header("Drag Layer")]
    [SerializeField]
    private RectTransform dragLayer; // The layer used to hold the item while dragging.

    [Header("Debug")]
    [SerializeField]
    private bool debugLogs = true; // Toggle for runtime debug messages.

    [SerializeField]
    private bool debugHover = false; // Toggle for hover logging on all inventory tiles.

    private InventoryItem heldItem; // Currently dragged item.
    private Vector2 originalLocalPos; // Local position before drag.
    private Vector2Int originalGridPos; // Original grid cell before drag.
    private int originalRotation; // Original rotation before drag.
    private InventoryInstance sourceInventory; // Inventory item came from.
    private InventoryGrid currentPreviewGrid; // Currently active preview grid.
    private bool dragging; // Whether a drag is active.
    private InputSystem_Actions inputActions; // Input system actions.

    /// <summary>
    /// Initialize the singleton and prepare the drag layer.
    /// This makes sure only one controller is active in the scene.
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeDragLayer();
            inputActions = new InputSystem_Actions();
            return;
        }

        Debug.LogWarning($"Multiple {nameof(InventoryDragController)} instances found. Using the first one.", this);
    }

    private void OnEnable()
    {
        if (inputActions != null)
            inputActions.Enable();
    }

    private void OnDisable()
    {
        if (inputActions != null)
            inputActions.Disable();
    }

    /// <summary>
    /// Ensures the drag layer exists and is the last child of the inventory system parent.
    /// This keeps dragged items rendered above sibling inventory UI.
    /// </summary>
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
        }

        if (transform.parent != null)
        {
            dragLayer.SetParent(transform.parent, false);
            dragLayer.SetAsLastSibling();
        }
        else
        {
            dragLayer.SetAsLastSibling();
        }
    }

    /// <summary>
    /// Runs each frame while an item is being dragged.
    /// Updates position, rotation input, and preview state.
    /// </summary>
    private void Update()
    {
        if (!dragging || heldItem == null)
            return;

        HandleDragMovement();
        HandleRotationInput();
        HandlePreview();
    }

    /// <summary>
    /// Begins dragging the provided inventory item.
    /// Removes it from the source grid and stores its original state.
    /// </summary>
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

        if (dragLayer != null)
            dragLayer.SetAsLastSibling();

        AudioManager.PlayPackageSound();

        item.transform.SetAsLastSibling();
        originalLocalPos = item.RectTransform.localPosition;
        originalGridPos = item.GridPosition;
        originalRotation = item.Rotation;

        sourceInventory.Grid.RemoveItem(item);
        if (dragLayer != null)
            item.RectTransform.SetParent(dragLayer, true);
        item.SetBackgroundVisible(false);

        DebugLog($"BeginDrag {item.name} from {sourceInventory.InventoryId}");
    }

    /// <summary>
    /// Makes the dragged item follow the cursor.
    /// </summary>
    private void HandleDragMovement()
    {
        Vector2 pointerPos = GetPointerPosition();
        if (pointerPos == Vector2.zero)
            return;

        heldItem.RectTransform.position = pointerPos;
    }

    /// <summary>
    /// Rotates the dragged item when the Rotate action is pressed (R key).
    /// </summary>
    private void HandleRotationInput()
    {
        if (inputActions == null)
            return;

        if (inputActions.Player.Rotate.WasPressedThisFrame())
            heldItem.RotateClockwise();
    }

    /// <summary>
    /// Updates placement preview on the currently hovered inventory grid.
    /// Clears the previous preview when the hover target changes.
    /// </summary>
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
            return;

        currentPreviewGrid.ShowPreview(hoveredTile.gridPosition, heldItem);
        DebugLog($"Preview on {currentPreviewGrid.Owner.InventoryId} at {hoveredTile.gridPosition}");
    }

    /// <summary>
    /// Completes the drag operation and attempts to place the item in the hovered inventory.
    /// If placement fails, the item is returned to its original inventory.
    /// </summary>
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
        if (heldItem?.MailData != null)
            heldItem.MailData.placedByPlayer = true;

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

        AudioManager.PlayPackageSound();

        heldItem = null;
        currentPreviewGrid = null;

        RestockManager.Instance?.TriggerRestock();
    }

    /// <summary>
    /// Returns the inventory tile currently under the mouse, if any.
    /// Uses UI raycasting through the EventSystem.
    /// </summary>
    private InventoryTile GetHoveredTile()
    {
        if (EventSystem.current == null)
        {
            DebugLogWarning("Missing EventSystem in scene.");
            return null;
        }

        var eventData = new PointerEventData(EventSystem.current)
        {
            position = GetPointerPosition()
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

    /// <summary>
    /// Aligns the dragged item visually with the destination grid cell.
    /// This accounts for the item's anchor offset and target item layer.
    /// </summary>
    private void SnapToGrid(Vector2Int pos, InventoryGrid targetGrid, RectTransform targetItemLayer)
    {
        Vector2 finalPos = targetGrid.GetItemWorldPosition(pos, heldItem, targetItemLayer);
        heldItem.RectTransform.localPosition = finalPos;
    }

    /// <summary>
    /// Returns the current pointer position from the new Input System.
    /// This supports mouse, touch, and pen pointers.
    /// </summary>
    private Vector2 GetPointerPosition()
    {
        return Pointer.current?.position.ReadValue() ?? Vector2.zero;
    }

    /// <summary>
    /// Restores the dragged item to its original inventory if placement fails.
    /// Keeps the original rotation and position when returning.
    /// </summary>
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
            AudioManager.PlayPackageSound();
        }
        else
        {
            DebugLogWarning($"Cannot return {heldItem.name}: missing source inventory.");
        }

        heldItem = null;
    }

    /// <summary>
    /// Logs an informational message when debug logging is enabled.
    /// </summary>
    private void DebugLog(string message)
    {
        if (debugLogs)
            Debug.Log(message, this);
    }

    /// <summary>
    /// Logs a warning when debug logging is enabled.
    /// </summary>
    private void DebugLogWarning(string message)
    {
        if (debugLogs)
            Debug.LogWarning(message, this);
    }

    public bool DebugHover => debugHover;
    public bool IsHoldingItem => heldItem != null || dragging;

    [ContextMenu("Debug Drag Controller")]
    public void DebugControllerState()
    {
        Debug.Log($"Dragging={dragging}, HeldItem={(heldItem == null ? "none" : heldItem.name)}, CurrentPreviewGrid={(currentPreviewGrid == null ? "none" : currentPreviewGrid.Owner.InventoryId)}", this);
    }

    /// <summary>
    /// Forcibly returns any currently held item back to its origin.
    /// This is intended for cleanup paths where the UI is being torn down
    /// and we must ensure no item remains in a transient dragged state.
    /// </summary>
    public void ForceReturnHeldItem()
    {
        // Clear any active placement preview first so it doesn't remain on closed inventories.
        if (currentPreviewGrid != null)
        {
            currentPreviewGrid.ClearPreview();
            currentPreviewGrid = null;
        }

        if (heldItem == null)
            return;

        // End drag state then return the item to its origin.
        dragging = false;
        ReturnItem();
    }
}