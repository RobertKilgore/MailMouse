using System.Collections;
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

    private void Start()
    {
        ValidateInventoryAddresses();
    }

    private void ValidateInventoryAddresses()
    {
        if (addressBook == null)
        {
            Debug.LogWarning("[Spawner] No MailAddressBook assigned; address validation skipped.", this);
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
            string message = "[Spawner] Address validation warning:";
            if (sceneAddressesMissingFromBook.Count > 0)
                message += $" {sceneAddressesMissingFromBook.Count} scene address(es) are missing from the address book ({string.Join(sceneAddressesMissingFromBook, ", ")}).";
            if (bookAddressesMissingFromScene.Count > 0)
                message += $" {bookAddressesMissingFromScene.Count} address book address(es) are not present in the scene ({string.Join(bookAddressesMissingFromScene, ", ")}).";

            Debug.LogWarning(message, this);
        }
        else if (checkedItems > 0)
        {
            Debug.Log($"[Spawner] Address validation passed: {checkedItems} inventory item address(es) matched the address book and vice versa.", this);
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
            placed = inventory.Grid.PlaceItem(itemData.gridPosition, item, debugLevel, false);
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

    private InventoryItem TrySpawnItemInInventoryAtAlternatePosition(InventoryInstance inventory, InventoryItemData itemData, int debugLevel = 0)
    {
        if (inventory == null || itemData == null)
            return null;

        InventoryItem prefab = GetPrefabById(itemData.prefabId);
        if (prefab == null)
        {
            Debug.LogWarning($"[Spawner] No prefab found with ID '{itemData.prefabId}' for fallback placement of item '{itemData.itemId}'.", this);
            return null;
        }

        InventoryItem item = Instantiate(prefab, inventory.ItemLayer);
        item.gameObject.SetActive(false);
        item.name = string.IsNullOrWhiteSpace(itemData.itemId) ? $"MailItem_{System.DateTime.Now.Ticks}" : itemData.itemId;
        item.InitializeFromData(itemData, inventory);

        List<Vector2Int> candidatePositions = new List<Vector2Int>();
        if (itemData.gridPosition != Vector2Int.zero)
            candidatePositions.Add(itemData.gridPosition);

        for (int y = 0; y < inventory.Grid.Height; y++)
        {
            for (int x = 0; x < inventory.Grid.Width; x++)
            {
                Vector2Int position = new Vector2Int(x, y);
                if (!candidatePositions.Contains(position))
                    candidatePositions.Add(position);
            }
        }

        if (randomizePositionOrder)
            Shuffle(candidatePositions);

        foreach (Vector2Int position in candidatePositions)
        {
            try
            {
                if (inventory.Grid.PlaceItem(position, item, debugLevel, false))
                {
                    item.gameObject.SetActive(true);
                    itemData.gridPosition = position;
                    Debug.Log($"[Spawner] Fallback placement succeeded for '{item.name}' at {position} in inventory '{inventory.InventoryId}'.", this);
                    return item;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Spawner] Fallback placement failed at {position} for '{item.name}': {ex}", this);
            }
        }

        Destroy(item.gameObject);
        return null;
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

                    InventoryItemData itemData = CreateItemDataFromPrefab(candidate, position, nextRotation, mailData);
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

        StartCoroutine(LoadInventoryDataRoutine(inventory, data));
    }

    private IEnumerator LoadInventoryDataRoutine(InventoryInstance inventory, InventoryData data)
    {
        if (inventory == null || data == null)
            yield break;

        yield return null;
        yield return new WaitForEndOfFrame();

        List<InventoryItemData> itemsToLoad = data.items != null ? new List<InventoryItemData>(data.items) : new List<InventoryItemData>();
        Debug.Log($"[InventorySpawner.LoadInventoryData] Loading {itemsToLoad.Count} items into inventory '{data.inventoryId}'", this);

        inventory.Grid.RebuildTileMap();
        inventory.Grid.BeginBatchUpdate();
        try
        {
            inventory.RebindInventoryData(null);
            inventory.RebindInventoryData(data);

            List<InventoryItemData> loadedItems = new List<InventoryItemData>();
            int successCount = 0;
            int failCount = 0;
            foreach (InventoryItemData itemData in itemsToLoad)
            {
                InventoryItem spawnedItem = SpawnItemInInventory(inventory, itemData);
                if (spawnedItem == null)
                {
                    spawnedItem = TrySpawnItemInInventoryAtAlternatePosition(inventory, itemData);
                }

                if (spawnedItem != null)
                {
                    loadedItems.Add(itemData);
                    successCount++;
                }
                else
                {
                    failCount++;
                    Debug.LogWarning($"[InventorySpawner] Failed to spawn item from data at position {itemData.gridPosition}", this);
                }
            }

            if (data.items == null)
                data.items = new List<InventoryItemData>();
            else
                data.items.Clear();

            data.items.AddRange(loadedItems);

            Debug.Log($"[InventorySpawner.LoadInventoryData] Loaded inventory '{data.inventoryId}': {successCount} items spawned, {failCount} failed; stored={data.items.Count}", this);
        }
        finally
        {
            inventory.Grid.EndBatchUpdate(false);
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
            mailData = ResolveMailData(prefab.DefaultMailData, mailData)
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

        MailData randomMail = ResolveMailData(prefab.DefaultMailData, null);
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
    private MailData ResolveMailData(MailData prefabMailData, MailData requestedMailData)
    {
        MailData resolved = CloneMailData(prefabMailData) ?? new MailData();

        if (requestedMailData != null)
        {
            if (!string.IsNullOrWhiteSpace(requestedMailData.address))
                resolved.address = requestedMailData.address;

            if (!string.IsNullOrWhiteSpace(requestedMailData.recipient))
                resolved.recipient = requestedMailData.recipient;

            if (!string.IsNullOrWhiteSpace(requestedMailData.name))
                resolved.name = requestedMailData.name;

            if (requestedMailData.complexity != 0f)
                resolved.complexity = requestedMailData.complexity;

            resolved.placedByPlayer = requestedMailData.placedByPlayer;
            resolved.packageModifier = requestedMailData.packageModifier;
            resolved.packageScore = requestedMailData.packageScore;
        }

        if (string.IsNullOrWhiteSpace(resolved.address))
        {
            MailAddressEntry randomEntry = addressBook != null ? addressBook.GetRandomEntry() : null;
            resolved.address = randomEntry != null ? randomEntry.address : null;
        }

        if (string.IsNullOrWhiteSpace(resolved.recipient) && !string.IsNullOrWhiteSpace(resolved.address) && addressBook != null)
        {
            resolved.recipient = addressBook.GetRandomRecipientForAddress(resolved.address);
        }

        if (string.IsNullOrWhiteSpace(resolved.address))
        {
            MailData fallback = addressBook != null ? addressBook.GetRandomMailData() : GenerateRandomMailData();
            if (fallback != null)
            {
                resolved.address = string.IsNullOrWhiteSpace(resolved.address) ? fallback.address : resolved.address;
                if (resolved.complexity == 0f)
                    resolved.complexity = fallback.complexity;
                if (string.IsNullOrWhiteSpace(resolved.name))
                    resolved.name = fallback.name;
            }
        }

        if (resolved.complexity == 0f)
            resolved.complexity = 1f;

        return resolved;
    }

    private MailData ResolveMailData(MailData requestedMailData)
    {
        return ResolveMailData(null, requestedMailData);
    }

    private MailData CloneMailData(MailData source)
    {
        if (source == null)
            return null;

        return new MailData
        {
            address = source.address,
            recipient = source.recipient,
            placedByPlayer = source.placedByPlayer,
            complexity = source.complexity,
            packageScore = source.packageScore,
            packageModifier = source.packageModifier,
            name = source.name
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

        // Fallback when no address book is configured: generate a synthetic recipient/address
        string rnd = System.Guid.NewGuid().ToString().Substring(0, 8);
        return new MailData
        {
            recipient = $"Player_{rnd}",
            address = $"Addr_{rnd}",
            placedByPlayer = false,
            complexity = 1f
        };
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

    /// <summary>
    /// Spawns an item into an InventoryData without UI, checking for space.
    /// Finds a valid position and adds the item to the data. Respects randomizePositionOrder setting.
    /// </summary>
    public bool SpawnItemIntoInventoryData(InventoryData inventoryData, InventoryItemData itemData)
    {
        if (!CanSpawnItemNow(inventoryData))
            return false;

        if (!CanSpawnItemNow(inventoryData))
            return false;

        if (inventoryData == null || itemData == null)
        {
            Debug.LogWarning("[Spawner] Cannot spawn item: inventoryData or itemData is null", this);
            return false;
        }

        if (inventoryData.items == null)
            inventoryData.items = new List<InventoryItemData>();

        // Try to find a valid position
        Vector2Int validPosition = FindValidPositionInData(inventoryData, itemData);
        itemData.gridPosition = validPosition;

        if (!CanPlaceItemAtInData(inventoryData, itemData, validPosition))
        {
            Debug.LogWarning($"[Spawner] No valid position found in inventory '{inventoryData.inventoryId}' for item shape: {itemData.shapeDefinition}", this);
            return false;
        }

        inventoryData.items.Add(itemData);
        SyncSpawnedItemToOpenInventoryUI(inventoryData, itemData);
        Debug.Log($"[Spawner] Spawned item into inventory data '{inventoryData.inventoryId}' at position {validPosition}", this);
        return true;
    }

    private void SyncSpawnedItemToOpenInventoryUI(InventoryData inventoryData, InventoryItemData itemData)
    {
        if (inventoryData == null || itemData == null)
            return;

        InventoryInstance openInstance = null;
        foreach (InventoryInstance instance in FindObjectsByType<InventoryInstance>(FindObjectsSortMode.None))
        {
            if (instance == null || instance.InventoryData != inventoryData)
                continue;

            if (!instance.gameObject.activeInHierarchy || !instance.enabled || instance.Grid == null)
                continue;

            openInstance = instance;
            break;
        }

        if (openInstance == null)
            return;

        if (!openInstance.AllowItemSpawns)
        {
            Debug.LogWarning($"[Spawner] Skipping live UI sync for inventory '{inventoryData.inventoryId}' because item spawns are disabled for this inventory.", this);
            return;
        }

        InventoryItem spawnedItem = SpawnItemInInventory(openInstance, itemData, 0);
        if (spawnedItem == null)
        {
            spawnedItem = TrySpawnItemInInventoryAtAlternatePosition(openInstance, itemData, 0);
        }

        if (spawnedItem == null)
        {
            Debug.LogWarning($"[Spawner] Added item to data for '{inventoryData.inventoryId}', but the live UI inventory could not place it. Stored position={itemData.gridPosition}", this);
        }
    }

    /// <summary>
    /// Spawns a random mail item into an InventoryData without UI.
    /// Always uses randomized position search for varied placement.
    /// </summary>
    public bool SpawnRandomMailIntoInventoryData(InventoryData inventoryData)
    {
        if (!CanSpawnItemNow(inventoryData))
            return false;

        if (inventoryData == null)
        {
            Debug.LogWarning("[Spawner] Cannot spawn random mail: inventoryData is null", this);
            return false;
        }

        InventoryItemData itemData = CreateRandomMailItemData(Vector2Int.zero);
        if (itemData == null)
        {
            Debug.LogWarning("[Spawner] Failed to create random mail item data", this);
            return false;
        }

        // Temporarily enable randomized position search for this spawn
        bool originalRandomize = randomizePositionOrder;
        randomizePositionOrder = true;
        bool success = SpawnItemIntoInventoryData(inventoryData, itemData);
        randomizePositionOrder = originalRandomize;

        return success;
    }

    private bool CanSpawnItemNow(InventoryData inventoryData)
    {
        if (InventoryDragController.Instance != null && InventoryDragController.Instance.IsHoldingItem)
        {
            Debug.LogWarning($"[Spawner] Blocked spawn for inventory '{inventoryData?.inventoryId ?? "unknown"}' because an item is currently being held.", this);
            return false;
        }

        if (inventoryData != null && !inventoryData.allowItemSpawns)
        {
            Debug.LogWarning($"[Spawner] Blocked spawn for inventory '{inventoryData.inventoryId}' because item spawns are disabled for this inventory.", this);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Tries to find a valid grid position where an item can fit in an InventoryData.
    /// If randomizePositionOrder is enabled, searches in random order; otherwise top-left to bottom-right.
    /// </summary>
    private Vector2Int FindValidPositionInData(InventoryData inventoryData, InventoryItemData itemData)
    {
        if (!TryGetItemDimensions(itemData.shapeDefinition, out int itemWidth, out int itemHeight))
            return Vector2Int.zero;

        // Build list of all possible positions
        List<Vector2Int> positions = new List<Vector2Int>();
        for (int y = 0; y < inventoryData.height; y++)
        {
            for (int x = 0; x < inventoryData.width; x++)
            {
                positions.Add(new Vector2Int(x, y));
            }
        }

        // Randomize search order if enabled
        if (randomizePositionOrder)
            Shuffle(positions);

        // Try each position until one works
        foreach (Vector2Int position in positions)
        {
            if (CanPlaceItemAtInData(inventoryData, itemData, position))
                return position;
        }

        return Vector2Int.zero;
    }

    /// <summary>
    /// Checks if an item can be placed at a specific grid position in InventoryData without overlapping.
    /// </summary>
    private bool CanPlaceItemAtInData(InventoryData inventoryData, InventoryItemData itemData, Vector2Int position)
    {
        if (!TryGetItemDimensions(itemData.shapeDefinition, out int itemWidth, out int itemHeight))
            return false;

        // Check if item fits within grid bounds
        if (position.x + itemWidth > inventoryData.width || position.y + itemHeight > inventoryData.height)
            return false;

        // Check for overlaps with existing items
        foreach (InventoryItemData existingItem in inventoryData.items)
        {
            if (DoItemsOverlapInData(position, itemData, existingItem.gridPosition, existingItem))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Gets the width and height of an item from its shape definition.
    /// </summary>
    private bool TryGetItemDimensions(string shapeDefinition, out int width, out int height)
    {
        width = 1;
        height = 1;

        if (string.IsNullOrWhiteSpace(shapeDefinition))
            return true;

        string[] rows = shapeDefinition.Replace("\r", "").Split('\n');
        if (rows.Length == 0)
            return true;

        height = rows.Length;
        width = 0;

        foreach (string row in rows)
        {
            if (row.Length > width)
                width = row.Length;
        }

        return true;
    }

    /// <summary>
    /// Checks if two items would overlap at their given positions.
    /// </summary>
    private bool DoItemsOverlapInData(Vector2Int pos1, InventoryItemData item1, Vector2Int pos2, InventoryItemData item2)
    {
        if (!TryGetItemDimensions(item1.shapeDefinition, out int width1, out int height1) ||
            !TryGetItemDimensions(item2.shapeDefinition, out int width2, out int height2))
        {
            return true; // Assume collision if we can't determine dimensions
        }

        // Check axis-aligned bounding box overlap
        if (pos1.x + width1 <= pos2.x || pos2.x + width2 <= pos1.x)
            return false; // No overlap on X axis

        if (pos1.y + height1 <= pos2.y || pos2.y + height2 <= pos1.y)
            return false; // No overlap on Y axis

        return true; // Boxes overlap
    }
}
