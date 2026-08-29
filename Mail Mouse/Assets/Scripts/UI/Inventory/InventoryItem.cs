using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField]
    private MailData mailData;

    [SerializeField]
    private string prefabId;

    [TextArea(3, 10)]
    [SerializeField]
    private string shapeDefinition = "X"; // ASCII-art shape definition using X for filled tiles.

    [Header("Tile Visual")]
    [SerializeField]
    private Color tileColor = new Color32(88, 88, 88, 179);

    private bool[,] shape;
    private bool[,] baseShape;
    private Vector2Int anchor;
    private Vector2Int gridPosition;
    private int rotation;
    private RectTransform rectTransform;
    private InventoryDragController dragController;
    private InventoryInstance ownerInventory;
    private RectTransform backgroundRoot;
    private bool backgroundVisible = true;
    private bool ignorePointerEnterUntilMove;
    private Vector2 enablePointerPosition;

    public MailData MailData => mailData;
    public string PrefabId => prefabId;
    public Vector2Int GridPosition => gridPosition;
    public int Rotation => rotation;
    public RectTransform RectTransform => rectTransform;
    public bool[,] Shape => shape;
    public Vector2Int Anchor => anchor;
    public int Width => shape != null ? shape.GetLength(0) : ComputeShapeWidth();
    public int Height => shape != null ? shape.GetLength(1) : ComputeShapeHeight();
    public InventoryInstance OwnerInventory => ownerInventory;

    /// <summary>
    /// Safely computes shape width from shapeDefinition without requiring Awake to have run.
    /// Used when the shape field hasn't been initialized yet (e.g., prefab asset references).
    /// </summary>
    public int ComputeShapeWidth()
    {
        if (string.IsNullOrEmpty(shapeDefinition))
            return 0;
        string[] rows = shapeDefinition.Replace("\r", "").Split('\n');
        return rows.Length > 0 ? rows[0].Length : 0;
    }

    /// <summary>
    /// Safely computes shape height from shapeDefinition without requiring Awake to have run.
    /// Used when the shape field hasn't been initialized yet (e.g., prefab asset references).
    /// </summary>
    public int ComputeShapeHeight()
    {
        if (string.IsNullOrEmpty(shapeDefinition))
            return 0;
        string[] rows = shapeDefinition.Replace("\r", "").Split('\n');
        return rows.Length;
    }

    /// <summary>
    /// Initializes runtime references, parses item shape data, and builds the visual representation.
    /// </summary>
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        dragController = InventoryDragController.Instance ?? FindFirstObjectByType<InventoryDragController>();
        ownerInventory = GetComponentInParent<InventoryInstance>();

        if (dragController == null)
            Debug.LogWarning($"No InventoryDragController found in scene for {name}.", this);

        // Still build visuals even if owner inventory isn't found yet (might be parented later)
        RefreshVisuals();
    }

    /// <summary>
    /// Editor-time validation to ensure the RectTransform and owner reference remain current.
    /// </summary>
    private void OnValidate()
    {
        rectTransform = GetComponent<RectTransform>();
        ownerInventory ??= GetComponentInParent<InventoryInstance>();
    }

    /// <summary>
    /// Called when the transform parent changes; rebuilds visuals if now parented under an InventoryInstance.
    /// </summary>
    private void OnTransformParentChanged()
    {
        // Try to find owner inventory in case it was reparented after Awake
        InventoryInstance newOwner = GetComponentInParent<InventoryInstance>();
        if (newOwner != null && newOwner != ownerInventory)
        {
            ownerInventory = newOwner;
            if (ownerInventory != null && rectTransform != null)
            {
                RefreshVisuals();
            }
        }
    }

    /// <summary>
    /// Ensures the item rect transform is centered for consistent placement calculations.
    /// </summary>
    private void AlignRoot()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (rectTransform == null)
        {
            Debug.LogWarning($"{name}: missing RectTransform; cannot AlignRoot().", this);
            return;
        }

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
    }

    /// <summary>
    /// Resizes the item UI to fit the defined tile shape and spacing.
    /// </summary>
    private void UpdateRectSize()
    {
        if (ownerInventory == null || ownerInventory.Grid == null || rectTransform == null)
            return;

        Vector2 cell = ownerInventory.Grid.CellSize;
        Vector2 spacing = ownerInventory.Grid.Spacing;

        float totalWidth = Width * cell.x + (Width - 1) * spacing.x;
        float totalHeight = Height * cell.y + (Height - 1) * spacing.y;

        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, totalWidth);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalHeight);
    }

    /// <summary>
    /// Parses the ASCII-art shape definition into a boolean occupancy map.
    /// 'X' characters are treated as filled tiles.
    /// </summary>
    private void BuildShapeFromDefinition()
    {
        string[] rows = shapeDefinition.Replace("\r", "").Split('\n');
        int height = rows.Length;
        int width = rows[0].Length;

        baseShape = new bool[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                baseShape[x, y] = rows[y][x] == 'X';
            }
        }

        BuildRotatedShape();
    }

    /// <summary>
    /// Builds the current rotated shape from the raw base definition.
    /// </summary>
    private void BuildRotatedShape()
    {
        if (baseShape == null)
            return;

        int normalizedRotation = ((rotation % 360) + 360) % 360;
        int steps = normalizedRotation / 90;

        shape = baseShape;
        for (int i = 0; i < steps; i++)
            shape = RotateShape(shape);
    }

    /// <summary>
    /// Applies a raw rotation value to the current shape state without updating visuals.
    /// </summary>
    private void SetRotationState(int targetRotation)
    {
        rotation = ((targetRotation % 360) + 360) % 360;
        BuildRotatedShape();
    }

    /// <summary>
    /// Rebuilds the item visuals after the shape definition changes.
    /// </summary>
    private void RebuildVisuals()
    {
        RefreshVisuals();
    }

    /// <summary>
    /// Editor-only context menu method to rebuild visuals from the inspector.
    /// </summary>
    [ContextMenu("Rebuild Visuals")]
    public void RebuildVisualsFromEditor()
    {
        RefreshVisuals();
    }

    /// <summary>
    /// Refreshes shape, layout, and background visuals for the item.
    /// </summary>
    private void RefreshVisuals()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (ownerInventory == null)
            ownerInventory = GetComponentInParent<InventoryInstance>();

        AlignRoot();
        BuildShapeFromDefinition();
        CalculateAnchor();
        UpdateRectSize();
        BuildBackgroundVisual();
        ApplyVisualRotation();
    }

    /// <summary>
    /// Builds the item background grid that visually represents its shape.
    /// Individual tiles are created for each filled cell in the shape map.
    /// </summary>
    private void BuildBackgroundVisual()
    {
        // Can't build visuals without the grid configuration from owner inventory
        if (ownerInventory == null)
            return;

        if (backgroundRoot != null)
            Destroy(backgroundRoot.gameObject);

        backgroundRoot = new GameObject("BackgroundRoot", typeof(RectTransform)).GetComponent<RectTransform>();
        backgroundRoot.SetParent(rectTransform, false);
        backgroundRoot.anchorMin = Vector2.zero;
        backgroundRoot.anchorMax = Vector2.one;
        backgroundRoot.offsetMin = Vector2.zero;
        backgroundRoot.offsetMax = Vector2.zero;
        backgroundRoot.pivot = rectTransform.pivot;
        backgroundRoot.localRotation = Quaternion.identity;
        backgroundRoot.localPosition = Vector3.zero;
        backgroundRoot.localScale = Vector3.one;
        backgroundRoot.SetAsFirstSibling();
        backgroundRoot.gameObject.SetActive(backgroundVisible);

        bool[,] visualShape = GetVisualBackgroundShape();
        int visualWidth = visualShape.GetLength(0);
        int visualHeight = visualShape.GetLength(1);

        Vector2 cell = ownerInventory.Grid.CellSize;
        Vector2 spacing = ownerInventory.Grid.Spacing;

        float totalWidth = visualWidth * cell.x + (visualWidth - 1) * spacing.x;
        float totalHeight = visualHeight * cell.y + (visualHeight - 1) * spacing.y;
        float startX = -totalWidth * 0.5f;
        float startY = totalHeight * 0.5f;

        for (int y = 0; y < visualHeight; y++)
        {
            for (int x = 0; x < visualWidth; x++)
            {
                if (!visualShape[x, y])
                    continue;

                CreateTile(x, y, startX, startY, cell, spacing);
            }
        }

        BuildSpacingFill(startX, startY, cell, spacing, visualWidth, visualHeight, visualShape);
        ApplyVisualRotation();
    }

    /// <summary>
    /// Ensures the item transform is rotated to match the logical item orientation.
    /// </summary>
    private void ApplyVisualRotation()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (rectTransform != null)
            rectTransform.localRotation = Quaternion.Euler(0, 0, -rotation);

        if (backgroundRoot != null)
            backgroundRoot.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// Creates a single filled tile visual for the item's background.
    /// </summary>
    private void CreateTile(int x, int y, float startX, float startY, Vector2 cell, Vector2 spacing)
    {
        GameObject tile = new GameObject($"Tile_{x}_{y}", typeof(RectTransform), typeof(Image));
        RectTransform rt = tile.GetComponent<RectTransform>();
        rt.SetParent(backgroundRoot, false);
        rt.sizeDelta = cell;
        rt.anchoredPosition = new Vector2(
            startX + x * (cell.x + spacing.x) + cell.x * 0.5f,
            startY - y * (cell.y + spacing.y) - cell.y * 0.5f
        );
        tile.GetComponent<Image>().color = tileColor;
    }

    /// <summary>
    /// Adds spacing tiles between adjacent shape cells to visually connect them.
    /// </summary>
    private void BuildSpacingFill(float startX, float startY, Vector2 cell, Vector2 spacing, int w, int h, bool[,] shapeMap)
    {
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w - 1; x++)
            {
                if (!shapeMap[x, y] || !shapeMap[x + 1, y])
                    continue;

                CreateSpacingTile(
                    startX + x * (cell.x + spacing.x) + cell.x + spacing.x * 0.5f,
                    startY - y * (cell.y + spacing.y) - cell.y * 0.5f,
                    new Vector2(spacing.x, cell.y)
                );
            }
        }

        for (int y = 0; y < h - 1; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (!shapeMap[x, y] || !shapeMap[x, y + 1])
                    continue;

                CreateSpacingTile(
                    startX + x * (cell.x + spacing.x) + cell.x * 0.5f,
                    startY - y * (cell.y + spacing.y) - cell.y - spacing.y * 0.5f,
                    new Vector2(cell.x, spacing.y)
                );
            }
        }
    }

    /// <summary>
    /// Creates an invisible spacing tile used to bridge adjacent filled cells.
    /// </summary>
    private void CreateSpacingTile(float x, float y, Vector2 size)
    {
        GameObject tile = new GameObject("Spacing", typeof(RectTransform), typeof(Image));
        RectTransform rt = tile.GetComponent<RectTransform>();
        rt.SetParent(backgroundRoot, false);
        rt.sizeDelta = size;
        rt.anchoredPosition = new Vector2(x, y);
        tile.GetComponent<Image>().color = tileColor;
    }

    /// <summary>
    /// Stores the item's current grid coordinate.
    /// </summary>
    public void SetGridPosition(Vector2Int newPos)
    {
        gridPosition = newPos;
    }

    /// <summary>
    /// Updates the current owning inventory instance reference.
    /// </summary>
    public void SetOwnerInventory(InventoryInstance inventory)
    {
        ownerInventory = inventory;
    }

    /// <summary>
    /// Provides runtime access to the current shape definition.
    /// </summary>
    public string ShapeDefinition
    {
        get => shapeDefinition;
        set
        {
            shapeDefinition = value;
            RebuildVisuals();
        }
    }

    /// <summary>
    /// Exposes the prefab's default mail data so spawners can copy it.
    /// </summary>
    public MailData DefaultMailData => mailData;

    /// <summary>
    /// Serializes the current item into inventory-friendly item data.
    /// </summary>
    public InventoryItemData ToItemData()
    {
        return new InventoryItemData
        {
            itemId = name,
            prefabId = prefabId,
            shapeDefinition = shapeDefinition,
            rotation = rotation,
            gridPosition = gridPosition,
            mailData = mailData
        };
    }

    /// <summary>
    /// Reinitializes this item using saved inventory item data.
    /// </summary>
    public void InitializeFromData(InventoryItemData itemData, InventoryInstance owner)
    {
        ownerInventory = owner;
        mailData = itemData.mailData;
        shapeDefinition = itemData.shapeDefinition;
        gridPosition = itemData.gridPosition;
        rotation = itemData.rotation;

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        RefreshVisuals();
    }

    /// <summary>
    /// Toggles the background visual for this item.
    /// </summary>
    public void SetBackgroundVisible(bool visible)
    {
        backgroundVisible = visible;

        if (backgroundRoot == null)
            return;

        backgroundRoot.gameObject.SetActive(visible);
    }

    /// <summary>
    /// Required interface implementation for pointer down.
    /// Left empty because only drag behavior is needed.
    /// </summary>
    public void OnPointerDown(PointerEventData eventData) { }

    private void OnEnable()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        enablePointerPosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        ignorePointerEnterUntilMove = true;
    }

    private void OnDisable()
    {
        InventoryHoverTooltip.HideTooltip();
    }

    /// <summary>
    /// Shows hover information when the pointer enters this inventory item.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        Vector2 currentPointerPosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        if (ignorePointerEnterUntilMove && currentPointerPosition == enablePointerPosition)
        {
            Debug.Log($"InventoryItem.OnPointerEnter ignored after enable: {name}", this);
            return;
        }

        ignorePointerEnterUntilMove = false;
        Debug.Log($"InventoryItem.OnPointerEnter: {name}", this);
        ShowTooltipUnderPointer();
    }

    /// <summary>
    /// Hides the hover tooltip when the pointer leaves this inventory item.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log($"InventoryItem.OnPointerExit: {name}", this);
        InventoryHoverTooltip.HideTooltip();
    }

    private void ShowTooltipUnderPointer()
    {
        string displayName = mailData != null && !string.IsNullOrWhiteSpace(mailData.name) ? mailData.name : name;
        string address = mailData != null ? mailData.address : null;
        string tooltip = string.IsNullOrWhiteSpace(address) ? displayName : $"{displayName}\n{address}";
        InventoryHoverTooltip.ShowTooltip(tooltip);
    }

    /// <summary>
    /// Notifies the drag controller that this item has started being dragged.
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        dragController?.BeginDrag(this);
    }

    /// <summary>
    /// Required interface implementation for drag events.
    /// No action needed because movement is handled globally.
    /// </summary>
    public void OnDrag(PointerEventData eventData) { }

    /// <summary>
    /// Notifies the drag controller that the drag has ended.
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        dragController?.EndDrag();
    }

    /// <summary>
    /// Rotates the item to the target orientation by repeatedly applying clockwise rotation.
    /// </summary>
    public void RotateTo(int targetRotation)
    {
        targetRotation = ((targetRotation % 360) + 360) % 360;

        while (rotation != targetRotation)
            RotateClockwise();
    }

    /// <summary>
    /// Rotates the item shape and anchor clockwise by 90 degrees.
    /// </summary>
    public void RotateClockwise()
    {
        SetRotationState(rotation + 90);
        CalculateAnchor();
        UpdateRectSize();
        BuildBackgroundVisual();
        ApplyVisualRotation();
    }

    /// <summary>
    /// Returns a 90-degree clockwise rotated boolean shape array.
    /// </summary>
    private bool[,] RotateShape(bool[,] original)
    {
        int w = original.GetLength(0);
        int h = original.GetLength(1);
        bool[,] rotated = new bool[h, w];

        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                rotated[h - 1 - y, x] = original[x, y];

        return rotated;
    }

    /// <summary>
    /// Returns the background shape oriented for current UI rotation.
    /// This avoids double-rotating the background when the item transform also rotates.
    /// </summary>
    private bool[,] GetVisualBackgroundShape()
    {
        if (shape == null)
            return shape;

        int normalizedRotation = ((rotation % 360) + 360) % 360;
        int undoSteps = (4 - (normalizedRotation / 90)) % 4;
        bool[,] visual = shape;

        for (int i = 0; i < undoSteps; i++)
            visual = RotateShape(visual);

        return visual;
    }

    /// <summary>
    /// Picks the best anchor tile for the item by choosing the filled cell closest to center.
    /// This determines how the item aligns to grid cells during placement.
    /// </summary>
    private void CalculateAnchor()
    {
        int w = shape.GetLength(0);
        int h = shape.GetLength(1);
        Vector2 center = new Vector2((w - 1) / 2f, (h - 1) / 2f);

        float best = float.MaxValue;
        Vector2Int bestCell = Vector2Int.zero;

        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                if (shape[x, y])
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d < best)
                    {
                        best = d;
                        bestCell = new Vector2Int(x, y);
                    }
                }

        anchor = bestCell;
    }

    /// <summary>
    /// Prints the item's current placement state and owner for debugging.
    /// </summary>
    [ContextMenu("Debug Item State")]
    public void DebugItemState()
    {
        Debug.Log($"Item '{name}' owner={(ownerInventory == null ? "none" : ownerInventory.InventoryId)} pos={gridPosition} rot={rotation}", this);
    }
}