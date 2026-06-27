using UnityEngine;

/// <summary>
/// Simple component to attach InventoryData to a GameObject.
/// Use this for mailbox GameObjects so the player controller can reference their data.
/// </summary>
public class InventoryDataHolder : MonoBehaviour
{
    [Tooltip("Inventory data for this object.")]
    public InventoryData inventoryData;

    [Tooltip("Ignore this inventory when running scene inventory validation.")]
    public bool ignoreForValidation;
}
