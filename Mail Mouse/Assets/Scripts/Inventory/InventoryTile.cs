using UnityEngine;

public class InventoryTile : MonoBehaviour
{
    [HideInInspector] public Vector2Int gridPosition;
    [HideInInspector] public RectTransform rect;
    [HideInInspector] public InventoryGrid grid;

    public InventoryGrid Grid => grid;
}