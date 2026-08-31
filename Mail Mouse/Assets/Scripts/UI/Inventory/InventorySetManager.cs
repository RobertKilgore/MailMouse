using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        if (inventoryInstanceParent == null)
            inventoryInstanceParent = transform;

        spawner = FindFirstObjectByType<InventorySpawner>();
        if (spawner == null)
            Debug.LogWarning("No InventorySpawner found in scene.", this);

    }

    /// <summary>
    /// Opens an inventory set: saves the current set, resolves or spawns inventory instances,
    /// positions them, loads provided inventory data, and shows them.
    /// </summary>
    public void OpenInventorySet(InventorySetDefinition setDefinition, List<InventoryData> orderedData = null)
    {
        Debug.Log($"[InventorySetManager.OpenInventorySet] Opening set: {(setDefinition != null ? setDefinition.name : "NULL")}");
        InventoryHoverTooltip.HideTooltip();

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

            inventoryInstance.transform.SetParent(inventoryInstanceParent, false);
            inventoryInstance.transform.SetAsLastSibling();
            inventoryInstance.gameObject.SetActive(false);

            Debug.Log($"[InventorySetManager] Processing inventory instance: {inventoryInstance.gameObject.name}");
            SetInventoryPosition(inventoryInstance, member.screenPosition);

            InventoryData inventoryData = null;
            if (orderedData != null && index < orderedData.Count)
                inventoryData = orderedData[index];

            // Detach the instance from its old backing data and clear any old visuals
            // before rebinding it to the new inventory data for this open.
            inventoryInstance.RebindInventoryData(null);

            if (inventoryData != null)
            {
                Debug.Log($"[InventorySetManager] Binding UI inventory | inventoryId={inventoryData.inventoryId} | dataHolderObject={inventoryDataHolderName(inventoryData)} | displayName={inventoryData.displayName} | targetInstance={inventoryInstance.name} | set={setDefinition.name} | inventoryType={inventoryData.inventoryType}", this);
                inventoryInstance.RebindInventoryData(inventoryData);
                if (spawner != null)
                    spawner.LoadInventoryData(inventoryInstance, inventoryData);

                RestockManager.Instance?.TriggerRestockForInventory(inventoryData);
            }
            else
            {
                inventoryInstance.RebindInventoryData(null);
            }

            Debug.Log($"[InventorySetManager] Before SetActive: {inventoryInstance.gameObject.name} active={inventoryInstance.gameObject.activeSelf}");
            inventoryInstance.transform.SetAsLastSibling();
            inventoryInstance.gameObject.SetActive(true);
            Debug.Log($"[InventorySetManager] After SetActive: {inventoryInstance.gameObject.name} active={inventoryInstance.gameObject.activeSelf}, activeInHierarchy={inventoryInstance.gameObject.activeInHierarchy}");
            activeInstances.Add(inventoryInstance);
        }

        transform.SetAsLastSibling();
        RefreshDeliveryListPanels();
        Debug.Log($"Opened inventory set '{setDefinition.name}'", this);
    }

    /// <summary>
    /// Closes the currently active inventory set: saves, clears, and hides active inventories.
    /// </summary>
    public void CloseInventorySet()
    {
        InventoryHoverTooltip.HideTooltip();

        if (activeSet == null)
            return;

        if (InventoryDragController.Instance != null && InventoryDragController.Instance.IsHoldingItem)
        {
            InventoryDragController.Instance.ForceReturnHeldItem();
        }

        foreach (InventoryInstance instance in activeInstances)
        {
            if (instance == null)
                continue;

            instance.gameObject.SetActive(false);
            ReturnInstanceToPool(instance);
        }

        string closedSetName = activeSet.name;
        activeInstances.Clear();
        activeSet = null;

        RefreshDeliveryListPanels();
        Debug.Log($"Closed inventory set '{closedSetName}'", this);
    }

    private void RefreshDeliveryListPanels()
    {
        DeliveryListController[] deliveryLists = Resources.FindObjectsOfTypeAll<DeliveryListController>();
        foreach (DeliveryListController deliveryList in deliveryLists)
        {
            if (deliveryList != null)
                deliveryList.RefreshFromPlayerInventory();
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

    private static string inventoryDataHolderName(InventoryData inventoryData)
    {
        if (inventoryData == null)
            return "null";

        InventoryDataHolder[] holders = FindObjectsByType<InventoryDataHolder>(FindObjectsSortMode.None);
        foreach (InventoryDataHolder holder in holders)
        {
            if (holder == null)
                continue;

            List<InventoryData> dataList = holder.GetAllInventoryData();
            if (dataList != null && dataList.Contains(inventoryData))
                return holder.gameObject.name;
        }

        return "Unknown";
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
