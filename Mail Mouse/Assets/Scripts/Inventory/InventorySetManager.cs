using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages inventory sets: opening, closing, saving, loading, and positioning inventories on screen.
/// </summary>
public class InventorySetManager : MonoBehaviour
{
    public static InventorySetManager Instance { get; private set; }

    [Header("Instance Pool")]
    [SerializeField]
    [Tooltip("Maximum number of inventory instances that may be spawned and cached at once.")]
    private int maxSpawnedInventoryInstances = 8;

    [SerializeField]
    [Tooltip("Optional parent transform for spawned inventory instances.")]
    private Transform inventoryInstanceParent;

    private InventorySetDefinition activeSet;
    private InventorySpawner spawner;
    private readonly Dictionary<InventoryInstance, List<InventoryInstance>> pooledInstances = new Dictionary<InventoryInstance, List<InventoryInstance>>();
    private readonly List<InventoryInstance> activeInstances = new List<InventoryInstance>();
    private readonly Dictionary<InventoryInstance, InventoryInstance> instanceToPrefab = new Dictionary<InventoryInstance, InventoryInstance>();
    private int totalSpawnedInstances;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Debug.LogWarning("Multiple InventorySetManager instances found.", gameObject);
            Destroy(gameObject);
            return;
        }

        spawner = FindFirstObjectByType<InventorySpawner>();
        if (spawner == null)
            Debug.LogWarning("No InventorySpawner found in scene.", this);

        if (inventoryInstanceParent == null)
            inventoryInstanceParent = transform;
    }

    /// <summary>
    /// Opens an inventory set: saves the current set, resolves or spawns inventory instances,
    /// positions them, loads provided inventory data, and shows them.
    /// </summary>
    public void OpenInventorySet(InventorySetDefinition setDefinition, List<InventoryData> orderedData = null)
    {
        if (setDefinition == null)
            return;

        // Close the current set
        CloseInventorySet();

        activeSet = setDefinition;
        activeInstances.Clear();

        // Fill and position inventories
        for (int index = 0; index < setDefinition.Members.Length; index++)
        {
            InventorySetMember member = setDefinition.Members[index];
            if (member.inventoryPrefab == null)
                continue;

            InventoryInstance inventoryInstance = ResolveInventoryInstance(member);
            if (inventoryInstance == null)
                continue;

            SetInventoryPosition(inventoryInstance, member.screenPosition);

            InventoryData inventoryData = null;
            if (orderedData != null && index < orderedData.Count)
                inventoryData = orderedData[index];
            // Release any previously held data before clearing visuals to avoid
            // accidentally writing an empty inventory back into the old data object.
            if (inventoryInstance.InventoryData != null)
            {
                inventoryInstance.SetInventoryData(null);
                inventoryInstance.ClearInventory();
            }

            if (inventoryData != null)
            {
                // Assign the target data and populate it via the spawner (which will
                // clear and then instantiate items into the grid).
                inventoryInstance.SetInventoryData(inventoryData);
                if (spawner != null)
                    spawner.LoadInventoryData(inventoryInstance, inventoryData);
            }
            else
            {
                // Ensure the instance is empty when there is no data to bind.
                inventoryInstance.SetInventoryData(null);
                inventoryInstance.ClearInventory();
            }

            inventoryInstance.gameObject.SetActive(true);
            activeInstances.Add(inventoryInstance);
        }

        Debug.Log($"Opened inventory set '{setDefinition.name}'", this);
    }

    /// <summary>
    /// Closes the currently active inventory set: saves, clears, and hides active inventories.
    /// </summary>
    public void CloseInventorySet()
    {
        if (activeSet == null)
            return;

        // Ensure any dragged item is returned to its origin before we save/close.
        InventoryDragController.Instance?.ForceReturnHeldItem();

        // Save all currently active inventories
        SaveInventorySet();

        foreach (InventoryInstance instance in activeInstances)
        {
            if (instance == null)
                continue;

            // Hide UI first
            instance.gameObject.SetActive(false);

            // Release any binding to external InventoryData so clearing the UI
            // does not write empty state back into mailbox/player data objects.
            instance.SetInventoryData(null);
            instance.ClearInventory();

            // Return to the pool for reuse
            ReturnInstanceToPool(instance);
        }

        string closedSetName = activeSet.name;
        activeInstances.Clear();
        activeSet = null;

        Debug.Log($"Closed inventory set '{closedSetName}'", this);
    }

    /// <summary>
    /// Saves all currently active inventories back to their data objects.
    /// </summary>
    private void SaveInventorySet()
    {
        if (spawner == null)
            return;

        foreach (InventoryInstance instance in activeInstances)
        {
            if (instance == null || instance.InventoryData == null)
                continue;

            spawner.SaveInventoryData(instance);
        }
    }

    private InventoryInstance ResolveInventoryInstance(InventorySetMember member)
    {
        return GetPooledInstance(member.inventoryPrefab);
    }

    private InventoryInstance GetPooledInstance(InventoryInstance prefab)
    {
        if (prefab == null)
            return null;

        if (pooledInstances.TryGetValue(prefab, out List<InventoryInstance> pool) && pool.Count > 0)
        {
            InventoryInstance instance = pool[pool.Count - 1];
            pool.RemoveAt(pool.Count - 1);
            return instance;
        }

        if (IsSpawnLimitReached())
        {
            if (!EvictInactiveInstance())
            {
                Debug.LogWarning($"Cannot spawn inventory instance for '{prefab.name}': spawn limit reached and no inactive instance available to evict.", this);
                return null;
            }
        }

        InventoryInstance newInstance = Instantiate(prefab, inventoryInstanceParent);
        newInstance.gameObject.SetActive(false);
        instanceToPrefab[newInstance] = prefab;
        totalSpawnedInstances++;
        return newInstance;
    }

    private bool EvictInactiveInstance()
    {
        foreach (var kvp in pooledInstances)
        {
            List<InventoryInstance> pool = kvp.Value;
            if (pool.Count == 0)
                continue;

            InventoryInstance evicted = pool[pool.Count - 1];
            pool.RemoveAt(pool.Count - 1);
            if (evicted != null)
            {
                instanceToPrefab.Remove(evicted);
                Destroy(evicted.gameObject);
                totalSpawnedInstances--;
                return true;
            }
        }

        return false;
    }

    private void ReturnInstanceToPool(InventoryInstance instance)
    {
        if (instance == null)
            return;

        if (!instanceToPrefab.TryGetValue(instance, out InventoryInstance prefab) || prefab == null)
        {
            Debug.LogWarning($"Unable to return inventory instance '{instance.name}' to pool because its prefab key is unknown.", this);
            return;
        }

        if (!pooledInstances.TryGetValue(prefab, out List<InventoryInstance> list))
        {
            list = new List<InventoryInstance>();
            pooledInstances[prefab] = list;
        }

        list.Add(instance);
    }

    private bool IsSpawnLimitReached()
    {
        return totalSpawnedInstances >= maxSpawnedInventoryInstances;
    }

    /// <summary>
    /// Positions an inventory UI at a specific screen location.
    /// </summary>
    private void SetInventoryPosition(InventoryInstance inventory, Vector2 screenPosition)
    {
        RectTransform rt = inventory.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = screenPosition;
        }
    }

    /// <summary>
    /// Returns whether an inventory set is currently open.
    /// </summary>
    public bool IsSetOpen => activeSet != null;

    /// <summary>
    /// Returns the currently active inventory set.
    /// </summary>
    public InventorySetDefinition ActiveSet => activeSet;
}
