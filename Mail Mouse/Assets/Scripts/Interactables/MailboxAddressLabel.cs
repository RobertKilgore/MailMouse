using TMPro;
using UnityEngine;

/// <summary>
/// Displays the mailbox's assigned address on a TMP label.
/// Uses the mailbox's InventoryDataHolder as the primary source and supports
/// an optional shared MailAddressBook lookup for a formatted display.
/// </summary>
[ExecuteAlways]
public class MailboxAddressLabel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InventoryDataHolder inventoryDataHolder;
    [SerializeField] private TMP_Text addressText;

    [Tooltip("Optional shared address catalog. If assigned, this can be used to resolve or format the mailbox address.")]
    [SerializeField] private MailAddressBook addressBook;


    [Header("Display")]
    [SerializeField] private string prefix = "";
    [SerializeField] private string suffix = "";
    [SerializeField] private bool keepOnlyNumericDigits = false;

    private void Reset()
    {
        TryAutoWire();
        RefreshDisplay();
    }

    private void Awake()
    {
        TryAutoWire();
        RefreshDisplay();
    }


    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            TryAutoWire();
            RefreshDisplay();
        }
    }

    private void TryAutoWire()
    {
        if (inventoryDataHolder == null)
            inventoryDataHolder = GetComponent<InventoryDataHolder>();

        if (addressText == null)
            addressText = GetComponentInChildren<TMP_Text>(true);
    }

    [ContextMenu("Refresh Address Display")]
    public void RefreshDisplay()
    {
        if (addressText == null)
            return;

        addressText.text = GetDisplayAddress();
    }

    public string GetDisplayAddress()
    {
        string resolvedAddress = GetAddressFromInventory();

        if (string.IsNullOrWhiteSpace(resolvedAddress) && addressBook != null)
        {
            resolvedAddress = ResolveAddressFromBook();
        }

        if (string.IsNullOrWhiteSpace(resolvedAddress))
            return string.Empty;

        if (keepOnlyNumericDigits)
        {
            string digitsOnly = string.Empty;
            foreach (char c in resolvedAddress)
            {
                if (char.IsDigit(c))
                    digitsOnly += c;
            }

            if (!string.IsNullOrEmpty(digitsOnly))
                resolvedAddress = digitsOnly;
        }

        return prefix + resolvedAddress + suffix;
    }

    private string GetAddressFromInventory()
    {
        if (inventoryDataHolder == null)
            return string.Empty;

        InventoryData primaryInventory = inventoryDataHolder.GetPrimaryInventoryData();
        if (primaryInventory == null)
            return string.Empty;

        return primaryInventory.address;
    }

    private string ResolveAddressFromBook()
    {
        if (inventoryDataHolder == null)
            return string.Empty;

        InventoryData primaryInventory = inventoryDataHolder.GetPrimaryInventoryData();
        if (primaryInventory == null || string.IsNullOrWhiteSpace(primaryInventory.address))
            return string.Empty;

        foreach (MailAddressEntry entry in addressBook.entries)
        {
            if (entry == null)
                continue;

            if (string.Equals(entry.address, primaryInventory.address, System.StringComparison.OrdinalIgnoreCase))
                return entry.address;
        }

        return primaryInventory.address;
    }
}
