using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MailAddressBook", menuName = "Mail/Address Book")]
/// <summary>
/// Stores address entries and recipients used to generate random mail metadata.
/// This is a ScriptableObject so the same address catalog can be shared across scene inventories.
/// </summary>
public class MailAddressBook : ScriptableObject
{
    [Tooltip("All addresses and their possible recipients used to generate random mail items.")]
    public List<MailAddressEntry> entries = new List<MailAddressEntry>();

    /// <summary>
    /// Returns a random address entry from the configured address book.
    /// </summary>
    public MailAddressEntry GetRandomEntry()
    {
        if (entries == null || entries.Count == 0)
            return null;

        return entries[Random.Range(0, entries.Count)];
    }

    /// <summary>
    /// Returns a random recipient associated with the specified address.
    /// </summary>
    public string GetRandomRecipientForAddress(string address)
    {
        MailAddressEntry entry = FindEntry(address);
        if (entry == null || entry.recipients == null || entry.recipients.Count == 0)
            return null;

        return entry.recipients[Random.Range(0, entry.recipients.Count)];
    }

    /// <summary>
    /// Returns random mail metadata with a valid address and recipient pair.
    /// </summary>
    public MailData GetRandomMailData()
    {
        MailAddressEntry entry = GetRandomEntry();
        if (entry == null)
            return null;

        // It's valid for an address to have no recipients; return the address and leave recipient null.
        if (entry.recipients == null || entry.recipients.Count == 0)
        {
            return new MailData
            {
                address = entry.address,
                recipient = null
            };
        }

        return new MailData
        {
            address = entry.address,
            recipient = entry.recipients[Random.Range(0, entry.recipients.Count)]
        };
    }

    /// <summary>
    /// Finds the entry that matches the specified address exactly.
    /// </summary>
    public MailAddressEntry FindEntry(string address)
    {
        if (string.IsNullOrWhiteSpace(address) || entries == null)
            return null;

        return entries.Find(entry => entry.address == address);
    }

    /// <summary>
    /// Returns the address that contains the specified recipient, or null if not found.
    /// </summary>
    public string GetAddressForRecipient(string recipient)
    {
        if (string.IsNullOrWhiteSpace(recipient) || entries == null)
            return null;

        foreach (MailAddressEntry entry in entries)
        {
            if (entry.recipients != null && entry.recipients.Contains(recipient))
                return entry.address;
        }

        return null;
    }
}

[System.Serializable]
public class MailAddressEntry
{
    [Tooltip("The address string used by mail items.")]
    public string address;

    [Tooltip("A list of valid recipients for this address.")]
    public List<string> recipients = new List<string>();
}
