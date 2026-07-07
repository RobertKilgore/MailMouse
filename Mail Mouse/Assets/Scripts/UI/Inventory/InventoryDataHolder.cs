using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Component that can attach inventory data to a GameObject.
/// Supports both a single primary inventory and a collection of inventory slots for cases like post office hubs.
/// </summary>
public class InventoryDataHolder : MonoBehaviour
{
    [Tooltip("Primary inventory data for this object.")]
    public InventoryData inventoryData;

    [Tooltip("Additional inventory data entries for multi-slot interactables such as post offices.")]
    public List<InventoryData> inventoryDataSet = new List<InventoryData>();

    [Tooltip("Ignore this inventory when running scene inventory validation.")]
    public bool ignoreForValidation;

    public InventoryData GetPrimaryInventoryData()
    {
        return inventoryData;
    }

    public List<InventoryData> GetAllInventoryData()
    {
        List<InventoryData> allData = new List<InventoryData>();

        if (inventoryData != null)
            allData.Add(inventoryData);

        if (inventoryDataSet != null)
        {
            foreach (InventoryData data in inventoryDataSet)
            {
                if (data != null && !allData.Contains(data))
                    allData.Add(data);
            }
        }

        return allData;
    }
}
