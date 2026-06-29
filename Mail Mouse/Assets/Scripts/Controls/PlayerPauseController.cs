using System.Linq;
using UnityEngine;

/// <summary>
/// Always-active input controller for player pause/menu toggling.
/// </summary>
public class PlayerPauseController : MonoBehaviour
{
    private InputSystem_Actions inputActions;
    private PauseMenuController pauseMenuController;
    private MenuController pauseMenuControllerComponent;

    private void Awake()
    {
        pauseMenuController = FindFirstObjectByType<PauseMenuController>(FindObjectsInactive.Include);
        if (pauseMenuController != null)
            pauseMenuControllerComponent = pauseMenuController.GetComponent<MenuController>();
        else
            Debug.LogWarning("PlayerPauseController requires a PauseMenuController in the scene.", this);
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
        if (inputActions == null || pauseMenuController == null)
            return;

        if (!inputActions.UI.Cancel.WasPressedThisFrame())
            return;

        bool menuOpen = MenuManager.AnyMenuOpen;
        if (menuOpen)
        {
            if (pauseMenuControllerComponent != null && MenuManager.Instance != null && MenuManager.Instance.GetActiveMenus().Any(menu => menu == pauseMenuControllerComponent))
            {
                pauseMenuController.ClosePauseMenu();
            }

            return;
        }

        pauseMenuController.OpenPauseMenu();
    }
}
