using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Central controller for player menu input. It routes menu requests through the shared MenuManager.
/// </summary>
public class PlayerMenuInputController : MonoBehaviour
{
    [Header("Menu References")]
    [SerializeField] private PauseMenuHandler pauseMenuHandler;
    [SerializeField] private MenuController inventoryMenuController;
    [SerializeField] private MenuController mapMenuController;
    [SerializeField] private InventoryPresentationController inventoryPresentationController;

    private InputSystem_Actions inputActions;

    private void Awake()
    {
        if (inventoryPresentationController == null)
            inventoryPresentationController = FindFirstObjectByType<InventoryPresentationController>(FindObjectsInactive.Include);

        if (pauseMenuHandler == null)
            pauseMenuHandler = GetComponent<PauseMenuHandler>();

        if (pauseMenuHandler == null)
            pauseMenuHandler = FindFirstObjectByType<PauseMenuHandler>(FindObjectsInactive.Include);
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
            RequestPauseToggle();
            return;
        }

        if (inputActions.Player.Interact.WasPressedThisFrame())
        {
            if (IsInventoryMenuOpen())
            {
                AudioManager.PlayInventoryCloseSound();
                inventoryMenuController.Close();
                return;
            }

            RequestInventoryToggle();
            return;
        }

        if (GetMapMenuAction()?.WasPressedThisFrame() == true)
        {
            RequestMapMenuToggle();
            return;
        }
    }

    public void RequestPauseToggle()
    {
        pauseMenuHandler?.TogglePauseMenu();
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

    public void RequestMapMenuToggle()
    {
        if (mapMenuController == null)
            return;

        if (mapMenuController.IsOpen || mapMenuController.gameObject.activeSelf)
        {
            mapMenuController.Close();
            return;
        }

        bool menuOpened = TryOpenMenu(mapMenuController);
        if (!menuOpened)
        {
            Debug.Log("Map menu open request was rejected by the menu system; skipping map menu toggle.");
        }
    }

    public void RequestInventoryToggle(InventoryType inventoryType = InventoryType.Player, params InventoryData[] inventoryData)
    {
        if (inventoryMenuController == null)
            return;

        if (IsInventoryMenuOpen())
        {
            if (inventoryType == InventoryType.Player)
            {
                AudioManager.PlayInventoryCloseSound();
            }
            inventoryMenuController.Close();
            return;
        }

        bool menuOpened = TryOpenMenu(inventoryMenuController);
        if (!menuOpened)
        {
            Debug.Log("Inventory menu open request was rejected by the menu system; skipping inventory set open.");
            return;
        }

        if (inventoryType == InventoryType.Player)
        {
            AudioManager.PlayInventoryOpenSound();
        }

        OpenInventorySet(inventoryType, inventoryData);
    }

    public void OpenInventorySet(InventoryType inventoryType, params InventoryData[] inventoryData)
    {
        if (inventoryPresentationController != null)
            inventoryPresentationController.TryOpenInventory(inventoryType, inventoryData);
    }

    private InputAction GetMapMenuAction()
    {
        if (inputActions?.asset == null)
            return null;

        return inputActions.asset.FindActionMap("Player")?.FindAction("MapMenu");
    }

    public bool WasInteractPressedThisFrame()
    {
        return inputActions?.Player.Interact.WasPressedThisFrame() == true;
    }

    private bool IsInventoryMenuOpen()
    {
        if (inventoryMenuController == null)
            return false;

        return inventoryMenuController.IsOpen || MenuManager.Instance?.GetActiveMenus().Contains(inventoryMenuController) == true;
    }
}
