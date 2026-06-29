using UnityEngine;

/// <summary>
/// Simple controller for a start menu scene.
/// </summary>
public class StartMenuController : MonoBehaviour
{
    private SceneFlowManager sceneFlowManager;

    public void OnPlayButtonPressed()
    {
        if (sceneFlowManager == null)
            sceneFlowManager = SceneFlowManager.GetOrCreateInstance();

        if (sceneFlowManager != null)
            sceneFlowManager.LoadGameplayScene();
    }

    public void OnQuitButtonPressed()
    {
        if (sceneFlowManager == null)
            sceneFlowManager = SceneFlowManager.GetOrCreateInstance();

        if (sceneFlowManager != null)
            sceneFlowManager.QuitGame();
    }
}
