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
            ValidateInventoryAddresses();
    }

    public void ValidateInventoryAddresses()
    {
        if (addressBook == null)
        {
            Debug.LogWarning("[AddressValidator] No MailAddressBook assigned; address validation skipped.", this);
            return;
        }

        InventoryDataHolder[] holders = FindObjectsByType<InventoryDataHolder>(FindObjectsSortMode.None);
        if (holders == null || holders.Length == 0)
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
                if (inventoryData == null || inventoryData.items == null)
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
            string message = "[AddressValidator] Address validation warning:";
            if (sceneAddressesMissingFromBook.Count > 0)
                message += $" {sceneAddressesMissingFromBook.Count} scene address(es) are missing from the address book ({sceneMissingText}).";
            if (bookAddressesMissingFromScene.Count > 0)
                message += $" {bookAddressesMissingFromScene.Count} address book address(es) are not present in the scene ({bookMissingText}).";

            Debug.LogWarning(message, this);
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
