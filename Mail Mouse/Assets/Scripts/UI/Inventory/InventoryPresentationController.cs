using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryPresentationController : MonoBehaviour
{
    [Header("Inventory Sets")]
    [SerializeField] private InventorySetDefinition playerInventorySetDefinition;
    [SerializeField] private InventorySetDefinition mailboxInventorySetDefinition;
    [SerializeField] private InventorySetDefinition postOfficeInventorySetDefinition;

    [Header("Player Inventory")]
    [SerializeField] private InventoryDataHolder playerInventoryDataHolder;

    private InventorySetManager setManager;

    private void Awake()
    {
        ResolveSetManager();
    }

    private void OnEnable()
    {
        ResolveSetManager();
    }

    private void ResolveSetManager()
    {
        if (setManager != null)
            return;

        setManager = InventorySetManager.Instance;
        if (setManager == null)
            setManager = FindFirstObjectByType<InventorySetManager>(FindObjectsInactive.Include);
    }

    public bool TryOpenInventory(InventoryType inventoryType, params InventoryData[] inventoryData)
    {
        if (setManager == null)
        {
            Debug.LogWarning("InventoryPresentationController: InventorySetManager not found.");
            return false;
        }

        InventorySetDefinition setDefinition = ResolveSetDefinition(inventoryType);
        if (setDefinition == null)
        {
            Debug.LogWarning($"InventoryPresentationController: No inventory set configured for {inventoryType}.");
            return false;
        }

        List<InventoryData> orderedData = BuildOrderedData(inventoryType, inventoryData);
        setManager.OpenInventorySet(setDefinition, orderedData);
        return true;
    }

    private InventorySetDefinition ResolveSetDefinition(InventoryType inventoryType)
    {
        return inventoryType switch
        {
            InventoryType.Player => playerInventorySetDefinition,
            InventoryType.Mailbox => mailboxInventorySetDefinition,
            InventoryType.PostOffice => postOfficeInventorySetDefinition,
            _ => null
        };
    }

    private List<InventoryData> BuildOrderedData(InventoryType inventoryType, IEnumerable<InventoryData> inventoryData)
    {
        List<InventoryData> orderedData = new List<InventoryData>();

        if (ShouldIncludePlayerInventory(inventoryType))
        {
            AddPlayerInventoryData(orderedData);
        }

        if (inventoryData != null)
        {
            foreach (InventoryData data in inventoryData)
            {
                AddInventoryDataIfNeeded(orderedData, data);
            }
        }

        return orderedData;
    }

    private bool ShouldIncludePlayerInventory(InventoryType inventoryType)
    {
        return inventoryType == InventoryType.Player || inventoryType == InventoryType.Mailbox || inventoryType == InventoryType.PostOffice;
    }

    private InventoryData GetPlayerInventoryData()
    {
        if (playerInventoryDataHolder == null)
            return null;
        return playerInventoryDataHolder.GetPrimaryInventoryData();
    }

    private void AddPlayerInventoryData(List<InventoryData> orderedData)
    {
        InventoryData playerInventoryData = GetPlayerInventoryData();
        if (playerInventoryData == null)
            return;

        if (orderedData.Contains(playerInventoryData))
        {
            orderedData.Remove(playerInventoryData);
        }

        orderedData.Insert(0, playerInventoryData);
    }

    private void AddInventoryDataIfNeeded(List<InventoryData> orderedData, InventoryData inventoryData)
    {
        if (inventoryData == null || orderedData.Contains(inventoryData))
            return;

        orderedData.Add(inventoryData);
    }
}
