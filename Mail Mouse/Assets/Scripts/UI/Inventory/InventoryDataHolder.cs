using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Component that can attach inventory data to a GameObject.
/// Maintains a single unified list of all inventory data (both primary and multi-slot entries).
/// </summary>
public class InventoryDataHolder : MonoBehaviour
{
    [Tooltip("All inventory data entries for this object. First entry is the primary inventory.")]
    public List<InventoryData> inventoryDataList = new List<InventoryData>();

    [Tooltip("Ignore this inventory when running scene inventory validation.")]
    public bool ignoreForValidation;

    /// <summary>
    /// Backward-compatibility property: returns the first inventory data in the list.
    /// Hidden from inspector but accessible to code.
    /// </summary>
    [HideInInspector]
    public InventoryData inventoryData
    {
        get { return GetPrimaryInventoryData(); }
        set
        {
            if (inventoryDataList == null)
                inventoryDataList = new List<InventoryData>();

            if (inventoryDataList.Count == 0)
                inventoryDataList.Add(value);
            else
                inventoryDataList[0] = value;
        }
    }

    /// <summary>
    /// Returns the first (primary) inventory data entry.
    /// </summary>
    public InventoryData GetPrimaryInventoryData()
    {
        if (inventoryDataList == null || inventoryDataList.Count == 0)
            return null;
        return inventoryDataList[0];
    }

    /// <summary>
    /// Returns all inventory data entries in the list.
    /// </summary>
    public List<InventoryData> GetAllInventoryData()
    {
        if (inventoryDataList == null)
            return new List<InventoryData>();
        return inventoryDataList;
    }

    /// <summary>
    /// Returns every package item currently stored across all inventory entries.
    /// </summary>
    public List<InventoryItemData> GetAllPackages()
    {
        List<InventoryItemData> packages = new List<InventoryItemData>();
        if (inventoryDataList == null)
            return packages;

        foreach (InventoryData inventory in inventoryDataList)
        {
            if (inventory == null || inventory.items == null)
                continue;

            foreach (InventoryItemData item in inventory.items)
            {
                if (item != null)
                    packages.Add(item);
            }
        }

        return packages;
    }

    /// <summary>
    /// Returns every unique package delivery address with the number of packages at that address,
    /// sorted by package count descending and then alphabetically by address.
    /// </summary>
    public List<KeyValuePair<string, int>> GetAllPackageDeliveryAddressCounts()
    {
        Dictionary<string, int> addressCounts = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);

        foreach (InventoryItemData package in GetAllPackages())
        {
            if (package == null || package.mailData == null)
                continue;

            string address = package.mailData.address;
            if (string.IsNullOrWhiteSpace(address))
                continue;

            string trimmedAddress = address.Trim();
            if (!addressCounts.TryGetValue(trimmedAddress, out int count))
                addressCounts[trimmedAddress] = 0;

            addressCounts[trimmedAddress] = count + 1;
        }

        List<KeyValuePair<string, int>> results = new List<KeyValuePair<string, int>>(addressCounts);
        results.Sort((left, right) =>
        {
            int countComparison = right.Value.CompareTo(left.Value);
            if (countComparison != 0)
                return countComparison;

            return string.Compare(left.Key, right.Key, System.StringComparison.OrdinalIgnoreCase);
        });

        return results;
    }

    /// <summary>
    /// Returns the unique delivery addresses only, ordered by package count descending and then alphabetically.
    /// </summary>
    public List<string> GetAllPackageDeliveryAddresses()
    {
        List<string> addresses = new List<string>();
        foreach (KeyValuePair<string, int> addressCount in GetAllPackageDeliveryAddressCounts())
        {
            addresses.Add(addressCount.Key);
        }

        return addresses;
    }

    /// <summary>
    /// Spawns a random item into the primary inventory data entry.
    /// </summary>
    public bool SpawnRandomItemIntoFirstInventory()
    {
        InventoryData primaryInventory = GetPrimaryInventoryData();
        if (primaryInventory == null)
            return false;

        InventorySpawner spawner = FindFirstObjectByType<InventorySpawner>(FindObjectsInactive.Include);
        if (spawner == null)
            return false;

        return spawner.SpawnRandomMailIntoInventoryData(primaryInventory);
    }

#if UNITY_EDITOR
    /// <summary>
    /// [EDITOR DEBUG ONLY] Spawns a random item into a random inventory data entry.
    /// This is a context menu function for debugging purposes.
    /// </summary>
    [ContextMenu("Spawn Random Item Into Random Inventory")]
    private void DebugSpawnRandomItemIntoRandomInventory()
    {
        if (inventoryDataList == null || inventoryDataList.Count == 0)
        {
            Debug.LogWarning("InventoryDataHolder: No inventory data to spawn item into", this);
            return;
        }

        InventorySpawner spawner = FindFirstObjectByType<InventorySpawner>(FindObjectsInactive.Include);
        if (spawner == null)
        {
            Debug.LogWarning("InventoryDataHolder: No InventorySpawner found in scene (searched active and inactive)", this);
            return;
        }

        // Pick a random inventory data
        InventoryData randomInventory = inventoryDataList[Random.Range(0, inventoryDataList.Count)];
        if (randomInventory == null)
        {
            Debug.LogWarning("InventoryDataHolder: Selected random inventory is null", this);
            return;
        }

        bool success = spawner.SpawnRandomMailIntoInventoryData(randomInventory);
        if (success)
        {
            Debug.Log($"InventoryDataHolder: Successfully spawned random item into '{randomInventory.inventoryId}'", this);
        }
        else
        {
            Debug.LogWarning($"InventoryDataHolder: Failed to spawn random item into '{randomInventory.inventoryId}'", this);
        }
    }
#endif
}
