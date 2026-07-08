using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages restocking of inventory slots.
/// Periodically checks if slots are empty and spawns a random mail item if needed.
/// Can also be manually triggered to restock on demand.
/// </summary>
public class RestockManager : MonoBehaviour
{
    [SerializeField]
    private InventoryDataHolder inventoryDataHolder;

    [SerializeField]
    private InventorySpawner inventorySpawner;

    [SerializeField]
    private float restockCheckInterval = 0.5f;

    private float nextRestockCheckTime;

    private void Start()
    {
        if (inventoryDataHolder == null)
        {
            inventoryDataHolder = GetComponentInChildren<InventoryDataHolder>();
        }

        if (inventorySpawner == null)
        {
            inventorySpawner = FindFirstObjectByType<InventorySpawner>(FindObjectsInactive.Include);
        }

        nextRestockCheckTime = Time.time + restockCheckInterval;
    }

    private void Update()
    {
        if (Time.time < nextRestockCheckTime)
        {
            return;
        }

        RestockEmptySlots();
        nextRestockCheckTime = Time.time + restockCheckInterval;
    }

    /// <summary>
    /// Manually triggers a restock check and immediately restocks empty slots.
    /// </summary>
    public void TriggerRestock()
    {
        Debug.Log("RestockManager: TriggerRestock called", this);
        RestockEmptySlots();
        nextRestockCheckTime = Time.time + restockCheckInterval;
    }

    private void RestockEmptySlots()
    {
        if (inventoryDataHolder == null || inventorySpawner == null)
        {
            Debug.LogWarning("RestockManager: inventoryDataHolder or inventorySpawner is null", this);
            return;
        }

        List<InventoryData> allSlots = inventoryDataHolder.GetAllInventoryData();
        Debug.Log($"RestockManager: Checking {allSlots.Count} slots for restocking", this);

        int restockedCount = 0;
        foreach (InventoryData slotData in allSlots)
        {
            if (slotData == null)
            {
                continue;
            }

            if (slotData.items == null)
            {
                slotData.items = new List<InventoryItemData>();
            }

            // Only restock empty slots
            if (slotData.items.Count == 0)
            {
                if (inventorySpawner.SpawnRandomMailIntoInventoryData(slotData))
                {
                    restockedCount++;
                }
            }
        }

        Debug.Log($"RestockManager: Restocked {restockedCount} empty slots", this);
    }
}
