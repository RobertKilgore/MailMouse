using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Represents a single cell in the inventory grid.
/// This component exists on the UI tile objects that make up the grid.
/// </summary>
public class InventoryTile : MonoBehaviour, IPointerEnterHandler
{
    [HideInInspector]
    public Vector2Int gridPosition; // The x/y coordinate of this tile inside the inventory grid.

    [HideInInspector]
    public RectTransform rect; // Cached RectTransform for this tile element.

    [HideInInspector]
    public InventoryGrid grid; // Reference to the owning InventoryGrid.

    /// <summary>
    /// Provides readonly access to the owning grid.
    /// </summary>
    public InventoryGrid Grid => grid;

    /// <summary>
    /// Called when the pointer first enters this tile's UI area.
    /// If hover debugging is enabled for the grid, output the tile position and inventory info.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        grid?.LogHoverTile(this);
    }
}