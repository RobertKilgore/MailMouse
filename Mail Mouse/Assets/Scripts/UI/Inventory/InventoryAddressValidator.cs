using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Validates that the scene's inventory addresses and the assigned address book stay in sync.
/// This runs independently from the spawner so it can execute even when the spawner is disabled.
/// </summary>
public class InventoryAddressValidator : MonoBehaviour
{
    [Header("Address Book")]
    [SerializeField]
    [Tooltip("The address book to validate scene inventory addresses against.")]
    private MailAddressBook addressBook;

    [Header("Validation")]
    [SerializeField]
    [Tooltip("Runs the validation pass when the scene starts.")]
    private bool validateOnStart = true;

    private void Start()
    {
        if (validateOnStart)
            StartCoroutine(ValidateAfterSceneInitialization());
    }

    private IEnumerator ValidateAfterSceneInitialization()
    {
        yield return null;
        ValidateInventoryAddresses();
    }

    public void ValidateInventoryAddresses()
    {
        if (addressBook == null)
        {
            Debug.LogWarning("[AddressValidator] No MailAddressBook assigned; address validation skipped.", this);
            return;
        }

        List<InventoryDataHolder> holders = new List<InventoryDataHolder>();
        InventoryDataHolder[] sceneHolders = FindObjectsByType<InventoryDataHolder>(FindObjectsSortMode.None);
        if (sceneHolders != null)
        {
            holders.AddRange(sceneHolders);
        }

        InventoryDataHolder[] allHolders = Resources.FindObjectsOfTypeAll<InventoryDataHolder>();
        if (allHolders != null)
        {
            foreach (InventoryDataHolder holder in allHolders)
            {
                if (holder == null || holders.Contains(holder))
                    continue;

                if (holder.gameObject.scene.IsValid())
                    holders.Add(holder);
            }
        }

        if (holders.Count == 0)
            return;

        HashSet<string> sceneAddresses = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        HashSet<string> bookAddresses = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        int checkedItems = 0;

        if (addressBook.entries != null)
        {
            foreach (MailAddressEntry entry in addressBook.entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.address))
                    continue;

                bookAddresses.Add(entry.address.Trim());
            }
        }

        foreach (InventoryDataHolder holder in holders)
        {
            if (holder == null)
                continue;

            List<InventoryData> inventories = holder.GetAllInventoryData();
            if (inventories == null)
                continue;

            foreach (InventoryData inventoryData in inventories)
            {
                if (inventoryData == null)
                    continue;

                if (!string.IsNullOrWhiteSpace(inventoryData.address))
                {
                    sceneAddresses.Add(inventoryData.address.Trim());
                }

                if (inventoryData.items == null)
                    continue;

                foreach (InventoryItemData itemData in inventoryData.items)
                {
                    if (itemData == null || itemData.mailData == null)
                        continue;

                    checkedItems++;
                    if (string.IsNullOrWhiteSpace(itemData.mailData.address))
                        continue;

                    sceneAddresses.Add(itemData.mailData.address.Trim());
                }
            }
        }

        List<string> sceneAddressesMissingFromBook = new List<string>();
        foreach (string address in sceneAddresses)
        {
            if (!bookAddresses.Contains(address))
                sceneAddressesMissingFromBook.Add(address);
        }

        List<string> bookAddressesMissingFromScene = new List<string>();
        foreach (string address in bookAddresses)
        {
            if (!sceneAddresses.Contains(address))
                bookAddressesMissingFromScene.Add(address);
        }

        if (sceneAddressesMissingFromBook.Count > 0 || bookAddressesMissingFromScene.Count > 0)
        {
            string sceneMissingText = BuildJoinedList(sceneAddressesMissingFromBook);
            string bookMissingText = BuildJoinedList(bookAddressesMissingFromScene);

            if (sceneAddressesMissingFromBook.Count > 0)
            {
                Debug.LogWarning($"[AddressValidator] Scene addresses missing from the address book: {sceneAddressesMissingFromBook.Count} ({sceneMissingText})", this);
            }

            if (bookAddressesMissingFromScene.Count > 0)
            {
                Debug.LogWarning($"[AddressValidator] Address book entries missing from the scene: {bookAddressesMissingFromScene.Count} ({bookMissingText})", this);
            }
        }
        else if (checkedItems > 0)
        {
            Debug.Log($"[AddressValidator] Address validation passed: {checkedItems} inventory item address(es) matched the address book and vice versa.", this);
        }
    }

    private string BuildJoinedList(List<string> values)
    {
        if (values == null || values.Count == 0)
            return "none";

        string result = string.Empty;
        for (int i = 0; i < values.Count; i++)
        {
            if (i > 0)
                result += ", ";
            result += values[i];
        }

        return result;
    }
}
