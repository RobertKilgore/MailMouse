using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Central controller for player menu input. It routes menu requests through the shared MenuManager.
/// </summary>
public class PlayerMenuInputController : MonoBehaviour
{
    [Header("Menu References")]
    [SerializeField] private MenuController pauseMenuController;
    [SerializeField] private MenuController inventoryMenuController;
    [SerializeField] private InventoryPresentationController inventoryPresentationController;

    private InputSystem_Actions inputActions;

    private void Awake()
    {
        if (inventoryPresentationController == null)
            inventoryPresentationController = FindFirstObjectByType<InventoryPresentationController>(FindObjectsInactive.Include);
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
            RequestMenuToggle(pauseMenuController);
            return;
        }

        if (inputActions.Player.Interact.WasPressedThisFrame())
        {
            RequestInventoryToggle();
            return;
        }
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

    public void RequestInventoryToggle(InventoryType inventoryType = InventoryType.Player, params InventoryData[] inventoryData)
    {
        if (inventoryMenuController == null)
            return;

        if (inventoryMenuController.IsOpen || MenuManager.Instance?.GetActiveMenus().Contains(inventoryMenuController) == true)
        {
            inventoryMenuController.Close();
            return;
        }

        bool menuOpened = TryOpenMenu(inventoryMenuController);
        if (!menuOpened)
        {
            Debug.Log("Inventory menu open request was rejected by the menu system; skipping inventory set open.");
            return;
        }

        OpenInventorySet(inventoryType, inventoryData);
    }

    public void OpenInventorySet(InventoryType inventoryType, params InventoryData[] inventoryData)
    {
        if (inventoryPresentationController != null)
            inventoryPresentationController.TryOpenInventory(inventoryType, inventoryData);
    }

    public bool WasInteractPressedThisFrame()
    {
        return inputActions?.Player.Interact.WasPressedThisFrame() == true;
    }
}
