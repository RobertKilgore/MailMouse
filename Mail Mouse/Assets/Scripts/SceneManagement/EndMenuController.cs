using UnityEngine;

/// <summary>
/// Simple controller for an end menu scene.
/// </summary>
public class EndMenuController : MonoBehaviour
{
    private SceneFlowManager sceneFlowManager;

    public void OnQuitButtonPressed()
    {
        AudioManager.PlayUIButtonClickSound();

        if (sceneFlowManager == null)
            sceneFlowManager = SceneFlowManager.GetOrCreateInstance();

        if (sceneFlowManager != null)
            sceneFlowManager.QuitGame();
    }
    public void OnMainMenuButtonPressed()
    {
        AudioManager.PlayUIButtonClickSound();

        if (sceneFlowManager == null)
            sceneFlowManager = SceneFlowManager.GetOrCreateInstance();

        if (sceneFlowManager != null)
            sceneFlowManager.LoadStartScene();
    }
}
