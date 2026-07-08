using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class InventoryData
{
    public string inventoryId;
    public string displayName;
    public string address;
    [FormerlySerializedAs("inventoryKind")]
    public InventoryType inventoryType = InventoryType.Mailbox;
    public int width = 8;  // Grid width in cells
    public int height = 6;  // Grid height in cells
    public List<InventoryItemData> items = new List<InventoryItemData>();
}

[Serializable]
public class InventoryItemData
{
    public string itemId;
    public string prefabId;
    public string shapeDefinition = "X";
    public int rotation = 0;
    public Vector2Int gridPosition = Vector2Int.zero;
    public MailData mailData = new MailData();

    private float _cachedSize = -1f;

    /// <summary>
    /// Gets the size of this item based on its shape definition. Size is cached after first calculation.
    /// </summary>
    public float GetSize()
    {
        // Return cached size if available
        if (_cachedSize >= 0f)
            return _cachedSize;

        // Calculate and cache the size
        if (string.IsNullOrWhiteSpace(shapeDefinition))
        {
            _cachedSize = 1f;
            return _cachedSize;
        }

        float count = 0f;
        string[] rows = shapeDefinition.Replace("\r", "").Split('\n');
        foreach (string row in rows)
        {
            foreach (char c in row)
            {
                if (c == 'X')
                    count += 1f;
            }
        }

        _cachedSize = Mathf.Max(1f, count);
        return _cachedSize;
    }

    /// <summary>
    /// Invalidates the cached size. Call this if shape definition changes at runtime.
    /// </summary>
    public void InvalidateSizeCache()
    {
        _cachedSize = -1f;
    }
}
