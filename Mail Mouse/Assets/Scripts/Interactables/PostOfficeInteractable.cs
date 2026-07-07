using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interactable used for a post office hub that opens a multi-slot inventory set.
/// Each slot persists its own InventoryData through an InventoryDataHolder so the contents remain stable across opens.
/// </summary>
public class PostOfficeInteractable : InteractableObject
{
    [Header("Post Office Setup")]
    [SerializeField] private PlayerMenuInputController playerMenuInputController;
    [SerializeField] private int slotCount = 3;
    [SerializeField] private bool autoPopulateEmptyInventories = true;
    [SerializeField] private InventorySpawner inventorySpawner;

    [Header("Inventory Data")]
    [SerializeField] private InventoryDataHolder inventoryDataHolder;

    private void Awake()
    {
        if (playerMenuInputController == null)
            playerMenuInputController = FindFirstObjectByType<PlayerMenuInputController>(FindObjectsInactive.Include);

        if (inventorySpawner == null)
            inventorySpawner = FindFirstObjectByType<InventorySpawner>(FindObjectsInactive.Include);

        if (inventoryDataHolder == null)
            inventoryDataHolder = GetComponent<InventoryDataHolder>();

        if (inventoryDataHolder == null)
            inventoryDataHolder = GetComponentInChildren<InventoryDataHolder>();
    }

    public void SetSlotCount(int count)
    {
        slotCount = Mathf.Max(1, count);
    }

    public void IncreaseSlotCount(int amount = 1)
    {
        SetSlotCount(slotCount + amount);
    }

    public override void OnFocused()
    {
        if (!CanHighlight)
            return;

        OutlineManager.EnableOutline(gameObject);
    }

    public override void OnUnfocused()
    {
        OutlineManager.DisableOutline(gameObject);
    }

    public override void Interact()
    {
        List<InventoryData> slots = BuildInventorySlots();
        if (slots.Count == 0)
            return;

        if (playerMenuInputController != null)
        {
            playerMenuInputController.RequestInventoryToggle(InventoryType.PostOffice, slots.ToArray());
        }
    }

    private List<InventoryData> BuildInventorySlots()
    {
        List<InventoryData> slots = new List<InventoryData>();

        for (int i = 0; i < Mathf.Max(1, slotCount); i++)
        {
            InventoryData slotData = GetOrCreateSlotData(i);
            if (slotData == null)
                continue;

            if (slotData.items == null)
                slotData.items = new List<InventoryItemData>();

            if (autoPopulateEmptyInventories && slotData.items.Count == 0)
                PopulateSlotWithRandomMail(slotData);

            slotData.inventoryType = InventoryType.PostOffice;
            slots.Add(slotData);
        }

        return slots;
    }

    private InventoryData GetOrCreateSlotData(int index)
    {
        if (inventoryDataHolder == null)
            return null;

        List<InventoryData> allData = inventoryDataHolder.GetAllInventoryData();
        if (index < allData.Count && allData[index] != null)
            return allData[index];

        InventoryData generated = new InventoryData
        {
            inventoryId = $"post_office_slot_{index + 1}",
            displayName = $"Post Office Slot {index + 1}",
            inventoryType = InventoryType.PostOffice,
            items = new List<InventoryItemData>()
        };

        while (inventoryDataHolder.inventoryDataSet.Count <= index)
            inventoryDataHolder.inventoryDataSet.Add(null);

        inventoryDataHolder.inventoryDataSet[index] = generated;
        return generated;
    }

    private void PopulateSlotWithRandomMail(InventoryData slotData)
    {
        if (inventorySpawner == null)
            return;

        InventoryItemData itemData = inventorySpawner.CreateRandomMailItemData(Vector2Int.zero);
        if (itemData == null)
            return;

        slotData.items.Add(itemData);
    }
}
