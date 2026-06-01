using UnityEngine;

public class InventoryInstance : MonoBehaviour
{
    [Header("Grid Data")]
    public InventoryGrid grid;

    [Header("UI Layers")]
    public RectTransform itemLayer;
    public RectTransform previewLayer;

    [Header("Optional ID")]
    public string inventoryId;

}