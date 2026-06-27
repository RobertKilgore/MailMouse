using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InventoryData
{
    public string inventoryId;
    public string displayName;
    public string address;
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
}
