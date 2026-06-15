using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Creates inventory item prefabs from data and binds inventory models to UI views.
/// </summary>
public class InventorySpawner : MonoBehaviour
{
    [Header("Prefab Catalog")]
    [SerializeField]
    [Tooltip("Inventory item prefabs used to instantiate items in any inventory instance.")]
    private InventoryItem[] itemPrefabs;

    [Header("Address Book")]
    [SerializeField]
    [Tooltip("Optional address book that supplies random mail metadata for generated items.")]
    private MailAddressBook addressBook;

    [Header("Search Strategy")]
    [SerializeField]
    [Tooltip("If enabled, position search will use a randomized cell order instead of starting at the top-left cell.")]
    private bool randomizePositionOrder = false;

    [SerializeField]
    [Tooltip("If enabled, rotation testing order will be randomized instead of always starting at 0 degrees.")]
    private bool randomizeRotationOrder = false;

    /// <summary>
    /// Returns true when the prefab catalog contains at least one non-null prefab.
    /// </summary>
    public bool HasPrefabs
    {
        get
        {
            if (itemPrefabs == null)
                return false;

            foreach (InventoryItem prefab in itemPrefabs)
            {
                if (prefab != null)
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Instantiates an inventory item prefab from saved item data and places it into the target inventory.
    /// </summary>
    public InventoryItem SpawnItemInInventory(InventoryInstance inventory, InventoryItemData itemData, int debugLevel = 0)
    {
        if (inventory == null || itemData == null)
        {
            Debug.Log($"[Spawner] Invalid parameters for SpawnItemInInventory.", this);
            return null;
        }

        InventoryItem prefab = GetPrefabById(itemData.prefabId);
        if (prefab == null)
        {
            Debug.LogWarning($"[Spawner] No prefab found with ID '{itemData.prefabId}' for item '{itemData.itemId}'.", this);
            return null;
        }

        if (debugLevel > 0)
            Debug.Log($"[Spawner] Instantiating prefab '{prefab.name}' for inventory '{inventory.InventoryId}' at {itemData.gridPosition} rot={itemData.rotation}", this);

        InventoryItem item = Instantiate(prefab, inventory.ItemLayer);
        item.gameObject.SetActive(false);
        item.name = string.IsNullOrWhiteSpace(itemData.itemId) ? $"MailItem_{System.DateTime.Now.Ticks}" : itemData.itemId;
        item.InitializeFromData(itemData, inventory);

        if (debugLevel > 0)
            Debug.Log($"[Spawner] Initialized item '{item.name}' shape={item.ShapeDefinition.Replace("\n", "|")} size={item.Width}x{item.Height} anchor={item.Anchor} rot={item.Rotation}", this);

        bool placed = false;
        try
        {
            placed = inventory.Grid.PlaceItem(itemData.gridPosition, item, debugLevel);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Spawner] Exception while placing item '{item.name}' at {itemData.gridPosition}: {ex}", this);
            placed = false;
        }

        if (!placed)
        {
            if (debugLevel > 0)
                Debug.LogWarning($"[Spawner] Placement failed for '{item.name}' at {itemData.gridPosition} (rot={itemData.rotation}). Destroying instance.", this);

            Destroy(item.gameObject);
            return null;
        }

        item.gameObject.SetActive(true);

        if (debugLevel > 0)
            Debug.Log($"[Spawner] Placed item '{item.name}' successfully in inventory '{inventory.InventoryId}'.", this);

        return item;
    }

    /// <summary>
    /// Instantiates an inventory item using explicit spawn options.
    /// If a selected prefab is not specified, a random prefab is picked from the catalog.
    /// If rotation or grid position is not specified, the method will search for a valid placement.
    /// </summary>
    public InventoryItem SpawnItemInInventory(InventoryInstance inventory, Vector2Int? gridPosition = null, int? rotation = null, InventoryItem selectedPrefab = null, MailData mailData = null, int debugLevel = 0)
    {
        if (inventory == null)
            return null;
        List<InventoryItem> candidates = GetCandidatePrefabs(selectedPrefab);
        if (candidates.Count == 0)
        {
            Debug.LogWarning($"No valid prefabs available to spawn in inventory '{inventory.InventoryId}'.", this);
            return null;
        }

        if (debugLevel > 0)
            Debug.Log($"[Spawner] Attempting to spawn item in inventory '{inventory.InventoryId}' with {candidates.Count} candidate prefabs (debug={debugLevel}).", this);
        else
            Debug.Log($"Attempting to spawn item in inventory '{inventory.InventoryId}'.", this);

        MailData resolvedMailData = ResolveMailData(mailData);

        for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
        {
            InventoryItem candidate = candidates[candidateIndex];
            string shapeDisplay = string.IsNullOrWhiteSpace(candidate.ShapeDefinition) ? "(empty)" : candidate.ShapeDefinition.Replace("\n", "|");
            if (debugLevel > 0)
            {
                Debug.Log($"  [Spawner] Candidate {candidateIndex + 1}/{candidates.Count}: '{candidate.name}' shape='{shapeDisplay}' size={candidate.Width}x{candidate.Height} anchor={candidate.Anchor}.", this);
                Debug.Log($"  [Spawner] Candidate '{candidate.name}' full shape lines:\n{candidate.ShapeDefinition}", this);
            }

            IEnumerable<Vector2Int> positions = GetPositionSequence(inventory, gridPosition);
            int positionCount = 0;
            foreach (Vector2Int position in positions)
            {
                positionCount++;
                if (debugLevel > 2)
                    Debug.Log($"    [Spawner] Candidate '{candidate.name}' position #{positionCount} = {position}.", this);

                IEnumerable<int> rotations = GetRotationSequence(rotation);
                foreach (int nextRotation in rotations)
                {
                    if (debugLevel > 1)
                        Debug.Log($"    [Spawner] Testing position {position} rotation {nextRotation} for prefab '{candidate.name}'.", this);

                    InventoryItemData itemData = CreateItemDataFromPrefab(candidate, position, nextRotation, resolvedMailData);
                    if (itemData == null)
                    {
                        if (debugLevel > 0)
                            Debug.LogWarning($"    [Spawner] CreateItemDataFromPrefab returned null for prefab '{candidate.name}' at {position} rot={nextRotation}.", this);
                        continue;
                    }

                    InventoryItem spawned = SpawnItemInInventory(inventory, itemData, debugLevel);
                    if (spawned != null)
                    {
                        if (debugLevel > 0)
                            Debug.Log($"    [Spawner] SUCCESS at position {position} rotation {nextRotation} using prefab '{candidate.name}'.", this);
                        else
                            Debug.Log($"    SUCCESS at position {position} rotation {nextRotation}.", this);

                        return spawned;
                    }

                    if (debugLevel > 1)
                        Debug.Log($"    [Spawner] Placement attempt failed for prefab '{candidate.name}' at {position} rot={nextRotation}.", this);
                }

                if (gridPosition.HasValue)
                    break;
            }

            if (selectedPrefab != null)
                break;
        }

        Debug.LogWarning($"Failed to find valid placement for any prefab in inventory '{inventory.InventoryId}' ({(inventory.Grid != null ? $"{inventory.Grid.Width}x{inventory.Grid.Height}" : "grid not assigned")}).", this);
        return null;
    }

    /// <summary>
    /// Loads inventory data into the UI instance, clearing existing items first.
    /// </summary>
    public void LoadInventoryData(InventoryInstance inventory, InventoryData data)
    {
        if (inventory == null || data == null)
            return;

        // Prevent clearing the inventory from writing an empty state back into the
        // same InventoryData object before the saved items are reloaded.
        if (inventory.InventoryData == data)
        {
            inventory.SetInventoryData(null);
        }

        inventory.Grid.BeginBatchUpdate();
        try
        {
            inventory.ClearInventory();
            inventory.SetInventoryData(data);

            foreach (InventoryItemData itemData in data.items)
            {
                SpawnItemInInventory(inventory, itemData);
            }
        }
        finally
        {
            inventory.Grid.EndBatchUpdate(true);
        }
    }

    /// <summary>
    /// Writes the current runtime inventory state back into the inventory data object.
    /// </summary>
    public void SaveInventoryData(InventoryInstance inventory)
    {
        if (inventory == null || inventory.InventoryData == null)
            return;

        // Delegate to the InventoryInstance so a single implementation owns persistence
        inventory.SaveInventoryData();
    }

    /// <summary>
    /// Creates item data using a prefab's default shape and specified placement metadata.
    /// </summary>
    public InventoryItemData CreateItemDataFromPrefab(InventoryItem prefab, Vector2Int gridPosition, int rotation, MailData mailData)
    {
        if (prefab == null || string.IsNullOrWhiteSpace(prefab.ShapeDefinition))
            return null;

        string prefabId = string.IsNullOrWhiteSpace(prefab.PrefabId) ? prefab.name : prefab.PrefabId;
        if (string.IsNullOrWhiteSpace(prefab.PrefabId))
            Debug.LogWarning($"[Spawner] Prefab '{prefab.name}' has no PrefabId assigned; falling back to prefab name '{prefabId}' for item data.", prefab);

        return new InventoryItemData
        {
            itemId = $"mail_{Random.Range(1000, 9999)}",
            prefabId = prefabId,
            shapeDefinition = prefab.ShapeDefinition,
            rotation = NormalizeRotation(rotation),
            gridPosition = gridPosition,
            mailData = mailData
        };
    }

    /// <summary>
    /// Creates a new inventory item data object for a random mail item.
    /// </summary>
    public InventoryItemData CreateRandomMailItemData(Vector2Int gridPosition)
    {
        InventoryItem prefab = GetRandomPrefab();
        if (prefab == null)
            return null;

        MailData randomMail = ResolveMailData(null);
        if (randomMail == null)
            return null;

        string prefabId = string.IsNullOrWhiteSpace(prefab.PrefabId) ? prefab.name : prefab.PrefabId;
        if (string.IsNullOrWhiteSpace(prefab.PrefabId))
            Debug.LogWarning($"[Spawner] Prefab '{prefab.name}' has no PrefabId assigned; falling back to prefab name '{prefabId}' for random mail item data.", prefab);

        return new InventoryItemData
        {
            itemId = $"mail_{Random.Range(1000, 9999)}",
            prefabId = prefabId,
            shapeDefinition = prefab.ShapeDefinition,
            rotation = 0,
            gridPosition = gridPosition,
            mailData = randomMail
        };
    }

    /// <summary>
    /// Ensures mail metadata is complete before item instantiation.
    /// If the caller provided partial data, this fills missing address or recipient values using the address book.
    /// </summary>
    private MailData ResolveMailData(MailData requestedMailData)
    {
        if (requestedMailData == null)
            return addressBook != null ? addressBook.GetRandomMailData() : GenerateRandomMailData();

        string address = requestedMailData.address;
        string recipient = requestedMailData.recipient;

        if (string.IsNullOrWhiteSpace(address))
        {
            MailAddressEntry randomEntry = addressBook != null ? addressBook.GetRandomEntry() : null;
            address = randomEntry != null ? randomEntry.address : null;
        }

        if (string.IsNullOrWhiteSpace(recipient))
        {
            if (addressBook != null && !string.IsNullOrWhiteSpace(address))
                recipient = addressBook.GetRandomRecipientForAddress(address);
        }

        if (string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(recipient))
        {
            MailData fallback = addressBook != null ? addressBook.GetRandomMailData() : GenerateRandomMailData();
            address = string.IsNullOrWhiteSpace(address) ? fallback.address : address;
            recipient = string.IsNullOrWhiteSpace(recipient) ? fallback.recipient : recipient;
        }

        return new MailData
        {
            address = address,
            recipient = recipient,
            packageModifier = requestedMailData.packageModifier,
            packageScore = requestedMailData.packageScore
        };
    }

    /// <summary>
    /// Finds a prefab in the configured catalog by its unique identifier.
    /// </summary>
    private InventoryItem GetPrefabById(string prefabId)
    {
        if (string.IsNullOrWhiteSpace(prefabId))
            return null;

        if (itemPrefabs != null)
        {
            foreach (InventoryItem prefab in itemPrefabs)
            {
                if (prefab == null)
                    continue;

                if (!string.IsNullOrWhiteSpace(prefab.PrefabId) && prefab.PrefabId == prefabId)
                    return prefab;
            }

            foreach (InventoryItem prefab in itemPrefabs)
            {
                if (prefab == null)
                    continue;

                if (prefab.name == prefabId)
                {
                    Debug.LogWarning($"[Spawner] Matched prefab by name '{prefabId}' because the prefab ID was missing or empty.", prefab);
                    return prefab;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Chooses a random prefab from the configured catalog.
    /// Skips null entries and prefabs with invalid shapes.
    /// </summary>
    private InventoryItem GetRandomPrefab()
    {
        if (itemPrefabs == null || itemPrefabs.Length == 0)
            return null;

        List<InventoryItem> validPrefabs = new List<InventoryItem>();
        foreach (InventoryItem prefab in itemPrefabs)
        {
            if (prefab != null && prefab.Width > 0 && prefab.Height > 0)
                validPrefabs.Add(prefab);
        }

        if (validPrefabs.Count == 0)
            return null;

        return validPrefabs[Random.Range(0, validPrefabs.Count)];
    }

    /// <summary>
    /// Builds the list of prefabs to attempt for placement.
    /// If a prefab is explicitly selected, only that prefab is returned.
    /// Otherwise the entire catalog is shuffled and returned.
    /// Prefabs with invalid or missing shape data are skipped.
    /// </summary>
    private List<InventoryItem> GetCandidatePrefabs(InventoryItem selectedPrefab)
    {
        if (selectedPrefab != null)
        {
            // Validate the selected prefab has a valid shape
            if (selectedPrefab.Width > 0 && selectedPrefab.Height > 0)
                return new List<InventoryItem> { selectedPrefab };
            
            Debug.LogWarning($"Selected prefab '{selectedPrefab.name}' has invalid or empty shape definition (computed as {selectedPrefab.Width}x{selectedPrefab.Height}).", selectedPrefab);
            return new List<InventoryItem>();
        }

        List<InventoryItem> candidates = new List<InventoryItem>();
        if (itemPrefabs != null)
        {
            foreach (InventoryItem prefab in itemPrefabs)
            {
                if (prefab == null)
                    continue;

                // Skip prefabs with invalid or missing shape data
                if (prefab.Width <= 0 || prefab.Height <= 0)
                {
                    Debug.LogWarning($"Prefab '{prefab.name}' has invalid or empty shape definition (computed as {prefab.Width}x{prefab.Height}).", prefab);
                    continue;
                }

                candidates.Add(prefab);
            }
        }

        if (candidates.Count == 0)
            return candidates;

        Shuffle(candidates);
        return candidates;
    }

    /// <summary>
    /// Shuffles the list in-place using a Fisher–Yates shuffle.
    /// This randomizes the candidate spawn order so that the same prefab is not always tried first.
    /// </summary>
    private void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            if (i == j)
                continue;

            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    /// <summary>
    /// Generates fallback random mail metadata when no address book is configured.
    /// </summary>
    private MailData GenerateRandomMailData()
    {
        if (addressBook != null)
            return addressBook.GetRandomMailData();

        return null;
    }

    /// <summary>
    /// Produces the coordinate sequence that should be attempted for placement.
    /// If the caller provided a position, only that position is returned.
    /// Otherwise every cell in the grid is enumerated.
    /// </summary>
    private IEnumerable<Vector2Int> GetPositionSequence(InventoryInstance inventory, Vector2Int? explicitPosition)
    {
        if (explicitPosition.HasValue)
        {
            yield return explicitPosition.Value;
            yield break;
        }

        int width = inventory.Grid.Width;
        int height = inventory.Grid.Height;
        List<Vector2Int> positions = new List<Vector2Int>(width * height);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
                positions.Add(new Vector2Int(x, y));
        }

        if (randomizePositionOrder)
            Shuffle(positions);

        foreach (Vector2Int position in positions)
            yield return position;
    }

    /// <summary>
    /// Produces the rotation sequence to test for placement.
    /// If the rotation is provided explicitly, only that rotation is returned.
    /// Otherwise the default four cardinal orientations are tried.
    /// </summary>
    private IEnumerable<int> GetRotationSequence(int? explicitRotation)
    {
        if (explicitRotation.HasValue)
        {
            yield return NormalizeRotation(explicitRotation.Value);
            yield break;
        }

        List<int> rotations = new List<int> { 0, 90, 180, 270 };
        if (randomizeRotationOrder)
            Shuffle(rotations);

        foreach (int rotation in rotations)
            yield return rotation;
    }

    /// <summary>
    /// Normalizes a raw rotation angle into the 0–359 degree range.
    /// </summary>
    private int NormalizeRotation(int rotation)
    {
        int normalized = rotation % 360;
        if (normalized < 0)
            normalized += 360;
        return normalized;
    }
}
