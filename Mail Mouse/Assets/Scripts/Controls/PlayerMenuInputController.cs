using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Central controller for player menu input. It routes menu requests through the shared MenuManager.
/// </summary>
public class PlayerMenuInputController : MonoBehaviour
{
    [Header("Menus")]
    [SerializeField] private MenuController pauseMenu;
    [SerializeField] private MenuController inventoryMenu;

    [Header("Inventory")]
    [SerializeField] private InventorySetDefinition playerInventorySet;
    [SerializeField] private InventoryDataHolder playerInventoryHolder;
    [SerializeField] private InventorySetDefinition nearbyInventorySet;
    [SerializeField] private Collider detectionCollider;

    private InputSystem_Actions inputActions;
    private InventorySetManager setManager;
    private InventoryDataHolder currentClosestInventory;

    private void Awake()
    {
        setManager = InventorySetManager.Instance ?? FindFirstObjectByType<InventorySetManager>(FindObjectsInactive.Include);
    }

    private void OnEnable()
    {
        if (inputActions == null)
            inputActions = new InputSystem_Actions();
        inputActions.Enable();
    }

    private void OnDisable()
    {
        if (inputActions != null)
            inputActions.Disable();
    }

    private void Update()
    {
        if (inputActions == null)
            return;

        if (inputActions.UI.Cancel.WasPressedThisFrame())
        {
            RequestMenuToggle(pauseMenu);
            return;
        }

        if (inputActions.Player.Interact.WasPressedThisFrame())
        {
            RequestInventoryToggle();
            return;
        }

        if (inputActions.Player.Interact2.WasPressedThisFrame())
        {
            HandleNearbyInventoryInteraction();
        }

        UpdateOutlineVisualization();
    }

    public void RequestMenuToggle(MenuController menu)
    {
        TryOpenMenu(menu);
    }

    private bool TryOpenMenu(MenuController menu)
    {
        if (menu == null)
            return false;

        if (MenuManager.Instance != null)
        {
            bool opened = MenuManager.Instance.ToggleMenu(menu);
            if (!opened)
            {
                return false;
            }

            return true;
        }

        return menu.Open();
    }

    public void RequestInventoryToggle()
    {
        ResolveSetManager();
        if (setManager == null)
        {
            Debug.LogWarning("InventorySetManager unavailable for inventory toggle.");
            return;
        }

        if (inventoryMenu == null)
            return;

        if (inventoryMenu.IsOpen || MenuManager.Instance?.GetActiveMenus().Contains(inventoryMenu) == true)
        {
            inventoryMenu.Close();
            return;
        }

        bool menuOpened = TryOpenMenu(inventoryMenu);
        if (!menuOpened)
        {
            Debug.Log("Inventory menu open request was rejected by the menu system; skipping inventory set open.");
            return;
        }

        List<InventoryData> ordered = null;
        if (playerInventoryHolder != null && playerInventoryHolder.inventoryData != null)
            ordered = new List<InventoryData> { playerInventoryHolder.inventoryData };

        if (setManager != null)
            setManager.OpenInventorySet(playerInventorySet, ordered);
    }

    public void HandleNearbyInventoryInteraction()
    {
        ResolveSetManager();
        if (setManager == null)
            return;

        if (inventoryMenu != null && (inventoryMenu.IsOpen || MenuManager.Instance?.GetActiveMenus().Contains(inventoryMenu) == true))
        {
            inventoryMenu.Close();
            return;
        }

        if (detectionCollider == null)
            return;

        bool menuOpened = TryOpenMenu(inventoryMenu);
        if (!menuOpened)
        {
            Debug.Log("Nearby inventory menu open request was rejected by the menu system; skipping inventory set open.");
            return;
        }

        InventoryDataHolder closestHolder = GetClosestNearbyInventory();
        if (closestHolder == null)
            return;

        InventoryData nearbyData = closestHolder.inventoryData;
        if (nearbyData == null)
            return;

        List<InventoryData> orderedData = new List<InventoryData>();
        if (playerInventoryHolder != null && playerInventoryHolder.inventoryData != null)
            orderedData.Add(playerInventoryHolder.inventoryData);
        orderedData.Add(nearbyData);

        setManager.OpenInventorySet(nearbyInventorySet, orderedData);
    }

    private void ResolveSetManager()
    {
        if (setManager != null)
            return;

        setManager = InventorySetManager.Instance ?? FindFirstObjectByType<InventorySetManager>(FindObjectsInactive.Include);
    }

    private InventoryDataHolder GetClosestNearbyInventory()
    {
        if (detectionCollider == null)
            return null;

        Bounds bounds = detectionCollider.bounds;
        float detectionRadius = bounds.extents.magnitude;
        Collider[] collidersInRange = Physics.OverlapSphere(transform.position, detectionRadius);

        InventoryDataHolder closest = null;
        float closestDistance = float.MaxValue;
        Vector3 playerPos = transform.position;

        foreach (Collider col in collidersInRange)
        {
            InventoryDataHolder holder = col.GetComponent<InventoryDataHolder>();
            if (holder == null || holder == playerInventoryHolder)
                continue;

            float distance = Vector3.Distance(playerPos, holder.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = holder;
            }
        }

        return closest;
    }

    private void UpdateOutlineVisualization()
    {
        if (detectionCollider == null)
            return;

        Bounds bounds = detectionCollider.bounds;
        float detectionRadius = bounds.extents.magnitude;
        Collider[] collidersInRange = Physics.OverlapSphere(transform.position, detectionRadius);

        InventoryDataHolder newClosest = null;
        float closestDistance = float.MaxValue;
        Vector3 playerPos = transform.position;

        foreach (Collider col in collidersInRange)
        {
            InventoryDataHolder holder = col.GetComponent<InventoryDataHolder>();
            if (holder == null || holder == playerInventoryHolder)
                continue;

            float distance = Vector3.Distance(playerPos, holder.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                newClosest = holder;
            }
        }

        if (newClosest != currentClosestInventory)
        {
            if (currentClosestInventory != null)
                OutlineManager.DisableOutline(currentClosestInventory.gameObject);

            currentClosestInventory = newClosest;
            if (currentClosestInventory != null)
                OutlineManager.EnableOutline(currentClosestInventory.gameObject);
        }
    }
}
