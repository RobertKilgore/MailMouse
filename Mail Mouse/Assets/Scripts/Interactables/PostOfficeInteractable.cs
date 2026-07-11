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

    [Header("Inventory Data")]
    [SerializeField] private InventoryDataHolder inventoryDataHolder;

    private void Awake()
    {
        if (playerMenuInputController == null)
            playerMenuInputController = FindFirstObjectByType<PlayerMenuInputController>(FindObjectsInactive.Include);

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

            slotData.inventoryType = InventoryType.PostOffice;
            slotData.allowItemPlacement = false;
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
        {
            InventoryData existingSlot = allData[index];
            Debug.Log($"PostOfficeInteractable: Retrieved existing slot {index} with {existingSlot.items?.Count ?? 0} items", this);
            return existingSlot;
        }

        InventoryData generated = new InventoryData
        {
            inventoryId = $"post_office_slot_{index + 1}",
            displayName = $"Post Office Slot {index + 1}",
            inventoryType = InventoryType.PostOffice,
            width = 5,
            height = 5,
            items = new List<InventoryItemData>()
        };

        Debug.Log($"PostOfficeInteractable: Created new slot {index}", this);

        while (inventoryDataHolder.inventoryDataList.Count <= index)
            inventoryDataHolder.inventoryDataList.Add(null);

        inventoryDataHolder.inventoryDataList[index] = generated;
        return generated;
    }
}
