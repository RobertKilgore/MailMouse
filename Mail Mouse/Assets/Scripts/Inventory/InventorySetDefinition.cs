using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Defines a set of inventories that should be displayed together with their UI positions.
/// </summary>
[System.Serializable]
public class InventorySetMember
{
    [Tooltip("The inventory prefab to use for this slot. If an instance already exists, it will be reused.")]
    public InventoryInstance inventoryPrefab;

    [Tooltip("The position where this inventory should be anchored on screen.")]
    public Vector2 screenPosition;
}

/// <summary>
/// Defines an inventory set: a collection of inventory slot definitions shown together with their layout.
/// </summary>
[CreateAssetMenu(fileName = "InventorySet_New", menuName = "Inventory/Inventory Set")]
public class InventorySetDefinition : ScriptableObject
{
    [SerializeField]
    [Tooltip("All inventory slots that belong to this set and their screen positions.")]
    private InventorySetMember[] members;

    public InventorySetMember[] Members => members;
}
