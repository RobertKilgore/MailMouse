using UnityEngine;

/// <summary>
/// Handles pause menu open, close, and scene transition actions.
/// Input is handled by a separate always-active player controller.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [Header("Pause Menu")]
    [SerializeField]
    [Tooltip("The MenuController used for the gameplay pause menu.")]
    private MenuController pauseMenuController;

    private void Awake()
    {
        if (pauseMenuController == null)
            pauseMenuController = GetComponent<MenuController>();

        if (pauseMenuController == null)
            Debug.LogWarning("PauseMenuController requires a MenuController reference or a MenuController on the same GameObject.", this);
    }

    public void OpenPauseMenu()
    {
        if (pauseMenuController == null)
            return;

        pauseMenuController.Open();
        pauseMenuController.transform.SetAsLastSibling();
    }

    public void ClosePauseMenu()
    {
        if (pauseMenuController == null)
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
