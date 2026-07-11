using UnityEngine;

/// <summary>
/// Handles pause menu open, close, and scene transition actions.
/// Input is handled by a separate always-active player controller.
/// </summary>
public class PauseMenuHandler : MonoBehaviour
{
    [Header("Menu References")]
    [SerializeField]
    [Tooltip("The MenuController used for the gameplay pause menu.")]
    private MenuController pauseMenuController;

    private void Awake()
    {
        if (pauseMenuController == null)
            pauseMenuController = GetComponent<MenuController>();

        if (pauseMenuController == null)
            Debug.LogWarning("PauseMenuHandler requires a MenuController reference or a MenuController on the same GameObject.", this);
    }

    public void TogglePauseMenu()
    {
        if (pauseMenuController == null)
        {
            Debug.LogWarning("[PauseMenuController] pauseMenuController is null.");
            return;
        }

        if (pauseMenuController.IsOpen || pauseMenuController.gameObject.activeSelf)
        {
            ClosePauseMenu();
        }
        else
        {
            OpenPauseMenu();
        }
    }

    public void OpenPauseMenu()
    {
        Debug.Log("[PauseMenuHandler] OpenPauseMenu() called.");

        if (pauseMenuController == null)
        {
            Debug.LogWarning("[PauseMenuHandler] pauseMenuController is null.");
            return;
        }

        Debug.Log($"[PauseMenuHandler] Opening menu '{pauseMenuController.name}'.");
        pauseMenuController.Open();
        pauseMenuController.transform.SetAsLastSibling();
        Debug.Log("[PauseMenuHandler] Calling AudioManager.PlayPauseOpenSound().");
        AudioManager.PlayPauseOpenSound();
    }

    public void ClosePauseMenu()
    {
        Debug.Log("[PauseMenuHandler] ClosePauseMenu() called.");

        if (pauseMenuController == null)
        {
            Debug.LogWarning("[PauseMenuHandler] pauseMenuController is null.");
            return;
        }

        Debug.Log($"[PauseMenuHandler] Closing menu '{pauseMenuController.name}'.");
        pauseMenuController.Close();
        Debug.Log("[PauseMenuHandler] Calling AudioManager.PlayPauseCloseSound().");
        AudioManager.PlayPauseCloseSound();
    }

    public void OnContinueButtonPressed()
    {
        AudioManager.PlayUIButtonClickSound();
        ClosePauseMenu();
    }

    public void OnRestartButtonPressed()
    {
        AudioManager.PlayUIButtonClickSound();
        ClosePauseMenu();
        SceneFlowManager.GetOrCreateInstance()?.RestartGameplayScene();
    }

    public void OnQuitToTitleButtonPressed()
    {
        AudioManager.PlayUIButtonClickSound();
        ClosePauseMenu();
        SceneFlowManager.GetOrCreateInstance()?.LoadStartScene();
    }

    public void OnQuitGameButtonPressed()
    {
        AudioManager.PlayUIButtonClickSound();
        ClosePauseMenu();
        SceneFlowManager.GetOrCreateInstance()?.QuitGame();
    }
}
