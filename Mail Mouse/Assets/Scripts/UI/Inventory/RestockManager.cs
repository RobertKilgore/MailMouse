using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages restocking of inventory slots.
/// Periodically checks if slots are empty and spawns a random mail item if needed.
/// Can also be manually triggered to restock on demand.
/// </summary>
public class RestockManager : MonoBehaviour
{
    public static RestockManager Instance { get; private set; }

    [SerializeField]
    private InventoryDataHolder inventoryDataHolder;

    [SerializeField]
    private InventorySpawner inventorySpawner;

    private readonly List<InventoryDataHolder> managedDataHolders = new List<InventoryDataHolder>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple RestockManager instances found; keeping the first one.", this);
        }
    }

    private void Start()
    {
        RefreshManagedDataHolders();

        if (inventorySpawner == null)
        {
            inventorySpawner = FindFirstObjectByType<InventorySpawner>(FindObjectsInactive.Include);
        }
    }

    /// <summary>
    /// Manually triggers a restock check and immediately restocks empty slots.
    /// </summary>
    public void TriggerRestock()
    {
        Debug.Log("RestockManager: TriggerRestock called", this);
        RestockEmptySlots();
    }

    public void TriggerRestockForInventory(InventoryData inventoryData)
    {
        if (inventoryData == null)
        {
            TriggerRestock();
            return;
        }

        if (!CanRestockNow())
            return;

        if (!IsManagedInventory(inventoryData))
            return;

        if (!CanRestockInventory(inventoryData))
            return;

        if (inventoryData.items == null)
            inventoryData.items = new List<InventoryItemData>();

        if (inventoryData.items.Count != 0)
            return;

        if (inventorySpawner != null && inventorySpawner.SpawnRandomMailIntoInventoryData(inventoryData))
        {
            Debug.Log($"RestockManager: Restocked inventory '{inventoryData.inventoryId}'", this);
        }
    }

    public void NotifyInventoryChanged(InventoryData inventoryData)
    {
        if (inventoryData == null)
            return;

        if (!CanRestockNow())
            return;

        TriggerRestockForInventory(inventoryData);
    }

    private bool CanRestockNow()
    {
        return InventoryDragController.Instance == null || !InventoryDragController.Instance.IsHoldingItem;
    }

    private bool CanRestockInventory(InventoryData inventoryData)
    {
        if (inventoryData == null)
            return false;

        if (!inventoryData.allowItemSpawns)
            return false;

        return true;
    }

    private void RefreshManagedDataHolders()
    {
        managedDataHolders.Clear();

        if (inventoryDataHolder != null && IsManagedHolder(inventoryDataHolder))
        {
            managedDataHolders.Add(inventoryDataHolder);
        }

        InventoryDataHolder[] holders = GetComponentsInChildren<InventoryDataHolder>(true);
        foreach (InventoryDataHolder holder in holders)
        {
            if (holder == null || holder == inventoryDataHolder)
                continue;

            if (IsManagedHolder(holder))
                managedDataHolders.Add(holder);
        }

        if (inventoryDataHolder == null && managedDataHolders.Count > 0)
        {
            inventoryDataHolder = managedDataHolders[0];
        }
    }

    private bool IsManagedHolder(InventoryDataHolder holder)
    {
        if (holder == null)
            return false;

        Transform holderTransform = holder.transform;
        return holderTransform == transform || holderTransform.IsChildOf(transform);
    }

    private bool IsManagedInventory(InventoryData inventoryData)
    {
        if (inventoryData == null)
            return false;

        foreach (InventoryDataHolder holder in managedDataHolders)
        {
            if (holder == null)
                continue;

            List<InventoryData> holderData = holder.GetAllInventoryData();
            if (holderData != null && holderData.Contains(inventoryData))
                return true;
        }

        return false;
    }

    private void RestockEmptySlots()
    {
        if (!CanRestockNow())
            return;

        if (inventoryDataHolder == null || inventorySpawner == null)
        {
            Debug.LogWarning("RestockManager: inventoryDataHolder or inventorySpawner is null", this);
            return;
        }

        List<InventoryData> allSlots = new List<InventoryData>();
        foreach (InventoryDataHolder holder in managedDataHolders)
        {
            if (holder == null)
                continue;

            List<InventoryData> holderData = holder.GetAllInventoryData();
            if (holderData == null)
                continue;

            allSlots.AddRange(holderData);
        }

        Debug.Log($"RestockManager: Checking {allSlots.Count} slots for restocking", this);

        int restockedCount = 0;
        foreach (InventoryData slotData in allSlots)
        {
            if (slotData == null)
            {
                continue;
            }

            if (!CanRestockInventory(slotData))
                continue;

            if (slotData.items == null)
            {
                slotData.items = new List<InventoryItemData>();
            }

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
