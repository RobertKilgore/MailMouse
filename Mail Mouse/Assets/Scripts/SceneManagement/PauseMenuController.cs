using UnityEngine;

/// <summary>
/// Handles pause menu opening, closing, and scene transition actions in gameplay.
/// Uses the UI "Cancel" input action to toggle the menu when no other UI is open.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [Header("Pause Menu")]
    [SerializeField]
    [Tooltip("The MenuController used for the gameplay pause menu.")]
    private MenuController pauseMenuController;

    private InputSystem_Actions inputActions;

    private bool IsPauseMenuOpen => pauseMenuController != null && MenuManager.Instance != null && MenuManager.Instance.GetActiveMenus().Contains(pauseMenuController);

    private void Awake()
    {
        if (pauseMenuController == null)
            pauseMenuController = GetComponent<MenuController>();

        if (pauseMenuController == null)
            Debug.LogWarning("PauseMenuController requires a MenuController reference or a MenuController on the same GameObject.", this);
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
            HandleCancelPressed();
        }
    }

    private void HandleCancelPressed()
    {
        if (pauseMenuController == null)
            return;

        if (IsPauseMenuOpen)
        {
            ClosePauseMenu();
            return;
        }

        if (MenuManager.AnyMenuOpen)
            return;

        OpenPauseMenu();
    }

    public void OpenPauseMenu()
    {
        if (pauseMenuController == null)
            return;

        if (IsPauseMenuOpen)
            return;

        pauseMenuController.Open();
        pauseMenuController.transform.SetAsLastSibling();
    }

    public void ClosePauseMenu()
    {
        if (pauseMenuController == null)
            return;

        if (!IsPauseMenuOpen)
            return;

        pauseMenuController.Close();
    }

    public void OnContinueButtonPressed()
    {
        ClosePauseMenu();
    }

    public void OnRestartButtonPressed()
    {
        ClosePauseMenu();
        SceneFlowManager.GetOrCreateInstance()?.RestartGameplayScene();
    }

    public void OnQuitToTitleButtonPressed()
    {
        ClosePauseMenu();
        SceneFlowManager.GetOrCreateInstance()?.LoadStartScene();
    }

    public void OnQuitGameButtonPressed()
    {
        ClosePauseMenu();
        SceneFlowManager.GetOrCreateInstance()?.QuitGame();
    }
}
