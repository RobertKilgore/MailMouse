using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Main inventory controller for the player.
/// Manages opening/closing the player inventory and mailbox inventories.
/// </summary>
public class PlayerInventoryController : MonoBehaviour
{
    [Header("Inventory Sets")]
    [SerializeField]
    [Tooltip("The inventory set to open when pressing E (player inventory).")]
    private InventorySetDefinition playerInventorySet;

    [Header("Mailbox UI Set")]
    [SerializeField]
    [Tooltip("The inventory set template to use for mailbox UIs. This is a UI layout (player + mailbox slots).")]
    private InventorySetDefinition mailboxInventorySet;

    [Header("Mailboxes")]
    [SerializeField]
    [Tooltip("Mailbox GameObjects that have an InventoryDataHolder attached. Index 0..9 maps to keys 1..0 respectively.")]
    private InventoryDataHolder[] mailboxes = new InventoryDataHolder[10];

    [Header("Player Data")]
    [SerializeField]
    [Tooltip("Optional InventoryData to populate the player UI slot when opening mailbox sets.")]
    private InventoryData playerInventoryData;

    [Header("Input")]
    [SerializeField]
    private KeyCode toggleInventoryKey = KeyCode.E;

    private InventorySetManager setManager;

    private void Start()
    {
        setManager = InventorySetManager.Instance ?? FindFirstObjectByType<InventorySetManager>();
        if (setManager == null)
            Debug.LogWarning("No InventorySetManager found in scene. Inventory management may not work.", this);
    }

    private void Update()
    {
        HandleToggleInventory();
        HandleMailboxKeys();
    }

    /// <summary>
    /// Toggles the player inventory set (or closes it if already open).
    /// </summary>
    private void HandleToggleInventory()
    {
        if (!Input.GetKeyDown(toggleInventoryKey))
            return;

        if (setManager == null)
            return;

        // If any set is open, close it
        if (setManager.IsSetOpen)
        {
            setManager.CloseInventorySet();
        }
        else if (playerInventorySet != null)
        {
            // Otherwise open the player inventory set
            setManager.OpenInventorySet(playerInventorySet);
        }
        else
        {
            Debug.LogWarning("Player inventory set not assigned.", this);
        }
    }

    /// <summary>
    /// Handles number key input to open mailbox inventories.
    /// Keys 1-9 map to mailboxes 0-8, key 0 maps to mailbox 9.
    /// </summary>
    private void HandleMailboxKeys()
    {
        for (int i = 0; i < 10; i++)
        {
            KeyCode key = i == 0 ? KeyCode.Alpha0 : (KeyCode)(KeyCode.Alpha1 + i - 1);

            if (Input.GetKeyDown(key))
            {
                if (setManager.IsSetOpen)
                {
                    setManager.CloseInventorySet();
                }
                else
                {
                    OpenMailbox(i);
                }

                break;
            }
        }
    }

    /// <summary>
    /// Opens a specific mailbox by index (0-9).
    /// Populates the mailbox UI set in order: first available member gets player data, next gets mailbox data, etc.
    /// </summary>
    private void OpenMailbox(int mailboxIndex)
    {
        if (setManager == null)
            return;

        if (mailboxIndex < 0 || mailboxIndex >= mailboxes.Length || mailboxes[mailboxIndex] == null)
        {
            Debug.LogWarning($"Mailbox {mailboxIndex} not assigned or out of range.", this);
            return;
        }

        if (mailboxInventorySet == null)
        {
            Debug.LogWarning("Mailbox inventory set template not assigned.", this);
            return;
        }

        InventoryDataHolder mailboxHolder = mailboxes[mailboxIndex];
        InventoryData mailboxData = mailboxHolder.inventoryData;

        // Build ordered data list: player data then mailbox data
        List<InventoryData> orderedData = new List<InventoryData>();
        if (playerInventoryData != null)
            orderedData.Add(playerInventoryData);
        if (mailboxData != null)
            orderedData.Add(mailboxData);

        setManager.OpenInventorySet(mailboxInventorySet, orderedData);
        Debug.Log($"Opened mailbox {mailboxIndex}", this);
    }

    /// <summary>
    /// Closes the currently active inventory set.
    /// </summary>
    public void CloseCurrentSet()
    {
        if (setManager != null && setManager.IsSetOpen)
            setManager.CloseInventorySet();
    }

    [ContextMenu("Debug Mailboxes")]
    public void DebugMailboxes()
    {
        for (int i = 0; i < mailboxes.Length; i++)
        {
            if (mailboxes[i] != null)
                Debug.Log($"Mailbox {i}: {mailboxes[i].inventoryData?.inventoryId ?? "no data"}", this);
            else
                Debug.Log($"Mailbox {i}: not assigned", this);
        }
    }
}
